const fs = require('fs');
const path = require('path');
const { convertNitroToSwf } = require('./pipeline');
const { Logger } = require('./logger');
const { ConverterError } = require('./errors');
const { safeFilename } = require('./utils');

function usage() {
  return `Usage:
  node index.js [options] input-furniture.nitro [output-furniture.swf]
  node index.js [options] input-folder [output-folder]

Options:
  --air-home <path>     AIR/Flex SDK directory, or direct path to mxmlc
  --work-dir <path>     Temporary project directory
  --keep-temp           Keep temporary files after a real conversion
  --debug-dir <path>    Write conversion-report.json
  --dry-run             Parse Nitro and build the AS3 project without compiling
  --skip-cleanup        Do not remove visualization <graphics> wrappers
  --skip-verify         Do not verify the final SWF
  --deploy              Copy SWF to Firewind-Web, update gamedata, and add catalog rows
  --deploy-skip-output  Deploy only; compile through a temporary SWF and do not keep output
  --web-root <path>     Firewind-Web root (default: /Users/Iaad/Documents/GitHub/Firewind-Web)
  --dcr-dir <path>      DCR hof_furni output directory
  --furnidata-file <p>  furnidata file to update
  --productdata-file <p> productdata file to update
  --no-db               Skip MySQL catalog/items_base update
  --no-gamedata         Skip DCR/furnidata/productdata update
  --mysql-path <path>   mysql binary path
  --db-host <host>      MySQL host (default: 127.0.0.1)
  --db-user <user>      MySQL user (default: root)
  --db-password <pass>  MySQL password (default: try none, then 4299)
  --db-no-password      Force blank MySQL password
  --db-name <name>      MySQL database (default: firewind)
  --catalog-page <name> Catalog page caption (default: Recently Added)
  --price-credits <n>   Catalog credit price (default: 3)
  --price-pixels <n>    Catalog pixel price (default: 0)
  --price-points <n>    Catalog point price (default: 0)
  --display-name <text> Furni display name for catalog/gamedata
  --description <text>  Furni description for catalog/gamedata
  --item-type <s|i>     Floor item s or wall item i (default: s)
  --sprite-id <n>       Force furnidata sprite id
  --variant-ids <list>  Force variants, e.g. 1,2,3
  --no-variants         Disable automatic color-map variant detection
  --walkable            Mark the item walkable/canstandon
  --sittable            Mark the item sittable/cansiton
  --not-stackable       Mark the item non-stackable
  --verbose             Show compiler and structural debug logs
  --quiet               Only print errors
  --help                Show this help

Environment:
  AIR_HOME or FLEX_HOME can be used instead of --air-home.`;
}

