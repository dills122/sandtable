import { type Meta, type StoryObj } from '@storybook/angular-vite';
import { OpeningForm } from './opening-form';
import { OpeningView } from './types';

const opening = (
  stage: string,
  visibleCut: OpeningView['visibleCut'],
  refs: string[],
): OpeningView => ({
  campaignId: 'campaign-story',
  audience: 'axis',
  stage,
  actingElementId: stage,
  revision: 1,
  visibleCut,
  publicActionRefs: refs,
});

const meta: Meta<OpeningForm> = {
  title: 'Patterns/Opening form',
  component: OpeningForm,
  tags: ['autodocs'],
  args: { heading: 'Initiative order', eyebrow: 'Axis seat', busy: false, error: '' },
  parameters: {
    docs: {
      description: {
        component:
          'Generic form for any v2 owner opening. Reads only the visible cut and action refs, so a new opening kind needs no new screen. Never shows refs; emits the chosen ref as intent.',
      },
    },
  },
};
export default meta;
type Story = StoryObj<OpeningForm>;
export const StringChoices: Story = {
  args: {
    opening: opening('initiative-order', { positionId: 'p1', choices: ['act-first', 'act-last'] }, [
      'r1',
      'r2',
    ]),
  },
};
export const ObjectOffers: Story = {
  args: {
    heading: 'Movement',
    opening: opening(
      'movement',
      {
        movementOffers: [
          {
            elementId: 'axis-infantry',
            originLocationId: 'A',
            destinationLocationId: 'B',
            totalCostNumerator: 3,
            totalCostDenominator: 2,
          },
          {
            elementId: 'axis-infantry',
            originLocationId: 'A',
            destinationLocationId: 'C',
            totalCostNumerator: 1,
            totalCostDenominator: 1,
          },
        ],
        weather: { sandstorm: false },
      },
      ['m1', 'm2'],
    ),
  },
};
export const NoChoices: Story = {
  args: { opening: opening('post-combat-pass', { choices: [] }, []) },
};
export const RuleNotImplemented: Story = {
  args: {
    ...StringChoices.args,
    outcome: { kind: 'rejected', code: 'rule-not-implemented', requestId: undefined },
  },
};
