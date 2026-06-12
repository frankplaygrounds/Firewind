const zlib = require('zlib');
const jpeg = require('jpeg-js');
const {
  TAG_DEFINE_BITS_JPEG2,
  TAG_DEFINE_BITS_JPEG3,
  TAG_DEFINE_BITS_LOSSLESS,
  TAG_DEFINE_BITS_LOSSLESS2
} = require('./SwfFile');
const { UnsupportedStructureError } = require('../errors');

const SUPPORTED_BITMAP_TAGS = new Set([
  TAG_DEFINE_BITS_JPEG2,
  TAG_DEFINE_BITS_JPEG3,
  TAG_DEFINE_BITS_LOSSLESS,
  TAG_DEFINE_BITS_LOSSLESS2
]);

function readCharacterId(tag) {
  if (!tag || !tag.payload || tag.payload.length < 2) {
    throw new UnsupportedStructureError('Bitmap tag is missing its character ID.');
  }
  return tag.payload.readUInt16LE(0);
}

function argbToRgba(raw, width, height, hasAlpha) {
  const expected = width * height * 4;
  if (raw.length < expected) {
    throw new UnsupportedStructureError('Lossless bitmap data is shorter than expected.');
  }

  const rgba = Buffer.alloc(expected);
  for (let index = 0; index < width * height; index += 1) {
    const source = index * 4;
    const target = index * 4;
    rgba[target] = raw[source + 1];
    rgba[target + 1] = raw[source + 2];
    rgba[target + 2] = raw[source + 3];
    rgba[target + 3] = hasAlpha ? raw[source] : 255;
  }
  return rgba;
}

function rgbaToArgb(rgba, width, height, hasAlpha) {
  const expected = width * height * 4;
  if (rgba.length < expected) {
    throw new UnsupportedStructureError('RGBA bitmap data is shorter than expected.');
  }

  const argb = Buffer.alloc(expected);
  for (let index = 0; index < width * height; index += 1) {
    const source = index * 4;
    const target = index * 4;
    argb[target] = hasAlpha ? rgba[source + 3] : 0;
    argb[target + 1] = rgba[source];
    argb[target + 2] = rgba[source + 1];
    argb[target + 3] = rgba[source + 2];
  }
  return argb;
}

function decodeIndexedLossless(tag, characterId, format, width, height, compressed) {
  if (tag.payload.length < 8) {
    throw new UnsupportedStructureError('Indexed lossless bitmap header is truncated.');
  }

  const hasAlpha = tag.code === TAG_DEFINE_BITS_LOSSLESS2;
  const colorCount = tag.payload[7] + 1;
  const colorEntrySize = hasAlpha ? 4 : 3;
  const rowBytes = Math.ceil(width / 4) * 4;
  const data = zlib.inflateSync(compressed);
  const paletteLength = colorCount * colorEntrySize;
  const expected = paletteLength + rowBytes * height;
  if (data.length < expected) {
    throw new UnsupportedStructureError('Indexed lossless bitmap data is shorter than expected.');
  }

  const rgba = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const paletteIndex = data[paletteLength + y * rowBytes + x];
      if (paletteIndex >= colorCount) {
        throw new UnsupportedStructureError('Indexed lossless bitmap references a color outside the palette.');
      }
      const color = paletteIndex * colorEntrySize;
      const target = (y * width + x) * 4;
      rgba[target] = data[color];
      rgba[target + 1] = data[color + 1];
      rgba[target + 2] = data[color + 2];
      rgba[target + 3] = hasAlpha ? data[color + 3] : 255;
    }
  }

  return { characterId, code: tag.code, format, width, height, rgba };
}

function decodeLossless(tag) {
  if (tag.payload.length < 7) {
    throw new UnsupportedStructureError('Lossless bitmap tag is truncated.');
  }

  const characterId = readCharacterId(tag);
  const format = tag.payload[2];
  const width = tag.payload.readUInt16LE(3);
  const height = tag.payload.readUInt16LE(5);
  if (width <= 0 || height <= 0) {
    throw new UnsupportedStructureError('Lossless bitmap has invalid dimensions.');
  }

  if (format === 3) {
    return decodeIndexedLossless(tag, characterId, format, width, height, tag.payload.subarray(8));
  }
  if (format !== 5) {
    throw new UnsupportedStructureError(`Lossless bitmap format ${format} is not supported.`);
  }

  const raw = zlib.inflateSync(tag.payload.subarray(7));
  const hasAlpha = tag.code === TAG_DEFINE_BITS_LOSSLESS2;
  return {
    characterId,
    code: tag.code,
    format,
    width,
    height,
    rgba: argbToRgba(raw, width, height, hasAlpha)
  };
}

