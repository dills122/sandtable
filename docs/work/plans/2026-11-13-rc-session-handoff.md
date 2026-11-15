# RC working session handoff — November 13, 2026

**Status:** working-tree packet; no commit, PR, source admission or release
approval. This note is updated as checks finish. Local branch
`codex/rc-status-reconciliation` began from `0f8a828a`; a read-only GitHub check
found `main` at the same commit and PR #336 merged. [Tracker #330](https://github.com/dills122/sandtable/issues/330)
remains open; its current-status note now records the merged PR without changing
existing history or checkboxes. The connected GitHub app could read but not edit
the issue (403); the configured GitHub CLI performed and verified the update.

## Work and evidence

- [Setup reconciliation](2026-11-13-graziani-setup-reconciliation.md) records
  Matruh, 6th NZ and supply decisions, alternatives and remaining evidence.
- [Arrival analysis](2026-11-12-graziani-arrival-scope.md) now links the French
  Motor Marine company setup to its OA marker, erratum and `D` definition; the
  company/counter identity still needs independent verification.
- [Source register](2026-11-12-graziani-source-register.md) includes sequence,
  arrival, logistics, repair, map and fog obligations. Its
  [counter](verify-graziani-register.py) counts only the bounded initial table,
  not all rules or unsupported mechanics.
- [Land sequence crosswalk](2026-11-13-graziani-land-sequence-crosswalk.md),
  [supply planning](2026-11-13-graziani-supply-planning.md) and
  [repair matrix](2026-11-13-graziani-repair-resource-matrix.md) distinguish
  original source facts, candidate interpretations and unresolved interactions.
- [Convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md) separates
  simplified lane/cargo limits, bombardment inputs and conditional loss arithmetic;
  source review and actual scenario reachability remain open.
- [Rail scope](2026-11-13-graziani-rail-scope.md) joins the Axis-use correction,
  construction limits, supply allowance and abstract resource questions.
- [Patrol/fog matrix](2026-11-13-graziani-patrol-fog-matrix.md) separates
  combat history, abstract supply, loss order, limited reports and dummy claims.
- Blind review packets cover [supply](graziani-supply-review/README.md),
  [trucks](graziani-truck-review/README.md),
  [Commonwealth arrivals](graziani-arrival-review/README.md),
  [Axis formation members](graziani-axis-formation-review/README.md),
  [arrival/supply/repair cases](graziani-rules-review/README.md) and
  [victory](graziani-victory-review/README.md),
  [transport](graziani-transport-review/README.md) and
  [ports](graziani-port-review/README.md) and
  [convoys](graziani-convoy-review/README.md) and
  [rail](graziani-rail-review/README.md) and
  [patrol](graziani-patrol-review/README.md) and
  [weather](graziani-weather-review/README.md) and
  [fortifications](graziani-fortification-review/README.md) and
  [French Motor Marine company](graziani-marine-review/README.md). The [review status](2026-11-13-graziani-review-status.md)
  reports raw packet issues and scope. Their blank templates fail closed;
  no new independent member-level reading is claimed. The prior bounded Matruh/NZ
  independent-agent reading remains in the separate [disposition](graziani-source-review/review-disposition.md).
- [Arrival laboratory contract](../../specs/scheduled-arrival-lab-v1.md) and
  `ArrivalLabState` exercise synthetic owner/system choices, mixed obligations,
  atomic placement, duplicate receipts and replay. They do not change actual
  `CampaignSnapshot`, Host, legal actions or historical content. The generic
  catalogue continues to turn 7; scenario terminalization is not implemented.
- Map seam and rights research add evidence, not topology or redistribution
  approval. Original scans remain outside Git.

## Validation snapshot

- Isolated Release build of `Cna.Core.Tests` passed, zero warnings/errors. Restore
  used cached packages with `NuGetAudit=false` after the isolated restore could
  not reach NuGet's vulnerability service; this is not a dependency audit.
- `*ScheduledArrival*`: 23 passed; `*GrazianiSequenceCatalogTests*`: 1 passed;
  `*StageEntry*`: 59 passed; `*LandSequence*`: 21 passed; `*Weather*`: 102 passed;
  `*CampaignContact*`: 24 passed; `*Initiative*`: 38 passed.
  These selections overlap and are not a deduplicated solution-wide count. The
  weather selection does not admit historical scenario effects, and the contact
  selection does not certify the full public visibility path.
