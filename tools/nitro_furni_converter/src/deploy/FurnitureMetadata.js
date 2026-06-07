const { xmlEscape } = require('../utils');

function titleize(name) {
  return String(name || 'Furniture')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

function numberOrDefault(value, fallback) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function booleanNumber(value, fallback) {
  if (value === undefined || value === null) return fallback ? 1 : 0;
  if (value === true || value === '1' || value === 1) return 1;
  if (value === false || value === '0' || value === 0) return 0;
  return fallback ? 1 : 0;
}

function buildFurnitureMetadata(descriptor, options = {}) {
  const className = descriptor.name;
  const libraryName = options.libraryName || descriptor.name;
  const dimensions = descriptor.logic && descriptor.logic.model
    ? descriptor.logic.model.dimensions || {}
    : {};
  const displayName = options.catalogName || options.displayName || titleize(className);
  const description = options.description || `Recently added furniture: ${displayName}`;
  const width = Math.max(0, Math.round(numberOrDefault(options.width, dimensions.x || 1)));
  const length = Math.max(0, Math.round(numberOrDefault(options.length, dimensions.y || 1)));
  const stackHeight = numberOrDefault(options.stackHeight, dimensions.z || 1);
  const type = options.itemType === 'i' ? 'i' : 's';

  return {
    className,
    libraryName,
    publicName: displayName,
    displayName,
    description,
    type,
    width,
    length,
    stackHeight,
    canStack: booleanNumber(options.canStack, true),
    canSit: booleanNumber(options.canSit, false),
    isWalkable: booleanNumber(options.isWalkable, false),
    interactionType: options.interactionType || 'default',
    interactionModesCount: Math.max(1, Math.round(numberOrDefault(options.interactionModesCount, 1))),
    vendingIds: options.vendingIds || '0',
    category: options.category || (type === 'i' ? 'wall_decoration' : 'other'),
    furniline: options.furniline || 'recently_added',
    defaultDir: Math.round(numberOrDefault(options.defaultDir, 0)),
    revision: Math.round(numberOrDefault(options.revision, 45508)),
    offerId: options.offerId ? Math.round(Number(options.offerId)) : null,
    buyout: booleanNumber(options.buyout, true),
    bc: booleanNumber(options.bc, false),
    specialType: Math.round(numberOrDefault(options.specialType, type === 'i' ? 5 : 1)),
    rare: booleanNumber(options.rare, false),
    priceCredits: Math.max(0, Math.round(numberOrDefault(options.priceCredits, 3))),
    pricePixels: Math.max(0, Math.round(numberOrDefault(options.pricePixels, 0))),
    pricePoints: Math.max(0, Math.round(numberOrDefault(options.pricePoints, 0))),
    amount: Math.max(1, Math.round(numberOrDefault(options.amount, 1))),
    allowGift: booleanNumber(options.allowGift, true),
    spriteId: options.spriteId ? Math.round(Number(options.spriteId)) : null
  };
}

function sortedVariantIds(ids) {
  return [...ids].sort((a, b) => {
    const an = Number(a);
    const bn = Number(b);
    if (Number.isFinite(an) && Number.isFinite(bn)) return an - bn;
    return String(a).localeCompare(String(b));
  });
}

function detectVariantIds(descriptor) {
  const ids = new Set();
  for (const visualization of descriptor.visualizations || []) {
    for (const id of Object.keys(visualization.colors || {})) ids.add(String(id));
  }
  return sortedVariantIds(ids);
}

function buildFurnitureMetadataVariants(descriptor, options = {}) {
  const explicitVariant = /\*[0-9]+$/.test(descriptor.name);
  const variantIds = options.variantIds
    ? sortedVariantIds(options.variantIds.map(String))
    : options.disableAutoVariants || explicitVariant
      ? []
      : detectVariantIds(descriptor);

  if (variantIds.length <= 1) {
    return [buildFurnitureMetadata(descriptor, options)];
  }

  const baseName = descriptor.name;
  return variantIds.map((variantId, index) => {
    const variantOptions = {
      ...options,
      libraryName: baseName,
      catalogName: options.catalogName
        ? `${options.catalogName} ${variantId}`
        : undefined,
      displayName: options.displayName
        ? `${options.displayName} ${variantId}`
        : `${titleize(baseName)} ${variantId}`,
      spriteId: options.spriteId ? Math.round(Number(options.spriteId)) + index : null,
      offerId: options.offerId ? Math.round(Number(options.offerId)) + index : null
    };
    const descriptorVariant = { ...descriptor, name: `${baseName}*${variantId}` };
    const metadata = buildFurnitureMetadata(descriptorVariant, variantOptions);
    metadata.variantId = variantId;
    metadata.baseClassName = baseName;
    return metadata;
  });
}

function roomFurnitypeXml(metadata, spriteId) {
  const offerId = metadata.offerId || spriteId;
  return [
    `<furnitype id="${spriteId}" classname="${xmlEscape(metadata.className)}">`,
    `<revision>${metadata.revision}</revision>`,
    `<category>${xmlEscape(metadata.category)}</category>`,
    `<defaultdir>${metadata.defaultDir}</defaultdir>`,
    `<xdim>${metadata.width}</xdim>`,
    `<ydim>${metadata.length}</ydim>`,
    `<partcolors />`,
    `<title>${xmlEscape(metadata.displayName)}</title>`,
    `<name>${xmlEscape(metadata.displayName)}</name>`,
    `<description>${xmlEscape(metadata.description)}</description>`,
    `<adurl />`,
    `<offerid>${offerId}</offerid>`,
    `<buyout>${metadata.buyout}</buyout>`,
    `<rentofferid>-1</rentofferid>`,
    `<rentbuyout>0</rentbuyout>`,
    `<bc>${metadata.bc}</bc>`,
    `<excludeddynamic>0</excludeddynamic>`,
    `<customparams />`,
    `<specialtype>${metadata.specialType}</specialtype>`,
    `<canstandon>${metadata.isWalkable}</canstandon>`,
    `<cansiton>${metadata.canSit}</cansiton>`,
    `<canlayon>0</canlayon>`,
    `<furniline>${xmlEscape(metadata.furniline)}</furniline>`,
    `<environment />`,
    `<rare>${metadata.rare}</rare>`,
    `</furnitype>`
  ].join('\n');
}

function wallFurnitypeXml(metadata, spriteId) {
  const offerId = metadata.offerId || spriteId;
  return [
    `<furnitype id="${spriteId}" classname="${xmlEscape(metadata.className)}">`,
    `<revision>${metadata.revision}</revision>`,
    `<category>${xmlEscape(metadata.category)}</category>`,
    `<title>${xmlEscape(metadata.displayName)}</title>`,
    `<name>${xmlEscape(metadata.displayName)}</name>`,
    `<description>${xmlEscape(metadata.description)}</description>`,
    `<adurl />`,
    `<offerid>${offerId}</offerid>`,
    `<buyout>${metadata.buyout}</buyout>`,
    `<rentofferid>-1</rentofferid>`,
    `<rentbuyout>0</rentbuyout>`,
    `<bc>${metadata.bc}</bc>`,
    `<excludeddynamic>0</excludeddynamic>`,
    `<specialtype>${metadata.specialType}</specialtype>`,
    `<furniline>${xmlEscape(metadata.furniline)}</furniline>`,
    `<environment />`,
    `<rare>${metadata.rare}</rare>`,
    `</furnitype>`
  ].join('\n');
}

function productXml(metadata) {
  return [
    `<product code="${xmlEscape(metadata.className)}">`,
    `<name>${xmlEscape(metadata.displayName)}</name>`,
    `<description>${xmlEscape(metadata.description)}</description>`,
    `</product>`
  ].join('');
}

module.exports = {
  buildFurnitureMetadata,
  buildFurnitureMetadataVariants,
  detectVariantIds,
  titleize,
  roomFurnitypeXml,
  wallFurnitypeXml,
  productXml
};
