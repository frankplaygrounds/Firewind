const fs = require('fs');
const path = require('path');
const { XMLBuilder, XMLParser } = require('fast-xml-parser');
const {
  SwfFile,
  TAG_DEFINE_BITS_JPEG2,
  TAG_DEFINE_BITS_JPEG3,
  TAG_DEFINE_BITS_LOSSLESS,
  TAG_DEFINE_BITS_LOSSLESS2
} = require('./SwfFile');
const { cloneAssetClassesInSwf } = require('./AbcAssetClassCloner');
const { decodeBitmapTag, encodeBitmapTag, scaleHalfNearest } = require('./BitmapTag');
const { stripGraphicsWrapper } = require('./GraphicTagCleaner');
const { classifyBinaryXml } = require('../verify/Verifier');
const { ConverterError, UnsupportedStructureError } = require('../errors');
const {
  fromLatin1Buffer,
  replaceScaleToken,
  scaleRegistration,
  toLatin1Buffer,
  xmlHeader
} = require('../utils');

const BITMAP_TAG_CODES = [
  TAG_DEFINE_BITS_JPEG2,
  TAG_DEFINE_BITS_JPEG3,
  TAG_DEFINE_BITS_LOSSLESS,
  TAG_DEFINE_BITS_LOSSLESS2
];

function xmlParser() {
  return new XMLParser({
    ignoreAttributes: false,
    ignoreDeclaration: true,
    attributeNamePrefix: '@_',
    parseTagValue: false,
    parseAttributeValue: false,
    trimValues: true,
    preserveOrder: false
  });
}

function xmlBuilder() {
  return new XMLBuilder({
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
    format: true,
    suppressEmptyNode: true
  });
}

function asArray(value) {
  if (value === undefined || value === null) return [];
  return Array.isArray(value) ? value : [value];
}

function setMaybeSingle(container, key, values) {
  if (!values.length) delete container[key];
  else container[key] = values.length === 1 ? values[0] : values;
}

function parseXmlBuffer(binary, label) {
  try {
    return xmlParser().parse(fromLatin1Buffer(binary.data));
  } catch (error) {
    throw new UnsupportedStructureError(`Could not parse ${label} XML: ${error.message}`, { cause: error });
  }
}

function buildXmlBuffer(parsed) {
  return toLatin1Buffer(`${xmlHeader()}\n${xmlBuilder().build(parsed)}\n`);
}

function attribute(entry, name) {
  return entry ? entry[`@_${name}`] : undefined;
}

function setAttribute(entry, name, value) {
  entry[`@_${name}`] = String(value);
}

function hasTruthyAttribute(entry, name) {
  const value = attribute(entry, name);
  return value !== undefined && value !== null && String(value) !== '' && String(value) !== '0';
}

function cloneAssetEntry(source, targetName, sourceName = null) {
  const x = scaleRegistration(attribute(source, 'x'));
  const y = scaleRegistration(attribute(source, 'y'));
  if (x === null || y === null) {
    throw new UnsupportedStructureError(`Asset ${attribute(source, 'name')} has invalid x/y registration.`);
  }

  const entry = {};
  setAttribute(entry, 'name', targetName);
  setAttribute(entry, 'x', x);
  setAttribute(entry, 'y', y);
  if (sourceName) setAttribute(entry, 'source', sourceName);
  if (hasTruthyAttribute(source, 'flipH')) setAttribute(entry, 'flipH', attribute(source, 'flipH'));
  if (hasTruthyAttribute(source, 'flipV')) setAttribute(entry, 'flipV', attribute(source, 'flipV'));
  return entry;
}

function findLibrary(parsedManifest) {
  const libraries = asArray(parsedManifest && parsedManifest.manifest && parsedManifest.manifest.library);
  if (!libraries.length) {
    throw new UnsupportedStructureError('Manifest XML does not contain a library element.');
  }
  return libraries[0];
}

function libraryName(parsedManifest) {
  return attribute(findLibrary(parsedManifest), 'name') || '';
}

