const fs = require('fs');
const os = require('os');
const path = require('path');
const { NitroReader } = require('./nitro/NitroReader');
const { AssetPlanner } = require('./furniture/AssetPlanner');
const { SwfProjectBuilder } = require('./project/SwfProjectBuilder');
const { MxmlcCompiler } = require('./compiler/MxmlcCompiler');
const { buildFurnitureMetadataVariants } = require('./deploy/FurnitureMetadata');
const { CatalogDeployer } = require('./deploy/CatalogDeployer');
const { GamedataUpdater } = require('./deploy/GamedataUpdater');
const { GraphicTagCleaner } = require('./swf/GraphicTagCleaner');
const { Verifier } = require('./verify/Verifier');
const { ConverterError } = require('./errors');
const { ensureDir, removeDir, safeFilename, writeText } = require('./utils');

function defaultWorkDir(inputFile) {
  const base = safeFilename(path.basename(inputFile, path.extname(inputFile)));
  return path.join(os.tmpdir(), `nitro-furni-converter-${base}-${Date.now()}`);
}

function writeDebugReport(debugDir, nitro, plan, project) {
  ensureDir(debugDir);
  const report = {
    input: nitro.inputPath,
    descriptorName: nitro.descriptor.name,
    nitroEntries: nitro.entries,
    assetsInXml: plan.assets.length,
    framesExported: plan.frames.length,
    generatedDirect: plan.generatedDirect,
    generatedAliases: plan.generatedAliases,
    warnings: plan.warnings,
    workDir: project.workDir
  };
  writeText(path.join(debugDir, 'conversion-report.json'), `${JSON.stringify(report, null, 2)}\n`);
}

async function convertNitroToSwf(options) {
  const {
    inputFile,
    outputFile,
    airHome = null,
    dryRun = false,
    keepTemp = false,
    workDir = null,
    debugDir = null,
    skipCleanup = false,
    skipVerify = false,
    deploy = false,
    deploySkipOutput = false,
    deployOptions = {},
    logger
  } = options;

  if (!inputFile) throw new ConverterError('Input .nitro file is required.', { exitCode: 2 });
  if (!fs.existsSync(inputFile)) throw new ConverterError(`Input file does not exist: ${inputFile}`);
  if (path.extname(inputFile).toLowerCase() !== '.nitro') {
    throw new ConverterError(`Input file must have a .nitro extension: ${inputFile}`);
  }
  if (!outputFile && !dryRun && !deploySkipOutput) throw new ConverterError('Output .swf file is required.', { exitCode: 2 });
  if (deploySkipOutput && !deploy) throw new ConverterError('--deploy-skip-output requires deployment.', { exitCode: 2 });

  const tempDir = workDir ? path.resolve(workDir) : defaultWorkDir(inputFile);
  let project = null;

  try {
    logger.step(`Reading ${inputFile}`);
    const nitro = new NitroReader().read(inputFile);
    logger.verbose(`Nitro entries: ${nitro.entries.map((entry) => entry.name).join(', ')}`);

    logger.step(`Planning assets for ${nitro.descriptor.name}`);
    const plan = new AssetPlanner().plan(nitro.descriptor);
    logger.info(`Assets: ${plan.assets.length}; PNG frames: ${plan.frames.length}; generated zoom assets: ${plan.generatedDirect.length} bitmap, ${plan.generatedAliases.length} alias.`);
    for (const warning of plan.warnings) logger.warn(warning);
    if (!plan.hasGeneratedZoom) logger.info('No missing zoomed-out assets were detected.');

    removeDir(tempDir);
    ensureDir(tempDir);
    logger.step(`Writing temporary AS3 project`);
    project = new SwfProjectBuilder({ logger }).build({
      workDir: tempDir,
      descriptor: nitro.descriptor,
      atlas: nitro.atlas,
      plan
    });

    if (debugDir) writeDebugReport(path.resolve(debugDir), nitro, plan, project);
    if (dryRun) {
      logger.info(`Dry run complete. Generated project: ${project.workDir}`);
      return { nitro, plan, project, outputFile: null, dryRun: true };
    }

    const resolvedOutput = deploySkipOutput
      ? path.join(tempDir, 'compiled', `${safeFilename(nitro.descriptor.name)}.swf`)
      : path.resolve(outputFile);
    ensureDir(path.dirname(resolvedOutput));
    new MxmlcCompiler({ airHome, logger }).compile(project, resolvedOutput);

    if (!skipCleanup) {
      logger.step('Cleaning graphical tags from final SWF');
      new GraphicTagCleaner({ logger }).cleanFile(resolvedOutput, resolvedOutput);
    } else {
      logger.warn('Skipped graphical tag cleanup by request.');
    }

    if (!skipVerify) {
      logger.step('Verifying final SWF');
      new Verifier({ logger }).verify({
        swfFile: resolvedOutput,
        plan,
        expectGraphicsClean: !skipCleanup
      });
    }

    let deployment = null;
    if (deploy) {
      logger.step('Deploying furniture to Firewind web/catalog');
      const metadataList = buildFurnitureMetadataVariants(nitro.descriptor, deployOptions.metadata || {});
      if (metadataList.length > 1) {
        logger.step(`Detected ${metadataList.length} furniture variation entries: ${metadataList.map((entry) => entry.className).join(', ')}`);
      }
      const dbResult = deployOptions.skipDb
        ? null
        : new CatalogDeployer({
          logger,
          mysqlOptions: deployOptions.mysql || {}
        }).deployMany(metadataList, {
          pageCaption: deployOptions.pageCaption || 'Recently Added',
          parentId: deployOptions.parentId === undefined ? -1 : deployOptions.parentId
        });

      const entries = metadataList.map((metadata, index) => {
        const spriteId = metadata.spriteId || (dbResult && dbResult[index] && dbResult[index].spriteId);
        if (!spriteId) {
          throw new ConverterError('Deployment without DB requires --sprite-id so furnidata can reference the SWF.');
        }
        return { metadata, spriteId };
      });

      const gamedataResult = deployOptions.skipGamedata
        ? null
        : new GamedataUpdater({ logger }).updateMany({
          swfFile: resolvedOutput,
          entries,
          webRoot: deployOptions.webRoot,
          furnidataFile: deployOptions.furnidataFile,
          productdataFile: deployOptions.productdataFile,
          dcrDir: deployOptions.dcrDir,
          backup: deployOptions.backupGamedata !== false
        });

      deployment = { metadata: metadataList, db: dbResult, gamedata: gamedataResult };
    }

    if (deploySkipOutput) {
      logger.step(`Deployed ${nitro.descriptor.name}; skipped persistent output SWF`);
    } else {
      logger.step(`Wrote ${resolvedOutput}`);
    }
    return {
      nitro,
      plan,
      project,
      outputFile: deploySkipOutput ? null : resolvedOutput,
      temporaryOutputFile: deploySkipOutput ? resolvedOutput : null,
      dryRun: false,
      deployment
    };
  } finally {
    if (project && !keepTemp && !dryRun) removeDir(tempDir);
  }
}

module.exports = { convertNitroToSwf };
