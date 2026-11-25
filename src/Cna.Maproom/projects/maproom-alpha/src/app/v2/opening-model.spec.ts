import { describe, expect, it } from 'vitest';
import { describeOpening, humanize } from './opening-model';
import { JsonValue, OpeningView } from './types';

const view = (visibleCut: JsonValue, refs: string[]): OpeningView => ({
  campaignId: 'c1',
  audience: 'axis',
  stage: 'x',
  actingElementId: 'x',
  revision: 1,
  visibleCut,
  publicActionRefs: refs,
});

describe('describeOpening', () => {
  it('labels string choices from the cut and keeps refs positional', () => {
    const d = describeOpening(
      view(
        {
          contractVersion: 2,
          campaignId: 'c1',
          positionId: 'p1',
          choices: ['act-first', 'act-last'],
        },
        ['r1', 'r2'],
      ),
    );
    expect(d.choices.map((c) => [c.ref, c.label])).toEqual([
      ['r1', 'act-first'],
      ['r2', 'act-last'],
    ]);
    expect(d.aligned).toBe(true);
    expect(d.facts).toEqual([{ label: 'Position id', value: 'p1' }]);
  });

  it('summarises object offers and folds numerator/denominator', () => {
    const d = describeOpening(
      view(
        {
          turn: 1,
          movementOffers: [
            {
              elementId: 'u1',
              originLocationId: 'A',
              destinationLocationId: 'B',
              totalCostNumerator: 3,
              totalCostDenominator: 2,
            },
            {
              elementId: 'u1',
              originLocationId: 'A',
              destinationLocationId: 'C',
              totalCostNumerator: 1,
              totalCostDenominator: 1,
            },
          ],
        },
        ['r1', 'r2'],
      ),
    );
    expect(d.choices[0].label).toBe(
      'Element id u1 · Origin location id A · Destination location id B · Total cost 3/2',
    );
    expect(d.choices[0].details).toContainEqual({ label: 'Total cost', value: '3/2' });
    expect(d.facts).toEqual([{ label: 'Turn', value: '1' }]);
  });

  it('prefers a label field on object choices', () => {
    const d = describeOpening(
      view(
        {
          options: [
            { label: 'Hold', cost: 1 },
            { label: 'Advance', cost: 2 },
          ],
        },
        ['a', 'b'],
      ),
    );
    expect(d.choices.map((c) => c.label)).toEqual(['Hold', 'Advance']);
  });

  it('falls back to Option N without dropping any ref when nothing aligns', () => {
    const d = describeOpening(view({ note: 'x', other: [1, 2, 3] }, ['a', 'b']));
    expect(d.aligned).toBe(false);
    expect(d.choices.map((c) => [c.ref, c.label])).toEqual([
      ['a', 'Option 1'],
      ['b', 'Option 2'],
    ]);
    expect(d.facts.find((f) => f.label === 'Other')?.items).toEqual(['1', '2', '3']);
  });

  it('does not guess between two equally plausible arrays', () => {
    const d = describeOpening(view({ left: ['a', 'b'], right: ['c', 'd'] }, ['r1', 'r2']));
    expect(d.aligned).toBe(false);
  });

  it('handles a non-object cut and an opening with no legal choices', () => {
    expect(describeOpening(view('plain', ['r1'])).facts).toEqual([
      { label: 'Visible state', value: 'plain' },
    ]);
    const empty = describeOpening(view({ choices: [] }, []));
    expect(empty.choices).toEqual([]);
    expect(empty.aligned).toBe(true);
  });

  it('renders nested structures and bounds pathological depth', () => {
    const d = describeOpening(view({ a: { b: { c: true, d: null } } }, []));
    expect(d.facts[0].children?.[0].children).toEqual([
      { label: 'C', value: 'Yes' },
      { label: 'D', value: 'None' },
    ]);
    let deep: JsonValue = 'leaf';
    for (let i = 0; i < 20; i++) deep = { k: deep };
    expect(describeOpening(view(deep, [])).truncated).toBe(true);
  });

  it('humanizes keys', () => {
    expect(humanize('originLocationId')).toBe('Origin location id');
    expect(humanize('act_first-now')).toBe('Act first now');
  });
});
