const { replaceScaleToken, scaleRegistration } = require('../utils');

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}

function sortedEntries(object) {
  return Object.entries(object || {}).sort(([a], [b]) => {
    const an = Number(a);
    const bn = Number(b);
    if (Number.isFinite(an) && Number.isFinite(bn)) return an - bn;
    return a.localeCompare(b);
  });
}

function scaleSort(name) {
  if (String(name).includes('_32_')) return 0;
  if (String(name).includes('_64_')) return 1;
  return 2;
}

function assetSort([a], [b]) {
  const scaleA = scaleSort(a);
  const scaleB = scaleSort(b);
  return scaleA === scaleB ? a.localeCompare(b) : scaleA - scaleB;
}

function frameAssetName(fullFrameName, furnitureName) {
  let assetName = String(fullFrameName).replace(/\.png$/i, '');
  const prefix = `${furnitureName}_`;
  if (assetName.startsWith(prefix)) assetName = assetName.slice(prefix.length);
  return assetName;
}

function buildFrameMap(descriptor) {
  const frames = descriptor.spritesheet && descriptor.spritesheet.frames
    ? descriptor.spritesheet.frames
    : {};
  const map = new Map();
  for (const [fullName, frameData] of Object.entries(frames)) {
    const assetName = frameAssetName(fullName, descriptor.name);
    if (!map.has(assetName)) {
      map.set(assetName, { assetName, fullName, frameData });
    }
  }
  return map;
}

function normalizedVisualizations(descriptor, warnings) {
  const visualizations = (descriptor.visualizations || []).map((visualization) => cloneJson(visualization));
  const has32 = visualizations.some((visualization) => Number(visualization.size) === 32);
  const visual64 = visualizations.find((visualization) => Number(visualization.size) === 64);

  if (!has32 && visual64) {
    const clone = cloneJson(visual64);
    clone.size = 32;
    clone.generatedFromSize = 64;
    visualizations.push(clone);
  } else if (!has32) {
    warnings.push('No size=64 visualization was available to clone into size=32.');
  }

  return visualizations.sort((a, b) => {
    const sizeA = Number(a.size || 0);
    const sizeB = Number(b.size || 0);
    const orderA = sizeA === 32 ? 0 : sizeA === 64 ? 1 : 2;
    const orderB = sizeB === 32 ? 0 : sizeB === 64 ? 1 : 2;
    return orderA === orderB ? sizeA - sizeB : orderA - orderB;
  });
}

class AssetPlanner {
  plan(descriptor) {
    const warnings = [];
    const frameMap = buildFrameMap(descriptor);
    const assets = new Map(
      Object.entries(descriptor.assets || {}).map(([name, asset]) => [name, cloneJson(asset)])
    );
    const framesToExport = new Map();
    const generatedDirect = [];
    const generatedAliases = [];

    for (const [assetName, frame] of frameMap.entries()) {
      framesToExport.set(assetName, {
        name: assetName,
        sourceFrameName: frame.fullName,
        sourceFrameData: frame.frameData,
        scale: 1,
        generated: false
      });
    }

    for (const [sourceName, sourceAsset] of Array.from(assets.entries())) {
      const targetName = replaceScaleToken(sourceName, 64, 32);
      if (!targetName || assets.has(targetName)) continue;

      const x = scaleRegistration(sourceAsset.x);
      const y = scaleRegistration(sourceAsset.y);
      if (x === null || y === null) {
        warnings.push(`Skipped ${sourceName}: missing or invalid x/y registration point.`);
        continue;
      }

      if (sourceAsset.source) {
        const targetSource = replaceScaleToken(sourceAsset.source, 64, 32);
        if (!targetSource) {
          warnings.push(`Skipped alias ${sourceName}: source ${sourceAsset.source} has no 64-size token.`);
          continue;
        }
        const alias = {
          sourceAsset: sourceName,
          generatedAsset: targetName,
          aliasSource: targetSource,
          x,
          y,
          flipH: Boolean(sourceAsset.flipH),
          flipV: Boolean(sourceAsset.flipV)
        };
        generatedAliases.push(alias);
        assets.set(targetName, {
          source: targetSource,
          x,
          y,
          ...(alias.flipH ? { flipH: true } : {}),
          ...(alias.flipV ? { flipV: true } : {})
        });
        continue;
      }

      const sourceFrame = frameMap.get(sourceName);
      if (!sourceFrame) {
        warnings.push(`Skipped ${sourceName}: no spritesheet frame was found for the direct bitmap asset.`);
        continue;
      }

      generatedDirect.push({
        sourceAsset: sourceName,
        generatedAsset: targetName,
        sourceFrameName: sourceFrame.fullName,
        x,
        y
      });
      assets.set(targetName, { x, y });
      if (!framesToExport.has(targetName)) {
        framesToExport.set(targetName, {
          name: targetName,
          sourceFrameName: sourceFrame.fullName,
          sourceFrameData: sourceFrame.frameData,
          scale: 0.5,
          generated: true
        });
      }
    }

    const validAliasSource = new Set(assets.keys());
    for (let index = generatedAliases.length - 1; index >= 0; index -= 1) {
      const alias = generatedAliases[index];
      if (validAliasSource.has(alias.aliasSource)) continue;
      generatedAliases.splice(index, 1);
      assets.delete(alias.generatedAsset);
      warnings.push(`Skipped alias ${alias.generatedAsset}: scaled source ${alias.aliasSource} was not generated or present.`);
    }

    return {
      furnitureName: descriptor.name,
      assets: Array.from(assets.entries()).sort(assetSort),
      frames: Array.from(framesToExport.values()).sort((a, b) => a.name.localeCompare(b.name)),
      frameMap,
      visualizations: normalizedVisualizations(descriptor, warnings),
      generatedDirect,
      generatedAliases,
      warnings,
      hasGeneratedZoom: generatedDirect.length > 0 || generatedAliases.length > 0
    };
  }
}

module.exports = {
  AssetPlanner,
  buildFrameMap,
  sortedEntries
};
