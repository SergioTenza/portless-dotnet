const esbuild = require('esbuild');

const isWatch = process.argv.includes('--watch');
const isProduction = !isWatch && process.argv.includes('--production');

const buildOptions = {
  entryPoints: ['src/extension.ts'],
  bundle: true,
  outfile: 'dist/extension.js',
  external: ['vscode'],
  format: 'cjs',
  platform: 'node',
  target: 'node18',
  minify: isProduction,
  sourcemap: !isProduction,
};

if (isWatch) {
  esbuild.context(buildOptions).then(ctx => {
    ctx.watch();
    console.log('Watching for changes...');
  });
} else {
  esbuild.build(buildOptions).then(() => {
    console.log('Build complete.');
  }).catch((err) => {
    console.error(err);
    process.exit(1);
  });
}
