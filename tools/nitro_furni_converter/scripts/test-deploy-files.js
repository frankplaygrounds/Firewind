const fs = require('fs');
const path = require('path');
const { buildFurnitureMetadataVariants } = require('../src/deploy/FurnitureMetadata');
const { GamedataUpdater } = require('../src/deploy/GamedataUpdater');
const { Logger } = require('../src/logger');

const root = path.resolve(__dirname, '..');
const tmp = path.join(root, '.tmp', 'deploy-files-test');
const webRoot = path.join(tmp, 'Firewind-Web');
const furnidata = path.join(webRoot, 'swf/gamedata/furnidata/test.txt');
const productdata = path.join(webRoot, 'swf/gamedata/productdata/test.txt');
const dcrDir = path.join(webRoot, 'swf/dcr/hof_furni');
const swf = path.join(tmp, 'test_furni.swf');

fs.rmSync(tmp, { recursive: true, force: true });
fs.mkdirSync(path.dirname(furnidata), { recursive: true });
fs.mkdirSync(path.dirname(productdata), { recursive: true });
fs.writeFileSync(swf, Buffer.from('FWS-test'));
fs.writeFileSync(furnidata, [
  "<?xml version='1.0' encoding='UTF-8'?>",
  '<furnidata>',
  '<roomitemtypes>',
  '</roomitemtypes>',
  '<wallitemtypes>',
  '</wallitemtypes>',
  '</furnidata>'
].join('\n'));
fs.writeFileSync(productdata, "<?xml version='1.0' encoding='utf-8'?>\n<productdata></productdata>");

const metadataList = buildFurnitureMetadataVariants({
  name: 'test_furni',
  logic: { model: { dimensions: { x: 2, y: 1, z: 0.5 } } },
  visualizations: [
    { size: 64, colors: { 1: { layers: {} }, 2: { layers: {} }, 3: { layers: {} } } }
  ]
}, {
  displayName: 'Test Furni',
  description: 'A deployment fixture',
  priceCredits: 7
});

new GamedataUpdater({ logger: new Logger({ quiet: true }) }).updateMany({
  swfFile: swf,
  entries: metadataList.map((metadata, index) => ({ metadata, spriteId: 12345 + index })),
  webRoot,
  furnidataFile: furnidata,
  productdataFile: productdata,
  dcrDir,
  backup: false
});

const furnidataXml = fs.readFileSync(furnidata, 'utf8');
const productdataXml = fs.readFileSync(productdata, 'utf8');
const copied = path.join(dcrDir, 'test_furni.swf');

if (!fs.existsSync(copied)) throw new Error('DCR copy was not written.');
if (!/classname="test_furni\*1"/.test(furnidataXml)) throw new Error('furnidata variant entry missing.');
if (!/classname="test_furni\*3"/.test(furnidataXml)) throw new Error('furnidata final variant entry missing.');
if (!/<xdim>2<\/xdim>/.test(furnidataXml)) throw new Error('furnidata dimensions missing.');
if (!/code="test_furni\*2"/.test(productdataXml)) throw new Error('productdata variant entry missing.');
if ((furnidataXml.match(/classname="test_furni\*/g) || []).length !== 3) {
  throw new Error('furnidata variants were not written exactly once.');
}

console.log('Gamedata deployment file test passed.');
