-- Merge the Arcturus catalog dump staged in firewind_arcturus_import into Firewind.
-- This keeps existing Firewind rows with non-conflicting ids while replacing rows
-- whose ids are supplied by the Arcturus catalog.

ALTER TABLE firewind.items_base
  MODIFY `type` varchar(3) NOT NULL DEFAULT 's',
  MODIFY `can_stack` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `can_sit` tinyint(1) NOT NULL DEFAULT '0',
  MODIFY `is_walkable` tinyint(1) NOT NULL DEFAULT '0',
  MODIFY `allow_recycle` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `allow_trade` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `allow_marketplace_sell` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `allow_gift` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `allow_inventory_stack` tinyint(1) NOT NULL DEFAULT '1',
  MODIFY `allow_group_item` tinyint(1) NOT NULL DEFAULT '0',
  MODIFY `interaction_type` varchar(500) NOT NULL DEFAULT 'default',
  MODIFY `vending_ids` varchar(255) NOT NULL DEFAULT '0',
  MODIFY `is_arrow` tinyint(1) NOT NULL DEFAULT '0',
  MODIFY `foot_figure` tinyint(1) NOT NULL DEFAULT '0';

ALTER TABLE firewind.catalog_items
  MODIFY `item_ids` varchar(666) NOT NULL;

