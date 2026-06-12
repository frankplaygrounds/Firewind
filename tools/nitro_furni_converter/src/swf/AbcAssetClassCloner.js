const { UnsupportedStructureError } = require('../errors');

const TAG_DO_ABC = 82;
const TRAIT_CLASS = 4;
const MULTINAME_QNAME = 0x07;
const MULTINAME_QNAME_A = 0x0d;

// Habbo's furniture loader asks ApplicationDomain for bitmap classes by name.
// When we add a _32_ bitmap tag, SymbolClass alone is not enough; this module
// clones the existing _64_ ABC class/trait and renames the clone to _32_.

function clone(value) {
  if (Buffer.isBuffer(value)) return Buffer.from(value);
  if (Array.isArray(value)) return value.map(clone);
  if (value && typeof value === 'object') {
    const out = {};
    for (const [key, item] of Object.entries(value)) out[key] = clone(item);
    return out;
  }
  return value;
}

class AbcReader {
  constructor(buffer) {
    this.buffer = buffer;
    this.offset = 0;
  }

  readU8() {
    if (this.offset >= this.buffer.length) throw new UnsupportedStructureError('ABC data is truncated.');
    return this.buffer[this.offset++];
  }

  readU16() {
    if (this.offset + 2 > this.buffer.length) throw new UnsupportedStructureError('ABC data is truncated.');
    const value = this.buffer.readUInt16LE(this.offset);
    this.offset += 2;
    return value;
  }

  readDouble() {
    if (this.offset + 8 > this.buffer.length) throw new UnsupportedStructureError('ABC data is truncated.');
    const bytes = Buffer.from(this.buffer.subarray(this.offset, this.offset + 8));
    this.offset += 8;
    return bytes;
  }

  readU30() {
    let result = 0;
    for (let index = 0; index < 5; index += 1) {
      const byte = this.readU8();
      result |= (byte & 0x7f) << (index * 7);
      if ((byte & 0x80) === 0) return result >>> 0;
    }
    throw new UnsupportedStructureError('Invalid ABC U30 value.');
  }

  readBytes(length) {
    if (this.offset + length > this.buffer.length) throw new UnsupportedStructureError('ABC data is truncated.');
    const out = Buffer.from(this.buffer.subarray(this.offset, this.offset + length));
    this.offset += length;
    return out;
  }
}

class AbcWriter {
  constructor() {
    this.chunks = [];
  }

  writeU8(value) {
    this.chunks.push(Buffer.from([value & 0xff]));
  }

  writeU16(value) {
    const buffer = Buffer.alloc(2);
    buffer.writeUInt16LE(value, 0);
    this.chunks.push(buffer);
  }

  writeU30(value) {
    let number = value >>> 0;
    do {
      let byte = number & 0x7f;
      number >>>= 7;
      if (number) byte |= 0x80;
      this.writeU8(byte);
    } while (number);
  }

  writeBytes(buffer) {
    this.chunks.push(Buffer.from(buffer));
  }

  buffer() {
    return Buffer.concat(this.chunks);
  }
}

function readCountedArray(reader, readItem) {
  const count = reader.readU30();
  const out = [null];
  for (let index = 1; index < count; index += 1) out[index] = readItem();
  return out;
}

function writeCountedArray(writer, items, writeItem) {
  writer.writeU30(items.length);
  for (let index = 1; index < items.length; index += 1) writeItem(items[index]);
}

function readU30FromBuffer(buffer, offset) {
  let result = 0;
  for (let index = 0; index < 5; index += 1) {
    if (offset + index >= buffer.length) throw new UnsupportedStructureError('ABC bytecode is truncated.');
    const byte = buffer[offset + index];
    result |= (byte & 0x7f) << (index * 7);
    if ((byte & 0x80) === 0) return { value: result >>> 0, offset: offset + index + 1 };
  }
  throw new UnsupportedStructureError('Invalid ABC bytecode U30 value.');
}

function encodeU30(value) {
  const writer = new AbcWriter();
  writer.writeU30(value);
  return writer.buffer();
}

function s24(buffer, offset) {
  if (offset + 3 > buffer.length) throw new UnsupportedStructureError('ABC bytecode branch is truncated.');
  return buffer.subarray(offset, offset + 3);
}

