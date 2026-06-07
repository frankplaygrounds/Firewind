const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const root = path.resolve(__dirname, '..');
const sampleNitro = process.env.SAMPLE_NITRO
  || '/Users/Iaad/Downloads/nitro-converter/nitro/rote_bgblock.nitro';
const tmpRoot = path.join(root, '.tmp', 'sample-workflow');
const outputSwf = path.join(tmpRoot, 'rote_bgblock.swf');
const debugDir = path.join(tmpRoot, 'debug');
const workDir = path.join(tmpRoot, 'project');

function run(args) {
  const result = cp.spawnSync(process.execPath, [path.join(root, 'index.js'), ...args], {
    cwd: root,
    stdio: 'inherit',
    env: process.env
  });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status);
}

if (!fs.existsSync(sampleNitro)) {
  console.error(`Sample Nitro not found: ${sampleNitro}`);
  console.error('Set SAMPLE_NITRO=/path/to/file.nitro to run the workflow with another file.');
  process.exit(1);
}

fs.rmSync(tmpRoot, { recursive: true, force: true });
fs.mkdirSync(tmpRoot, { recursive: true });

console.log('1. Dry-run parse/project/zoom workflow');
run([
  '--dry-run',
  '--work-dir', workDir,
  '--debug-dir', debugDir,
  '--verbose',
  sampleNitro,
  outputSwf
]);

if (process.env.RUN_FULL === '1') {
  console.log('\n2. Full compile/cleanup/verify workflow');
  run([
    '--work-dir', workDir,
    '--debug-dir', debugDir,
    '--verbose',
    sampleNitro,
    outputSwf
  ]);
  console.log(`Full workflow output: ${outputSwf}`);
} else {
  console.log('\nDry-run complete. Set RUN_FULL=1 with AIR_HOME/FLEX_HOME configured to compile a SWF.');
}
