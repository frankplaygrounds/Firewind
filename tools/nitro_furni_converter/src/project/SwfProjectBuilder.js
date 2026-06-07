const path = require('path');
const { AtlasExporter } = require('../images/AtlasExporter');
const {
  assetXml,
  indexXml,
  logicXml,
  manifestXml,
  visualizationXml
} = require('../furniture/XmlBuilder');
const {
  ensureDir,
  safeClassName,
  safeFilename,
  toLatin1Buffer,
  writeBuffer,
  writeText
} = require('../utils');

function asClass(className, baseClass, embedSource, mimeType) {
  return `package
{
    import ${baseClass};

    [Embed(source="${embedSource}", mimeType="${mimeType}")]
    public class ${className} extends ${baseClass.split('.').pop()}
    {
    }
}
`;
}

class SwfProjectBuilder {
  constructor({ logger }) {
    this.logger = logger;
  }

  build({ workDir, descriptor, atlas, plan }) {
    const rootClassName = safeClassName(descriptor.name);
    const srcDir = path.join(workDir, 'src');
    const dataDir = path.join(workDir, 'data');
    ensureDir(srcDir);
    ensureDir(dataDir);

    const exporter = new AtlasExporter(atlas);
    const framesForFiles = plan.frames.map((frame) => ({
      ...frame,
      fileBase: safeFilename(frame.name)
    }));
    const exportedFrames = exporter.exportFrames(framesForFiles, dataDir);

    const binaries = [
      {
        className: `${rootClassName}_manifest`,
        file: `${rootClassName}_manifest.bin`,
        data: manifestXml(descriptor, plan),
        rootProperty: 'manifest'
      },
      {
        className: `${rootClassName}_index`,
        file: `${rootClassName}_index.bin`,
        data: indexXml(descriptor),
        rootProperty: 'index'
      },
      {
        className: `${rootClassName}_${rootClassName}_assets`,
        file: `${rootClassName}_assets.bin`,
        data: assetXml(plan),
        rootProperty: `${rootClassName}_assets`
      },
      {
        className: `${rootClassName}_${rootClassName}_logic`,
        file: `${rootClassName}_logic.bin`,
        data: logicXml(descriptor),
        rootProperty: `${rootClassName}_logic`
      },
      {
        className: `${rootClassName}_${rootClassName}_visualization`,
        file: `${rootClassName}_visualization.bin`,
        data: visualizationXml(descriptor, plan),
        rootProperty: `${rootClassName}_visualization`
      }
    ];

    for (const binary of binaries) {
      writeBuffer(path.join(dataDir, binary.file), toLatin1Buffer(binary.data));
      writeText(
        path.join(srcDir, `${binary.className}.as`),
        asClass(
          binary.className,
          'mx.core.ByteArrayAsset',
          `../data/${binary.file}`,
          'application/octet-stream'
        )
      );
    }

    for (const frame of exportedFrames) {
      const className = `${rootClassName}_${safeClassName(frame.name)}`;
      writeText(
        path.join(srcDir, `${className}.as`),
        asClass(className, 'mx.core.BitmapAsset', `../data/${frame.fileBase}.png`, 'image/png')
      );
    }

    const rootLines = [
      'package',
      '{',
      '    import flash.display.Sprite;',
      '',
      `    public class ${rootClassName} extends Sprite`,
      '    {'
    ];

    for (const binary of binaries) {
      rootLines.push(`        public static const ${binary.rootProperty}:Class = ${binary.className};`);
    }
    for (const frame of exportedFrames) {
      rootLines.push(`        public static var ${safeClassName(frame.name)}:Class = ${rootClassName}_${safeClassName(frame.name)};`);
    }
    rootLines.push('');
    rootLines.push(`        public function ${rootClassName}()`);
    rootLines.push('        {');
    rootLines.push('        }');
    rootLines.push('    }');
    rootLines.push('}');

    const mainFile = path.join(srcDir, `${rootClassName}.as`);
    writeText(mainFile, `${rootLines.join('\n')}\n`);

    this.logger.verbose(`Generated AS3 project at ${workDir}`);
    return {
      workDir,
      srcDir,
      dataDir,
      mainFile,
      rootClassName,
      binaries,
      exportedFrames
    };
  }
}

module.exports = { SwfProjectBuilder };
