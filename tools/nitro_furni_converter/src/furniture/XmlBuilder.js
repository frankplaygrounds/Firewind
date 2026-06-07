const { sortedEntries } = require('./AssetPlanner');
const { xmlEscape, xmlHeader } = require('../utils');

function isPrimitive(value) {
  return ['string', 'number', 'boolean'].includes(typeof value);
}

function boolValue(value) {
  return value === true ? '1' : value === false ? '0' : value;
}

function attrs(object, keys = null, skip = []) {
  const source = keys
    ? keys.map((key) => [key, object ? object[key] : undefined])
    : Object.entries(object || {});
  const skipSet = new Set(skip);
  const parts = [];

  for (const [key, value] of source) {
    if (skipSet.has(key) || value === undefined || value === null || !isPrimitive(value)) continue;
    parts.push(`${key}="${xmlEscape(boolValue(value))}"`);
  }

  return parts.length ? ` ${parts.join(' ')}` : '';
}

function assetXml(plan) {
  const lines = [xmlHeader(), '<assets>'];
  for (const [name, asset] of plan.assets) {
    const attributes = [
      `name="${xmlEscape(name)}"`,
      `x="${xmlEscape(asset.x || 0)}"`,
      `y="${xmlEscape(asset.y || 0)}"`
    ];
    if (asset.source) attributes.push(`source="${xmlEscape(asset.source)}"`);
    if (asset.flipH) attributes.push('flipH="1"');
    if (asset.flipV) attributes.push('flipV="1"');
    lines.push(`  <asset ${attributes.join(' ')} />`);
  }
  lines.push('</assets>');
  return lines.join('\n');
}

function logicXml(descriptor) {
  const logic = descriptor.logic || {};
  const model = logic.model || {};
  const dimensions = model.dimensions || {};
  const directions = Array.isArray(model.directions) ? model.directions : [];
  const lines = [xmlHeader(), `<objectData type="${xmlEscape(descriptor.name)}">`, `  <model${attrs(model, null, ['dimensions', 'directions'])}>`];

  if (Object.keys(dimensions).length) {
    lines.push(`    <dimensions${attrs(dimensions)} />`);
  }

  if (directions.length) {
    lines.push('    <directions>');
    for (const direction of directions) {
      if (typeof direction === 'object') lines.push(`      <direction${attrs(direction)} />`);
      else lines.push(`      <direction id="${xmlEscape(direction)}" />`);
    }
    lines.push('    </directions>');
  }

  lines.push('  </model>');
  lines.push('</objectData>');
  return lines.join('\n');
}

function indexXml(descriptor) {
  return [
    xmlHeader(),
    `<object type="${xmlEscape(descriptor.name)}" visualization="${xmlEscape(descriptor.visualizationType || '')}" logic="${xmlEscape(descriptor.logicType || '')}"/>`
  ].join('\n');
}

function manifestXml(descriptor, plan) {
  const lines = [
    xmlHeader(),
    '<manifest>',
    `   <library name="${xmlEscape(descriptor.name)}" version="0.1">`,
    '      <assets>',
    '         <asset name="index" mimeType="text/xml"/>',
    `         <asset name="${xmlEscape(descriptor.name)}_visualization" mimeType="text/xml"/>`,
    `         <asset name="${xmlEscape(descriptor.name)}_assets" mimeType="text/xml"/>`,
    `         <asset name="${xmlEscape(descriptor.name)}_logic" mimeType="text/xml"/>`
  ];

  for (const frame of plan.frames) {
    lines.push(`         <asset name="${xmlEscape(frame.name)}" mimeType="image/png"/>`);
  }

  lines.push('      </assets>');
  lines.push('   </library>');
  lines.push('</manifest>');
  return lines.join('\n');
}

function layersXml(layers, indent = '      ') {
  const lines = [];
  if (!layers || !Object.keys(layers).length) return lines;
  lines.push(`${indent}<layers>`);
  for (const [id, layer] of sortedEntries(layers)) {
    lines.push(`${indent}  <layer id="${xmlEscape(id)}"${attrs(layer, null, ['id'])} />`);
  }
  lines.push(`${indent}</layers>`);
  return lines;
}

