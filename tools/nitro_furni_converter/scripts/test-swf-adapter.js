const fs = require('fs');
const path = require('path');
const { SwfZoomAdapter } = require('../src/swf/SwfZoomAdapter');
const { SwfFile, encodeSymbolClass } = require('../src/swf/SwfFile');
const { encodeBitmapTag, decodeBitmapTag } = require('../src/swf/BitmapTag');
const { AbcFile } = require('../src/swf/AbcAssetClassCloner');
const { classifyBinaryXml } = require('../src/verify/Verifier');
const { Logger } = require('../src/logger');
const { fromLatin1Buffer, toLatin1Buffer } = require('../src/utils');

const root = path.resolve(__dirname, '..');
const tmp = path.join(root, '.tmp', 'swf-adapter-test');
const input = path.join(tmp, 'test_furni.swf');

function tag(code, payload) {
  return { code, payload };
}

function binaryData(characterId, text) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(characterId, 0);
  header.writeUInt32LE(0, 2);
  return Buffer.concat([header, toLatin1Buffer(text)]);
}

function fixtureImage() {
  const width = 4;
  const height = 4;
  const rgba = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const offset = (y * width + x) * 4;
      rgba[offset] = x * 40;
      rgba[offset + 1] = y * 40;
      rgba[offset + 2] = 160;
      rgba[offset + 3] = 255;
    }
  }
  return { width, height, rgba };
}

function requireIncludes(text, needle, label) {
  if (!text.includes(needle)) {
    console.error(text);
    throw new Error(`SWF adapter test failed: ${label} does not contain ${needle}.`);
  }
}

function methodInfo() {
  return {
    paramCount: 0,
    returnType: 0,
    paramTypes: [],
    name: 0,
    flags: 0,
    options: [],
    paramNames: []
  };
}

function methodBody(method, code = Buffer.from([0x47])) {
  return {
    method,
    maxStack: 0,
    localCount: 1,
    initScopeDepth: 0,
    maxScopeDepth: 0,
    code,
    exceptions: [],
    traits: []
  };
}

function sourceScriptInitCode() {
  return Buffer.from([
    0xd0, 0x30,
    0x5d, 0x01,
    0x58, 0x00,
    0x68, 0x01,
    0x47
  ]);
}

function doAbcForClass(className) {
  const abc = new AbcFile();
  abc.minorVersion = 16;
  abc.majorVersion = 46;
  abc.strings = [null, Buffer.from('', 'utf8'), Buffer.from(className, 'utf8')];
  abc.namespaces = [null, { kind: 0x16, name: 1 }];
  abc.multinames = [null, { kind: 0x07, ns: 1, name: 2 }];
  abc.methods = [methodInfo(), methodInfo(), methodInfo()];
  abc.instances = [{
    name: 1,
    superName: 0,
    flags: 0,
    protectedNs: null,
    interfaces: [],
    iinit: 0,
    traits: []
  }];
  abc.classes = [{ cinit: 1, traits: [] }];
  abc.scripts = [{
    init: 2,
    traits: [{ name: 1, kind: 4, attr: 0, slotId: 1, classi: 0, metadata: [] }]
  }];
  abc.bodies = [methodBody(0), methodBody(1), methodBody(2, sourceScriptInitCode())];

  const flags = Buffer.alloc(4);
  return Buffer.concat([flags, Buffer.from('frame1\0', 'utf8'), abc.write()]);
}

fs.rmSync(tmp, { recursive: true, force: true });
fs.mkdirSync(tmp, { recursive: true });

const manifestXml = [
  '<?xml version="1.0" encoding="ISO-8859-1" ?>',
  '<manifest>',
  '  <library name="test_furni" version="0.1">',
  '    <assets>',
  '      <asset name="index" mimeType="text/xml"/>',
  '      <asset name="test_furni_assets" mimeType="text/xml"/>',
  '      <asset name="test_furni_visualization" mimeType="text/xml"/>',
  '      <asset name="test_furni_64_a_0_0" mimeType="image/png"/>',
  '    </assets>',
  '  </library>',
  '</manifest>'
].join('\n');

const assetsXml = [
  '<?xml version="1.0" encoding="ISO-8859-1" ?>',
  '<assets>',
  '  <asset name="test_furni_64_a_0_0" x="9" y="7"/>',
  '  <asset name="test_furni_64_a_2_0" x="9" y="7" source="test_furni_64_a_0_0" flipH="1"/>',
  '</assets>'
].join('\n');

