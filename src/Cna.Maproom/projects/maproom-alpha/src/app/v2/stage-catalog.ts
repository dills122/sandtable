/**
 * Maps `/opening-kinds` enum names to the stage strings `/openings` and `/actions` accept.
 * The mapping is not derivable: the opening and action stage for Retreat Before Assault differ
 * (`retreat-before-assault` vs `rba-decline`), and SourceAxisPriority is `source-axis-priority-motorization`.
 * Source: `CampaignGrain.QueryOwnerOpeningAsync` / `SubmitOwnerActionAsync` and
 * `CampaignDurableOutwardApi.RequireKnownSourceElement`. No automated drift check spans the .NET boundary; unmapped kinds from `/opening-kinds` surface new ones.
 *
 * The renderer never branches on this table: it only decides which request to send. Unknown kinds
 * from a newer Host are listed as unmapped and cannot be opened from this client.
 */
export type ElementRule =
  /** Caller supplies the acting unit id (movement, combat and similar per-unit stages). */
  | { readonly kind: 'unit' }
  /** Seat-level decision; the Host ignores the element, but the route requires a non-empty value. */
  | { readonly kind: 'seat' }
  /** Setup stage with one fixed element key the Host validates. */
  | { readonly kind: 'fixed'; readonly id: string };

export interface StageEntry {
  readonly kind: string;
  readonly openingStage: string;
  readonly actionStage: string;
  readonly category: 'gameplay' | 'setup';
  readonly title: string;
  readonly element: ElementRule;
}

const unit: ElementRule = { kind: 'unit' };
const seat: ElementRule = { kind: 'seat' };
const g = (
  kind: string,
  stage: string,
  title: string,
  element: ElementRule,
  actionStage = stage,
): StageEntry => ({
  kind,
  openingStage: stage,
  actionStage,
  category: 'gameplay',
  title,
  element,
});
const s = (kind: string, stage: string, title: string, id: string): StageEntry => ({
  kind,
  openingStage: stage,
  actionStage: stage,
  category: 'setup',
  title,
  element: { kind: 'fixed', id },
});

