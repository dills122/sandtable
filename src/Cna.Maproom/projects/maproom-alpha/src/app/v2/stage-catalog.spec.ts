import { describe, expect, it } from 'vitest';
import {
  STAGE_CATALOG,
  elementFor,
  partitionKinds,
  stageForKind,
  stageForOpeningStage,
} from './stage-catalog';

describe('stage catalog', () => {
  it('has unique kinds and stage strings', () => {
    expect(new Set(STAGE_CATALOG.map((e) => e.kind)).size).toBe(STAGE_CATALOG.length);
    expect(new Set(STAGE_CATALOG.map((e) => e.openingStage)).size).toBe(STAGE_CATALOG.length);
  });

  it('covers all 43 Host opening kinds, 20 gameplay and 23 setup', () => {
    expect(STAGE_CATALOG).toHaveLength(43);
    expect(STAGE_CATALOG.filter((e) => e.category === 'gameplay')).toHaveLength(20);
  });

  it('keeps the asymmetric opening/action stage names', () => {
    expect(stageForKind('RetreatBeforeAssault')).toMatchObject({
      openingStage: 'retreat-before-assault',
      actionStage: 'rba-decline',
    });
    expect(stageForKind('SourceAxisPriority')?.actionStage).toBe(
      'source-axis-priority-motorization',
    );
    expect(stageForOpeningStage('movement')?.kind).toBe('Movement');
  });

  it('reports kinds from a newer Host as unmapped instead of guessing', () => {
    const { known, unmapped } = partitionKinds(['Movement', 'BrandNewKind']);
    expect(known.map((e) => e.kind)).toEqual(['Movement']);
    expect(unmapped).toEqual(['BrandNewKind']);
  });

  it('chooses the acting element by rule', () => {
    expect(elementFor(stageForKind('SourceCairoSetup')!, undefined)).toBe(
      'land.graziani-commonwealth-cairo-setup',
    );
    expect(elementFor(stageForKind('InitiativeOrder')!, undefined)).toBe('initiative-order');
    expect(elementFor(stageForKind('Movement')!, 'axis-infantry')).toBe('axis-infantry');
    expect(() => elementFor(stageForKind('Movement')!, undefined)).toThrow();
  });
});
