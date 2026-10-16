import type { Preview } from '@storybook/angular-vite';
const preview: Preview = {
  parameters: {
    layout: 'padded',
    a11y: { test: 'error' },
    options: {
      storySort: {
        order: ['Start here', 'Foundations', 'Material', 'Components', 'Patterns', 'Workspace'],
      },
    },
    controls: { expanded: true },
  },
};
export default preview;