SET @add_catalog_extradata_sql := IF(
  (SELECT COUNT(*)
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = 'firewind'
     AND TABLE_NAME = 'catalog_items'
     AND COLUMN_NAME = 'extradata') = 0,
  'ALTER TABLE firewind.catalog_items ADD COLUMN `extradata` varchar(500) NOT NULL DEFAULT '''' AFTER `song_id`',
  'SELECT 1'
);
PREPARE add_catalog_extradata_stmt FROM @add_catalog_extradata_sql;
EXECUTE add_catalog_extradata_stmt;
DEALLOCATE PREPARE add_catalog_extradata_stmt;

ALTER TABLE firewind.catalog_items
  MODIFY `extradata` varchar(500) NOT NULL DEFAULT '';

ALTER TABLE firewind.catalog_pages
  MODIFY `caption` varchar(128) NOT NULL;

REPLACE INTO firewind.items_base (
  `id`,
  `public_name`,
  `item_name`,
  `type`,
  `width`,
  `length`,
  `stack_height`,
  `can_stack`,
  `can_sit`,
  `is_walkable`,
  `sprite_id`,
  `allow_recycle`,
  `allow_trade`,
  `allow_marketplace_sell`,
  `allow_gift`,
  `allow_inventory_stack`,
  `allow_group_item`,
  `interaction_type`,
  `interaction_modes_count`,
  `vending_ids`,
  `is_arrow`,
  `foot_figure`,
  `height_adjustable`,
  `effectM`,
  `effectF`
)
SELECT
  `id`,
  `public_name`,
  `item_name`,
  CASE
    WHEN LOWER(`type`) IN ('s', 'i', 'e', 'h', 'v', 'r') THEN LOWER(`type`)
    WHEN LOWER(`type`) = 'b' THEN 'i'
    ELSE 's'
  END AS `type`,
  `width`,
  `length`,
  `stack_height`,
  `allow_stack`,
  `allow_sit`,
  `allow_walk`,
  `sprite_id`,
  `allow_recycle`,
  `allow_trade`,
  `allow_marketplace_sell`,
  `allow_gift`,
  `allow_inventory_stack`,
  IF(
    `interaction_type` IN ('guild_furni', 'guild_gate', 'guild_item', 'gld_item', 'gld_gate')
      OR `item_name` LIKE 'gld_%',
    1,
    0
  ) AS `allow_group_item`,
  IF(`interaction_type` = '', 'default', `interaction_type`) AS `interaction_type`,
  `interaction_modes_count`,
  `vending_ids`,
  IF(`interaction_type` = 'arrowplate' OR `item_name` LIKE 'wf_arrow%', 1, 0) AS `is_arrow`,
  IF(`interaction_type` = 'clothing' OR `clothing_on_walk` <> '', 1, 0) AS `foot_figure`,
  `multiheight`,
  `effect_id_male`,
  `effect_id_female`
FROM firewind_arcturus_import.items_base;

UPDATE firewind.items_base
SET `can_stack` = IF(`can_stack` = 2, 1, 0),
    `can_sit` = IF(`can_sit` = 2, 1, 0),
    `is_walkable` = IF(`is_walkable` = 2, 1, 0)
WHERE `can_stack` NOT IN (0, 1)
   OR `can_sit` NOT IN (0, 1)
   OR `is_walkable` NOT IN (0, 1);

-- Firewind's pet purchase flow expects a normalized `pet` interaction type
-- and derives the actual pet id from item names like `a0 pet0`.
UPDATE firewind.items_base
SET `interaction_type` = 'pet'
WHERE `item_name` REGEXP '^a0 pet[0-9]+$'
  AND CAST(SUBSTRING(`item_name`, 7) AS UNSIGNED) BETWEEN 0 AND 22
  AND (`interaction_type` = 'default' OR `interaction_type` REGEXP '^pet[0-9]+$');

REPLACE INTO firewind.catalog_pages (
  `id`,
  `parent_id`,
  `caption`,
  `icon_color`,
  `icon_image`,
  `visible`,
  `enabled`,
  `min_rank`,
  `club_only`,
  `order_num`,
  `page_layout`,
  `page_headline`,
  `page_teaser`,
  `page_special`,
  `page_text1`,
  `page_text2`,
  `page_text_details`,
  `page_text_teaser`,
  `vip_only`,
  `page_link_description`,
  `page_link_pagename`
)
SELECT
  `id`,
  `parent_id`,
  `caption`,
  `icon_color`,
  `icon_image`,
  `visible`,
  `enabled`,
  `min_rank`,
  `club_only`,
  `order_num`,
  CASE `page_layout`
    WHEN 'default_3x3' THEN 'default_3x3'
    WHEN 'frontpage' THEN 'frontpage'
    WHEN 'spaces' THEN 'spaces'
    WHEN 'recycler' THEN 'recycler'
    WHEN 'recycler_info' THEN 'recycler_info'
    WHEN 'recycler_prizes' THEN 'recycler_prizes'
    WHEN 'trophies' THEN 'trophies'
    WHEN 'plasto' THEN 'plasto'
    WHEN 'marketplace' THEN 'marketplace'
    WHEN 'marketplace_own_items' THEN 'marketplace_own_items'
    WHEN 'pets' THEN 'pets'
    WHEN 'pets2' THEN 'pets2'
    WHEN 'pets3' THEN 'pets2'
    WHEN 'petcustomization' THEN 'pets2'
    WHEN 'info_pets' THEN 'pets2'
    WHEN 'club_buy' THEN 'club_buy'
    WHEN 'vip_buy' THEN 'club_buy'
    WHEN 'loyalty_vip_buy' THEN 'club_buy'
    WHEN 'club_gift' THEN 'club_gifts'
    WHEN 'club_gifts' THEN 'club_gifts'
    WHEN 'spaces_new' THEN 'spaces_new'
    WHEN 'guilds' THEN 'guild_frontpage'
    WHEN 'guild_forum' THEN 'guild_frontpage'
    WHEN 'guild_furni' THEN 'guild_custom_furni'
    WHEN 'bots' THEN 'bots'
    ELSE 'default_3x3'
  END AS `page_layout`,
  COALESCE(`page_headline`, ''),
  COALESCE(`page_teaser`, ''),
  COALESCE(`page_special`, ''),
  COALESCE(`page_text1`, ''),
  COALESCE(`page_text2`, ''),
  COALESCE(`page_text_details`, ''),
  COALESCE(`page_text_teaser`, ''),
  `vip_only`,
  '',
  ''
FROM firewind_arcturus_import.catalog_pages;

REPLACE INTO firewind.catalog_items (
  `id`,
  `page_id`,
  `item_ids`,
  `catalog_name`,
  `cost_credits`,
  `cost_pixels`,
  `cost_points`,
  `amount`,
  `vip`,
  `achievement`,
  `song_id`,
  `extradata`,
  `limited_sells`,
  `limited_stack`,
  `offer_active`,
  `allow_gift`
)
SELECT
  c.`id`,
  c.`page_id`,
  c.`item_ids`,
  c.`catalog_name`,
  c.`cost_credits`,
  IF(c.`points_type` = 0, c.`cost_points`, 0) AS `cost_pixels`,
  IF(c.`points_type` <> 0, c.`cost_points`, 0) AS `cost_points`,
  c.`amount`,
  c.`club_only`,
  0,
  c.`song_id`,
  COALESCE(c.`extradata`, ''),
  c.`limited_sells`,
  c.`limited_stack`,
  IF(c.`have_offer` = '1', 1, 0),
  IFNULL(b.`allow_gift`, 1)
FROM firewind_arcturus_import.catalog_items c
LEFT JOIN firewind_arcturus_import.items_base b
  ON b.`id` = CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(c.`item_ids`, ';', 1), ':', 1) AS UNSIGNED);

CREATE TABLE IF NOT EXISTS firewind.catalog_clothing LIKE firewind_arcturus_import.catalog_clothing;
REPLACE INTO firewind.catalog_clothing SELECT * FROM firewind_arcturus_import.catalog_clothing;

CREATE TABLE IF NOT EXISTS firewind.catalog_featured_pages LIKE firewind_arcturus_import.catalog_featured_pages;
REPLACE INTO firewind.catalog_featured_pages SELECT * FROM firewind_arcturus_import.catalog_featured_pages;

CREATE TABLE IF NOT EXISTS firewind.crafting_altars_recipes LIKE firewind_arcturus_import.crafting_altars_recipes;
REPLACE INTO firewind.crafting_altars_recipes SELECT * FROM firewind_arcturus_import.crafting_altars_recipes;

CREATE TABLE IF NOT EXISTS firewind.crafting_recipes LIKE firewind_arcturus_import.crafting_recipes;
REPLACE INTO firewind.crafting_recipes SELECT * FROM firewind_arcturus_import.crafting_recipes;

CREATE TABLE IF NOT EXISTS firewind.crafting_recipes_ingredients LIKE firewind_arcturus_import.crafting_recipes_ingredients;
REPLACE INTO firewind.crafting_recipes_ingredients SELECT * FROM firewind_arcturus_import.crafting_recipes_ingredients;

CREATE TABLE IF NOT EXISTS firewind.items_crackable LIKE firewind_arcturus_import.items_crackable;
REPLACE INTO firewind.items_crackable SELECT * FROM firewind_arcturus_import.items_crackable;

-- Arcturus includes some badge/room-bundle placeholder item ids in catalog_items
-- that do not exist in items_base. Firewind purchases every id in item_ids, so
-- missing extras are stripped from mixed bundles and unsupported placeholder-only
-- offers are removed.
UPDATE firewind.catalog_items SET `item_ids` = '5257' WHERE `id` = 3660;
UPDATE firewind.catalog_items SET `item_ids` = '5264' WHERE `id` = 3667;
UPDATE firewind.catalog_items SET `item_ids` = '5271' WHERE `id` = 3674;
UPDATE firewind.catalog_items SET `item_ids` = '9484' WHERE `id` = 7887;
UPDATE firewind.catalog_items SET `item_ids` = '9485' WHERE `id` = 7888;
UPDATE firewind.catalog_items SET `item_ids` = '9496' WHERE `id` = 7899;
UPDATE firewind.catalog_items SET `item_ids` = '9503' WHERE `id` = 7906;
UPDATE firewind.catalog_items SET `item_ids` = '44570' WHERE `id` = 9989;
UPDATE firewind.catalog_items SET `item_ids` = '44580' WHERE `id` = 9990;
UPDATE firewind.catalog_items SET `item_ids` = '44590' WHERE `id` = 9991;
UPDATE firewind.catalog_items SET `item_ids` = '44600' WHERE `id` = 9992;

DELETE FROM firewind.catalog_items
WHERE `id` IN (10261, 10262, 10263, 10362, 11048, 11049, 11050, 11051, 11052, 11053, 11054);

-- The R63 client expects the original Firewind dog/cat pet product sprites.
-- Arcturus' synthetic 50000/50001 placeholders make the client fail while
-- parsing the pet-race response for these pages.
UPDATE firewind.catalog_items
SET `item_ids` = '20305',
    `offer_active` = 1
WHERE `id` = 10231
  AND `catalog_name` = 'a0 pet0';

UPDATE firewind.catalog_items
SET `item_ids` = '20304',
    `offer_active` = 1
WHERE `id` = 10232
  AND `catalog_name` = 'a0 pet1';

-- After importing, run tools/normalize_furnidata_titles.py against the exported
-- furnidata XML. This Habbo.swf reads <title>, not <name>, for infostand names.
-- Then run tools/sync_furnidata_flags.py to align can_sit/is_walkable with the
-- assets the client loads, and tools/sync_furni_external_texts.py to populate
-- roomItem/wallItem localization aliases.

-- Keep Firewind's room decoration pseudo-items compatible with the room effect
-- inventory serializer while using the Arcturus catalog rows that sell them.
UPDATE firewind.items_base
SET `public_name` = '',
    `item_name` = 'a2 l',
    `type` = 'i',
    `width` = 1,
    `length` = 1,
    `stack_height` = 1,
    `can_stack` = 0,
    `can_sit` = 0,
    `is_walkable` = 0,
    `sprite_id` = 3002,
    `allow_recycle` = 0,
    `allow_trade` = 0,
    `allow_marketplace_sell` = 0,
    `allow_gift` = 1,
    `allow_inventory_stack` = 0,
    `interaction_type` = 'roomeffect',
    `interaction_modes_count` = 1,
    `vending_ids` = '0'
WHERE `id` = 19894;

UPDATE firewind.items_base
SET `public_name` = '',
    `item_name` = 'wallpaper 1',
    `type` = 'i',
    `width` = 1,
    `length` = 1,
    `stack_height` = 1,
    `can_stack` = 0,
    `can_sit` = 0,
    `is_walkable` = 0,
    `sprite_id` = 3001,
    `allow_recycle` = 0,
    `allow_trade` = 0,
    `allow_marketplace_sell` = 0,
    `allow_gift` = 1,
    `allow_inventory_stack` = 0,
    `interaction_type` = 'roomeffect',
    `interaction_modes_count` = 1,
    `vending_ids` = '0'
WHERE `id` = 19896;

UPDATE firewind.items_base
SET `public_name` = '',
    `item_name` = 'landscape 10',
    `type` = 'i',
    `width` = 1,
    `length` = 1,
    `stack_height` = 1,
    `can_stack` = 0,
    `can_sit` = 0,
    `is_walkable` = 0,
    `sprite_id` = 4055,
    `allow_recycle` = 0,
    `allow_trade` = 0,
    `allow_marketplace_sell` = 0,
    `allow_gift` = 1,
    `allow_inventory_stack` = 0,
    `interaction_type` = 'roomeffect',
    `interaction_modes_count` = 1,
    `vending_ids` = '0'
WHERE `id` = 19926;

UPDATE firewind.items_base
SET `public_name` = 'DEAL_HC_1',
    `item_name` = 'DEAL_HC_1',
    `type` = 'h',
    `width` = 0,
    `length` = 0,
    `stack_height` = 0,
    `can_stack` = 0,
    `can_sit` = 0,
    `is_walkable` = 0,
    `sprite_id` = 0,
    `allow_recycle` = 0,
    `allow_trade` = 0,
    `allow_marketplace_sell` = 0,
    `allow_gift` = 0,
    `allow_inventory_stack` = 0,
    `interaction_type` = 'default',
    `interaction_modes_count` = 0,
    `vending_ids` = '0'
WHERE `id` = 20060;

REPLACE INTO firewind.catalog_items (
  `id`, `page_id`, `item_ids`, `catalog_name`, `cost_credits`, `cost_pixels`,
  `cost_points`, `amount`, `vip`, `achievement`, `song_id`, `extradata`,
  `limited_sells`, `limited_stack`, `offer_active`, `allow_gift`
) VALUES
  (88002118, 8, '20060', 'HABBO_CLUB_VIP_1_MONTH', 100, 0, 0, 1, '0', 0, 0, '', 0, 0, 1, 0),
  (88002119, 8, '20060', 'HABBO_CLUB_VIP_3_MONTH', 250, 0, 0, 3, '0', 0, 0, '', 0, 0, 1, 0),
  (88002120, 8, '20060', 'HABBO_CLUB_VIP_6_MONTH', 500, 0, 0, 6, '0', 0, 0, '', 0, 0, 1, 0);

DELETE FROM firewind.catalog_items
WHERE `id` BETWEEN 13000 AND 13039
  AND `catalog_name` LIKE 'wf\_%';

DELETE FROM firewind.catalog_items
WHERE `page_id` = 231
  AND `item_ids` = '40010'
  AND `id` BETWEEN 10322 AND 10344;

UPDATE firewind.catalog_pages
SET `page_layout` = 'default_3x3',
    `visible` = '1',
    `enabled` = '0'
WHERE `id` = 209;

REPLACE INTO firewind.catalog_pages (
  `id`, `parent_id`, `caption`, `icon_color`, `icon_image`, `visible`, `enabled`,
  `min_rank`, `club_only`, `order_num`, `page_layout`, `page_headline`,
  `page_teaser`, `page_special`, `page_text1`, `page_text2`,
  `page_text_details`, `page_text_teaser`, `vip_only`,
  `page_link_description`, `page_link_pagename`
) VALUES
  (190000001, 209, 'By Line A-M', 1, 72, '1', '0', 1, '0', 1, 'default_3x3', '', '', '', '', '', '', '', '0', '', ''),
  (190000002, 209, 'By Line N-Z', 1, 72, '1', '0', 1, '0', 2, 'default_3x3', '', '', '', '', '', '', '', '0', '', ''),
  (190000003, 2, 'More Furni', 1, 45, '1', '0', 1, '0', 13, 'default_3x3', '', '', '', '', '', '', '', '0', '', '');

UPDATE firewind.catalog_pages
SET `parent_id` = 190000001
WHERE `parent_id` = 209
  AND `id` NOT IN (190000001, 190000002)
  AND UPPER(LEFT(`caption`, 1)) BETWEEN 'A' AND 'M';

UPDATE firewind.catalog_pages
SET `parent_id` = 190000002
WHERE `parent_id` = 209
  AND `id` NOT IN (190000001, 190000002);

UPDATE firewind.catalog_pages
SET `parent_id` = 222,
    `order_num` = `order_num` + 200
WHERE `parent_id` = 765;

UPDATE firewind.catalog_pages
SET `visible` = '0',
    `enabled` = '0'
WHERE `id` = 765;

UPDATE firewind.catalog_pages
SET `parent_id` = 190000003
WHERE `parent_id` = 3
  AND `id` NOT IN (588, 589, 200, 201, 202, 310, 203, 204, 205, 206, 207, 208, 423);

UPDATE firewind.catalog_pages
SET `visible` = '0', `enabled` = '0'
WHERE `id` IN (1000, 1001, 1002, 1003, 1004, 1005, 1006);

-- Pet layouts render exactly one pet offer; stale furniture rows on those pages
-- can make the client choose the wrong product and skip the dynamic pet SWF.
DELETE ci
FROM firewind.catalog_items ci
JOIN firewind.catalog_pages p ON p.id = ci.page_id
WHERE p.page_layout = 'pets'
  AND ci.catalog_name NOT REGEXP '^a0 pet[0-9]+$';

-- This client revision only supports root parents with one subcategory level.
-- Apply the screenshot-style final catalog layout as the last import step.
SOURCE SQL Files/arcturus_firewind_catalog_layout.sql
