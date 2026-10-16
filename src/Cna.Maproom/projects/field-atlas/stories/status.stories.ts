import { AtlasStatus } from '@sandtable/field-atlas';
import type { Meta, StoryObj } from '@storybook/angular-vite';
const meta: Meta<AtlasStatus> = {
  title: 'Components/Status',
  component: AtlasStatus,
  tags: ['autodocs'],
  args: { tone: 'neutral' },
  argTypes: { tone: { control: 'select', options: ['neutral', 'success', 'warning', 'danger'] } },
  render: (args) => ({
    props: args,
    template: `<fa-status [tone]="tone">Connection status</fa-status>`,
  }),
};
export default meta;
export const Neutral: StoryObj<AtlasStatus> = {};
export const States: StoryObj<AtlasStatus> = {
  render: () => ({
    template: `<div class="fa-stack"><fa-status>Local fixture</fa-status><fa-status tone="success">Draft reviewed</fa-status><fa-status tone="warning">Disconnected</fa-status><fa-status tone="danger">Order rejected</fa-status></div>`,
  }),
};