function manifestAssetEntries(parsedManifest) {
  const library = findLibrary(parsedManifest);
  if (!library.assets) library.assets = {};
  const entries = asArray(library.assets.asset);
  return { library, entries };
}

function addManifestAsset(parsedManifest, assetName) {
  const { library, entries } = manifestAssetEntries(parsedManifest);
  if (entries.some((entry) => attribute(entry, 'name') === assetName)) return false;
  entries.push({ '@_name': assetName, '@_mimeType': 'image/png' });
  setMaybeSingle(library.assets, 'asset', entries);
  return true;
}

function visualizationEntries(parsedVisualization) {
  const root = parsedVisualization && parsedVisualization.visualizationData;
  if (!root) throw new UnsupportedStructureError('Visualization XML does not contain visualizationData.');
  return { root, entries: asArray(root.visualization) };
}

function ensureSize32Visualization(parsedVisualization) {
  const { root, entries } = visualizationEntries(parsedVisualization);
  if (entries.some((entry) => Number(attribute(entry, 'size')) === 32)) return false;

  const source = entries.find((entry) => Number(attribute(entry, 'size')) === 64);
  if (!source) return false;
  const clone = JSON.parse(JSON.stringify(source));
  setAttribute(clone, 'size', 32);
  entries.unshift(clone);
  setMaybeSingle(root, 'visualization', entries);
  return true;
}

function symbolMaps(swf) {
  const byName = new Map();
  const byId = new Map();
  for (const symbol of swf.parseSymbols()) {
    byName.set(symbol.name, symbol);
    byId.set(symbol.characterId, symbol);
  }
  return { byName, byId };
}

function findSymbolForAsset(swf, maps, assetName, libName) {
  const candidates = [];
  if (libName) candidates.push(`${libName}_${assetName}`);
  candidates.push(assetName);

  for (const candidate of candidates) {
    const symbol = maps.byName.get(candidate);
    if (symbol) return symbol;
  }

  const suffix = `_${assetName}`;
  const matching = [];
  for (const symbol of maps.byName.values()) {
    if (symbol.name.endsWith(suffix)) matching.push(symbol);
  }
  if (matching.length === 1) return matching[0];
  if (matching.length > 1 && libName) {
    return matching.find((symbol) => symbol.name.startsWith(`${libName}_`)) || matching[0];
  }
  return matching[0] || null;
}

function targetSymbolName(sourceSymbolName, sourceAssetName, targetAssetName, libName) {
  if (sourceSymbolName.endsWith(sourceAssetName)) {
    return `${sourceSymbolName.slice(0, -sourceAssetName.length)}${targetAssetName}`;
  }
  return libName ? `${libName}_${targetAssetName}` : targetAssetName;
}

function tagForAsset(swf, maps, assetName, libName) {
  const symbol = findSymbolForAsset(swf, maps, assetName, libName);
  if (!symbol) return null;
  const tagRecord = swf.tagByCharacterId(symbol.characterId, BITMAP_TAG_CODES);
  if (!tagRecord) return { symbol, tagRecord: null };
  return { symbol, tagRecord };
}

function insertBitmapTag(swf, bitmapTag) {
  swf.insertTagBefore(
    (tag) => tag.code === 82 || tag.code === 76 || tag.code === 1 || tag.code === 0,
    bitmapTag
  );
}

function tempSibling(file) {
  return path.join(
    path.dirname(file),
    `.${path.basename(file)}.adapt-${process.pid}-${Date.now()}.tmp`
  );
}

class SwfZoomAdapter {
  constructor({ logger }) {
    this.logger = logger;
  }