function isMultinameOpcode(opcode) {
  return new Set([
    0x04, 0x05, 0x45, 0x46, 0x4a, 0x4c, 0x4e, 0x4f, 0x59, 0x5d, 0x5e,
    0x60, 0x61, 0x66, 0x68, 0x6a, 0x80, 0x86
  ]).has(opcode);
}

function patchU30Operand(opcode, value, patch) {
  if (opcode === 0x58 && value === patch.sourceClassIndex) return patch.targetClassIndex;
  if (isMultinameOpcode(opcode) && value === patch.sourceNameIndex) return patch.targetNameIndex;
  return value;
}

function patchBytecode(code, patch) {
  const out = [];
  let offset = 0;
  while (offset < code.length) {
    const opcode = code[offset];
    out.push(Buffer.from([opcode]));
    offset += 1;

    if ([0x08, 0x25, 0x2c, 0x2d, 0x2e, 0x2f, 0x31, 0x40, 0x41, 0x42, 0x53, 0x55, 0x56,
      0x58, 0x59, 0x5a, 0x5d, 0x5e, 0x60, 0x61, 0x62, 0x63, 0x66, 0x68, 0x6a, 0x6c,
      0x6d, 0x6e, 0x6f, 0x80, 0x86].includes(opcode)) {
      const operand = readU30FromBuffer(code, offset);
      offset = operand.offset;
      out.push(encodeU30(patchU30Operand(opcode, operand.value, patch)));
    } else if ([0x32, 0x43, 0x44, 0x45, 0x46, 0x4a, 0x4c, 0x4e, 0x4f].includes(opcode)) {
      const first = readU30FromBuffer(code, offset);
      const second = readU30FromBuffer(code, first.offset);
      offset = second.offset;
      out.push(encodeU30(patchU30Operand(opcode, first.value, patch)), encodeU30(second.value));
    } else if (opcode === 0x24 || opcode === 0x65) {
      if (offset >= code.length) throw new UnsupportedStructureError('ABC bytecode U8 operand is truncated.');
      out.push(code.subarray(offset, offset + 1));
      offset += 1;
    } else if ((opcode >= 0x0c && opcode <= 0x1a) || opcode === 0x10) {
      out.push(s24(code, offset));
      offset += 3;
    } else if (opcode === 0x1b) {
      out.push(s24(code, offset));
      offset += 3;
      const caseCount = readU30FromBuffer(code, offset);
      offset = caseCount.offset;
      out.push(encodeU30(caseCount.value));
      for (let index = 0; index <= caseCount.value; index += 1) {
        out.push(s24(code, offset));
        offset += 3;
      }
    }
  }

  return Buffer.concat(out);
}

function bytecodeInitializesClass(code, classIndex, nameIndex) {
  let offset = 0;
  let sawNewClass = false;
  let sawInitProperty = false;

  while (offset < code.length) {
    const opcode = code[offset];
    offset += 1;

    if ([0x08, 0x25, 0x2c, 0x2d, 0x2e, 0x2f, 0x31, 0x40, 0x41, 0x42, 0x53, 0x55, 0x56,
      0x58, 0x59, 0x5a, 0x5d, 0x5e, 0x60, 0x61, 0x62, 0x63, 0x66, 0x68, 0x6a, 0x6c,
      0x6d, 0x6e, 0x6f, 0x80, 0x86].includes(opcode)) {
      const operand = readU30FromBuffer(code, offset);
      offset = operand.offset;
      if (opcode === 0x58 && operand.value === classIndex) sawNewClass = true;
      if (opcode === 0x68 && operand.value === nameIndex) sawInitProperty = true;
    } else if ([0x32, 0x43, 0x44, 0x45, 0x46, 0x4a, 0x4c, 0x4e, 0x4f].includes(opcode)) {
      const first = readU30FromBuffer(code, offset);
      const second = readU30FromBuffer(code, first.offset);
      offset = second.offset;
    } else if (opcode === 0x24 || opcode === 0x65) {
      offset += 1;
      if (offset > code.length) throw new UnsupportedStructureError('ABC bytecode U8 operand is truncated.');
    } else if ((opcode >= 0x0c && opcode <= 0x1a) || opcode === 0x10) {
      offset += 3;
      if (offset > code.length) throw new UnsupportedStructureError('ABC bytecode branch is truncated.');
    } else if (opcode === 0x1b) {
      offset += 3;
      const caseCount = readU30FromBuffer(code, offset);
      offset = caseCount.offset + (caseCount.value + 1) * 3;
      if (offset > code.length) throw new UnsupportedStructureError('ABC bytecode switch is truncated.');
    }
  }

  return sawNewClass && sawInitProperty;
}

