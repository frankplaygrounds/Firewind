class ConverterError extends Error {
  constructor(message, options = {}) {
    super(message);
    this.name = this.constructor.name;
    this.code = options.code || 'CONVERTER_ERROR';
    this.exitCode = options.exitCode || 1;
    this.details = options.details || null;
    if (options.cause) this.cause = options.cause;
  }
}

class NitroFormatError extends ConverterError {
  constructor(message, options = {}) {
    super(message, { ...options, code: 'NITRO_FORMAT_ERROR' });
  }
}

class UnsupportedStructureError extends ConverterError {
  constructor(message, options = {}) {
    super(message, { ...options, code: 'UNSUPPORTED_STRUCTURE' });
  }
}

class CompilationError extends ConverterError {
  constructor(message, options = {}) {
    super(message, { ...options, code: 'COMPILATION_ERROR' });
  }
}

class SwfFormatError extends ConverterError {
  constructor(message, options = {}) {
    super(message, { ...options, code: 'SWF_FORMAT_ERROR' });
  }
}

module.exports = {
  ConverterError,
  NitroFormatError,
  UnsupportedStructureError,
  CompilationError,
  SwfFormatError
};
