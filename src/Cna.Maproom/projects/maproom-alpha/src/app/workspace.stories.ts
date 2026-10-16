import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular-vite';
import { Workspace } from './workspace';
import { provideMaproomForms } from './forms';
export default {
  title: 'Workspace/Operations shell',
  component: Workspace,
  decorators: [applicationConfig({ providers: [provideMaproomForms()] })],
  parameters: { layout: 'fullscreen' },
} satisfies Meta<Workspace>;
export const SyntheticExercise: StoryObj<Workspace> = {};