function parseArgs(argv) {
  const args = argv.slice(2);
  const options = {
    airHome: null,
    workDir: null,
    debugDir: null,
    keepTemp: false,
    dryRun: false,
    skipCleanup: false,
    skipVerify: false,
    deploy: false,
    deploySkipOutput: false,
    deployOptions: {
      metadata: {},
      mysql: {}
    },
    verbose: false,
    quiet: false
  };
  const positional = [];

  for (let index = 0; index < args.length; index += 1) {
    const arg = args[index];
    if (arg === '--help' || arg === '-h') {
      options.help = true;
    } else if (arg === '--air-home') {
      options.airHome = args[++index];
    } else if (arg === '--work-dir') {
      options.workDir = args[++index];
    } else if (arg === '--debug-dir') {
      options.debugDir = args[++index];
    } else if (arg === '--keep-temp') {
      options.keepTemp = true;
    } else if (arg === '--dry-run') {
      options.dryRun = true;
    } else if (arg === '--skip-cleanup') {
      options.skipCleanup = true;
    } else if (arg === '--skip-verify') {
      options.skipVerify = true;
    } else if (arg === '--deploy') {
      options.deploy = true;
    } else if (arg === '--deploy-skip-output') {
      options.deploy = true;
      options.deploySkipOutput = true;
    } else if (arg === '--web-root') {
      options.deployOptions.webRoot = path.resolve(args[++index]);
    } else if (arg === '--dcr-dir') {
      options.deployOptions.dcrDir = path.resolve(args[++index]);
    } else if (arg === '--furnidata-file') {
      options.deployOptions.furnidataFile = path.resolve(args[++index]);
    } else if (arg === '--productdata-file') {
      options.deployOptions.productdataFile = path.resolve(args[++index]);
    } else if (arg === '--no-db') {
      options.deployOptions.skipDb = true;
    } else if (arg === '--no-gamedata') {
      options.deployOptions.skipGamedata = true;
    } else if (arg === '--no-gamedata-backup') {
      options.deployOptions.backupGamedata = false;
    } else if (arg === '--mysql-path') {
      options.deployOptions.mysql.mysqlPath = path.resolve(args[++index]);
    } else if (arg === '--db-host') {
      options.deployOptions.mysql.host = args[++index];
    } else if (arg === '--db-user') {
      options.deployOptions.mysql.user = args[++index];
    } else if (arg === '--db-password') {
      options.deployOptions.mysql.password = args[++index];
    } else if (arg === '--db-no-password') {
      options.deployOptions.mysql.password = '';
    } else if (arg === '--db-name') {
      options.deployOptions.mysql.database = args[++index];
    } else if (arg === '--catalog-page') {
      options.deployOptions.pageCaption = args[++index];
    } else if (arg === '--catalog-parent-id') {
      options.deployOptions.parentId = Number(args[++index]);
    } else if (arg === '--price-credits') {
      options.deployOptions.metadata.priceCredits = Number(args[++index]);
    } else if (arg === '--price-pixels') {
      options.deployOptions.metadata.pricePixels = Number(args[++index]);
    } else if (arg === '--price-points') {
      options.deployOptions.metadata.pricePoints = Number(args[++index]);
    } else if (arg === '--amount') {
      options.deployOptions.metadata.amount = Number(args[++index]);
    } else if (arg === '--display-name') {
      options.deployOptions.metadata.displayName = args[++index];
    } else if (arg === '--description') {
      options.deployOptions.metadata.description = args[++index];
    } else if (arg === '--item-type') {
      options.deployOptions.metadata.itemType = args[++index];
    } else if (arg === '--sprite-id') {
      options.deployOptions.metadata.spriteId = Number(args[++index]);
    } else if (arg === '--variant-ids') {
      options.deployOptions.metadata.variantIds = args[++index]
        .split(',')
        .map((value) => value.trim())
        .filter(Boolean);
    } else if (arg === '--no-variants') {
      options.deployOptions.metadata.disableAutoVariants = true;
    } else if (arg === '--width') {
      options.deployOptions.metadata.width = Number(args[++index]);
    } else if (arg === '--length') {
      options.deployOptions.metadata.length = Number(args[++index]);
    } else if (arg === '--stack-height') {
      options.deployOptions.metadata.stackHeight = Number(args[++index]);
    } else if (arg === '--interaction-type') {
      options.deployOptions.metadata.interactionType = args[++index];
    } else if (arg === '--interaction-modes-count') {
      options.deployOptions.metadata.interactionModesCount = Number(args[++index]);
    } else if (arg === '--category') {
      options.deployOptions.metadata.category = args[++index];
    } else if (arg === '--furniline') {
      options.deployOptions.metadata.furniline = args[++index];
    } else if (arg === '--walkable') {
      options.deployOptions.metadata.isWalkable = true;
    } else if (arg === '--sittable') {
      options.deployOptions.metadata.canSit = true;
    } else if (arg === '--not-stackable') {
      options.deployOptions.metadata.canStack = false;
    } else if (arg === '--verbose') {
      options.verbose = true;
    } else if (arg === '--quiet') {
      options.quiet = true;
    } else if (arg.startsWith('--')) {
      throw new ConverterError(`Unknown option: ${arg}`, { exitCode: 2 });
    } else {
      positional.push(arg);
    }
  }

  if (options.help) return options;
  if (positional.length < 1 || positional.length > 2) {
    throw new ConverterError(usage(), { exitCode: 2 });
  }
  options.inputFile = path.resolve(positional[0]);
  options.outputFile = positional[1] ? path.resolve(positional[1]) : null;
  return options;
}