function readMultiname(reader) {
  const kind = reader.readU8();
  if (kind === 0x07 || kind === 0x0d) return { kind, ns: reader.readU30(), name: reader.readU30() };
  if (kind === 0x0f || kind === 0x10) return { kind, name: reader.readU30() };
  if (kind === 0x11 || kind === 0x12) return { kind };
  if (kind === 0x09 || kind === 0x0e) return { kind, name: reader.readU30(), nsSet: reader.readU30() };
  if (kind === 0x1b || kind === 0x1c) return { kind, nsSet: reader.readU30() };
  if (kind === 0x1d) {
    const name = reader.readU30();
    const params = [];
    const count = reader.readU30();
    for (let index = 0; index < count; index += 1) params.push(reader.readU30());
    return { kind, name, params };
  }
  throw new UnsupportedStructureError(`Unsupported ABC multiname kind ${kind}.`);
}

function writeMultiname(writer, item) {
  writer.writeU8(item.kind);
  if (item.kind === 0x07 || item.kind === 0x0d) {
    writer.writeU30(item.ns);
    writer.writeU30(item.name);
  } else if (item.kind === 0x0f || item.kind === 0x10) {
    writer.writeU30(item.name);
  } else if (item.kind === 0x09 || item.kind === 0x0e) {
    writer.writeU30(item.name);
    writer.writeU30(item.nsSet);
  } else if (item.kind === 0x1b || item.kind === 0x1c) {
    writer.writeU30(item.nsSet);
  } else if (item.kind === 0x1d) {
    writer.writeU30(item.name);
    writer.writeU30(item.params.length);
    for (const param of item.params) writer.writeU30(param);
  }
}

function readMethodInfo(reader) {
  const paramCount = reader.readU30();
  const returnType = reader.readU30();
  const paramTypes = [];
  for (let index = 0; index < paramCount; index += 1) paramTypes.push(reader.readU30());
  const name = reader.readU30();
  const flags = reader.readU8();
  const options = [];
  if (flags & 0x08) {
    const optionCount = reader.readU30();
    for (let index = 0; index < optionCount; index += 1) {
      options.push({ value: reader.readU30(), kind: reader.readU8() });
    }
  }
  const paramNames = [];
  if (flags & 0x80) {
    for (let index = 0; index < paramCount; index += 1) paramNames.push(reader.readU30());
  }
  return { paramCount, returnType, paramTypes, name, flags, options, paramNames };
}

function writeMethodInfo(writer, method) {
  writer.writeU30(method.paramCount);
  writer.writeU30(method.returnType);
  for (const paramType of method.paramTypes) writer.writeU30(paramType);
  writer.writeU30(method.name);
  writer.writeU8(method.flags);
  if (method.flags & 0x08) {
    writer.writeU30(method.options.length);
    for (const option of method.options) {
      writer.writeU30(option.value);
      writer.writeU8(option.kind);
    }
  }
  if (method.flags & 0x80) {
    for (const name of method.paramNames) writer.writeU30(name);
  }
}

