const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const { SwfFormatError } = require('../errors');

const TAG_END = 0;
const TAG_DEFINE_BINARY_DATA = 87;
const TAG_SYMBOL_CLASS = 76;

const TAG_NAMES = {
  0: 'End',
  1: 'ShowFrame',
  9: 'SetBackgroundColor',
  36: 'DefineBitsLossless2',
  41: 'ProductInfo',
  43: 'FrameLabel',
  65: 'ScriptLimits',
  69: 'FileAttributes',
  76: 'SymbolClass',
  77: 'Metadata',
  82: 'DoABC',
  87: 'DefineBinaryData'
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
  tagName
};
