#!/usr/bin/env node

const { runCli } = require('./src/cli');

runCli(process.argv).catch((error) => {
  const message = error && error.message ? error.message : String(error);
  console.error(`nitro-furni-converter: error: ${message}`);
  if (error && error.details) {
    for (const line of error.details) console.error(`  ${line}`);
  }
  process.exitCode = error && Number.isInteger(error.exitCode) ? error.exitCode : 1;
});
