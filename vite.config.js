import { defineConfig } from 'vite';
import { resolve } from 'path';

const root = resolve(__dirname, '0 - Apresentacao/Sistema.MVC');
const wwwroot = resolve(root, 'wwwroot');

export default defineConfig({
  build: {
    outDir: resolve(wwwroot, 'dist'),
    emptyOutDir: true,
    rollupOptions: {
      input: {
        site: resolve(wwwroot, 'js/site.js'),
        login: resolve(wwwroot, 'js/login.js')
      },
      output: {
        entryFileNames: '[name].js',
        assetFileNames: '[name][extname]'
      }
    }
  }
});
