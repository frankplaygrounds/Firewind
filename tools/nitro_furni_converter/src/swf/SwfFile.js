const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const { SwfFormatError } = require('../errors');

const TAG_END = 0;
const TAG_DEFINE_BINARY_DATA = 87;
const TAG_SYMBOL_CLASS = 76;
const TAG_DEFINE_BITS_LOSSLESS = 20;
const TAG_DEFINE_BITS_JPEG2 = 21;
const TAG_DEFINE_BITS_JPEG3 = 35;
const TAG_DEFINE_BITS_LOSSLESS2 = 36;
const TAG_DEFINE_BITS_JPEG4 = 90;

const CHARACTER_ID_TAGS = new Set([
  2, 6, 7, 11, 13, 14, 20, 21, 22, 32, 33, 34, 35, 36, 37, 39, 46, 48, 56,
  60, 62, 75, 83, 84, 87, 90, 91
]);

const TAG_NAMES = {
  0: 'End',
  1: 'ShowFrame',
  9: 'SetBackgroundColor',
  20: 'DefineBitsLossless',
  21: 'DefineBitsJPEG2',
  35: 'DefineBitsJPEG3',
  36: 'DefineBitsLossless2',
  41: 'ProductInfo',
  43: 'FrameLabel',
  65: 'ScriptLimits',
  69: 'FileAttributes',
  76: 'SymbolClass',
  77: 'Metadata',
  82: 'DoABC',
  87: 'DefineBinaryData',
  90: 'DefineBitsJPEG4'
};

function tagName(code) {
  return TAG_NAMES[code] || `Tag${code}`;
}

function readUi16(buffer, offset, context) {
  if (offset + 2 > buffer.length) throw new SwfFormatError(`Truncated SWF ${context}.`);
  return buffer.readUInt16LE(offset);
}

function readUi32(buffer, offset, context) {
  if (offset + 4 > buffer.length) throw new SwfFormatError(`Truncated SWF ${context}.`);
  return buffer.readUInt32LE(offset);
}

function frameHeaderLength(body) {
  if (!body.length) throw new SwfFormatError('SWF body is empty.');
  const rectNbits = body[0] >> 3;
  const rectLength = Math.ceil((5 + rectNbits * 4) / 8);
  return rectLength + 4;
}

function encodeTag(tag) {
  const length = tag.payload.length;
  if (length < 0x3f) {
    const header = Buffer.alloc(2);
    header.writeUInt16LE((tag.code << 6) | length, 0);
    return Buffer.concat([header, tag.payload]);
  }
  const header = Buffer.alloc(6);
  header.writeUInt16LE((tag.code << 6) | 0x3f, 0);
  header.writeUInt32LE(length, 2);
  return Buffer.concat([header, tag.payload]);
}

function readCString(buffer, offset) {
  const end = buffer.indexOf(0, offset);
  if (end === -1) throw new SwfFormatError('Truncated SWF C string.');
  return {
    value: buffer.subarray(offset, end).toString('utf8'),
    offset: end + 1
  };
}

function parseSymbolClass(payload) {
  const count = readUi16(payload, 0, 'SymbolClass count');
  let offset = 2;
  const symbols = [];
  for (let index = 0; index < count; index += 1) {
    const characterId = readUi16(payload, offset, 'SymbolClass character id');
    offset += 2;
    const string = readCString(payload, offset);
    offset = string.offset;
    symbols.push({ characterId, name: string.value });
  }
  return symbols;
}

function encodeSymbolClass(symbols) {
  const chunks = [Buffer.alloc(2)];
  chunks[0].writeUInt16LE(symbols.length, 0);
  for (const symbol of symbols) {
    const id = Buffer.alloc(2);
    id.writeUInt16LE(symbol.characterId, 0);
    chunks.push(id, Buffer.from(symbol.name, 'utf8'), Buffer.from([0]));
  }
  return Buffer.concat(chunks);
}

function tagCharacterId(tag) {
  if (!CHARACTER_ID_TAGS.has(tag.code) || tag.payload.length < 2) return null;
  return tag.payload.readUInt16LE(0);
}

class SwfFile {
  constructor({ signature, version, frameHeader, tags }) {
    this.signature = signature;
    this.version = version;
    this.frameHeader = frameHeader;
    this.tags = tags;
  }

  static read(file) {
    const inputPath = path.resolve(file);
    const raw = fs.readFileSync(inputPath);
    if (raw.length < 8) throw new SwfFormatError('File is too small to be a SWF.');

    const signature = raw.subarray(0, 3).toString('ascii');
    if (signature !== 'FWS' && signature !== 'CWS') {
      throw new SwfFormatError(`Unsupported SWF signature ${signature}; only FWS and CWS are supported.`);
    }

    let body;
    try {
      body = signature === 'CWS' ? zlib.inflateSync(raw.subarray(8)) : raw.subarray(8);
    } catch (error) {
      throw new SwfFormatError(`Could not decompress CWS body: ${error.message}`, { cause: error });
    }

    const headerLength = frameHeaderLength(body);
    if (headerLength > body.length) throw new SwfFormatError('Truncated SWF frame header.');
    const frameHeader = body.subarray(0, headerLength);
    const tags = [];
    let offset = headerLength;

    while (offset < body.length) {
      const codeAndLength = readUi16(body, offset, 'tag header');
      offset += 2;
      const code = codeAndLength >> 6;
      let length = codeAndLength & 0x3f;
      if (length === 0x3f) {
        length = readUi32(body, offset, 'long tag length');
        offset += 4;
      }
      if (offset + length > body.length) {
        throw new SwfFormatError(`Truncated SWF ${tagName(code)} payload.`);
      }
      tags.push({ code, payload: Buffer.from(body.subarray(offset, offset + length)) });
      offset += length;
      if (code === TAG_END) break;
    }

    return new SwfFile({ signature, version: raw[3], frameHeader: Buffer.from(frameHeader), tags });
  }

