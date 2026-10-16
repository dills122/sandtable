import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular-vite';
import { DraftForm } from './draft-form';
import { provideMaproomForms } from './forms';
const meta: Meta<DraftForm> = {
  title: 'Patterns/Draft form',
  component: DraftForm,
  tags: ['autodocs'],
  decorators: [applicationConfig({ providers: [provideMaproomForms()] })],
  args: { formation: 'Fox group', disabled: false },
  parameters: {
    docs: {
      description: {
        component:
          'App-owned Formly/Material specimen. Validates a destination and reviews a local draft; never sends an order. Dynamic values pass through an explicit typed adapter.',
      },
    },
  },
};
export default meta;
export const Editable: StoryObj<DraftForm> = {};
export const Disabled: StoryObj<DraftForm> = { args: { disabled: true } };
