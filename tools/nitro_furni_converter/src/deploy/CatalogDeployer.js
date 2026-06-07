const { ConverterError } = require('../errors');
const { MysqlClient, DEFAULT_MYSQL, sqlNumber, sqlString } = require('./MysqlClient');

function toInt(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? Math.trunc(number) : fallback;
}

function pageInsertSql(pageId, caption, parentId, orderNum) {
  return `
INSERT INTO catalog_pages
  (id, parent_id, caption, icon_color, icon_image, visible, enabled, min_rank, club_only, order_num,
   page_layout, page_headline, page_teaser, page_special, page_text1, page_text2, page_text_details,
   page_text_teaser, vip_only, page_link_description, page_link_pagename)
VALUES
  (${sqlNumber(pageId)}, ${sqlNumber(parentId, -1)}, ${sqlString(caption)}, 1, 1, '1', '1', 1, '0', ${sqlNumber(orderNum, 999)},
   'default_3x3', '', '', '', 'Recently imported furniture.', '', 'Click an item for more information.', '',
   '0', '', '');`;
}

class CatalogDeployer {
  constructor({ logger, mysqlOptions = {} }) {
    this.logger = logger;
    this.mysql = new MysqlClient({
      mysqlPath: mysqlOptions.mysqlPath || DEFAULT_MYSQL,
      host: mysqlOptions.host || '127.0.0.1',
      user: mysqlOptions.user || 'root',
      password: mysqlOptions.password === undefined ? 'auto' : mysqlOptions.password,
      database: mysqlOptions.database || 'firewind'
    });
  }

  ensurePage({ caption = 'Recently Added', parentId = -1 } = {}) {
    const existing = this.mysql.queryOne(
      `SELECT id FROM catalog_pages WHERE caption=${sqlString(caption)} LIMIT 1;`
    );
    if (existing) {
      const pageId = toInt(existing[0]);
      this.mysql.run(
        `UPDATE catalog_pages SET visible='1', enabled='1', min_rank=1 WHERE id=${pageId};`
      );
      return pageId;
    }

    const maxOrder = toInt(this.mysql.queryScalar(
      `SELECT COALESCE(MAX(order_num), 0) FROM catalog_pages WHERE parent_id=${sqlNumber(parentId, -1)};`
    ), 0);
    const pageId = this.pageIdCandidate();
    this.mysql.run(pageInsertSql(pageId, caption, parentId, maxOrder + 1));
    return pageId;
  }

  pageIdCandidate() {
    let candidate = 190000000;
    for (let attempts = 0; attempts < 10000; attempts += 1) {
      const exists = toInt(this.mysql.queryScalar(
        `SELECT COUNT(*) FROM catalog_pages WHERE id=${candidate};`
      ));
      if (!exists) return candidate;
      candidate += 1;
    }
    throw new ConverterError('Could not find a free catalog_pages.id candidate.');
  }

  nextSpriteId() {
    return toInt(this.mysql.queryScalar('SELECT COALESCE(MAX(sprite_id), 0) + 1 FROM items_base;'), 1);
  }

  ensureItemBase(metadata) {
    const existing = this.mysql.queryOne(
      `SELECT id, sprite_id FROM items_base WHERE item_name=${sqlString(metadata.className)} ORDER BY id DESC LIMIT 1;`
    );
    if (existing) {
      const itemBaseId = toInt(existing[0]);
      const spriteId = metadata.spriteId || toInt(existing[1]) || this.nextSpriteId();
      this.mysql.run(`
UPDATE items_base SET
  public_name=${sqlString(metadata.publicName)},
  type=${sqlString(metadata.type)},
  width=${sqlNumber(metadata.width, 1)},
  length=${sqlNumber(metadata.length, 1)},
  stack_height=${sqlNumber(metadata.stackHeight, 1)},
  can_stack=${sqlNumber(metadata.canStack, 1)},
  can_sit=${sqlNumber(metadata.canSit, 0)},
  is_walkable=${sqlNumber(metadata.isWalkable, 0)},
  sprite_id=${sqlNumber(spriteId, 1)},
  interaction_type=${sqlString(metadata.interactionType)},
  interaction_modes_count=${sqlNumber(metadata.interactionModesCount, 1)},
  vending_ids=${sqlString(metadata.vendingIds)},
  allow_gift=${sqlNumber(metadata.allowGift, 1)}
WHERE id=${itemBaseId};`);
      return { itemBaseId, spriteId, existed: true };
    }

    const spriteId = metadata.spriteId || this.nextSpriteId();
    const inserted = this.mysql.queryScalar(`
INSERT INTO items_base
  (public_name, item_name, type, width, length, stack_height, can_stack, can_sit, is_walkable,
   sprite_id, allow_recycle, allow_trade, allow_marketplace_sell, allow_gift, allow_inventory_stack,
   allow_group_item, interaction_type, interaction_modes_count, vending_ids, is_arrow, foot_figure,
   height_adjustable, effectM, effectF)
VALUES
  (${sqlString(metadata.publicName)}, ${sqlString(metadata.className)}, ${sqlString(metadata.type)},
   ${sqlNumber(metadata.width, 1)}, ${sqlNumber(metadata.length, 1)}, ${sqlNumber(metadata.stackHeight, 1)},
   ${sqlNumber(metadata.canStack, 1)}, ${sqlNumber(metadata.canSit, 0)}, ${sqlNumber(metadata.isWalkable, 0)},
   ${sqlNumber(spriteId, 1)}, 1, 1, 1, ${sqlNumber(metadata.allowGift, 1)}, 1,
   0, ${sqlString(metadata.interactionType)}, ${sqlNumber(metadata.interactionModesCount, 1)},
   ${sqlString(metadata.vendingIds)}, 0, 0, '0', 0, 0);
SELECT LAST_INSERT_ID();`);
    const itemBaseId = toInt(inserted);
    if (!itemBaseId) throw new ConverterError(`Could not create items_base row for ${metadata.className}.`);
    return { itemBaseId, spriteId, existed: false };
  }