- Source comparison self-tests: 18 passed; arrival review checks: 8 passed; Axis
  formation join checks: 4 passed; register counter checks: 4 passed; review-status checks: 5 passed. The
  packet-wiring test uses synthetic hash verification; the separate status run
  checked the actual source bytes. A combined invocation initially failed four
  imports because the packet directories were absent from `PYTHONPATH`; with
  those paths supplied, all 39 tests passed. Supply,
  truck, arrival, Axis, rules, victory, transport, port, convoy, rail, patrol,
  weather, fortification and Marine blank review templates correctly report
  unresolved entries against the matching source files.
- Supply pilot arithmetic passed with one Axis ammunition row explicitly unknown;
  this is not independent source verification. Relative file-link checks found
  no missing targets, and `git diff --check` passed.
- Changed-C# format check passed. `just test-suite core` passed 3,689 tests in
  59m 23s on the earlier compiled working-tree version, before the final laboratory
  event version, Unicode validation and sequence test changes. It does not test
  those later changes; the isolated focused selections above do. A second
  `just test-suite core` on that session's later code passed **3,692 tests**, with zero
  failures or skips, in 58m 09s. Its retained local selection is
  `artifacts/local-tests/core-f_z6wn59`; it predates the subsequent content
  draft analyzer and is not release-head CI.
- A separate Host-project restore in an isolated artifacts directory produced no
  result after a default-source attempt and a local-cache-only retry; both were
  stopped. No Host suite pass or restore diagnosis is claimed from them.
- Two historical source-pin failures recorded in the October baseline were
  rerun on this tree and now pass without pin edits in this session:
  `verify-combat-inherited-breakdown-completion-v1.py` reports eight traces,
  16 cuts, eight retries and 426 mutations; `verify-combat-cycle-sequence-v1.py`
  reports 112 catalogue positions and 38 mutations. The latter explicitly does
  not prove runtime replay. These checks do not certify P01 or release readiness.
- Release-head CI, full solution, frontend, Host and Runner suites and independent
  release review remain separate and are not implied by focused checks.

## Next dependency

Subsequent work added a [source/ruling review](2026-11-13-arrival-ruling-review.md)
and a [campaign content draft analyzer](../../specs/campaign-scheduled-arrival-content-draft-v1.md).
The analyzer checks real content definitions and identifies deferred placements
without changing legacy validation or admitting a campaign. Isolated Release
build passed; focused analyzer tests passed 5, existing ContentValidation tests
passed 9, `*ScheduledArrival*` passed 28, and `*StageEntry*` passed 59 (overlapping
selections). Changed-C# format verification passed after retrying outside the
sandbox's named-pipe restriction; the formatter reported workspace-load warnings.
A full Core rerun and release-head CI for this later code remain outstanding.

The scenario inventory can advance independently, but the roadmap's public
Combat path also requires a versioned contact-based successor to `CMB-CON-005`,
the remaining profile/capability evidence and Tasks 020–025. The existing
contact journal and bounded native owner cuts are foundations, not that complete
publication certificate; see the [fog architecture](../../research/combat-fog-of-war-architecture-spike.md)
and [delivery plan](../../design/combat-cycle-implementation-plan.md).

Do not wire the laboratory into an existing `ExplicitNone` setup. A real synthetic
campaign slice first needs admitted setup/content identity, a new event/snapshot
path compatible with existing versions, unit/representation creation, complete
non-land obligation policy, strict projection, authenticated owner actions and
atomic Host receipts. In parallel, obtain independent source checks and a rights
disposition; historical setup, map, accepted rulings and complete reachable rules
remain open. See [arrival handoff](2026-11-12-graziani-arrival-authority-handoff.md)
and the [RC checklist](2026-10-24-rc-checklist.md).

## Arrival draft identity and world contribution

A [versioned draft identity](../../specs/campaign-arrival-draft-identity-v1.md)
now binds the complete candidate content, scenario, scheduled groups,
permitted destinations and supplied ruleset/ruling hashes. It is internal and
cannot admit a content pack or campaign. The associated world contribution
constructs synthetic Core element and map-representation values after checking
the exact slot, owner, destination and existing world bindings; it neither
writes an event nor advances a campaign. Default readiness still needs a ruling.
Legacy `ExplicitNone`, content validation and authoritative versions remain
unchanged. Next implement an admitted versioned setup and persistent lifecycle,
with atomic event/replay and owner projection/Host receipt tests; do not treat
candidate values as a playable arrival.