const visualizationXml = [
  '<?xml version="1.0" encoding="ISO-8859-1" ?>',
  '<visualizationData type="test_furni">',
  '  <visualization size="64" layerCount="1" angle="45"/>',
  '</visualizationData>'
].join('\n');

const bitmap = encodeBitmapTag(10, fixtureImage(), 36);
const swf = new SwfFile({
  signature: 'FWS',
  version: 10,
  frameHeader: Buffer.from([0x00, 0x00, 0x18, 0x01, 0x00]),
  tags: [
    tag(87, binaryData(1, manifestXml)),
    tag(87, binaryData(2, assetsXml)),
    tag(87, binaryData(3, visualizationXml)),
    bitmap,
    tag(82, doAbcForClass('test_furni_test_furni_64_a_0_0')),
    tag(76, encodeSymbolClass([
      { characterId: 1, name: 'test_furni_manifest' },
      { characterId: 2, name: 'test_furni_test_furni_assets' },
      { characterId: 3, name: 'test_furni_test_furni_visualization' },
      { characterId: 10, name: 'test_furni_test_furni_64_a_0_0' },
      { characterId: 0, name: 'test_furni' }
    ])),
    tag(1, Buffer.alloc(0)),
    tag(0, Buffer.alloc(0))
  ]
});
swf.write(input);

const result = new SwfZoomAdapter({ logger: new Logger({ quiet: true }) }).adaptFile(input);
if (!result.changed || result.generatedDirect.length !== 1 || result.generatedAliases.length !== 1) {
  throw new Error('SWF adapter test failed: expected one generated bitmap and one generated alias.');
}

const adapted = SwfFile.read(input);
const binaries = classifyBinaryXml(adapted);
const adaptedAssetsXml = fromLatin1Buffer(binaries.assets.data);
const adaptedManifestXml = fromLatin1Buffer(binaries.manifest.data);
const adaptedVisualizationXml = fromLatin1Buffer(binaries.visualization.data);
requireIncludes(adaptedAssetsXml, 'name="test_furni_32_a_0_0"', 'assets XML');
requireIncludes(adaptedAssetsXml, 'source="test_furni_32_a_0_0"', 'assets XML');
requireIncludes(adaptedManifestXml, 'name="test_furni_32_a_0_0"', 'manifest XML');
requireIncludes(adaptedVisualizationXml, 'size="32"', 'visualization XML');

const symbols = adapted.parseSymbols();
const targetSymbol = symbols.find((symbol) => symbol.name === 'test_furni_test_furni_32_a_0_0');
if (!targetSymbol) throw new Error('SWF adapter test failed: missing generated SymbolClass mapping.');
const tagRecord = adapted.tagByCharacterId(targetSymbol.characterId, [36]);
if (!tagRecord) throw new Error('SWF adapter test failed: missing generated bitmap tag.');
const generatedBitmap = decodeBitmapTag(tagRecord.tag);
if (generatedBitmap.width !== 2 || generatedBitmap.height !== 2) {
  throw new Error('SWF adapter test failed: generated bitmap was not half-sized.');
}
const abcBytes = Buffer.concat(adapted.tags.filter((item) => item.code === 82).map((item) => item.payload));
if (!abcBytes.includes(Buffer.from('test_furni_test_furni_32_a_0_0'))) {
  throw new Error('SWF adapter test failed: generated ABC class was not cloned.');
}
const abcPayload = adapted.tags.find((item) => item.code === 82).payload;
const abcStart = abcPayload.indexOf(0, 4) + 1;
const parsedAbc = AbcFile.read(abcPayload.subarray(abcStart));
const targetClassIndex = parsedAbc.findClassIndex('test_furni_test_furni_32_a_0_0');
const targetScript = parsedAbc.scripts.find((script) => script.traits.some((trait) => trait.classi === targetClassIndex));
const targetBody = parsedAbc.bodies.find((body) => body.method === targetScript.init);
if (!targetBody || !targetBody.code.includes(Buffer.from([0x58, targetClassIndex]))) {
  throw new Error('SWF adapter test failed: generated ABC class initializer was not cloned.');
}

const second = new SwfZoomAdapter({ logger: new Logger({ quiet: true }) }).adaptFile(input);
if (second.changed) throw new Error('SWF adapter test failed: second run should be idempotent.');

console.log('Synthetic SWF zoom adapter test passed.');
