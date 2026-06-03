-- Firewind r63 catalog layout for the imported Arcturus catalogue.
-- Keeps the client-compatible shape as: root pages > direct subcategories.

UPDATE firewind.catalog_pages
SET `visible` = '0',
    `enabled` = '0'
WHERE `id` IN (10, 11, 220);

REPLACE INTO firewind.catalog_pages (
  `id`, `parent_id`, `caption`, `icon_color`, `icon_image`, `visible`, `enabled`,
  `min_rank`, `club_only`, `order_num`, `page_layout`, `page_headline`,
  `page_teaser`, `page_special`, `page_text1`, `page_text2`,
  `page_text_details`, `page_text_teaser`, `vip_only`,
  `page_link_description`, `page_link_pagename`
) VALUES
  (190000004, -1, 'Exchange', 1, 146, '1', '1', 1, '0', 11, 'default_3x3', '', '', '', '', '', '', '', '0', '', ''),
  (190000005, -1, 'Diamond Shop', 1, 263, '1', '1', 1, '0', 12, 'default_3x3', '', '', '', '', '', '', '', '0', '', ''),
  (190000010, 8, 'Club Membership', 1, 172, '1', '1', 1, '0', 0, 'club_buy', '', '', '', '', '', '', '', '0', '', '');

UPDATE firewind.catalog_pages
SET `parent_id` = -1,
    `visible` = '1',
    `min_rank` = 1,
    `club_only` = '0',
    `vip_only` = '0'
WHERE `id` IN (1, 801, 8, 2, 209, 372, 526, 219, 107, 218, 190000004, 190000005);

UPDATE firewind.catalog_pages
SET `caption` = 'Frontpage',
    `enabled` = '1',
    `order_num` = 1,
    `page_layout` = 'frontpage'
WHERE `id` = 1;

UPDATE firewind.catalog_pages
SET `caption` = 'Limited Edition',
    `enabled` = '0',
    `order_num` = 2,
    `page_layout` = 'default_3x3'
WHERE `id` = 801;

UPDATE firewind.catalog_pages
SET `caption` = 'Static Club',
    `enabled` = '0',
    `order_num` = 3,
    `page_layout` = 'default_3x3'
WHERE `id` = 8;

UPDATE firewind.catalog_pages
SET `caption` = 'Classic Furni',
    `enabled` = '0',
    `order_num` = 4,
    `page_layout` = 'default_3x3'
WHERE `id` = 2;

UPDATE firewind.catalog_pages
SET `caption` = 'Furni By Line',
    `enabled` = '0',
    `order_num` = 5,
    `page_layout` = 'default_3x3'
WHERE `id` = 209;

UPDATE firewind.catalog_pages
SET `caption` = 'Furni By Theme',
    `enabled` = '0',
    `order_num` = 6,
    `page_layout` = 'default_3x3'
WHERE `id` = 372;

UPDATE firewind.catalog_pages
SET `caption` = 'Duckets Shop',
    `enabled` = '0',
    `order_num` = 7,
    `page_layout` = 'default_3x3'
WHERE `id` = 526;

UPDATE firewind.catalog_pages
SET `caption` = 'Games',
    `enabled` = '0',
    `order_num` = 8,
    `page_layout` = 'default_3x3'
WHERE `id` = 219;

UPDATE firewind.catalog_pages
SET `caption` = 'Habbo Groups',
    `enabled` = '0',
    `order_num` = 9,
    `page_layout` = 'guild_frontpage'
WHERE `id` = 107;

UPDATE firewind.catalog_pages
SET `caption` = 'Wired',
    `enabled` = '0',
    `order_num` = 10,
    `page_layout` = 'default_3x3'
WHERE `id` = 218;

UPDATE firewind.catalog_items
SET `page_id` = 190000010
WHERE `page_id` = 8;

UPDATE firewind.catalog_items
SET `page_id` = 190000004
WHERE `page_id` IN (12, 307, 6700);

UPDATE firewind.catalog_items
SET `page_id` = 190000005
WHERE `page_id` IN (169, 733, 13555);

