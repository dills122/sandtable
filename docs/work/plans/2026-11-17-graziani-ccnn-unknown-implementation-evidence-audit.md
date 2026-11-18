# 1st CCNN HQ unknown-state implementation evidence: audit

**November 17, 2026. Disposition: usable as a bounded evidence handoff with clarification; not implementation approval or readiness.** Compared the [handoff](2026-11-17-graziani-ccnn-unknown-implementation-evidence.md) with the [independent source review](2026-11-17-graziani-blocker-ccnn-hq-review.md) and [policy critique](2026-11-17-graziani-ccnn-gun-capacity-policy-review.md). This is a document comparison, not an independent source or runtime review.

## Findings

1. **Clarify gun classification before implementing.** The handoff correctly records `g` as permitting an artillery TOE point and does not select a present point. But its “Initial assignment” row describes a “missing required listing” without preserving the policy critique's qualification: the chart does not independently establish whether that artillery point is a gun for the key's gun-listing rule. Treat applicability of that instruction as unresolved, not as an established classification of an initial gun. Neither the permitted category nor a hypothetical point establishes an actual gun or weapon model.
2. **Treat unknown as knowledge, not equipment.** The handoff's unknown count/presence and weapon identity are appropriate for an unselected interpretation. An unknown weapon must not imply that a weapon or point exists: identity is unresolved conditional on an assignment, while assignment itself is unresolved. Preserve the literal `N` plus `g` reading of one point and the competing reading without ranking them as source fact. Neither zero nor one is an uncontested printed count; no Legion HQ `65/17` may be borrowed.
3. **Admission does not authorize gameplay.** The handoff separates content admission from operation and readiness. Its consumer matrix intentionally calls for a separately defined safe policy rather than selecting one. Until that policy and representation are reviewed, unknown-dependent decisions remain unresolved; admitting data does not create legal actions, supply values, combat capability, or readiness. A selected count itself can affect capacity, targeting, replacement and losses, even when weapon attributes remain unknown.
4. **Implementation prerequisites remain open.** The matrix preserves provenance, version identity, replay and fog boundaries, but does not settle ownership, deployment scope, loss/replacement transitions, migration, or behavior on later contrary evidence. Define those explicitly in a reviewed proposal before implementing; retain unresolved status if safe representation or behavior cannot be established. No new ruling follows from this audit.

## Proposed acceptance checks

These are proposed checks, not existing test results or authorization to implement:

- Round-trip raw `N`, `g`, source locators and scan identity alongside explicit unknown presence/count and conditional unknown weapon identity. Distinguish unknown from zero, absent, invalid and omitted; do not instantiate a gun or usable generic weapon. Preserve the unresolved applicability of the gun-listing rule.
- Verify capacity 1 and permission for an artillery point do not create an occupied or empty point. Verify separate Legion HQ entries provide no model or ratings to this HQ. If a one-point interpretation is separately approved, record it as a versioned inference, with deployment scope, not a source count.
- For each affected consumer, test its separately approved handling of unknown presence and unknown attributes: combat, movement, supply, losses, transfer, counts/capacity, targeting, replacement, visibility and readiness. Exercise both unselected uncertainty and any separately approved one-point interpretation. Unknown must not silently become zero or a usable value; absent policy leaves the affected operation unresolved.
- Test loss and replacement transitions, deterministic replay, content/policy version changes and explicit save migration, including later contrary evidence for new and existing campaigns. Check friendly and opposing projections and errors for fog-of-war leaks.

No runtime or pinned content changed; no gameplay tests ran.