function collectNitroFiles(inputDir) {
  const files = [];
  const stack = [inputDir];
  while (stack.length) {
    const current = stack.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(fullPath);
      } else if (entry.isFile() && entry.name.toLowerCase().endsWith('.nitro')) {
        files.push(fullPath);
      }
    }
  }
  return files.sort((a, b) => a.localeCompare(b));
}

function batchWorkDir(baseWorkDir, inputDir, inputFile) {
  if (!baseWorkDir) return null;
  const relative = path.relative(inputDir, inputFile).replace(/\.nitro$/i, '');
  return path.join(baseWorkDir, safeFilename(relative));
}

function batchDebugDir(baseDebugDir, inputDir, inputFile) {
  if (!baseDebugDir) return null;
  const relative = path.relative(inputDir, inputFile).replace(/\.nitro$/i, '');
  return path.join(baseDebugDir, safeFilename(relative));
}

function outputForBatch(options, inputDir, inputFile) {
  if (options.deploySkipOutput || !options.outputFile) return null;
  const relative = path.relative(inputDir, inputFile).replace(/\.nitro$/i, '.swf');
  return path.join(options.outputFile, relative);
}

async function runSingle(options, logger) {
  const outputFile = options.outputFile
    || (options.deploySkipOutput || options.dryRun
      ? null
      : path.resolve(path.dirname(options.inputFile), `${path.basename(options.inputFile, path.extname(options.inputFile))}.swf`));
  await convertNitroToSwf({ ...options, outputFile, logger });
}

async function runDirectory(options, logger) {
  if (options.outputFile && fs.existsSync(options.outputFile) && !fs.statSync(options.outputFile).isDirectory()) {
    throw new ConverterError('Directory input requires an output directory, not an output file.', { exitCode: 2 });
  }
  if (!options.outputFile && !options.deploySkipOutput && !options.dryRun) {
    throw new ConverterError('Directory input requires an output directory unless --deploy-skip-output is used.', { exitCode: 2 });
  }

  const files = collectNitroFiles(options.inputFile);
  if (!files.length) throw new ConverterError(`No .nitro files found in ${options.inputFile}.`);

  let failed = 0;
  let completed = 0;
  for (let index = 0; index < files.length; index += 1) {
    const inputFile = files[index];
    const relative = path.relative(options.inputFile, inputFile);
    logger.info(`[${index + 1}/${files.length}] ${relative}`);
    try {
      await convertNitroToSwf({
        ...options,
        inputFile,
        outputFile: outputForBatch(options, options.inputFile, inputFile),
        workDir: batchWorkDir(options.workDir, options.inputFile, inputFile),
        debugDir: batchDebugDir(options.debugDir, options.inputFile, inputFile),
        logger
      });
      completed += 1;
    } catch (error) {
      failed += 1;
      const message = error && error.message ? error.message : String(error);
      logger.warn(`Failed ${relative}: ${message}`);
      if (error && error.details && options.verbose) {
        for (const line of error.details) logger.warn(line);
      }
    }
  }

  logger.info(`Processed ${files.length} Nitro file(s): ${completed} completed, ${failed} failed.`);
  if (failed) throw new ConverterError(`${failed} Nitro file(s) failed in directory mode.`);
}

async function runCli(argv) {
  const options = parseArgs(argv);
  if (options.help) {
    console.log(usage());
    return;
  }
  const logger = new Logger({ verbose: options.verbose, quiet: options.quiet });
  const stat = fs.statSync(options.inputFile);
  if (stat.isDirectory()) {
    await runDirectory(options, logger);
  } else {
    await runSingle(options, logger);
  }
}

module.exports = {
  runCli,
  parseArgs,
  usage
};
