const fs = require('fs');
const path = require('path');

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

function removeDir(dir) {
  fs.rmSync(dir, { recursive: true, force: true });
}

function writeText(file, content) {
  ensureDir(path.dirname(file));
  fs.writeFileSync(file, content, 'utf8');
}

function writeBuffer(file, content) {
  ensureDir(path.dirname(file));
  fs.writeFileSync(file, content);
}

function safeClassName(value) {
  const text = String(value || 'Furniture');
  return text
    .replace(/[^A-Za-z0-9_$]/g, '_')
    .replace(/^[^A-Za-z_$]/, '_$&');
}

function safeFilename(value) {
  return String(value || 'file').replace(/[^A-Za-z0-9._-]/g, '_');
}

function xmlEscape(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function xmlHeader() {
  return '<?xml version="1.0" encoding="ISO-8859-1" ?>';
}

function toLatin1Buffer(text) {
  return Buffer.from(String(text), 'latin1');
}

function fromLatin1Buffer(buffer) {
  return Buffer.from(buffer).toString('latin1');
}

function replaceScaleToken(name, sourceSize = 64, targetSize = 32) {
  const source = `_${sourceSize}_`;
  const target = `_${targetSize}_`;
  if (!String(name).includes(source)) return null;
  return String(name).replace(source, target);
}

function roundHalfUp(value) {
  if (value < 0) return Math.ceil(value - 0.5);
  return Math.floor(value + 0.5);
}

function scaleRegistration(value) {
  const number = Number(value);
  if (!Number.isFinite(number)) return null;
  return roundHalfUp(number / 2);
}

function formatList(items) {
  return items.length ? items.join(', ') : '(none)';
}

module.exports = {
  ensureDir,
  removeDir,
  writeText,
  writeBuffer,
  safeClassName,
  safeFilename,
  xmlEscape,
  xmlHeader,
  toLatin1Buffer,
  fromLatin1Buffer,
  replaceScaleToken,
  roundHalfUp,
  scaleRegistration,
  formatList
};