  catalogIdCandidate(itemBaseId) {
    let candidate = toInt(itemBaseId);
    for (let attempts = 0; attempts < 1000; attempts += 1) {
      const exists = toInt(this.mysql.queryScalar(
        `SELECT COUNT(*) FROM catalog_items WHERE id=${candidate};`
      ));
      if (!exists) return candidate;
      candidate += 1;
    }
    throw new ConverterError('Could not find a free catalog_items.id candidate.');
  }

  ensureCatalogItem(metadata, pageId, itemBaseId) {
    const existing = this.mysql.queryOne(`
SELECT id FROM catalog_items
WHERE catalog_name=${sqlString(metadata.className)}
   OR (page_id=${sqlNumber(pageId)} AND item_ids=${sqlString(itemBaseId)})
ORDER BY id DESC
LIMIT 1;`);
    if (existing) {
      const catalogItemId = toInt(existing[0]);
      this.mysql.run(`
UPDATE catalog_items SET
  page_id=${sqlNumber(pageId)},
  item_ids=${sqlString(itemBaseId)},
  catalog_name=${sqlString(metadata.className)},
  cost_credits=${sqlNumber(metadata.priceCredits, 3)},
  cost_pixels=${sqlNumber(metadata.pricePixels, 0)},
  cost_points=${sqlNumber(metadata.pricePoints, 0)},
  amount=${sqlNumber(metadata.amount, 1)},
  vip='0',
  achievement=0,
  song_id=0,
  extradata='',
  limited_sells=0,
  limited_stack=0,
  offer_active=1,
  allow_gift=${sqlNumber(metadata.allowGift, 1)}
WHERE id=${catalogItemId};`);
      return { catalogItemId, existed: true };
    }

    const catalogItemId = this.catalogIdCandidate(itemBaseId);
    this.mysql.run(`
INSERT INTO catalog_items
  (id, page_id, item_ids, catalog_name, cost_credits, cost_pixels, cost_points, amount, vip,
   achievement, song_id, extradata, limited_sells, limited_stack, offer_active, allow_gift)
VALUES
  (${sqlNumber(catalogItemId)}, ${sqlNumber(pageId)}, ${sqlString(itemBaseId)}, ${sqlString(metadata.className)},
   ${sqlNumber(metadata.priceCredits, 3)}, ${sqlNumber(metadata.pricePixels, 0)},
   ${sqlNumber(metadata.pricePoints, 0)}, ${sqlNumber(metadata.amount, 1)}, '0',
   0, 0, '', 0, 0, 1, ${sqlNumber(metadata.allowGift, 1)});`);
    return { catalogItemId, existed: false };
  }

  deploy(metadata, options = {}) {
    return this.deployMany([metadata], options)[0];
  }

  deployMany(metadataList, options = {}) {
    const pageId = this.ensurePage({
      caption: options.pageCaption || 'Recently Added',
      parentId: options.parentId === undefined ? -1 : options.parentId
    });
    const results = [];
    for (const metadata of metadataList) {
      const item = this.ensureItemBase(metadata);
      const catalog = this.ensureCatalogItem(metadata, pageId, item.itemBaseId);

      this.logger.step(`${item.existed ? 'Updated' : 'Created'} items_base #${item.itemBaseId} sprite #${item.spriteId} (${metadata.className})`);
      this.logger.step(`${catalog.existed ? 'Updated' : 'Created'} catalog item #${catalog.catalogItemId} on page #${pageId} (${metadata.className})`);

      results.push({
        pageId,
        itemBaseId: item.itemBaseId,
        spriteId: item.spriteId,
        catalogItemId: catalog.catalogItemId,
        metadata
      });
    }

    return results;
  }
}

module.exports = { CatalogDeployer };
