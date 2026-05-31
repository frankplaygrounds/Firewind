ALTER TABLE user_bots
  ADD COLUMN bot_type ENUM('generic','rentable') NOT NULL DEFAULT 'rentable' AFTER template_id,
  ADD COLUMN expire_timestamp INT(11) NOT NULL DEFAULT 0 AFTER bot_type;

UPDATE user_bots
SET bot_type = 'rentable',
    expire_timestamp = UNIX_TIMESTAMP() + 604800
WHERE expire_timestamp = 0;