function readTraits(reader) {
  const count = reader.readU30();
  const traits = [];
  for (let index = 0; index < count; index += 1) {
    const name = reader.readU30();
    const kindAttr = reader.readU8();
    const kind = kindAttr & 0x0f;
    const attr = kindAttr >> 4;
    const trait = { name, kind, attr };
    if (kind === 0 || kind === 6) {
      trait.slotId = reader.readU30();
      trait.typeName = reader.readU30();
      trait.vindex = reader.readU30();
      if (trait.vindex !== 0) trait.vkind = reader.readU8();
    } else if (kind === 1 || kind === 2 || kind === 3) {
      trait.dispId = reader.readU30();
      trait.method = reader.readU30();
    } else if (kind === 4) {
      trait.slotId = reader.readU30();
      trait.classi = reader.readU30();
    } else if (kind === 5) {
      trait.slotId = reader.readU30();
      trait.functioni = reader.readU30();
    } else {
      throw new UnsupportedStructureError(`Unsupported ABC trait kind ${kind}.`);
    }
    trait.metadata = [];
    if (attr & 0x04) {
      const metadataCount = reader.readU30();
      for (let metaIndex = 0; metaIndex < metadataCount; metaIndex += 1) trait.metadata.push(reader.readU30());
    }
    traits.push(trait);
  }
  return traits;
}

function writeTraits(writer, traits) {
  writer.writeU30(traits.length);
  for (const trait of traits) {
    writer.writeU30(trait.name);
    writer.writeU8((trait.attr << 4) | trait.kind);
    if (trait.kind === 0 || trait.kind === 6) {
      writer.writeU30(trait.slotId);
      writer.writeU30(trait.typeName);
      writer.writeU30(trait.vindex);
      if (trait.vindex !== 0) writer.writeU8(trait.vkind);
    } else if (trait.kind === 1 || trait.kind === 2 || trait.kind === 3) {
      writer.writeU30(trait.dispId);
      writer.writeU30(trait.method);
    } else if (trait.kind === 4) {
      writer.writeU30(trait.slotId);
      writer.writeU30(trait.classi);
    } else if (trait.kind === 5) {
      writer.writeU30(trait.slotId);
      writer.writeU30(trait.functioni);
    }
    if (trait.attr & 0x04) {
      writer.writeU30(trait.metadata.length);
      for (const metadata of trait.metadata) writer.writeU30(metadata);
    }
  }
}

function readInstanceInfo(reader) {
  const item = {
    name: reader.readU30(),
    superName: reader.readU30(),
    flags: reader.readU8(),
    protectedNs: null,
    interfaces: [],
    iinit: null,
    traits: []
  };
  if (item.flags & 0x08) item.protectedNs = reader.readU30();
  const interfaceCount = reader.readU30();
  for (let index = 0; index < interfaceCount; index += 1) item.interfaces.push(reader.readU30());
  item.iinit = reader.readU30();
  item.traits = readTraits(reader);
  return item;
}

function writeInstanceInfo(writer, item) {
  writer.writeU30(item.name);
  writer.writeU30(item.superName);
  writer.writeU8(item.flags);
  if (item.flags & 0x08) writer.writeU30(item.protectedNs);
  writer.writeU30(item.interfaces.length);
  for (const iface of item.interfaces) writer.writeU30(iface);
  writer.writeU30(item.iinit);
  writeTraits(writer, item.traits);
}

function readClassInfo(reader) {
  return {
    cinit: reader.readU30(),
    traits: readTraits(reader)
  };
}

function writeClassInfo(writer, item) {
  writer.writeU30(item.cinit);
  writeTraits(writer, item.traits);
}

function readScriptInfo(reader) {
  return {
    init: reader.readU30(),
    traits: readTraits(reader)
  };
}

function writeScriptInfo(writer, item) {
  writer.writeU30(item.init);
  writeTraits(writer, item.traits);
}

function readMethodBody(reader) {
  const body = {
    method: reader.readU30(),
    maxStack: reader.readU30(),
    localCount: reader.readU30(),
    initScopeDepth: reader.readU30(),
    maxScopeDepth: reader.readU30(),
    code: null,
    exceptions: [],
    traits: []
  };
  body.code = reader.readBytes(reader.readU30());
  const exceptionCount = reader.readU30();
  for (let index = 0; index < exceptionCount; index += 1) {
    body.exceptions.push({
      from: reader.readU30(),
      to: reader.readU30(),
      target: reader.readU30(),
      excType: reader.readU30(),
      varName: reader.readU30()
    });
  }
  body.traits = readTraits(reader);
  return body;
}