function directionsXml(directions, indent = '      ') {
  const lines = [];
  if (!directions || !Object.keys(directions).length) return lines;
  lines.push(`${indent}<directions>`);
  for (const [id, direction] of sortedEntries(directions)) {
    lines.push(`${indent}  <direction id="${xmlEscape(id)}"${attrs(direction, null, ['id', 'layers'])}>`);
    const dirLayers = direction && direction.layers ? direction.layers : {};
    for (const [layerId, layer] of sortedEntries(dirLayers)) {
      lines.push(`${indent}    <layer id="${xmlEscape(layerId)}"${attrs(layer, null, ['id'])} />`);
    }
    lines.push(`${indent}  </direction>`);
  }
  lines.push(`${indent}</directions>`);
  return lines;
}

function colorsXml(colors, indent = '      ') {
  const lines = [];
  if (!colors || !Object.keys(colors).length) return lines;
  lines.push(`${indent}<colors>`);
  for (const [id, color] of sortedEntries(colors)) {
    lines.push(`${indent}  <color id="${xmlEscape(id)}"${attrs(color, null, ['id', 'layers'])}>`);
    const colorLayers = color && color.layers ? color.layers : {};
    for (const [layerId, layer] of sortedEntries(colorLayers)) {
      lines.push(`${indent}    <colorLayer id="${xmlEscape(layerId)}"${attrs(layer, null, ['id'])} />`);
    }
    lines.push(`${indent}  </color>`);
  }
  lines.push(`${indent}</colors>`);
  return lines;
}

function animationsXml(animations, indent = '      ') {
  const lines = [];
  if (!animations || !Object.keys(animations).length) return lines;
  lines.push(`${indent}<animations>`);
  for (const [id, animation] of sortedEntries(animations)) {
    lines.push(`${indent}  <animation id="${xmlEscape(id)}"${attrs(animation, null, ['id', 'layers'])}>`);
    const layers = animation && animation.layers ? animation.layers : {};
    for (const [layerId, layer] of sortedEntries(layers)) {
      lines.push(`${indent}    <animationLayer id="${xmlEscape(layerId)}"${attrs(layer, null, ['id', 'frameSequences'])}>`);
      const sequences = layer && layer.frameSequences ? layer.frameSequences : {};
      for (const [sequenceId, sequence] of sortedEntries(sequences)) {
        lines.push(`${indent}      <frameSequence id="${xmlEscape(sequenceId)}"${attrs(sequence, null, ['id', 'frames'])}>`);
        const frames = sequence && sequence.frames ? sequence.frames : {};
        for (const [frameId, frame] of sortedEntries(frames)) {
          const frameAttrs = { ...(frame || {}) };
          if (frameAttrs.id === undefined) frameAttrs.id = frameId;
          lines.push(`${indent}        <frame${attrs(frameAttrs)} />`);
        }
        lines.push(`${indent}      </frameSequence>`);
      }
      lines.push(`${indent}    </animationLayer>`);
    }
    lines.push(`${indent}  </animation>`);
  }
  lines.push(`${indent}</animations>`);
  return lines;
}

function visualizationXml(descriptor, plan) {
  const lines = [xmlHeader(), `<visualizationData type="${xmlEscape(descriptor.name)}">`, '  <graphics>'];

  for (const visualization of plan.visualizations) {
    const baseAttrs = {
      size: visualization.size || 0,
      layerCount: visualization.layerCount || 0,
      angle: visualization.angle || 0
    };
    const extra = attrs(visualization, null, [
      'size',
      'layerCount',
      'angle',
      'layers',
      'directions',
      'colors',
      'animations',
      'generatedFromSize'
    ]);
    lines.push(`    <visualization${attrs(baseAttrs)}${extra}>`);
    lines.push(...layersXml(visualization.layers, '      '));
    lines.push(...directionsXml(visualization.directions, '      '));
    lines.push(...colorsXml(visualization.colors, '      '));
    lines.push(...animationsXml(visualization.animations, '      '));
    lines.push('    </visualization>');
  }

  lines.push('  </graphics>');
  lines.push('</visualizationData>');
  return lines.join('\n');
}

module.exports = {
  assetXml,
  logicXml,
  indexXml,
  manifestXml,
  visualizationXml
};
