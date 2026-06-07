const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const { NitroFormatError } = require('../errors');

function requireBytes(buffer, offset, length, context) {
  if (offset + length > buffer.length) {
    throw new NitroFormatError(`Invalid .nitro file: truncated ${context}.`);
  }
}

function decompressEntry(data, name) {
  try {
    return zlib.inflateSync(data);
  } catch (inflateError) {
    try {
      return zlib.gunzipSync(data);
    } catch {
      throw new NitroFormatError(`Could not decompress ${name}: ${inflateError.message}`, {
        cause: inflateError
      });
    }
  }
}

function validateDescriptor(json, sourceFile) {
  if (!json || typeof json !== 'object') {
    throw new NitroFormatError(`Descriptor in ${sourceFile} is not a JSON object.`);
  }
  if (!json.name || typeof json.name !== 'string') {
    json.name = path.basename(sourceFile, path.extname(sourceFile));
  }
  if (!json.assets || typeof json.assets !== 'object') {
    throw new NitroFormatError(`Descriptor for ${json.name} has no assets object.`);
  }
  if (!json.spritesheet || !json.spritesheet.frames || typeof json.spritesheet.frames !== 'object') {
    throw new NitroFormatError(`Descriptor for ${json.name} has no spritesheet.frames object.`);
  }
  if (!Array.isArray(json.visualizations)) {
    throw new NitroFormatError(`Descriptor for ${json.name} has no visualizations array.`);
  }
  return json;
}

class NitroReader {
  read(file) {
    const inputPath = path.resolve(file);
    let buffer;
    try {
      buffer = fs.readFileSync(inputPath);
    } catch (error) {
      throw new NitroFormatError(`Could not read input file: ${inputPath}`, { cause: error });
    }

    if (buffer.length < 2) {
      throw new NitroFormatError('Invalid .nitro file: missing entry count.');
    }

    let offset = 0;
    const count = buffer.readInt16BE(offset);
    offset += 2;
    if (count <= 0 || count > 1024) {
      throw new NitroFormatError(`Invalid .nitro file: suspicious entry count ${count}.`);
    }

    const entries = [];
    let descriptor = null;
    let atlas = null;

    for (let index = 0; index < count; index += 1) {
      requireBytes(buffer, offset, 2, `entry ${index} name length`);
      const nameLength = buffer.readInt16BE(offset);
      offset += 2;
      if (nameLength <= 0 || nameLength > 4096) {
        throw new NitroFormatError(`Invalid .nitro file: suspicious name length ${nameLength}.`);
      }

      requireBytes(buffer, offset, nameLength, `entry ${index} name`);
      const name = buffer.subarray(offset, offset + nameLength).toString('utf8');
      offset += nameLength;

      requireBytes(buffer, offset, 4, `entry ${name} payload length`);
      const compressedLength = buffer.readInt32BE(offset);
      offset += 4;
      if (compressedLength < 0) {
        throw new NitroFormatError(`Invalid .nitro file: negative payload length for ${name}.`);
      }

      requireBytes(buffer, offset, compressedLength, `entry ${name} payload`);
      const compressed = buffer.subarray(offset, offset + compressedLength);
      offset += compressedLength;

      const data = decompressEntry(compressed, name);
      entries.push({ name, compressedLength, length: data.length });

      if (name.toLowerCase().endsWith('.json')) {
        try {
          descriptor = validateDescriptor(JSON.parse(data.toString('utf8')), inputPath);
        } catch (error) {
          if (error instanceof NitroFormatError) throw error;
          throw new NitroFormatError(`Could not parse JSON descriptor ${name}: ${error.message}`, {
            cause: error
          });
        }
      } else if (name.toLowerCase().endsWith('.png')) {
        atlas = data;
      }
    }

    if (offset !== buffer.length) {
      throw new NitroFormatError(`Invalid .nitro file: ${buffer.length - offset} trailing byte(s).`);
    }
    if (!descriptor) throw new NitroFormatError(`No JSON descriptor found in ${inputPath}.`);
    if (!atlas) throw new NitroFormatError(`No PNG atlas found in ${inputPath}.`);

    return {
      inputPath,
      descriptor,
      atlas,
      entries
    };
  }
}

module.exports = { NitroReader };
