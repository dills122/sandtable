import { AtlasUnitCounter } from '@sandtable/field-atlas';
import type { Meta, StoryObj } from '@storybook/angular-vite';
const meta: Meta<AtlasUnitCounter> = {
  title: 'Components/Unit counter',
  component: AtlasUnitCounter,
  tags: ['autodocs'],
  args: {
    code: 'FOX',
    label: 'Fox group, infantry, K-7',
    kind: 'infantry',
    selected: false,
    disabled: false,
  },
  argTypes: { kind: { control: 'select', options: ['infantry', 'armour', 'recon'] } },
};
export default meta;
type Story = StoryObj<AtlasUnitCounter>;
export const Default: Story = {};
export const Selected: Story = { args: { selected: true } };
export const Disabled: Story = { args: { disabled: true } };
export const Armour: Story = {
  args: { code: 'HAR', kind: 'armour', label: 'Harrier group, armour, G-5' },
};