DROP TEMPORARY TABLE IF EXISTS firewind.tmp_catalog_source_target;
CREATE TEMPORARY TABLE firewind.tmp_catalog_source_target (
  `source_id` INT PRIMARY KEY,
  `target_id` INT NOT NULL,
  `prefix_children` TINYINT(1) NOT NULL DEFAULT 0,
  `source_order` INT NOT NULL
);

INSERT INTO firewind.tmp_catalog_source_target (`source_id`, `target_id`, `prefix_children`, `source_order`) VALUES
  (190000001, 209, 0, 1000),
  (190000002, 209, 0, 2000),
  (190000003, 209, 0, 3000),
  (301, 209, 1, 4000),
  (50, 209, 1, 4100),
  (593, 209, 1, 4200),
  (700, 209, 1, 4300),
  (6, 209, 1, 4400),
  (27, 209, 1, 4500),
  (503, 209, 1, 4600),
  (419, 209, 1, 4700),
  (708, 209, 1, 4800),
  (511, 209, 1, 4900),
  (180, 209, 1, 5000),

  (50000, 2, 0, 100),
  (13, 2, 1, 200),
  (117, 2, 1, 300),
  (604, 2, 0, 400),
  (222, 2, 0, 500),
  (500, 2, 1, 600),
  (498, 2, 1, 700),
  (125, 2, 1, 800),

  (3, 526, 0, 100),
  (624, 526, 0, 200),

  (14, 801, 1, 100),
  (300, 801, 0, 200),
  (327, 801, 0, 300),
  (348, 801, 0, 400),
  (311, 801, 0, 500),
  (766, 801, 0, 600),
  (557, 801, 0, 700),
  (599, 801, 0, 800),
  (664, 801, 0, 900),
  (345, 801, 1, 1000),
  (262, 801, 0, 1100),
  (736, 801, 1, 1200),

  (4, 372, 1, 100),
  (277, 372, 0, 200),
  (221, 372, 0, 300),
  (234, 372, 0, 400),
  (246, 372, 0, 500),
  (680, 372, 0, 600),
  (763, 372, 0, 700),
  (91, 372, 1, 800),
  (373, 372, 1, 1000),
  (374, 372, 1, 2000),
  (375, 372, 1, 3000),
  (376, 372, 1, 4000),
  (377, 372, 1, 5000),
  (402, 372, 1, 6000),
  (387, 372, 1, 7000),
  (442, 372, 1, 8000),
  (253, 372, 1, 9000),
  (674, 372, 1, 10000),
  (1125, 372, 1, 11000),
  (1161, 372, 1, 11100),
  (1143, 372, 1, 11200),
  (1134, 372, 1, 11300),
  (1122, 372, 1, 11400),
  (1128, 372, 1, 11500),
  (1116, 372, 1, 11600),
  (1158, 372, 1, 11700),
  (1137, 372, 1, 11800),
  (1155, 372, 1, 11900),
  (1119, 372, 1, 12000),
  (1146, 372, 1, 12100),
  (1113, 372, 1, 12200),
  (1131, 372, 1, 12300),
  (1140, 372, 1, 12400),
  (1152, 372, 1, 12500),
  (1149, 372, 1, 12600),
  (183, 372, 1, 13000),
  (184, 372, 1, 13100),
  (103, 372, 1, 13200),
  (1184, 372, 1, 13300),
  (1191, 372, 1, 13400),
  (1189, 372, 1, 13500),
  (1193, 372, 1, 13600),
  (1187, 372, 1, 13700),
  (637, 372, 1, 13800),
  (1195, 372, 1, 13900),
  (213, 372, 1, 14000),
  (1170, 372, 1, 14100),
  (1172, 372, 1, 14200),
  (1174, 372, 1, 14300),
  (189, 372, 1, 14400),
  (1176, 372, 1, 14500),
  (1164, 372, 1, 14600),
  (1182, 372, 1, 14700),
  (1213, 372, 1, 14800),
  (600, 372, 1, 14900),
  (1178, 372, 1, 15000),
  (61, 372, 1, 15100),
  (1180, 372, 1, 15200),
  (1219, 372, 1, 15300),
  (186, 372, 1, 15400),
  (193, 372, 1, 15500),
  (1167, 372, 1, 15600),
  (665, 372, 1, 15700),
  (668, 372, 1, 15800),
  (1215, 372, 1, 15900),
  (1216, 372, 1, 16000),
  (1221, 372, 1, 16100),
  (1225, 372, 1, 16200);