  adaptSwf(swf) {
    const binaries = classifyBinaryXml(swf);
    if (!binaries.assets || !binaries.manifest || !binaries.visualization) {
      return {
        changed: false,
        skipped: true,
        reason: 'missing Habbo assets/manifest/visualization XML binaries'
      };
    }

    const parsedAssets = parseXmlBuffer(binaries.assets, 'assets');
    const parsedManifest = parseXmlBuffer(binaries.manifest, 'manifest');
    const parsedVisualization = parseXmlBuffer(binaries.visualization, 'visualization');
    if (!parsedAssets.assets) {
      throw new UnsupportedStructureError('Assets XML does not contain an assets root.');
    }

    const libName = libraryName(parsedManifest);
    const maps = symbolMaps(swf);
    const assetEntries = asArray(parsedAssets.assets.asset);
    const assetByName = new Map();
    for (const entry of assetEntries) {
      const name = attribute(entry, 'name');
      if (name) assetByName.set(name, entry);
    }

    const generatedDirect = [];
    const generatedAliases = [];
    const abcMappings = [];
    const warnings = [];
    let assetXmlChanged = false;
    let manifestXmlChanged = false;
    let visualizationXmlChanged = false;

    for (const sourceEntry of assetEntries) {
      const sourceName = attribute(sourceEntry, 'name');
      const sourceReference = attribute(sourceEntry, 'source');
      if (!sourceName || sourceReference) continue;

      const targetName = replaceScaleToken(sourceName, 64, 32);
      if (!targetName) continue;

      const targetEntry = assetByName.get(targetName);
      const targetTag = tagForAsset(swf, maps, targetName, libName);
      const sourceTag = tagForAsset(swf, maps, sourceName, libName);
      if (targetEntry && targetTag && targetTag.tagRecord) {
        if (sourceTag && sourceTag.tagRecord) {
          abcMappings.push({
            sourceClassName: sourceTag.symbol.name,
            targetClassName: targetTag.symbol.name,
            existing: true
          });
        }
        continue;
      }
      if (targetTag && !targetTag.tagRecord) {
        warnings.push(`Skipped ${targetName}: an exported symbol already exists but is not a supported bitmap tag.`);
        continue;
      }

      if (!sourceTag || !sourceTag.tagRecord) {
        warnings.push(`Skipped ${sourceName}: no supported bitmap symbol was found.`);
        continue;
      }

      try {
        const entryToAdd = targetEntry ? null : cloneAssetEntry(sourceEntry, targetName);
        const bitmap = decodeBitmapTag(sourceTag.tagRecord.tag);
        const scaled = scaleHalfNearest(bitmap);
        const characterId = swf.nextCharacterId();
        const encoded = encodeBitmapTag(characterId, scaled, sourceTag.tagRecord.tag.code);
        const symbolName = targetSymbolName(sourceTag.symbol.name, sourceName, targetName, libName);
        if (maps.byName.has(symbolName)) {
          throw new UnsupportedStructureError(`symbol ${symbolName} already exists.`);
        }
        insertBitmapTag(swf, encoded);
        swf.addSymbol(characterId, symbolName);
        maps.byName.set(symbolName, { characterId, name: symbolName });
        maps.byId.set(characterId, { characterId, name: symbolName });
        abcMappings.push({
          sourceClassName: sourceTag.symbol.name,
          targetClassName: symbolName,
          existing: false
        });

        if (entryToAdd) {
          assetEntries.push(entryToAdd);
          assetByName.set(targetName, entryToAdd);
          assetXmlChanged = true;
        }
        if (addManifestAsset(parsedManifest, targetName)) manifestXmlChanged = true;
        generatedDirect.push({
          sourceAsset: sourceName,
          generatedAsset: targetName,
          sourceClassName: sourceTag.symbol.name,
          targetClassName: symbolName,
          sourceTagCode: sourceTag.tagRecord.tag.code,
          characterId,
          symbolName
        });
      } catch (error) {
        warnings.push(`Skipped ${sourceName}: ${error.message}`);
      }
    }

    for (const sourceEntry of assetEntries.slice()) {
      const sourceName = attribute(sourceEntry, 'name');
      const sourceReference = attribute(sourceEntry, 'source');
      if (!sourceName || !sourceReference) continue;

      const targetName = replaceScaleToken(sourceName, 64, 32);
      const targetReference = replaceScaleToken(sourceReference, 64, 32);
      if (!targetName || !targetReference || assetByName.has(targetName)) continue;
      if (!assetByName.has(targetReference)) {
        warnings.push(`Skipped alias ${targetName}: scaled source ${targetReference} is unavailable.`);
        continue;
      }

      try {
        const entry = cloneAssetEntry(sourceEntry, targetName, targetReference);
        assetEntries.push(entry);
        assetByName.set(targetName, entry);
        assetXmlChanged = true;
        generatedAliases.push({
          sourceAsset: sourceName,
          generatedAsset: targetName,
          aliasSource: targetReference
        });
      } catch (error) {
        warnings.push(`Skipped alias ${targetName}: ${error.message}`);
      }
    }

    if (generatedDirect.length || generatedAliases.length) {
      if (ensureSize32Visualization(parsedVisualization)) visualizationXmlChanged = true;
    }

    let abcCloned = 0;
    if (abcMappings.length) {
      const abcResult = cloneAssetClassesInSwf(swf, abcMappings);
      abcCloned = abcResult.cloned;
      if (abcCloned) this.logger.verbose(`Cloned/repaired ${abcCloned} ABC asset class initializer(s) for zoom bitmaps.`);
    }

    if (!generatedDirect.length && !generatedAliases.length && !abcCloned) {
      return {
        changed: false,
        skipped: false,
        generatedDirect,
        generatedAliases,
        abcCloned,
        warnings
      };
    }

    setMaybeSingle(parsedAssets.assets, 'asset', assetEntries);
    if (assetXmlChanged) swf.setBinaryData(binaries.assets, buildXmlBuffer(parsedAssets));
    if (manifestXmlChanged) swf.setBinaryData(binaries.manifest, buildXmlBuffer(parsedManifest));
    if (visualizationXmlChanged) swf.setBinaryData(binaries.visualization, buildXmlBuffer(parsedVisualization));

    return {
      changed: true,
      skipped: false,
      generatedDirect,
      generatedAliases,
      abcCloned,
      warnings
    };
  }

