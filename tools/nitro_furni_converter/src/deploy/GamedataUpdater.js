const fs = require('fs');
const path = require('path');
const { ConverterError } = require('../errors');
const { ensureDir } = require('../utils');
const { productXml, roomFurnitypeXml, wallFurnitypeXml } = require('./FurnitureMetadata');

const DEFAULT_WEB_ROOT = '/Users/Iaad/Documents/GitHub/Firewind-Web';
const DEFAULT_FURNIDATA = path.join(
  DEFAULT_WEB_ROOT,
  'swf/gamedata/furnidata/85f660e06014da6f36394b3341f826337d649194.txt'
);
const DEFAULT_PRODUCTDATA = path.join(
  DEFAULT_WEB_ROOT,
  'swf/gamedata/productdata/d23cfe9124378fb2e0820b7b41c97714fea5112f.txt'
);
const DEFAULT_DCR_HOF_FURNI = path.join(DEFAULT_WEB_ROOT, 'swf/dcr/hof_furni');

function backupFile(file) {
  if (!fs.existsSync(file)) return null;
  const stamp = new Date().toISOString().replace(/[-:T.Z]/g, '').slice(0, 14);
  const backup = `${file}.bak-${stamp}`;
  fs.copyFileSync(file, backup);
  return backup;
}

function removeExistingFurnitype(xml, className) {
  const escaped = className.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const pattern = new RegExp(`<furnitype\\b[^>]*\\bclassname="${escaped}"[^>]*>[\\s\\S]*?<\\/furnitype>\\s*`, 'g');
  return xml.replace(pattern, '');
}

function insertBeforeClosing(xml, closingTag, insertion, file) {
  const index = xml.lastIndexOf(closingTag);
  if (index === -1) {
    throw new ConverterError(`Could not find ${closingTag} in ${file}.`);
  }
  const prefix = xml.slice(0, index).replace(/\s*$/, '\n');
  const suffix = xml.slice(index);
  return `${prefix}${insertion}\n${suffix}`;
}

function updateFurnidata(file, metadata, spriteId, { backup = true } = {}) {
  updateFurnidataMany(file, [{ metadata, spriteId }], { backup });
}

function updateProductdata(file, metadata, { backup = true } = {}) {
  updateProductdataMany(file, [metadata], { backup });
}

function updateFurnidataMany(file, entries, { backup = true } = {}) {
  if (!fs.existsSync(file)) throw new ConverterError(`furnidata file does not exist: ${file}`);
  if (backup) backupFile(file);
  let xml = fs.readFileSync(file, 'utf8');
  for (const entry of entries) xml = removeExistingFurnitype(xml, entry.metadata.className);

  const roomSnippets = entries
    .filter((entry) => entry.metadata.type !== 'i')
    .map((entry) => roomFurnitypeXml(entry.metadata, entry.spriteId));
  const wallSnippets = entries
    .filter((entry) => entry.metadata.type === 'i')
    .map((entry) => wallFurnitypeXml(entry.metadata, entry.spriteId));

  if (roomSnippets.length) {
    xml = insertBeforeClosing(xml, '</roomitemtypes>', roomSnippets.join('\n'), file);
  }
  if (wallSnippets.length) {
    xml = insertBeforeClosing(xml, '</wallitemtypes>', wallSnippets.join('\n'), file);
  }
  fs.writeFileSync(file, xml, 'utf8');
}

function updateProductdataMany(file, metadataList, { backup = true } = {}) {
  if (!fs.existsSync(file)) throw new ConverterError(`productdata file does not exist: ${file}`);
  if (backup) backupFile(file);
  let xml = fs.readFileSync(file, 'utf8');
  for (const metadata of metadataList) {
    const escaped = metadata.className.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    xml = xml.replace(
      new RegExp(`<product\\b[^>]*\\bcode="${escaped}"[^>]*>[\\s\\S]*?<\\/product>\\s*`, 'g'),
      ''
    );
  }
  const updated = insertBeforeClosing(xml, '</productdata>', metadataList.map(productXml).join('\n'), file);
  fs.writeFileSync(file, updated, 'utf8');
}

function copySwfToDcr(swfFile, dcrDir, className) {
  if (!fs.existsSync(swfFile)) throw new ConverterError(`SWF output does not exist: ${swfFile}`);
  ensureDir(dcrDir);
  const target = path.join(dcrDir, `${className}.swf`);
  fs.copyFileSync(swfFile, target);
  return target;
}

class GamedataUpdater {
  constructor({ logger }) {
    this.logger = logger;
  }

  update({ swfFile, metadata, spriteId, webRoot = DEFAULT_WEB_ROOT, furnidataFile, productdataFile, dcrDir, backup = true }) {
    return this.updateMany({
      swfFile,
      entries: [{ metadata, spriteId }],
      webRoot,
      furnidataFile,
      productdataFile,
      dcrDir,
      backup
    });
  }

  updateMany({ swfFile, entries, webRoot = DEFAULT_WEB_ROOT, furnidataFile, productdataFile, dcrDir, backup = true }) {
    if (!entries.length) throw new ConverterError('No furniture metadata entries supplied for gamedata update.');
    const resolvedFurnidata = furnidataFile || path.join(
      webRoot,
      'swf/gamedata/furnidata/85f660e06014da6f36394b3341f826337d649194.txt'
    );
    const resolvedProductdata = productdataFile || path.join(
      webRoot,
      'swf/gamedata/productdata/d23cfe9124378fb2e0820b7b41c97714fea5112f.txt'
    );
    const resolvedDcrDir = dcrDir || path.join(webRoot, 'swf/dcr/hof_furni');

    const copiedSwf = copySwfToDcr(swfFile, resolvedDcrDir, entries[0].metadata.libraryName || entries[0].metadata.className);
    updateFurnidataMany(resolvedFurnidata, entries, { backup });
    updateProductdataMany(resolvedProductdata, entries.map((entry) => entry.metadata), { backup });

    this.logger.step(`Copied SWF to ${copiedSwf}`);
    this.logger.step(`Updated furnidata ${resolvedFurnidata} (${entries.length} item entr${entries.length === 1 ? 'y' : 'ies'})`);
    this.logger.step(`Updated productdata ${resolvedProductdata} (${entries.length} product entr${entries.length === 1 ? 'y' : 'ies'})`);

    return {
      copiedSwf,
      furnidataFile: resolvedFurnidata,
      productdataFile: resolvedProductdata
    };
  }
}

module.exports = {
  GamedataUpdater,
  DEFAULT_WEB_ROOT,
  DEFAULT_FURNIDATA,
  DEFAULT_PRODUCTDATA,
  DEFAULT_DCR_HOF_FURNI,
  updateFurnidata,
  updateFurnidataMany,
  updateProductdata,
  updateProductdataMany,
  copySwfToDcr
};