The Marmarica recovered scan was checked again in the existing research stream;
this does not complete independent reading. The remaining review packets still
need a separate reader, along with source-rights disposition and open rulings.

Validation for this follow-up: isolated Release build passed with zero warnings;
5 draft-identity/world tests, 7 canonical-content tests, 30 V5 pack tests,
5 synthetic-catalog tests, 28 scheduled-arrival tests and 59 stage-entry tests
passed (selections may overlap). Nine review-status/register Python tests passed.
Changed-file format verification passed with workspace-load warnings; `git diff
--check` passed. A full Core attempt using the isolated output path encountered
fixture lookup failures because those tests locate the repository from the binary
path; it was stopped. A repository-local retry ran for several minutes without a
completion summary and was cancelled. Neither full Core nor exact-head CI is
claimed. The unrelated `*ContentPackTests*` filter matched zero tests; the
specific V5 and canonical selections above replaced it.

## Arrival replay draft follow-up

[Replay draft v1](../../specs/campaign-arrival-replay-draft-v1.md) adds
canonical candidate event bytes, deterministic world reconstruction and receipt
recovery after replay. Wrong identity, event effects, sequence and conflicting
retries reject. The draft reconstructs the selected scenario's initial world
but lacks actual campaign phase ordering, creation admission and other obligations. It is not a
Chronicle event or durable Host integration.

The normal Host already has `CampaignDurableAuthorityJournal`: its SQLite
transaction and cold replay are the target for eventual arrival integration.
The temporary grain's process-local atomicity is not durable. The next vertical
slice needs an admitted Core setup/content identity, initial world and pending
obligations, command/event/snapshot codecs and projection, then an authenticated
owner submission through the existing journal with cold restart, lost-reply,
concurrent submission and fog tests. Do not store draft events in the journal.

Release build passed with zero warnings. Focused selections passed 8 draft
identity/world/replay tests, 28 scheduled-arrival tests and 59 stage-entry tests;
9 source-review Python tests passed. Changed-file format verification passed with
workspace-load warnings; `git diff --check` passed. Full Core, Host and exact-head
CI remain unrun for this follow-up. Same-reader Cirene and repair source checks
are recorded in the ruling review; independent review and the repair-availability
ruling remain open.

## Initial world and slot ordering follow-up

The replay draft now constructs selected scenario initial placements using the
same construction path as the existing campaign factory. The legacy path still
requires its original artifact/world validation; candidate construction accepts
only a validated draft identity and does not admit a campaign. A later scheduled
slot cannot resolve while any earlier scheduled group is pending; same-slot
groups remain reorderable. These checks do not model phase progression or
non-land obligations.