  cleanVisualizationIfChanged(swf, result, options) {
    if (!result.changed || options.skipCleanup) return 0;
    const binaries = classifyBinaryXml(swf);
    if (!binaries.visualization) return 0;
    const cleaned = stripGraphicsWrapper(fromLatin1Buffer(binaries.visualization.data));
    if (!cleaned.changed) return 0;
    swf.setBinaryData(binaries.visualization, toLatin1Buffer(cleaned.xml));
    return 1;
  }

  adaptFile(inputFile, options = {}) {
    const file = path.resolve(inputFile);
    if (!fs.existsSync(file)) throw new ConverterError(`SWF file does not exist: ${file}`);
    if (path.extname(file).toLowerCase() !== '.swf') {
      throw new ConverterError(`Input file must have a .swf extension: ${file}`);
    }

    const swf = SwfFile.read(file);
    const result = this.adaptSwf(swf);
    const cleaned = this.cleanVisualizationIfChanged(swf, result, options);
    if (cleaned) result.cleanedVisualizations = cleaned;

    for (const warning of result.warnings || []) this.logger.warn(warning);
    if (result.skipped) {
      this.logger.verbose(`Skipped ${path.basename(file)}: ${result.reason}`);
      return { ...result, file };
    }
    if (!result.changed) {
      this.logger.verbose(`Skipped ${path.basename(file)}: zoomed-out assets are already bundled.`);
      return { ...result, file };
    }

    if (options.dryRun) {
      this.logger.info(`Would adapt ${file}: ${result.generatedDirect.length} bitmap(s), ${result.generatedAliases.length} alias(es), ${result.abcCloned || 0} ABC repair(s).`);
      return { ...result, file, dryRun: true };
    }

    const tmp = tempSibling(file);
    try {
      swf.write(tmp);
      if (!options.skipVerify) SwfFile.read(tmp);
      fs.renameSync(tmp, file);
    } catch (error) {
      fs.rmSync(tmp, { force: true });
      throw error;
    }

    this.logger.info(`Adapted ${file}: ${result.generatedDirect.length} bitmap(s), ${result.generatedAliases.length} alias(es), ${result.abcCloned || 0} ABC repair(s).`);
    return { ...result, file, outputFile: file };
  }
}

module.exports = { SwfZoomAdapter };
