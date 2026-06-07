const { SwfFile } = require('../swf/SwfFile');
const { ConverterError } = require('../errors');
const { fromLatin1Buffer } = require('../utils');

function classifyBinaryXml(swf) {
  const names = new Map(swf.parseSymbols().map((symbol) => [symbol.characterId, symbol.name]));
  const out = {};

  for (const binary of swf.binaryData()) {
    const name = names.get(binary.characterId) || '';
    const text = fromLatin1Buffer(binary.data.subarray(0, 500));
    if (/<manifest\b/i.test(text) || /_manifest$/i.test(name)) out.manifest = binary;
    else if (/<assets\b/i.test(text) || /_assets$/i.test(name)) out.assets = binary;
    else if (/<visualizationData\b/i.test(text) || /_visualization$/i.test(name)) out.visualization = binary;
    else if (/<objectData\b/i.test(text) || /_logic$/i.test(name)) out.logic = binary;
    else if (/<object\b/i.test(text) || /_index$/i.test(name)) out.index = binary;
  }

  return out;
}

function requireIncludes(text, value, context) {
  if (!text.includes(value)) {
    throw new ConverterError(`Verification failed: ${context} does not contain ${value}.`);
  }
}

function assertNoDuplicateAssetNames(assetsXml) {
  const counts = new Map();
  for (const match of assetsXml.matchAll(/<asset\b[^>]*\bname="([^"]+)"/g)) {
    const name = match[1];
    counts.set(name, (counts.get(name) || 0) + 1);
  }
  const duplicates = [...counts.entries()].filter(([, count]) => count > 1).map(([name]) => name);
  if (duplicates.length) {
    throw new ConverterError(`Verification failed: duplicate asset XML entries: ${duplicates.join(', ')}.`);
  }
}

class Verifier {
  constructor({ logger }) {
    this.logger = logger;
  }

  verify({ swfFile, plan, expectGraphicsClean = true }) {
    const swf = SwfFile.read(swfFile);
    const binaries = classifyBinaryXml(swf);
    const missing = ['manifest', 'assets', 'visualization', 'logic', 'index'].filter((key) => !binaries[key]);
    if (missing.length) {
      throw new ConverterError(`Verification failed: missing SWF binary XML assets: ${missing.join(', ')}.`);
    }

    const assetsXml = fromLatin1Buffer(binaries.assets.data);
    const manifestXml = fromLatin1Buffer(binaries.manifest.data);
    const visualizationXml = fromLatin1Buffer(binaries.visualization.data);

    assertNoDuplicateAssetNames(assetsXml);
    if (expectGraphicsClean && /<\/?graphics>/i.test(visualizationXml)) {
      throw new ConverterError('Verification failed: visualization XML still contains a graphics wrapper.');
    }

    for (const injection of [...plan.generatedDirect, ...plan.generatedAliases]) {
      requireIncludes(assetsXml, `name="${injection.generatedAsset}"`, 'assets XML');
    }
    for (const injection of plan.generatedDirect) {
      requireIncludes(manifestXml, `name="${injection.generatedAsset}"`, 'manifest XML');
    }

    this.logger.verbose(`Verified SWF structure: ${JSON.stringify(swf.tagCounts())}`);
    return {
      binaryAssets: Object.keys(binaries).length,
      generatedDirect: plan.generatedDirect.length,
      generatedAliases: plan.generatedAliases.length
    };
  }
}

module.exports = {
  Verifier,
  classifyBinaryXml
};
