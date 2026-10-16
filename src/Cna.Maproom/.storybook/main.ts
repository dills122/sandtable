import { resolve } from 'node:path';
import type { StorybookConfig } from '@storybook/angular-vite';
const config: StorybookConfig = {
  stories: [
    '../projects/field-atlas/stories/**/*.mdx',
    '../projects/field-atlas/stories/**/*.stories.ts',
    '../projects/maproom-alpha/src/**/*.stories.ts',
  ],
  addons: ['@storybook/addon-docs', '@storybook/addon-a11y'],
  framework: { name: '@storybook/angular-vite', options: {} },
  core: { disableTelemetry: true },
  async viteFinal(config) {
    const { mergeConfig } = await import('vite');
    return mergeConfig(config, {
      optimizeDeps: { include: ['@lucide/angular'] },
      server: {
        watch: { ignored: ['**/playwright-report/**', '**/test-results/**', '**/dist/**'] },
      },
      resolve: {
        alias: { '@sandtable/field-atlas': resolve('projects/field-atlas/src/public-api.ts') },
      },
    });
  },
};
export default config;