UPDATE firewind.catalog_pages source_page
JOIN firewind.tmp_catalog_source_target target ON target.`source_id` = source_page.`id`
LEFT JOIN (
  SELECT `page_id`, COUNT(*) AS `item_count`
  FROM firewind.catalog_items
  WHERE `item_ids` <> ''
    AND `amount` > 0
  GROUP BY `page_id`
) item_counts ON item_counts.`page_id` = source_page.`id`
SET source_page.`parent_id` = CASE WHEN IFNULL(item_counts.`item_count`, 0) > 0 THEN target.`target_id` ELSE source_page.`parent_id` END,
    source_page.`visible` = CASE WHEN IFNULL(item_counts.`item_count`, 0) > 0 THEN '1' ELSE '0' END,
    source_page.`enabled` = CASE WHEN IFNULL(item_counts.`item_count`, 0) > 0 THEN '1' ELSE '0' END,
    source_page.`order_num` = target.`source_order`
WHERE source_page.`id` NOT IN (1, 801, 8, 2, 209, 372, 526, 219, 107, 218, 190000004, 190000005);

UPDATE firewind.catalog_pages child_page
JOIN firewind.tmp_catalog_source_target target ON target.`source_id` = child_page.`parent_id`
JOIN firewind.catalog_pages source_page ON source_page.`id` = target.`source_id`
SET child_page.`parent_id` = target.`target_id`,
    child_page.`visible` = '1',
    child_page.`enabled` = '1',
    child_page.`caption` = CASE
      WHEN target.`prefix_children` = 1
        THEN LEFT(CONCAT(source_page.`caption`, ' - ', child_page.`caption`), 128)
      ELSE child_page.`caption`
    END,
    child_page.`order_num` = target.`source_order` + GREATEST(child_page.`order_num`, 0) + 1
WHERE child_page.`id` NOT IN (1, 801, 8, 2, 209, 372, 526, 219, 107, 218, 190000004, 190000005);

DROP TEMPORARY TABLE firewind.tmp_catalog_source_target;

UPDATE firewind.catalog_pages
SET `caption` = 'Bunny Extras'
WHERE `id` = 1234535
  AND `caption` = 'Random Shit';

UPDATE firewind.catalog_pages
SET `parent_id` = 190000004,
    `visible` = '0',
    `enabled` = '0'
WHERE `id` IN (12, 307, 6700);

UPDATE firewind.catalog_pages
SET `parent_id` = 190000005,
    `visible` = '0',
    `enabled` = '0'
WHERE `id` IN (169, 733, 13555);

UPDATE firewind.catalog_pages
SET `visible` = '0',
    `enabled` = '0'
WHERE `id` = 7
   OR `parent_id` = 7;

UPDATE firewind.catalog_pages
SET `visible` = '0',
    `enabled` = '0'
WHERE `parent_id` = -1
  AND `id` NOT IN (1, 801, 8, 2, 209, 372, 526, 219, 107, 218, 190000004, 190000005);

CREATE TEMPORARY TABLE firewind.tmp_catalog_empty_pages (`id` INT PRIMARY KEY);
INSERT INTO firewind.tmp_catalog_empty_pages (`id`)
SELECT page.`id`
FROM firewind.catalog_pages page
WHERE page.`visible` = '1'
  AND page.`id` <> 1
  AND NOT EXISTS (
    SELECT 1
    FROM firewind.catalog_items ci
    WHERE ci.`page_id` = page.`id`
      AND ci.`item_ids` <> ''
      AND ci.`amount` > 0
  )
  AND NOT EXISTS (
    SELECT 1
    FROM firewind.catalog_pages child
    WHERE child.`parent_id` = page.`id`
      AND child.`visible` = '1'
  );

UPDATE firewind.catalog_pages page
JOIN firewind.tmp_catalog_empty_pages empty_page ON empty_page.`id` = page.`id`
SET page.`visible` = '0',
    page.`enabled` = '0';

DROP TEMPORARY TABLE firewind.tmp_catalog_empty_pages;
