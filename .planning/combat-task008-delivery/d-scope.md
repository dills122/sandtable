# Reserve scope research — prospective, not implementation acceptance

Read-only scope after B2, canonical combat-reserve-designation-v1 spec/schema/fixture/verifier,
combat-inherited-successors-v1 schema and combat-cycle-sequence-v1 Authority identity framing.
Completion2 already includes mandatory cycle/cycleId and atomic ordinal1 opening. Never add a
separate opening event. D can validate exact event bytes from real retained predecessor;019A applies
that same event. Until019A, no post-completion state/Movement entry/terminal retry acceptance.

Bounded proposal for lead validation at D start:
- D1: Reserve models, designation replay, pre-completion codec+bounded World writer, tests,fixturelink.
- D2: completion/authority models+identity codec, history-derived OpeningBase+completion codec,
  D kernel evidence seam, tests, existingReservecodec adaptation if required (<=5primaryfiles).
- 019A immediately after: terminal replay/readback/retry projection of same accepted event.
Full D packet acceptance requires019A; do not synthesize terminal state to unblock E/H.

D consumes request+Created11+4opening+1Weather+4stage. Entry state10/nine receipts. Own original
infantry optional designation none→I gives state11. Completion empty gives state11/tenreceipts;
afterdesignation gives state12/elevenreceipts. Owner from firstsideorder, notAxis holder. Both Reserve
andMovement catalog positions nullactiveSide. Preserve CP0/ammo10/TOE/location/RNG/Weather/order/history.
openedAuthorityVersion=completionresult, openingPrefix=precompletionprefix. OpeningBase literal profile
isolated-first-opening retained forhashcompatibility, but neverexternaltrustedbase or composedseedlimit.

Implementation gap: CampaignWorldV7InitialCodec rejects Reserve I and writerprivate. D needs bounded
causal Reserve World serialization, retaining initialreader invariant; no generalrestoreclaim.
No existingcycle/Reservehistoryruntime models surfaced. Required typed work, notmissingcontract.
Fixture16traces (4weathertypes×bothorders×empty/I),40cuts,152fingerprints+4isolatedparity cases.
Fullacceptance requires real A/B1/C/B2/D/019A chain, terminal retry, forensic/crosshistory/raw checks.
No code/spec changes or new provider/contentresearch. Proposal awaits current B2 acceptance and
lead exactschema review; compatible execution split, not dropped contract.

Lead inspected canonical Reserve spec/schema, InitialCodec and World7 model. Typed World uses
CampaignElementStateV6 in CampaignCombatWorldV7.cs; reconstruct element/world from retained immutable
fields for designation. Initial writer is ~145lines; bounded Reserve codec can validate exact derived
world and write it without relaxing initialcodec. No genericSnapshot admission justified.

Reserve baseline /tmp/d-reserve-baseline.log passed16traces40cuts2342leaf733raw1578boundary,
4frozenkernel parity cases and14sourcepins. Command PYTHONDONTWRITEBYTECODE=1 python3
docs/specs/verify-combat-reserve-designation-v1.py; contract-only evidence.
