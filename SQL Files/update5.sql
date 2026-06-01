ALTER TABLE user_bots
  ADD COLUMN bot_type ENUM('generic','rentable') NOT NULL DEFAULT 'rentable' AFTER template_id,
  ADD COLUMN expire_timestamp INT(11) NOT NULL DEFAULT 0 AFTER bot_type,
  ADD COLUMN dance_id TINYINT(1) NOT NULL DEFAULT 0 AFTER expire_timestamp,
  ADD COLUMN chat_auto ENUM('0','1') NOT NULL DEFAULT '0' AFTER dance_id,
  ADD COLUMN chat_random ENUM('0','1') NOT NULL DEFAULT '0' AFTER chat_auto,
  ADD COLUMN chat_delay INT(11) NOT NULL DEFAULT 7 AFTER chat_random,
  ADD COLUMN chat_lines TEXT NOT NULL AFTER chat_delay;

UPDATE user_bots
SET bot_type = 'rentable',
    expire_timestamp = UNIX_TIMESTAMP() + 604800
WHERE expire_timestamp = 0;
