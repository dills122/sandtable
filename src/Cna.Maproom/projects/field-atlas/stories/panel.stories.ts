import { AtlasPanel } from '@sandtable/field-atlas';
import type { Meta, StoryObj } from '@storybook/angular-vite';
const meta: Meta<AtlasPanel> = {
  title: 'Components/Panel',
  component: AtlasPanel,
  tags: ['autodocs'],
  args: { heading: 'Order inspector', eyebrow: 'Selected formation' },
  parameters: {
    docs: {
      description: {
        component:
          'A bordered section with a heading and optional actions slot. Keeps spacing consistent in the workspace. Heading uses h2; compose beneath the page h1.',
      },
    },
  },
};
export default meta;
export const Default: StoryObj<AtlasPanel> = {
  render: (args) => ({
    props: args,
    template: `<fa-panel [heading]="heading" [eyebrow]="eyebrow"><p>Inspect the selected formation before preparing a draft.</p></fa-panel>`,
  }),
};
export const WithAction: StoryObj<AtlasPanel> = {
  render: (args) => ({
    props: args,
    template: `<fa-panel [heading]="heading" [eyebrow]="eyebrow"><button panel-actions class="fa-text-action">Locate on map</button><p>Secondary actions share the section header.</p></fa-panel>`,
  }),
};
