const cp = require('child_process');
const { ConverterError } = require('../errors');

const DEFAULT_MYSQL = '/usr/local/mysql-5.7.31-macos10.14-x86_64/bin/mysql';

function accessDenied(output) {
  return /Access denied/i.test(output || '');
}

class MysqlClient {
  constructor({
    mysqlPath = DEFAULT_MYSQL,
    host = '127.0.0.1',
    user = 'root',
    password = 'auto',
    database = 'firewind'
  } = {}) {
    this.mysqlPath = mysqlPath;
    this.host = host;
    this.user = user;
    this.password = password;
    this.database = database;
    this.resolvedPassword = password === 'auto' ? undefined : password;
  }

  args(password) {
    const args = [
      '--protocol=TCP',
      '-h',
      this.host,
      '-u',
      this.user,
      '--batch',
      '--raw',
      '--skip-column-names'
    ];
    if (password) args.push(`-p${password}`);
    if (this.database) args.push(this.database);
    return args;
  }

  runWithPassword(sql, password) {
    return cp.spawnSync(this.mysqlPath, this.args(password), {
      input: sql,
      encoding: 'utf8',
      maxBuffer: 1024 * 1024 * 20
    });
  }

  run(sql) {
    if (this.password === 'auto' && this.resolvedPassword === undefined) {
      const noPassword = this.runWithPassword(sql, '');
      if (noPassword.status === 0) {
        this.resolvedPassword = '';
        return noPassword.stdout || '';
      }
      const output = `${noPassword.stdout || ''}${noPassword.stderr || ''}`;
      if (!accessDenied(output)) {
        throw new ConverterError(`mysql failed: ${output.trim() || `exit ${noPassword.status}`}`);
      }
      const fallback = this.runWithPassword(sql, '4299');
      if (fallback.status === 0) {
        this.resolvedPassword = '4299';
        return fallback.stdout || '';
      }
      throw new ConverterError(`mysql failed: ${(fallback.stderr || fallback.stdout || '').trim()}`);
    }

    const result = this.runWithPassword(sql, this.resolvedPassword || '');
    if (result.error) {
      throw new ConverterError(`Could not start mysql: ${result.error.message}`, { cause: result.error });
    }
    if (result.status !== 0) {
      throw new ConverterError(`mysql failed: ${(result.stderr || result.stdout || '').trim()}`);
    }
    return result.stdout || '';
  }

  queryRows(sql) {
    const stdout = this.run(sql);
    if (!stdout.trim()) return [];
    return stdout
      .trimEnd()
      .split(/\r?\n/)
      .map((line) => line.split('\t'));
  }

  queryOne(sql) {
    return this.queryRows(sql)[0] || null;
  }

  queryScalar(sql) {
    const row = this.queryOne(sql);
    return row ? row[0] : null;
  }
}

function sqlString(value) {
  if (value === null || value === undefined) return 'NULL';
  return `'${String(value)
    .replace(/\\/g, '\\\\')
    .replace(/\0/g, '\\0')
    .replace(/\n/g, '\\n')
    .replace(/\r/g, '\\r')
    .replace(/\x1a/g, '\\Z')
    .replace(/'/g, "\\'")}'`;
}

function sqlNumber(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? String(number) : String(fallback);
}

module.exports = {
  MysqlClient,
  DEFAULT_MYSQL,
  sqlString,
  sqlNumber
};