The normal durable journal's existing owner submission keys openings by an
on-map acting element. Arrival groups need a distinct, side-private opening
subject; see [owner admission dependency](../../specs/campaign-arrival-replay-draft-v1.md#owner-admission-dependency).
No durable Host or admitted campaign arrival was implemented by this follow-up.

The scenario scan exposed a probable §60.93(B–C) pointer error, documented in
[review evidence](2026-11-13-arrival-ruling-review.md#land-and-air-supply-cross-reference).
It is a same-reader inference, not an accepted ruling. The hash-checked review
status command exits 1 with 16 second readings still blank and the pilot's raw
locator differences still open.

Final isolated Release build passed with no warnings; focused tests passed 9
arrival-draft, 9 campaign-world and 59 stage-entry tests. Changed-C# format
verification passed with workspace-load warnings; `git diff --check` passed.
Full Core, Host and exact-head CI were not run for this follow-up.

## Admission and Host session start

The [working session](2026-11-13-arrival-admission-host-session.md) defines a
synthetic end-to-end target, staged acceptance evidence and a 3–5 hour Core /
4–8 hour Host estimate rather than claiming that either part is already done.

As an initial cold-reconstruction prerequisite, `CampaignArrivalDraftIdentityReader`
reads bounded canonical candidate bytes, revalidates the complete content and
schedule, and requires the reconstructed bytes and expected hash to match. The
expected hash must itself come from trusted authority. Tests reconstruct identity,
world and a retryable event from encoded bytes and reject changed/invalid,
noncanonical, oversized and wrong-hash inputs. This remains a draft; admitted
creation, real campaign phase progression, durable Host and authentication are
not implemented by this change.

Final isolated Release build passed with zero warnings; 11 identity/world/replay
and 7 existing content-canonical tests passed. Changed-C# format verification
passed with workspace-load warnings. Full Core, Host, boundary and exact-head CI
remain unrun for this follow-up.

## Fixed synthetic arrival profile

The [synthetic profile](../../specs/campaign-synthetic-arrival-profile-v1.md)
constructs one repository-owned candidate from the existing movement fixture,
with a single Axis arrival and one destination. Resolution requires exact
repository-built bytes and hash; a different self-consistent candidate rejects.
The profile binds the current ruleset and a synthetic policy hash but does not
pin historical catalog versions across builds, declare all stage obligations,
or admit campaign creation or durable Host writes. These remain prerequisites
for the session plan's end-to-end target.

Isolated Release build passed with zero warnings; 12 focused identity, world,
profile and replay tests passed. Changed-C# formatting passed with workspace-load
warnings and `git diff --check` passed. A local-only restore of the Host test
project produced no result and was cancelled; no Host suite or exact-head CI
pass is claimed.

## Synthetic World6 durable journal checkpoint

The pinned successor now covers a deferred Axis unit, a replay-verified
six-record synthetic preamble, its owner-only arrival, and replay-derived
checkpoint and receipt. The internal Host integration uses the existing SQLite
journal, authenticated seats and opaque references. Atomic creation and
arrival rollback, concurrent retry, rehashed evidence tampering, normal-profile
compatibility and separate-process commit/retry are covered by focused tests.

This does not admit the profile through normal `CreateCampaign`, expose an HTTP
route, or make the scenario playable. Preamble verification now computes typed
events directly; v2 internal journal records retain their full bytes. Ordinary
campaign event integration, general admission,
observation and public privacy boundaries remain open. Historical scenario
reviews, source rights and rulings remain separate gates. See the updated
[session plan](2026-11-13-arrival-admission-host-session.md#current-synthetic-journal-checkpoint).

Focused Core successor tests passed 13 cases; focused Host admission, arrival,
rollback and existing-journal compatibility checks passed. Exact-head full
solution and CI coverage are not claimed. An earlier Core binary ran 3,625
passing tests before cancellation after 59 minutes; that run was incomplete
and predates the final successor and Host changes.

## Graziani production content model checkpoint

The production `ContentScenario` now has an in-memory scheduled-arrival
record with source origin, side, turn/stage, member IDs and optional entry-area
ID. The normal content validator checks timing, source and member references,
ownership and duplicate/initial-placement conflicts. Existing content formats
reject nonempty schedules instead of dropping them; the unchanged empty form
retains its legacy canonical bytes. New successor formats serialize schedules
and the existing combat-fact subset; neither is admitted scenario content, a
playable unit or a normal campaign arrival.
See [content boundary](../../specs/campaign-scenario-arrival-content-v1.md).

The original Commonwealth chart package was visually checked again at its
schedule, entry instructions and NZ OA pages. It lists the artillery at 2/3
alongside two RTR units and identifies Cairo as the default entry area subject to
exceptions and stacking/formation constraints. Its OA marks `3/2` and six
18/25-pounders. This is same-stream verification; source rights, complete unit
and map data, independent review and admissible profile remain open.

Twenty-seven focused scenario-content tests passed, as did existing content validation
(9), Combat content (8), Breakdown content (38), v5 content (30), and prior
draft-analyzer (5) selections; two Host outward API tests passed. These are
focused checks, not full Core, full Host or exact-head CI. A new canonical
scheduled-content envelope now preserves the schedule, but it lacks combat
seeds and is not a campaign artifact. The ordinary engine still needs a
historical-capable profile, creation/world and event/snapshot admission. The
old V6 profile remains synthetic and narrowly scoped. The NZ regiment's `x`
chart entry identifies it as battalion-equivalent despite its historical name;
its artillery characteristics and missing engine support are recorded in the
content boundary. No Graziani campaign or arrival is operational.

The `sandtable.content-scheduled.v1` envelope round-trips and hashes validated
source-linked schedules without changing legacy content bytes. A successor
combat-facts validation path checks seeds for initially placed and scheduled
units; it does not make V5/V6 serialization or campaign creation accept those
records. A second versioned envelope retains the existing V5 combat-fact subset
and scheduled TOE seeds, including unsupported classes as source data. It is
not a complete artillery or runtime-admission profile. Same-stream review of
Commonwealth chart pp. 22 and 27 located the NZ `x` counter and 18/25-pounder
characteristics; see the content boundary. Actual artillery remains unsupported
by the normal combat vocabulary. These focused checks do not prove the
historical unit, map or scenario is admitted.

The partial production source-facts catalog contains the eight Polish brigade
OA members, three turn-two records, turn-three 1st field artillery, turn-four
3rd Hussars, four 7th Indian brigade units and their printed truck quantities,
and partial gun, tank and infantry component facts. Land §4.45 supplies the
interpretation needed to derive TOE seeds for all 21 members; 7 RTR's weapon
profile remains incomplete. This does not supply full weapon characteristics or the transport
and formation rules needed for these arrivals to play.
Three source-listed brigade headquarters can be serialized without inventing
combat components; their authority and other headquarters rules remain absent.
The separate 6th NZ Brigade chart candidate preserves four members and printed
truck quantities at turn 6/3; Land §20.11 conflicts, so timing remains unaccepted.
The six listed chart rows collectively round-trip as 21 member IDs in a
structural test with placeholder force and map properties. The test is not a
complete scenario, source-acceptance decision, or playable campaign.
Eleven individually listed Commonwealth placements are recorded as partial
source data, including co-location at C4131 and C3922;
current World V6 does not support that same-side combat co-location.
The Axis garrison rows require expansion into their constituent counters;
they are not represented as single elements.
The OA also supplies partial combat facts and source-backed strength seeds for
those 11 Commonwealth units. Their formations, full capabilities, and the Axis
force remain incomplete.
The scheduled-content envelope also retains grouped initial counters without
assuming attachment. Tobruk's source groups list 22 land counters; ten infantry
have partial facts and strengths. Bardia's 11-counter group is a candidate: its
30th HQ row prints a conflicting `32GaF` abbreviation; original counter section 2
labels 30 GaF Bardia and 32 GaF Tobruk, supporting an OA typo. Independent
review remains open. Four Bardia infantry have partial facts and strengths.
These do not implement the garrisons in World V6.
Giarabub has six source-listed counters with partial combat facts and strengths.
Seven other individually identified Axis counters have source placements and
strengths. Parenthesized AA ratings are retained as markers and rejected by the
current combat capability; their conditions remain unimplemented.

The scheduled-content format also binds a source-backed entry area to existing
map-location IDs, checks its source and references, and includes its membership
in the content hash. The source summary lists Cairo as E1730, E1829, E1830,
E1930 and E1931; tests use those identifiers with placeholder test geometry.
This does not establish the map topology or determine legal placement choices.

## Real Graziani source-format follow-up

The September errata's Cirene and Marmarica OAs now contribute two partial
initial groups: 29 listed counters at C4120 and C3918, including two attached
light-tank battalions. Fourteen infantry/machine-gun and two 47/32 anti-tank
records have source-linked characteristics and starting TOE. The initial group
format retains printed 45 medium/5 heavy and 40 medium/5 heavy trucks; the
unspecified light quantities remain unknown. These facts are not complete
formations, truck assignments or admitted units. Same-reader source transcription
still needs independent review.

Both divisional artillery regiments also have partial source-linked gun and
light-AA facts. The source-content format now validates and hashes a shared
nine-point limit on their two gun components separately from the AA allowance;
their printed starting strength is nine gun and three AA points. Other legal
weapon assignments and ongoing play-time enforcement remain unsupported.

Three alternative Commonwealth groups preserve eight previously listed initial
units and shared truck quantities from §60.41; a scenario must not combine
them with the overlapping independent placements. A same-reader check against
Italian chart §4.48 and Commonwealth chart §4.47 corrected the 149 mm Vichy
gun vulnerability to 5 and the 60-pounder defensive assault rating to 0. The
Benghazi OA's VIII/II engineer has code `bb`, which conflicts with the Italian
characteristics chart's `bbb` engineer code. Counter section 2 labels VIII/II
Benghazi with the same engineer symbol used by the 62nd and 63rd engineers,
supporting a `bbb` candidate correction. Independent ruling remains open.
Counter section 2 separately depicts Soluch anti-tank and artillery counters;
the OA lists twelve 100/17 howitzers for the latter. Both have partial source
records at A4130, but World6 cannot admit their co-location.

The normal `CampaignCreationExecution` still resolves the synthetic V6 catalog;
no Graziani campaign can be created. World V6 rejects co-located friendly combat
units, while the normal assault path requires one sealed element and component
per role with tightly pinned TOE, ratings and morale. Observation also expects
every independent content element in the world. Snapshot11 admission is tied to
the breakdown setup catalog and the initial operation stage, so a resolver
change alone cannot admit the six-turn scenario. Supporting real grouped forces
and deferred arrivals requires a successor spanning world validation, combat,
observation, event/snapshot replay and Host privacy; removing one guard is not
sufficient. Historical topology, formation relationships, rules and source
acceptance remain incomplete. No normal admission or playable RC is claimed.

Next implementation cut, using actual Graziani data rather than another fixture:

1. Finish a source-backed initial-force and map artifact with explicit unresolved
   decisions and a versioned capability/ruling identity. Preserve deferred units,
   group membership, truck quantities and source-only ratings without inventing
   assignments or geometry. Independent source and rights review remain gates.
2. Define a successor authoritative world and creation event/snapshot that can
   distinguish initial, deferred and eliminated units and retain legal co-located
   formations. Validate actual starting TOE and source limits; reconstruct from
   canonical bytes. Do not reinterpret World6 or Snapshot11 histories.
3. Extend normal creation/catalog resolution, observation and the Host journal
   together for that successor. Test creation and cold replay from a real pack,
   idempotent commands, owner visibility and refusal of incomplete/unsupported
   content. `CampaignDurableAuthorityJournal.CreateCampaign` already commits
   the Core creation bytes and snapshot atomically; extend its adapter and
   verified load path rather than storing the source envelope as an event. Load
   reconstructs the original creation request and calls `CampaignHostAdapter.Import`,
   so immutable historical content must remain resolvable by its exact identity
   after restart and upgrades, or recovery must explicitly fail closed.
   No synthetic content should be added to establish admission.
4. Implement the rules required by the admitted force, including multi-unit
   combat, armor/artillery, stacking and movement, arrival decisions, supply,
   repair, stage progression and victory; each needs a source-backed contract
   and deterministic replay. A creation success alone is not a playable RC.

Focused checks on the corrected Core build passed: 31 scenario-content, 17
scheduled serializer, 30 legacy V5, 9 content-validation and 38 breakdown
content tests. Two Host outward API, 17 existing campaign-creation and 81 Core
user-space boundary tests passed. Full Host passed 249 tests and full Runner passed 470 tests; Game and
Intelligence contract suites passed 8 and 10. Nine source-review status tests
also passed. Full Core passed 3,780 tests with no failures or skips in 57m 41s.
These are local results, not exact-head CI; earlier Core and Host attempts were
interrupted after the code changed.
The full solution format check passed with workspace-load warnings.
The register counter reports 67 rows in its bounded initial table, each with
outstanding evidence; it does not count unsupported mechanics or certify source
completeness. Local scenario, Axis chart, errata and compiled scan hashes were
rechecked against their recorded package hashes; this is not an independent
reading or a rights disposition.

The Release solution build initially failed with NU1903 for `Aspire.Hosting`
and `Aspire.Hosting.AppHost` 13.4.6 ([GHSA-f7vh-h5g3-7jrh](https://github.com/advisories/GHSA-f7vh-h5g3-7jrh)).
The advisory lists 13.6.5 as patched and describes a macOS Dashboard/linked
attacker-controlled container scenario. `Cna.AppHost` now pins
`Aspire.AppHost.Sdk` 13.6.5; after an outside-sandbox NuGet restore, the Release
solution build passed with one ASPIRE010 warning about the disabled CLI bundle.
The restored AppHost resolves both hosting packages to 13.6.5, and its NuGet
vulnerability listing found no reported vulnerable packages from the configured
source; the same NuGet listing across the solution reported none for its other
projects. The AppHost reached its started state and was stopped; individual
service health was not verified. Package listing is not a security audit, exact-head
CI or release approval.
