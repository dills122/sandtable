# Combat P01 ordinal-two retreat, custody, and reunion subgate

**Status (2026-10-17): geometry PASS for bounded proposal; executable P01 support UNRESOLVED.** No ordinal-two native admission or public Combat activation follows from this report.

## Evidence boundary

This subgate covers the 300 terminal R03 rows in the read-only `p01-evidence` inventory at source commit `ef0d0bac3deeb4ab86b04d3b90c3431810469051`. Its `manifest.json` SHA-256 is `39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35`; all 11 compressed row chunks matched their listed SHA-256 hashes in this check. Indices are contiguous 0-299 and row keys unique. The earlier `bounded-profile-audit.json` SHA-256 is `ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429`. This check compared its 300 labels and location/adjacency facts with each row's own canonical `afterControl` World; it did not rerun the original R03 verifier or independently certify result-table reachability.

Pinned physical sources were byte-checked and visually inspected: `CNA_LandGameRules.pdf` SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, PDF pp24-25 and 40; `ERR_CampaignforNorthAfrica.pdf` SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`, PDF p2. The exact selected content file `docs/specs/fixtures/combat-content-v7.canonical.json` hashes to `ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847`, matching each positive row's content binding. Hex names are synthetic source coordinates (`sourceCoordinate: null`); claims here concern this bounded content graph, not historical map geography.

| Rule / adopted policy | Consequence checked |
| --- | --- |
| Land §15.82, PDF p24; `CMB-SET-003` | Defender retreat one hex away from attacker on least-resistance line toward nearest friendly supply; one CP, or explicit refusal with extra 10% loss. |
| Land §§15.73-15.76, 15.83-15.88, PDF pp24-25; September errata §15.27, PDF p2 | Both result rolls can cause zero-loss Engaged, Retreat, capture, loss, or surrender; Retreat overrides Engaged. Loss and capture calculations precede final movement. |
| Land §§28.11-28.17, 28.21-28.25, PDF p40; September errata §28.17, PDF p2 | Captured infantry becomes prisoners; initial relocation up to three hexes avoids enemy-occupied/controlled and impassable entered hexes; one infantry TOE forms a guard; corrected moving guard ratio is 1:5; unguarded prisoners escape; reunion threshold is eight CP ignoring enemy units/ZOC, then delayed replacement. |
| `docs/design/combat-settlement-disclosure-v1.md` `CMB-SET-001` through `008` | Bounded proposal fixes capture origin at victim's pre-retreat hex, permits one certified guard rendezvous or unguarded escape, and requires every immediate obligation to close. These are digital policy choices, not printed source instructions. |

## Row partition

| Partition | Rows | Evidence conclusion |
| --- | ---: | --- |
| Candidate-positive | 44 | `r012-c01` through `r055-c01`, indices 36, 42, ..., 294. Each has adjacent singleton infantry, normal weather, actor CP 4, defender CP 0, TOE 10 and ammo 10 each, Cohesion 0, no current target use/attack history, and empty custody, guard, relation and settlement lists. Both sides' representation locations equal their element locations. 22 Axis actors, then 22 Commonwealth actors. Geometry matrix below applies to all 44. |
| Candidate-absent | 256 | 244 lack adjacent opposing target; 12 are adjacent paid rows with phasing ammo 0. Split 128/128 by acting side. Prior paid Contact/settlement in 36 rows is historical, not a fresh assault. Two independent one-stacking-point battalions have no mandatory ZOC attack under Land §10.11 and the pinned stacking chart. This proves bounded physical candidate absence; it does **not** prove an executable empty selection transition or native ordinal-two support. |

The 44 positive indices are `{36+6n | 0 <= n < 44}`. Row keys are `r012-c01` to `r055-c01` with root ordinal increasing by one; all are `armed` cut 1. The audit used the authenticated active-cycle acting side, not the original cycle's side. A side-fixed check would misclassify the latter 22.

## Geometry certificate for all 44 candidates

Selected content has only these five undirected, feature-free edges:

`axis-supply -- axis-rear -- assault-west -- assault-east -- commonwealth-rear -- commonwealth-supply`.

All six locations are on-map Clear hexes. Only `assault-west` (Axis) and `assault-east` (Commonwealth) are occupied at decision time; each holds one independent battalion/representation. Supply-direction anchors are `axis-supply` and `commonwealth-supply`. There is no other unit, occupied rear hex, hexside feature, existing guard, prisoner, vehicle, or terrain barrier in these rows. Both units have one stacking point, below the >1 ZOC threshold; no entered rear hex is enemy controlled by this fixture. The graph gives a unique route to either friendly supply anchor.

| Actor | Defender's required one-hex route | Distance from attacker after move | Post-combat defender CP |
| --- | --- | ---: | ---: |
| Axis | `assault-east -> commonwealth-rear` | 2 edges | 3 after defense cost, 4 after retreat |
| Commonwealth | `assault-west -> axis-rear` | 2 edges | 3 after defense cost, 4 after retreat |

Both routes move farther from the attacker, toward the defender's sole friendly supply anchor. Both destinations were empty before combat. The actual one-CP retreat remains within CPA 10; refusal spends no retreat CP. Actor CP becomes 9 after its five-CP assault cost. Thus all immediate settlement CP values are exact integers. This establishes route feasibility for a one-hex defender Retreat, including zero-loss Retreat, before any particular seed is chosen.

The adopted capture-origin convention and two possible retreat dispositions yield the following **eight** location cases. Each row in this table applies to 22 terminal inputs. `victim` identifies the captured side, hence `captor` is the other side. Guard route starts at victim's pre-retreat hex and ends at captor's final infantry hex; escape route starts at capture origin and ends at victim's surviving unit. Paths are unique in this graph.

| Actor | Defender moves? | Victim | Initial guard rendezvous | Escape reunion |
| --- | --- | --- | --- | --- |
| Axis | no | Axis | `assault-west -> assault-east` | `assault-west` |
| Axis | no | Commonwealth | `assault-east -> assault-west` | `assault-east` |
| Axis | yes | Axis | `assault-west -> assault-east -> commonwealth-rear` | `assault-west` |
| Axis | yes | Commonwealth | `assault-east -> assault-west` | `assault-east -> commonwealth-rear` |
| Commonwealth | no | Commonwealth | `assault-east -> assault-west` | `assault-east` |
| Commonwealth | no | Axis | `assault-west -> assault-east` | `assault-west` |
| Commonwealth | yes | Commonwealth | `assault-east -> assault-west -> axis-rear` | `assault-east` |
| Commonwealth | yes | Axis | `assault-west -> assault-east` | `assault-west -> axis-rear` |

Maximum guard route is two edges, below Land §28.12's three-hex initial relocation allowance. Each entered hex is either vacated by the retreating defender or held by the captor; no guard route enters the victim's final occupied hex. The capture-origin hex can still hold victim survivors when prisoners leave it: that departure is an accepted `CMB-SET-004` location convention whose executable handling still needs proof. Maximum escape reunion is one Clear edge (the other cases are co-located), within Land §28.24's eight-CP test. It leaves the captured TOE out of the victim's current strength and creates the proposed one-month replacement entitlement. One guard TOE can cover the design's maximum three-prisoner lot under corrected 1:5 moving ratio **if** that result bound is independently proved; donor after-loss TOE at least seven is likewise conditional on result-table proof. No geometry counterexample appeared in any of the 44 row/branch combinations.

If a raw Retreat is refused, survivors remain adjacent and the adopted relation is post-assault Contact; a completed Retreat separates the original pair. Without raw Retreat, raw Engaged persists even at zero loss; otherwise adjacent survivors become Contact. Guard and prisoner assets do not inherit original pair relations. Those are placement/relation obligations, not extra map routes.

## Stop conditions and unresolved claims

1. **Result reachability and quantities unresolved here.** This subgate covers every adopted placement *shape* for both capture roles and both retreat dispositions, but relies on separate result/settlement table proof for the complete set of reachable percentages, capture shares, maximum lot three, minimum surviving donor TOE seven, and any joint result constraints. A chart outcome outside those bounds invalidates guard and reunion admission even though paths exist. No seed-specific narrowing is allowed.
2. **Voluntary surrender remains outside initial RC bounded coverage.** Land §15.88, PDF p25, middle column, allows voluntary surrender; September errata §15.88 (PDF p2) distinguishes assault at -17 Cohesion from mere adjacency at -26, and §50.12 requires an assault for ammunitionless surrender. Neither the 44-row physical shape nor this bounded decision proposal supports the optional source-legal surrender choice. At selection time, both participants have ammo 10 and Cohesion 0. The owner accepted its exclusion for the initial RC under `CMB-POL-009`; this does not establish complete source-legal action coverage. The adopted cost policy treats post-commit ammo 0 as the already-paid assault, not a new surrender trigger. Any state requiring automatic surrender handling must fail P01 admission until separately proved. Any later assault context needs its own source-backed proof.
3. **Native ordinal-two support fails the current gate.** `CampaignCombatCertification` admits ordinal one only; current R03 replay and this geometry check are evidence inputs, not an executable P01/P02 certificate. Any choice publication, decline-only RBA, branch settlement, privacy equivalence, future custody upkeep and delayed replacement effects still require their own frozen contracts/tests and native admission. Do not infer ordinal-two capability from ordinal-one state traces.
4. **Accepted policy still needs executable proof.** The capture-origin convention, relocation through an origin still occupied by victim survivors, fixed guard rendezvous, immediate unguarded escape, deterministic recipient, and refusal/unguarded timeout choices are accepted digital policies under `CMB-POL-004` and `CMB-POL-005`. Printed rules support constraints and consequences, not those exact transaction boundaries. Their implementation, hidden-blocker/public-profile equivalence, and authorized disclosure need separate proof.

**Subgate verdict:** 44/44 candidate-positive rows pass bounded retreat/custody/reunion *geometry*; 256/256 candidate-absent rows retain a physical no-assault proof. Overall P01 positive support stays **UNRESOLVED** pending result bounds, surrender policy and executable/contract gates. No unsupported physical route was found in these 44 rows; this is not a native or full-game support certificate.
