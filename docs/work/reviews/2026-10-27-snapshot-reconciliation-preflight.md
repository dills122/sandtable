# Snapshot evidence reconciliation preflight

Base: `38bf158dde122af8d54eeefba2d6c845e6da80a0` (merged PR334, all reported checks passed).
Branch: `codex/rc-snapshot-evidence`. Ruflo task: `task-1793144652408-bzjsxa`.
Status: user approved dependent refresh and one final review; implementation and focused validation in progress. Original preflight observations below remain historical evidence.

## Observed evidence

- `python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py`: exit1, declared source pin for `CampaignElementMovedV3Factory.cs`. Full log retained at `artifacts/rc-snapshot/baseline.log`.
- `just test-class core '*CombatInheritedSnapshotTests'`: exit0, 11 passed, zero failed/skipped. Includes build; test duration8.174s. Log `artifacts/rc-snapshot/native-baseline.log`.
- Both stale hashes match exact source bytes at `5c16fdb6`.
- Current event-runtime diff adds native Combat serializer/deserializer arms. Existing Snapshot serializer and projector suffix is byte-identical to pinned source. This is source comparison, not proof that every new event is supported by historical Snapshot12.
- Movement diff extracts a World-based cost overload (existing caller delegates with `prior.World`) and adds a fail-closed stacking guard. Current stacking authority has one battalion entry of value1. No gameplay change proposed here; affected native coverage remains to be selected for final validation.
- No source, pin, fixture, legacy registration or archive edited during preflight.

## Required scope

Two direct source changes fan out to six fixtures and at least44 structured-reference dependents. Candidate inventory below includes keyed and list-form references; this is not an exhaustive generated-identity/reader closure. Native manifests also pin dependent fixtures and schemas. Refreshing two root hashes alone would invalidate their readers; changing manifests can change generated identities. Existing versioned native revisions must retain old replay support. Outward's independent anchor failures remain separate and must not be silently rewritten or waived.

Proposed repair: freeze full hash/identity closure, retain original archives and P01 goldens, refresh only justified current-revision dependencies through owning generators, compare semantic payloads, run owning focused oracles/native consumers plus negative pin/mixed-revision checks. One new independent review requested because prior PR334 allowance8/8 is exhausted. No new reviewer launched.

## Source and archive hashes

```json
{
  "base": "38bf158dde122af8d54eeefba2d6c845e6da80a0",
  "sources": [
    {
      "path": "src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs",
      "pinnedCommit": "5c16fdb6",
      "oldSha256": "283102f40d891c1b901bd03e4a12499638bc4fb98ebbf2cb7644dae5b98c042a",
      "currentSha256": "5db23d19429d0e35474c386c1e6651be678982ab71ecedd61711378e0db93062"
    },
    {
      "path": "src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs",
      "pinnedCommit": "5c16fdb6",
      "oldSha256": "85bbff98008afad1d06dd6cb071cc8b26883e9f3ebcb4265df215beb8094f2c1",
      "currentSha256": "158c45673fed68bc2e2fd761a53be589f8663b35dca7ed7a98c6b5b447397246"
    }
  ],
  "preserved": {
    "docs/specs/fixtures/combat-evidence-legacy-oracle-support-v1.zip": "848d084745f0582f5f4ca39013f202774213df947b711069aa2476dbe0c3cf8c",
    "docs/specs/fixtures/combat-evidence-legacy-v1.zip": "3d7614b580ad5c82727170e4ff193e03f4931afbc7e7ed7b14d5eb49c45a6a98"
  },
  "unchangedCurrentSnapshotSerializerAndProjector": true
}
```

## Candidate dependency scope

- `docs/specs/combat-actual-assault-v1.schema.json`
- `docs/specs/combat-actual-moved-assault-v1.schema.json`
- `docs/specs/combat-actual-moved-continuation-v1.schema.json`
- `docs/specs/combat-actual-moved-cycle-control-v1.schema.json`
- `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json`
- `docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json`
- `docs/specs/combat-actual-moved-repeat-movement-v1.schema.json`
- `docs/specs/combat-actual-paid-assault-v1.schema.json`
- `docs/specs/combat-actual-post-assault-continuation-v1.schema.json`
- `docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json`
- `docs/specs/combat-actual-repeated-movement-completion-v1.schema.json`
- `docs/specs/combat-actual-repeated-movement-v1.schema.json`
- `docs/specs/combat-actual-round-entry-v1.schema.json`
- `docs/specs/combat-actual-selection-v1.schema.json`
- `docs/specs/combat-outward-composition-v1.schema.json`
- `docs/specs/fixtures/combat-actual-assault-v1.json`
- `docs/specs/fixtures/combat-actual-round-entry-v1.json`
- `docs/specs/fixtures/combat-actual-selection-v1.json`
- `docs/specs/fixtures/combat-authority-composition-v1.json`
- `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json`
- `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json`
- `docs/specs/fixtures/combat-inherited-cycle-control-v1.json`
- `docs/specs/fixtures/combat-inherited-movement-lifecycle-v1.json`
- `docs/specs/fixtures/combat-inherited-movement-v1.json`
- `docs/specs/fixtures/combat-inherited-no-attack-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-active-fallback-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-closure-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-lifecycle-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-movement-completion-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-second-move-v1.json`
- `docs/specs/fixtures/combat-inherited-reaction-trigger-v1.json`
- `docs/specs/fixtures/combat-inherited-reserve-cycle-v1.json`
- `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json`
- `docs/specs/fixtures/combat-inherited-reserve-movement-v1.json`
- `docs/specs/fixtures/combat-inherited-reserve-release-v1.json`
- `docs/specs/fixtures/combat-inherited-selection-v1.json`
- `docs/specs/fixtures/combat-inherited-snapshot-v1.json`
- `docs/specs/fixtures/combat-inherited-successors-v1.json`
- `docs/specs/fixtures/combat-opening-preamble-v1.json`
- `docs/specs/fixtures/combat-outward-composition-v1.json`
- `docs/specs/fixtures/combat-positive-entry-v1.json`
- `docs/specs/fixtures/combat-reserve-designation-v1.json`
- `docs/specs/fixtures/combat-side-projection-v1.json`
- `docs/specs/fixtures/combat-weather-v1.json`

## Approved scope and discovered compatibility requirement

User approved dependent regeneration, native revision compatibility and one additional
fresh-context review (instance9/9). Snapshot's original oracle passed after initial
pin repair, but the inherited-successor owning oracle exposed a genuine source
assumption: exact router inventory excluded all later native Combat arms. The
correction retains20 inherited declarations and adds an explicit28-arm extension
allowlist, preserving exact equality. Negative probes reject each of38 missing
router names, an unknown arm and inherited/native overlap. Four opening traces,
eight replay cuts,216 event mutations,32 raw rejections and88 boundaries pass.

Native Actual Selection also required whole-revision dependency validation; fixed
original-only hashes rejected refreshed predecessor fixtures. It now uses existing
CombatEvidenceRevisions acquisition. Prior27 registrations remain frozen, with a
separate Snapshot revision and retained PR334 ZIP. No event/state/rules schema or
public admission changes are authorized by this evidence refresh.

Outward's prerequisite correction reattaches24 stale line references to unique
existing IDs and updates two declared design source hashes. Requirement text,
planned tests, owners and acceptance statuses stay identical. Final generated
index acceptance remains pending until retained proof artifacts are rebuilt.