function decodeJpeg(tag) {
  const characterId = readCharacterId(tag);
  let jpegData;
  let alpha = null;

  if (tag.code === TAG_DEFINE_BITS_JPEG3) {
    if (tag.payload.length < 6) {
      throw new UnsupportedStructureError('DefineBitsJPEG3 tag is truncated.');
    }
    const alphaOffset = tag.payload.readUInt32LE(2);
    const jpegStart = 6;
    const jpegEnd = jpegStart + alphaOffset;
    if (jpegEnd > tag.payload.length) {
      throw new UnsupportedStructureError('DefineBitsJPEG3 alpha offset is outside the tag payload.');
    }
    jpegData = tag.payload.subarray(jpegStart, jpegEnd);
    alpha = zlib.inflateSync(tag.payload.subarray(jpegEnd));
  } else {
    jpegData = tag.payload.subarray(2);
  }

  const decoded = jpeg.decode(jpegData, {
    useTArray: true,
    maxMemoryUsageInMB: 512
  });
  const rgba = Buffer.from(decoded.data);
  const pixelCount = decoded.width * decoded.height;
  if (alpha) {
    if (alpha.length < pixelCount) {
      throw new UnsupportedStructureError('DefineBitsJPEG3 alpha data is shorter than expected.');
    }
    for (let index = 0; index < pixelCount; index += 1) {
      rgba[index * 4 + 3] = alpha[index];
    }
  }

  return {
    characterId,
    code: tag.code,
    width: decoded.width,
    height: decoded.height,
    rgba
  };
}

function decodeBitmapTag(tag) {
  if (tag.code === TAG_DEFINE_BITS_LOSSLESS || tag.code === TAG_DEFINE_BITS_LOSSLESS2) {
    return decodeLossless(tag);
  }
  if (tag.code === TAG_DEFINE_BITS_JPEG2 || tag.code === TAG_DEFINE_BITS_JPEG3) {
    return decodeJpeg(tag);
  }
  throw new UnsupportedStructureError(`Bitmap tag code ${tag.code} is not supported for adaptation.`);
}

function scaleHalfNearest(image) {
  const width = Math.max(1, Math.ceil(image.width / 2));
  const height = Math.max(1, Math.ceil(image.height / 2));
  const rgba = Buffer.alloc(width * height * 4);

  for (let y = 0; y < height; y += 1) {
    const sy = Math.min(image.height - 1, Math.floor((y + 0.5) * image.height / height));
    for (let x = 0; x < width; x += 1) {
      const sx = Math.min(image.width - 1, Math.floor((x + 0.5) * image.width / width));
      const source = (sy * image.width + sx) * 4;
      const target = (y * width + x) * 4;
      image.rgba.copy(rgba, target, source, source + 4);
    }
  }

  return { width, height, rgba };
}

function encodeLosslessBitmap(characterId, image) {
  const header = Buffer.alloc(7);
  header.writeUInt16LE(characterId, 0);
  header[2] = 5;
  header.writeUInt16LE(image.width, 3);
  header.writeUInt16LE(image.height, 5);
  const argb = rgbaToArgb(image.rgba, image.width, image.height, true);
  return {
    code: TAG_DEFINE_BITS_LOSSLESS2,
    payload: Buffer.concat([header, zlib.deflateSync(argb, { level: 9 })])
  };
}

function encodeJpegBitmap(characterId, image, withAlpha) {
  const encoded = jpeg.encode({
    data: Buffer.from(image.rgba),
    width: image.width,
    height: image.height
  }, 90);
  const jpegData = Buffer.from(encoded.data);

  if (!withAlpha) {
    const header = Buffer.alloc(2);
    header.writeUInt16LE(characterId, 0);
    return { code: TAG_DEFINE_BITS_JPEG2, payload: Buffer.concat([header, jpegData]) };
  }

  const header = Buffer.alloc(6);
  header.writeUInt16LE(characterId, 0);
  header.writeUInt32LE(jpegData.length, 2);
  const alpha = Buffer.alloc(image.width * image.height);
  for (let index = 0; index < image.width * image.height; index += 1) {
    alpha[index] = image.rgba[index * 4 + 3];
  }

  return {
    code: TAG_DEFINE_BITS_JPEG3,
    payload: Buffer.concat([header, jpegData, zlib.deflateSync(alpha, { level: 9 })])
  };
}

function encodeBitmapTag(characterId, image, sourceCode) {
  if (sourceCode === TAG_DEFINE_BITS_JPEG2) return encodeJpegBitmap(characterId, image, false);
  if (sourceCode === TAG_DEFINE_BITS_JPEG3) return encodeJpegBitmap(characterId, image, true);
  return encodeLosslessBitmap(characterId, image);
}

module.exports = {
  SUPPORTED_BITMAP_TAGS,
  decodeBitmapTag,
  encodeBitmapTag,
  scaleHalfNearest
};
