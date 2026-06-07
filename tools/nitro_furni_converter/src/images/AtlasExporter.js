const { PNG } = require('pngjs');
const path = require('path');
const { UnsupportedStructureError } = require('../errors');
const { ensureDir, writeBuffer } = require('../utils');

function decodePng(buffer, label = 'PNG') {
  try {
    return PNG.sync.read(buffer);
  } catch (error) {
    throw new UnsupportedStructureError(`${label} could not be decoded as PNG: ${error.message}`, {
      cause: error
    });
  }
}

function encodePng(image) {
  return PNG.sync.write(image, {
    colorType: 6,
    inputColorType: 6
  });
}

function validateFrame(frameData, assetName) {
  if (!frameData || typeof frameData !== 'object') {
    throw new UnsupportedStructureError(`Missing spritesheet frame for ${assetName}.`);
  }
  if (frameData.rotated) {
    throw new UnsupportedStructureError(
      `Frame ${assetName} is rotated in the atlas; rotated TexturePacker frames are not supported yet.`
    );
  }
  const frame = frameData.frame;
  if (!frame || !Number.isFinite(Number(frame.x)) || !Number.isFinite(Number(frame.y))
    || !Number.isFinite(Number(frame.w)) || !Number.isFinite(Number(frame.h))) {
    throw new UnsupportedStructureError(`Frame ${assetName} has invalid x/y/w/h data.`);
  }
  return {
    x: Number(frame.x),
    y: Number(frame.y),
    width: Number(frame.w),
    height: Number(frame.h)
  };
}

function crop(image, frameData, assetName) {
  const frame = validateFrame(frameData, assetName);
  if (frame.width <= 0 || frame.height <= 0) {
    throw new UnsupportedStructureError(`Frame ${assetName} has non-positive dimensions.`);
  }
  if (frame.x < 0 || frame.y < 0 || frame.x + frame.width > image.width || frame.y + frame.height > image.height) {
    throw new UnsupportedStructureError(`Frame ${assetName} is outside the PNG atlas bounds.`);
  }

  const out = new PNG({ width: frame.width, height: frame.height });
  for (let y = 0; y < frame.height; y += 1) {
    const sourceStart = ((frame.y + y) * image.width + frame.x) * 4;
    const targetStart = y * frame.width * 4;
    image.data.copy(out.data, targetStart, sourceStart, sourceStart + frame.width * 4);
  }
  return out;
}

function scaleHalfNearest(image) {
  const width = Math.max(1, Math.ceil(image.width / 2));
  const height = Math.max(1, Math.ceil(image.height / 2));
  const out = new PNG({ width, height });

  for (let y = 0; y < height; y += 1) {
    const sy = Math.min(image.height - 1, Math.floor((y + 0.5) * image.height / height));
    for (let x = 0; x < width; x += 1) {
      const sx = Math.min(image.width - 1, Math.floor((x + 0.5) * image.width / width));
      const source = (sy * image.width + sx) * 4;
      const target = (y * width + x) * 4;
      image.data.copy(out.data, target, source, source + 4);
    }
  }

  return out;
}

class AtlasExporter {
  constructor(atlasBuffer) {
    this.atlas = decodePng(atlasBuffer, 'Nitro atlas');
  }

  exportFrames(frames, outputDir) {
    ensureDir(outputDir);
    const exported = [];

    for (const frame of frames) {
      const image = frame.scale === 0.5
        ? scaleHalfNearest(crop(this.atlas, frame.sourceFrameData, frame.name))
        : crop(this.atlas, frame.sourceFrameData, frame.name);
      const buffer = encodePng(image);
      const fileBase = frame.fileBase || frame.name;
      const outputFile = path.join(outputDir, `${fileBase}.png`);
      writeBuffer(outputFile, buffer);
      exported.push({
        ...frame,
        fileBase,
        outputFile,
        width: image.width,
        height: image.height
      });
    }

    return exported;
  }
}

module.exports = {
  AtlasExporter,
  decodePng,
  encodePng,
  crop,
  scaleHalfNearest
};
