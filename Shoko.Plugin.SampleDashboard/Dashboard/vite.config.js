import { defineConfig } from 'vite';

export default defineConfig({
  // Every URL in the built page is resolved against the path the plugin
  // serves it from.
  base: '/plugin/SampleDashboard/',
  build: {
    // The plugin's build passes its own folder, under obj/, so the output
    // never lands in the source tree.
    outDir: process.env.DASHBOARD_OUT_DIR || 'dist',
    emptyOutDir: true,
  },
  server: {
    // `pnpm dev` against a local server: the API calls go to Shoko.
    proxy: {
      '/api': 'http://localhost:8111',
    },
  },
});