function writeMethodBody(writer, body) {
  writer.writeU30(body.method);
  writer.writeU30(body.maxStack);
  writer.writeU30(body.localCount);
  writer.writeU30(body.initScopeDepth);
  writer.writeU30(body.maxScopeDepth);
  writer.writeU30(body.code.length);
  writer.writeBytes(body.code);
  writer.writeU30(body.exceptions.length);
  for (const exception of body.exceptions) {
    writer.writeU30(exception.from);
    writer.writeU30(exception.to);
    writer.writeU30(exception.target);
    writer.writeU30(exception.excType);
    writer.writeU30(exception.varName);
  }
  writeTraits(writer, body.traits);
}

class AbcFile {
  constructor() {
    this.minorVersion = 0;
    this.majorVersion = 0;
    this.ints = [null];
    this.uints = [null];
    this.doubles = [null];
    this.strings = [null];
    this.namespaces = [null];
    this.nsSets = [null];
    this.multinames = [null];
    this.methods = [];
    this.metadata = [];
    this.instances = [];
    this.classes = [];
    this.scripts = [];
    this.bodies = [];
  }

  static read(buffer) {
    const reader = new AbcReader(buffer);
    const abc = new AbcFile();
    abc.minorVersion = reader.readU16();
    abc.majorVersion = reader.readU16();
    abc.ints = readCountedArray(reader, () => reader.readU30());
    abc.uints = readCountedArray(reader, () => reader.readU30());
    abc.doubles = readCountedArray(reader, () => reader.readDouble());
    abc.strings = readCountedArray(reader, () => reader.readBytes(reader.readU30()));
    abc.namespaces = readCountedArray(reader, () => ({ kind: reader.readU8(), name: reader.readU30() }));
    abc.nsSets = readCountedArray(reader, () => {
      const namespaces = [];
      const count = reader.readU30();
      for (let index = 0; index < count; index += 1) namespaces.push(reader.readU30());
      return { namespaces };
    });
    abc.multinames = readCountedArray(reader, () => readMultiname(reader));

    const methodCount = reader.readU30();
    for (let index = 0; index < methodCount; index += 1) abc.methods.push(readMethodInfo(reader));

    const metadataCount = reader.readU30();
    for (let index = 0; index < metadataCount; index += 1) {
      const name = reader.readU30();
      const entries = [];
      const count = reader.readU30();
      for (let entryIndex = 0; entryIndex < count; entryIndex += 1) {
        entries.push({ key: reader.readU30(), value: reader.readU30() });
      }
      abc.metadata.push({ name, entries });
    }

    const classCount = reader.readU30();
    for (let index = 0; index < classCount; index += 1) abc.instances.push(readInstanceInfo(reader));
    for (let index = 0; index < classCount; index += 1) abc.classes.push(readClassInfo(reader));

    const scriptCount = reader.readU30();
    for (let index = 0; index < scriptCount; index += 1) abc.scripts.push(readScriptInfo(reader));

    const bodyCount = reader.readU30();
    for (let index = 0; index < bodyCount; index += 1) abc.bodies.push(readMethodBody(reader));

    if (reader.offset !== buffer.length) {
      throw new UnsupportedStructureError('ABC parser did not consume the full DoABC payload.');
    }
    return abc;
  }

  write() {
    const writer = new AbcWriter();
    writer.writeU16(this.minorVersion);
    writer.writeU16(this.majorVersion);
    writeCountedArray(writer, this.ints, (item) => writer.writeU30(item));
    writeCountedArray(writer, this.uints, (item) => writer.writeU30(item));
    writeCountedArray(writer, this.doubles, (item) => writer.writeBytes(item));
    writeCountedArray(writer, this.strings, (item) => {
      writer.writeU30(item.length);
      writer.writeBytes(item);
    });
    writeCountedArray(writer, this.namespaces, (item) => {
      writer.writeU8(item.kind);
      writer.writeU30(item.name);
    });
    writeCountedArray(writer, this.nsSets, (item) => {
      writer.writeU30(item.namespaces.length);
      for (const ns of item.namespaces) writer.writeU30(ns);
    });
    writeCountedArray(writer, this.multinames, (item) => writeMultiname(writer, item));

    writer.writeU30(this.methods.length);
    for (const method of this.methods) writeMethodInfo(writer, method);

    writer.writeU30(this.metadata.length);
    for (const item of this.metadata) {
      writer.writeU30(item.name);
      writer.writeU30(item.entries.length);
      for (const entry of item.entries) {
        writer.writeU30(entry.key);
        writer.writeU30(entry.value);
      }
    }

    writer.writeU30(this.instances.length);
    for (const item of this.instances) writeInstanceInfo(writer, item);
    for (const item of this.classes) writeClassInfo(writer, item);

    writer.writeU30(this.scripts.length);
    for (const item of this.scripts) writeScriptInfo(writer, item);

    writer.writeU30(this.bodies.length);
    for (const body of this.bodies) writeMethodBody(writer, body);

    return writer.buffer();
  }

