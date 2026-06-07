class Logger {
  constructor({ verbose = false, quiet = false } = {}) {
    this.verboseEnabled = verbose;
    this.quiet = quiet;
  }

  info(message) {
    if (!this.quiet) console.log(message);
  }

  step(message) {
    this.info(`[pipeline] ${message}`);
  }

  warn(message) {
    if (!this.quiet) console.warn(`[warning] ${message}`);
  }

  verbose(message) {
    if (this.verboseEnabled && !this.quiet) console.log(`[debug] ${message}`);
  }
}

module.exports = { Logger };
