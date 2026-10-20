# Private Content8 pre-issue source seal v1

**Status:** Core-only, unissuable. This is a candidate source input for a future
Content8/Setup8 CreationRequest, not a CreationRequest, Created event, campaign,
receipt, predecessor ledger, or admission decision.

The canonical UTF-8 JSON object has the exact field order below and no whitespace:

1. `contractVersion`: integer `1`.
2. `kind`: `private-combat-auto-surrender-source-preissue`.
3. `content`: object with `hash` and `bytesBase64`.
4. `rulesSource`: object with `hash` and `bytesBase64`.
5. `setup`: object with `hash` and `bytesBase64`.
6. `closedInitialInventory`: object with `attackerElementId`,
   `attackerAmmunition`, `defenderElementId`, `defenderAmmunition`,
   `cohesionLevel`, and `externalAmmunitionSources` (empty array).

Each byte field is RFC 4648 standard Base64 of the **complete original canonical
artifact bytes**, not normalized Content7 or reconstructed JSON. Each hash is
`sha256:` plus lowercase SHA-256 over those bytes. Core accepts only the three
frozen artifacts in [the source contract](combat-auto-surrender-source-v1.md),
including the source's one-orientation Axis Ammo 10, Commonwealth Ammo 0,
Cohesion 0, and empty initial external inventory. Deserialization revalidates
the retained bytes and compares the entire input with the canonical encoding;
alternate spellings, extra fields, and altered source bytes fail closed. The
seal ID is `source.` plus lowercase SHA-256 over ASCII
`sandtable.combat.auto-surrender.preissue-source.v1\0` followed by those
canonical envelope bytes. It is an integrity label, **not an authority receipt**.
The approved fixture triplet produces a 19,725-byte envelope and seal ID
`source.740dd3c33bab93108c6638a4c5d1dc2f6109ae9926da5c5d79650ebdfaf8c648`.

No actor, decision clock, campaign ID, RNG seed, Rules10/configuration selection,
or ordinal slot is established by this seal. In particular, the current
`CampaignCombatCreationContext` and Created11 are Content7/Setup7-specific and
cannot be used to issue this profile. A future versioned creation contract must
select its own approved Rules/config bytes and authenticate actor, campaign,
clock, slot, Created event and all eleven predecessor ledgers before any P01
assessment. The existing 300 Content7 roots remain unchanged and unadmitted.