  string(index) {
    const value = this.strings[index];
    return value ? value.toString('utf8') : '';
  }

  multinameLocalName(index) {
    const item = this.multinames[index];
    if (!item) return '';
    if (item.kind === MULTINAME_QNAME || item.kind === MULTINAME_QNAME_A) return this.string(item.name);
    return '';
  }

  addString(value) {
    const text = String(value);
    for (let index = 1; index < this.strings.length; index += 1) {
      if (this.string(index) === text) return index;
    }
    this.strings.push(Buffer.from(text, 'utf8'));
    return this.strings.length - 1;
  }

  addMultinameFrom(sourceMultinameIndex, targetName) {
    const source = this.multinames[sourceMultinameIndex];
    if (!source || (source.kind !== MULTINAME_QNAME && source.kind !== MULTINAME_QNAME_A)) {
      throw new UnsupportedStructureError('Asset class ABC name is not a QName and cannot be cloned safely.');
    }
    const targetString = this.addString(targetName);
    const target = clone(source);
    target.name = targetString;
    this.multinames.push(target);
    return this.multinames.length - 1;
  }

  findClassIndex(className) {
    return this.instances.findIndex((item) => this.multinameLocalName(item.name) === className);
  }

  hasClass(className) {
    return this.findClassIndex(className) !== -1;
  }

  cloneMethod(methodIndex) {
    const method = clone(this.methods[methodIndex]);
    this.methods.push(method);
    const newMethodIndex = this.methods.length - 1;
    const body = this.bodies.find((item) => item.method === methodIndex);
    if (body) {
      const newBody = clone(body);
      newBody.method = newMethodIndex;
      this.bodies.push(newBody);
    }
    return newMethodIndex;
  }

  cloneMethodWithPatchedBody(methodIndex, patch) {
    const method = clone(this.methods[methodIndex]);
    this.methods.push(method);
    const newMethodIndex = this.methods.length - 1;
    const body = this.bodies.find((item) => item.method === methodIndex);
    if (body) {
      const newBody = clone(body);
      newBody.method = newMethodIndex;
      newBody.code = patchBytecode(newBody.code, patch);
      this.bodies.push(newBody);
    }
    return newMethodIndex;
  }

  bodyForMethod(methodIndex) {
    return this.bodies.find((item) => item.method === methodIndex) || null;
  }

  classScriptTrait(classIndex, className = null) {
    for (const script of this.scripts) {
      const trait = script.traits.find((item) => (
        item.kind === TRAIT_CLASS
        && item.classi === classIndex
        && (!className || this.multinameLocalName(item.name) === className)
      ));
      if (trait) return { script, trait };
    }
    return null;
  }

  scriptInitializesClass(script, classIndex, nameIndex) {
    const body = this.bodyForMethod(script.init);
    return body ? bytecodeInitializesClass(body.code, classIndex, nameIndex) : false;
  }

  removeUninitializedClassTraits(classIndex) {
    for (const script of this.scripts) {
      script.traits = script.traits.filter((trait) => (
        trait.kind !== TRAIT_CLASS
        || trait.classi !== classIndex
        || this.scriptInitializesClass(script, classIndex, trait.name)
      ));
    }
  }

  addClonedClassScript({ sourceScript, sourceClassIndex, sourceNameIndex, targetClassIndex, targetNameIndex, targetTrait }) {
    const init = this.cloneMethodWithPatchedBody(sourceScript.init, {
      sourceClassIndex,
      targetClassIndex,
      sourceNameIndex,
      targetNameIndex
    });
    this.scripts.push({
      init,
      traits: [targetTrait]
    });
  }

