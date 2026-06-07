const fs = require('fs');
const path = require('path');
const { GraphicTagCleaner } = require('../src/swf/GraphicTagCleaner');
const { SwfFile } = require('../src/swf/SwfFile');
const { Logger } = require('../src/logger');
const { toLatin1Buffer, fromLatin1Buffer } = require('../src/utils');

const root = path.resolve(__dirname, '..');
const tmp = path.join(root, '.tmp', 'swf-cleaner-test');
const input = path.join(tmp, 'input.swf');
const output = path.join(tmp, 'output.swf');

function tag(code, payload) {
  return { code, payload };
}

function symbolClass(entries) {
  const chunks = [Buffer.alloc(2)];
  chunks[0].writeUInt16LE(entries.length, 0);
  for (const [characterId, name] of entries) {
    const id = Buffer.alloc(2);
    id.writeUInt16LE(characterId, 0);
    chunks.push(id, Buffer.from(name, 'utf8'), Buffer.from([0]));
  }
  return Buffer.concat(chunks);
}

function binaryData(characterId, text) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(characterId, 0);
  header.writeUInt32LE(0, 2);
  return Buffer.concat([header, toLatin1Buffer(text)]);
}

fs.rmSync(tmp, { recursive: true, force: true });
fs.mkdirSync(tmp, { recursive: true });

const xml = [
  '<?xml version="1.0" encoding="ISO-8859-1" ?>',
  '<visualizationData type="test_furni">',
  '  <graphics>',
  '    <visualization size="64" layerCount="1" angle="45"/>',
  '  </graphics>',
  '</visualizationData>'
].join('\n');

const swf = new SwfFile({
  signature: 'FWS',
  version: 10,
  frameHeader: Buffer.from([0x00, 0x00, 0x18, 0x01, 0x00]),
  tags: [
    tag(76, symbolClass([[1, 'test_furni_test_furni_visualization'], [0, 'test_furni']])),
    tag(87, binaryData(1, xml)),
    tag(0, Buffer.alloc(0))
  ]
});
swf.write(input);

new GraphicTagCleaner({ logger: new Logger({ quiet: true }) }).cleanFile(input, output);
const cleaned = SwfFile.read(output).binaryData()[0];
const cleanedXml = fromLatin1Buffer(cleaned.data);

if (/<\/?graphics>/i.test(cleanedXml)) {
  console.error(cleanedXml);
  throw new Error('SWF cleaner test failed: graphics wrapper remained.');
}
if (!/<visualization\b/i.test(cleanedXml)) {
  console.error(cleanedXml);
  throw new Error('SWF cleaner test failed: visualization child was not preserved.');
}

console.log('Synthetic SWF cleaner test passed.');