  write(file) {
    const body = Buffer.concat([this.frameHeader, ...this.tags.map(encodeTag)]);
    const header = Buffer.alloc(8);
    header.write(this.signature, 0, 3, 'ascii');
    header[3] = this.version;
    header.writeUInt32LE(body.length + 8, 4);
    const payload = this.signature === 'CWS' ? zlib.deflateSync(body, { level: 9 }) : body;
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, Buffer.concat([header, payload]));
  }

  parseSymbols() {
    const symbols = [];
    for (const tag of this.tags) {
      if (tag.code === TAG_SYMBOL_CLASS) symbols.push(...parseSymbolClass(tag.payload));
    }
    return symbols;
  }

  usedCharacterIds() {
    const ids = new Set();
    for (const tag of this.tags) {
      const id = tagCharacterId(tag);
      if (id !== null) ids.add(id);
    }
    for (const symbol of this.parseSymbols()) ids.add(symbol.characterId);
    return ids;
  }

  nextCharacterId() {
    const ids = this.usedCharacterIds();
    for (let id = 1; id <= 0xffff; id += 1) {
      if (!ids.has(id)) return id;
    }
    throw new SwfFormatError('No free SWF character IDs remain.');
  }

  tagByCharacterId(characterId, codes = null) {
    const allowed = codes ? new Set(codes) : null;
    for (let tagIndex = 0; tagIndex < this.tags.length; tagIndex += 1) {
      const tag = this.tags[tagIndex];
      if (allowed && !allowed.has(tag.code)) continue;
      if (tagCharacterId(tag) === characterId) return { tag, tagIndex };
    }
    return null;
  }

  insertTagBefore(predicate, tag) {
    const index = this.tags.findIndex(predicate);
    if (index === -1) {
      this.tags.push(tag);
      return this.tags.length - 1;
    }
    this.tags.splice(index, 0, tag);
    return index;
  }

  addSymbol(characterId, name) {
    let symbolTag = null;
    for (const tag of this.tags) {
      if (tag.code === TAG_SYMBOL_CLASS) {
        symbolTag = tag;
        break;
      }
    }

    if (!symbolTag) {
      symbolTag = { code: TAG_SYMBOL_CLASS, payload: encodeSymbolClass([]) };
      this.insertTagBefore((tag) => tag.code === 1 || tag.code === TAG_END, symbolTag);
    }

    const symbols = parseSymbolClass(symbolTag.payload);
    const same = symbols.find((symbol) => symbol.characterId === characterId && symbol.name === name);
    if (same) return false;
    if (symbols.some((symbol) => symbol.characterId === characterId)) {
      throw new SwfFormatError(`Character ID ${characterId} is already exported with another symbol name.`);
    }
    if (symbols.some((symbol) => symbol.name === name)) {
      throw new SwfFormatError(`Symbol name ${name} is already exported with another character ID.`);
    }

    symbols.push({ characterId, name });
    symbolTag.payload = encodeSymbolClass(symbols);
    return true;
  }

  binaryData() {
    const out = [];
    for (let tagIndex = 0; tagIndex < this.tags.length; tagIndex += 1) {
      const tag = this.tags[tagIndex];
      if (tag.code !== TAG_DEFINE_BINARY_DATA) continue;
      if (tag.payload.length < 6) throw new SwfFormatError('Truncated DefineBinaryData tag.');
      out.push({
        characterId: tag.payload.readUInt16LE(0),
        reserved: tag.payload.readUInt32LE(2),
        data: Buffer.from(tag.payload.subarray(6)),
        tagIndex
      });
    }
    return out;
  }

  setBinaryData(binary, data) {
    const header = Buffer.alloc(6);
    header.writeUInt16LE(binary.characterId, 0);
    header.writeUInt32LE(binary.reserved, 2);
    this.tags[binary.tagIndex].payload = Buffer.concat([header, Buffer.from(data)]);
  }

  tagCounts() {
    const counts = {};
    for (const tag of this.tags) counts[tagName(tag.code)] = (counts[tagName(tag.code)] || 0) + 1;
    return counts;
  }
}

module.exports = {
  SwfFile,
  TAG_END,
  TAG_DEFINE_BINARY_DATA,
  TAG_SYMBOL_CLASS,
  TAG_DEFINE_BITS_LOSSLESS,
  TAG_DEFINE_BITS_JPEG2,
  TAG_DEFINE_BITS_JPEG3,
  TAG_DEFINE_BITS_LOSSLESS2,
  TAG_DEFINE_BITS_JPEG4,
  CHARACTER_ID_TAGS,
  encodeSymbolClass,
  tagCharacterId,
  tagName
};
