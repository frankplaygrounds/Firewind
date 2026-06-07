const { XMLBuilder, XMLParser } = require('fast-xml-parser');
const { SwfFile } = require('./SwfFile');
const { fromLatin1Buffer, toLatin1Buffer, xmlHeader } = require('../utils');

function symbolNameById(swf) {
  const out = new Map();
  for (const symbol of swf.parseSymbols()) out.set(symbol.characterId, symbol.name);
  return out;
}

function isVisualizationBinary(binary, name) {
  const sample = fromLatin1Buffer(binary.data.subarray(0, 400));
  return /<visualizationData\b/i.test(sample) || /_visualization$/i.test(name || '');
}

function mergeGraphicsIntoVisualization(parsed) {
  const root = parsed && parsed.visualizationData;
  if (!root || root.graphics === undefined) return false;
  const graphics = root.graphics || {};
  delete root.graphics;

  for (const [key, value] of Object.entries(graphics)) {
    if (key.startsWith('@_')) continue;
    if (root[key] === undefined) {
      root[key] = value;
    } else {
      const left = Array.isArray(root[key]) ? root[key] : [root[key]];
      const right = Array.isArray(value) ? value : [value];
      root[key] = [...left, ...right];
    }
  }
  return true;
}

function stripGraphicsWrapper(xmlText) {
  const parser = new XMLParser({
    ignoreAttributes: false,
    ignoreDeclaration: true,
    attributeNamePrefix: '@_',
    parseTagValue: false,
    parseAttributeValue: false,
    trimValues: true,
    preserveOrder: false
  });
  const builder = new XMLBuilder({
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
    format: true,
    suppressEmptyNode: true
  });

  try {
    const parsed = parser.parse(xmlText);
    if (!mergeGraphicsIntoVisualization(parsed)) return { changed: false, xml: xmlText };
    return {
      changed: true,
      xml: `${xmlHeader()}\n${builder.build(parsed)}\n`
    };
  } catch {
    const cleaned = xmlText
      .replace(/<graphics>\s*/i, '')
      .replace(/\s*<\/graphics>/i, '');
    return {
      changed: cleaned !== xmlText,
      xml: cleaned
    };
  }
}

class GraphicTagCleaner {
  constructor({ logger }) {
    this.logger = logger;
  }

  cleanFile(inputFile, outputFile = inputFile) {
    const swf = SwfFile.read(inputFile);
    const names = symbolNameById(swf);
    let cleaned = 0;

    for (const binary of swf.binaryData()) {
      const name = names.get(binary.characterId) || '';
      if (!isVisualizationBinary(binary, name)) continue;
      const xml = fromLatin1Buffer(binary.data);
      const result = stripGraphicsWrapper(xml);
      if (!result.changed) continue;
      swf.setBinaryData(binary, toLatin1Buffer(result.xml));
      cleaned += 1;
    }

    swf.write(outputFile);
    this.logger.verbose(cleaned
      ? `Removed graphics wrapper from ${cleaned} visualization binary asset(s).`
      : 'No visualization graphics wrapper was present.');

    return { cleaned };
  }
}

module.exports = {
  GraphicTagCleaner,
  stripGraphicsWrapper
};