  cloneAssetClass(sourceClassName, targetClassName) {
    const sourceClassIndex = this.findClassIndex(sourceClassName);
    if (sourceClassIndex === -1) {
      throw new UnsupportedStructureError(`ABC class ${sourceClassName} was not found.`);
    }

    const source = this.classScriptTrait(sourceClassIndex, sourceClassName);
    if (!source) {
      throw new UnsupportedStructureError(`ABC script trait for ${sourceClassName} was not found.`);
    }

    let targetClassIndex = this.findClassIndex(targetClassName);
    if (targetClassIndex !== -1) {
      const target = this.classScriptTrait(targetClassIndex, targetClassName);
      const targetMultiname = this.instances[targetClassIndex].name;
      const targetNameIndex = target ? target.trait.name : targetMultiname;
      if (target && this.scriptInitializesClass(target.script, targetClassIndex, targetNameIndex)) {
        return false;
      }

      this.removeUninitializedClassTraits(targetClassIndex);
      const newTrait = clone(source.trait);
      newTrait.name = targetMultiname;
      newTrait.classi = targetClassIndex;
      newTrait.slotId = source.trait.slotId || 1;
      this.addClonedClassScript({
        sourceScript: source.script,
        sourceClassIndex,
        sourceNameIndex: source.trait.name,
        targetClassIndex,
        targetNameIndex: targetMultiname,
        targetTrait: newTrait
      });
      return true;
    }

    const targetMultiname = this.addMultinameFrom(this.instances[sourceClassIndex].name, targetClassName);
    const newInstance = clone(this.instances[sourceClassIndex]);
    newInstance.name = targetMultiname;
    newInstance.iinit = this.cloneMethod(newInstance.iinit);

    const newClass = clone(this.classes[sourceClassIndex]);
    newClass.cinit = this.cloneMethod(newClass.cinit);

    this.instances.push(newInstance);
    this.classes.push(newClass);
    targetClassIndex = this.instances.length - 1;

    const newTrait = clone(source.trait);
    newTrait.name = targetMultiname;
    newTrait.classi = targetClassIndex;
    newTrait.slotId = source.trait.slotId || 1;

    this.addClonedClassScript({
      sourceScript: source.script,
      sourceClassIndex,
      sourceNameIndex: source.trait.name,
      targetClassIndex,
      targetNameIndex: targetMultiname,
      targetTrait: newTrait
    });

    return true;
  }
}

function splitDoAbcPayload(payload) {
  if (payload.length < 5) throw new UnsupportedStructureError('DoABC tag is truncated.');
  let offset = 4;
  const end = payload.indexOf(0, offset);
  if (end === -1) throw new UnsupportedStructureError('DoABC tag name is truncated.');
  return {
    flags: payload.subarray(0, 4),
    name: payload.subarray(offset, end + 1),
    abc: payload.subarray(end + 1)
  };
}

function joinDoAbcPayload(parts, abcBuffer) {
  return Buffer.concat([parts.flags, parts.name, abcBuffer]);
}

function cloneAssetClassesInSwf(swf, mappings) {
  const pending = mappings.slice();
  let cloned = 0;

  for (const tag of swf.tags) {
    if (tag.code !== TAG_DO_ABC || !pending.length) continue;
    const parts = splitDoAbcPayload(tag.payload);
    let abc;
    try {
      abc = AbcFile.read(parts.abc);
    } catch {
      continue;
    }

    let changed = false;
    for (let index = pending.length - 1; index >= 0; index -= 1) {
      const mapping = pending[index];
      if (!abc.hasClass(mapping.sourceClassName)) continue;
      const didClone = abc.cloneAssetClass(mapping.sourceClassName, mapping.targetClassName);
      pending.splice(index, 1);
      if (didClone) {
        cloned += 1;
        changed = true;
      }
    }

    if (changed) tag.payload = joinDoAbcPayload(parts, abc.write());
  }

  if (pending.length) {
    throw new UnsupportedStructureError(
      `Could not clone ABC class(es): ${pending.map((item) => item.sourceClassName).join(', ')}.`
    );
  }

  return { cloned };
}

module.exports = {
  AbcFile,
  cloneAssetClassesInSwf
};
