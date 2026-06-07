const fs = require('fs');
const path = require('path');
const cp = require('child_process');
const { CompilationError } = require('../errors');

function isExecutableFile(file) {
  try {
    return fs.existsSync(file) && fs.statSync(file).isFile();
  } catch {
    return false;
  }
}

function sdkRootFromCompiler(compiler, airHome) {
  if (airHome) {
    const resolved = path.resolve(airHome);
    if (/mxmlc(\.bat|\.cmd)?$/i.test(path.basename(resolved))) {
      return path.dirname(path.dirname(resolved));
    }
    return resolved;
  }
  return path.dirname(path.dirname(compiler));
}

function playerglobalInfo(sdkRoot) {
  const playerRoot = path.join(sdkRoot, 'frameworks', 'libs', 'player');
  if (!fs.existsSync(playerRoot)) return null;
  const versions = fs.readdirSync(playerRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .filter((name) => fs.existsSync(path.join(playerRoot, name, 'playerglobal.swc')))
    .sort((a, b) => Number(a) - Number(b));
  const version = versions[versions.length - 1];
  if (!version) return null;
  const swc = path.join(playerRoot, version, 'playerglobal.swc');
  const header = fs.readFileSync(swc).subarray(0, 2);
  if (!header.equals(Buffer.from('PK'))) return null;
  return { playerRoot, version };
}

function findOnPath(command) {
  const result = cp.spawnSync(process.platform === 'win32' ? 'where' : 'which', [command], {
    encoding: 'utf8'
  });
  if (result.status !== 0 || !result.stdout) return null;
  return result.stdout.split(/\r?\n/).map((line) => line.trim()).find(Boolean) || null;
}

function compilerCandidates(airHome) {
  const candidates = [];
  if (airHome) {
    const resolved = path.resolve(airHome);
    if (/mxmlc(\.bat|\.cmd)?$/i.test(path.basename(resolved))) {
      candidates.push(resolved);
    } else {
      candidates.push(path.join(resolved, 'bin', process.platform === 'win32' ? 'mxmlc.bat' : 'mxmlc'));
      candidates.push(path.join(resolved, 'bin', 'mxmlc'));
      candidates.push(path.join(resolved, 'bin', 'mxmlc.bat'));
    }
  }
  for (const envName of ['AIR_HOME', 'FLEX_HOME']) {
    if (!process.env[envName]) continue;
    candidates.push(path.join(process.env[envName], 'bin', process.platform === 'win32' ? 'mxmlc.bat' : 'mxmlc'));
    candidates.push(path.join(process.env[envName], 'bin', 'mxmlc'));
    candidates.push(path.join(process.env[envName], 'bin', 'mxmlc.bat'));
  }
  const pathCompiler = findOnPath('mxmlc');
  if (pathCompiler) candidates.push(pathCompiler);
  return [...new Set(candidates)];
}

function resolveMxmlc(airHome) {
  const candidates = compilerCandidates(airHome);
  for (const candidate of candidates) {
    if (isExecutableFile(candidate)) return candidate;
  }

  throw new CompilationError('Could not find AIR/Flex mxmlc compiler.', {
    details: [
      'Install the Apache Flex SDK or Harman AIR SDK.',
      'Then pass --air-home /path/to/sdk or set AIR_HOME/FLEX_HOME.',
      `Checked: ${candidates.length ? candidates.join(', ') : 'PATH only'}`
    ]
  });
}

function filterCompilerOutput(text) {
  return String(text || '')
    .split(/\r?\n/)
    .map((line) => line.trimEnd())
    .filter((line) => {
      const trimmed = line.trim();
      if (!trimmed) return false;
      if (trimmed === 'command line') return false;
      if (trimmed === "Warning: 'static-link-runtime-shared-libraries' is not fully supported.") return false;
      return true;
    });
}

class MxmlcCompiler {
  constructor({ airHome = null, logger }) {
    this.airHome = airHome;
    this.logger = logger;
  }

  compile(project, outputFile) {
    const compiler = resolveMxmlc(this.airHome);
    const sdkRoot = sdkRootFromCompiler(compiler, this.airHome);
    const playerglobal = playerglobalInfo(sdkRoot);
    const args = [
      '-static-link-runtime-shared-libraries=true',
      '-use-network=false',
      `-source-path+=${project.srcDir}`,
      `-output=${outputFile}`,
      project.mainFile
    ];
    if (playerglobal) args.unshift(`-target-player=${playerglobal.version}`);

    this.logger.step(`Compiling SWF with ${compiler}`);
    const command = process.platform === 'win32' && /\.(bat|cmd)$/i.test(compiler)
      ? { file: 'cmd.exe', args: ['/c', compiler, ...args] }
      : { file: compiler, args };

    const result = cp.spawnSync(command.file, command.args, {
      cwd: project.workDir,
      encoding: 'utf8',
      env: playerglobal
        ? { ...process.env, PLAYERGLOBAL_HOME: playerglobal.playerRoot, FLEX_HOME: sdkRoot }
        : process.env,
      maxBuffer: 1024 * 1024 * 20
    });

    const output = [
      ...filterCompilerOutput(result.stdout),
      ...filterCompilerOutput(result.stderr)
    ];
    for (const line of output) this.logger.verbose(line);

    if (result.error) {
      throw new CompilationError(`Failed to start mxmlc: ${result.error.message}`, {
        cause: result.error,
        details: output
      });
    }
    if (result.status !== 0) {
      throw new CompilationError(`mxmlc failed with exit code ${result.status}.`, {
        details: output
      });
    }
    if (!fs.existsSync(outputFile)) {
      throw new CompilationError(`mxmlc finished but did not create ${outputFile}.`, {
        details: output
      });
    }
  }
}

module.exports = {
  MxmlcCompiler,
  resolveMxmlc
};