export const STAGE_CATALOG: readonly StageEntry[] = [
  g('Movement', 'movement', 'Movement', unit),
  g('InitiativeOrder', 'initiative-order', 'Initiative order', seat),
  g('ReserveDesignation', 'reserve-designation', 'Reserve designation', seat),
  g('Reaction', 'reaction', 'Reaction', seat),
  g(
    'MovementSegmentCompletion',
    'movement-segment-completion',
    'Movement segment completion',
    seat,
  ),
  g('CombatSelection', 'combat-selection', 'Combat selection', unit),
  g(
    'RetreatBeforeAssault',
    'retreat-before-assault',
    'Retreat before assault',
    unit,
    'rba-decline',
  ),
  g('ForceAssignment', 'force-assignment', 'Force assignment', unit),
  g('PostAssaultRetreat', 'post-assault-retreat', 'Post-assault retreat', unit),
  g('Custody', 'custody', 'Custody', unit),
  g('ReserveRelease', 'reserve-release', 'Reserve release', unit),
  g('CycleControl', 'cycle-control', 'Cycle control', seat),
  g('TruckConvoyPass', 'truck-convoy-pass', 'Truck convoy pass', seat),
  g('PostCombatPass', 'post-combat-pass', 'Post-combat pass', seat),
  g('SecondReserve', 'second-reserve', 'Second reserve', seat),
  g('SecondMovement', 'second-movement', 'Second movement', unit),
  g('SecondMovementCompletion', 'second-movement-completion', 'Second movement completion', seat),
  g(
    'StageEntryInitiativeDeclaration',
    'stage-entry-initiative-declaration',
    'Stage-entry initiative declaration',
    seat,
  ),
  g('RepeatMovement', 'repeat-movement', 'Repeat movement', seat),
  g('RepeatCombatSelection', 'repeat-combat-selection', 'Repeat combat selection', unit),
  s('PregameAxisSupply', 'pregame-axis-supply', 'Pregame Axis supply', 'land.pregame-axis-supply'),
  s(
    'PregameAxisSupplyFinish',
    'pregame-axis-supply-finish',
    'Pregame Axis supply finish',
    'land.pregame-axis-supply-finish',
  ),
  s('SourceAxisDump', 'source-axis-dump', 'Axis dumps', 'land.graziani-axis-dumps'),
  s(
    'SourceCommonwealthSupply',
    'source-commonwealth-supply',
    'Commonwealth supply',
    'land.graziani-commonwealth-supply',
  ),
  s('SourceDummy', 'source-dummy', 'Dummy groups', 'land.graziani-dummy-groups'),
  s('SourceMotorization', 'source-motorization', 'Motorization', 'land.graziani-motorization'),
  s(
    'SourceUnattachedCargo',
    'source-unattached-cargo',
    'Unattached cargo',
    'land.graziani-unattached-cargo',
  ),
  s('SourceActions', 'source-actions', 'Source actions', 'land.graziani-source-actions'),
  s(
    'SourceAxisGt1Supply',
    'source-axis-gt1-supply',
    'Axis GT1 supply',
    'land.graziani-axis-gt1-supply',
  ),
  s(
    'SourceCommonwealthSupplyPlan',
    'source-commonwealth-supply-plan',
    'Commonwealth supply plan',
    'land.graziani-commonwealth-supply-plan',
  ),
  s('SourceAutoblinda', 'source-autoblinda', 'Autoblinda', 'land.graziani-axis-autoblinda'),
  s(
    'SourcePriorityMotorization',
    'source-priority-motorization',
    'Commonwealth priority motorization',
    'land.graziani-commonwealth-priority-motorization',
  ),
  s(
    'SourceAttachedInitialLoads',
    'source-attached-initial-loads',
    'Attached initial loads',
    'land.graziani-commonwealth-attached-initial-loads',
  ),
  s(
    'SourceInitialWrecks',
    'source-initial-wrecks',
    'Initial wrecks',
    'land.graziani-commonwealth-initial-wrecks',
  ),
  s(
    'SourceAxisFreeSetup',
    'source-axis-free-setup',
    'Axis free setup',
    'land.graziani-axis-free-setup',
  ),
  s(
    'SourceAxisCorpsSetup',
    'source-axis-corps-setup',
    'Axis corps setup',
    'land.graziani-axis-corps-setup',
  ),
  s(
    'SourceMalettiHierarchy',
    'source-maletti-hierarchy',
    'Maletti hierarchy',
    'land.graziani-axis-maletti-hierarchy',
  ),
  s(
    'SourceGt2Correction',
    'source-gt2-correction',
    'GT2 correction',
    'land.graziani-axis-gt2-correction',
  ),
  s(
    'SourceCwPriorityCatalog',
    'source-cw-priority-catalog',
    'Commonwealth priority catalog',
    'land.graziani-commonwealth-priority-catalog',
  ),
  s(
    'SourceMatruhFormation',
    'source-matruh-formation',
    'Matruh formation',
    'land.graziani-commonwealth-matruh-formation',
  ),
  s(
    'SourceCwFreeGuns',
    'source-cw-free-guns',
    'Commonwealth free guns',
    'land.graziani-commonwealth-free-guns',
  ),
  s(
    'SourceAxisPriority',
    'source-axis-priority-motorization',
    'Axis priority motorization',
    'land.graziani-axis-priority-motorization',
  ),
  s(
    'SourceCairoSetup',
    'source-cairo-setup',
    'Cairo setup',
    'land.graziani-commonwealth-cairo-setup',
  ),
];

const byKind = new Map(STAGE_CATALOG.map((e) => [e.kind, e]));
const byOpeningStage = new Map(STAGE_CATALOG.map((e) => [e.openingStage, e]));

export function stageForKind(kind: string): StageEntry | undefined {
  return byKind.get(kind);
}
export function stageForOpeningStage(stage: string): StageEntry | undefined {
  return byOpeningStage.get(stage);
}

/** Splits Host-reported kinds into ones this client can open and ones from a newer Host. */
export function partitionKinds(kinds: readonly string[]): {
  known: StageEntry[];
  unmapped: string[];
} {
  const known: StageEntry[] = [];
  const unmapped: string[] = [];
  for (const kind of kinds) {
    const entry = byKind.get(kind);
    if (entry) known.push(entry);
    else unmapped.push(kind);
  }
  return { known, unmapped };
}

/** The element id to send for a stage. Unit stages need a caller-supplied unit id. */
export function elementFor(entry: StageEntry, unitId: string | undefined): string {
  switch (entry.element.kind) {
    case 'fixed':
      return entry.element.id;
    case 'seat':
      return entry.openingStage;
    case 'unit':
      if (!unitId) throw new Error(`${entry.title} needs an acting unit id`);
      return unitId;
  }
}
