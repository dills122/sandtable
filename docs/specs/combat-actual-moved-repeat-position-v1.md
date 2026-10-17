# Actual moved repeat Combat Position v1 (P01 contract design)

**Status:** private ordinal-two contract design and acceptance matrix; executable reference and
support certificate pending. This document grants no native admission, public action, or complete
source-legal Combat claim. R03 is the authenticated terminal predecessor; P01, P02 selection, and
P03 native consumption are separate gates.

## Evidence boundary and policy

Input catalogue is the 300 genuine R03 terminal rows (36 paid, 264 armed) at source commit
`ef0d0bac3deeb4ab86b04d3b90c3431810469051`. Retained evidence lives outside Git at
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`. Pin `summary.json` SHA-256
`847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b`, `manifest.json`
`39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35`, and
`bounded-profile-audit.json` `ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429`.
The manifest pins each of 11 compressed chunks. Physical sources are Land Rules PDF SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and September
errata PDF SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
The common charts and adopted three-cell amendment remain pinned by
[source freeze](../research/combat-source-freeze-v1.md); the retained evidence folder does not
contain the original common-chart scan.

Accepted `CMB-POL-001` and DES-005 define a **bounded singleton infantry exercise**. Land §15.88
permits voluntary surrender. The owner accepted `CMB-POL-009` on 2026-10-17 to **exclude voluntary
surrender as a policy choice for the initial RC** because no typed decision, full-TOE custody
settlement, or fallback is defined for it. Report this exclusion as a limit of the exercise; no
result may be described as covering every source-legal
Combat action. Automatic ammunitionless surrender is a distinct source condition and cannot be
inferred from post-cost zero ammo. A profile requiring automatic surrender fails P01 admission
until that source condition has its own contract and proof.
Actual Reaction Before Assault movement is also outside this profile: RBA is explicit decline-only,
and a timeout or unavailable response cancels under `CMB-POL-004` rather than inventing a decline.
Gun, armor, Probe, Barrage, Anti-Armor, mandatory ZOC assault, multi-unit, partial TOE, hidden
participant, and unsupported Weather profiles fail closed.

## Proposed reference boundary

The executable P01 reference, when built, takes an original CreationRequest, **all eleven separate
R03 input ledgers**, and the twelfth P01 input ledger. It reconstructs the entire R03 Source,
Control and Proof with the frozen R03 verifier. An R03 row, canonical hash, caller-supplied parsed
World, or sibling audit report is never an authority credential. Original ordinal-one MovementEnd
proof and active ordinal-two MovementEnd proof stay distinct. The R03 suffix, including exact
Combat Position Determination, active cycle, version, prefix, receipt and parent Source bytes,
must match byte for byte before P01 classifies or opens a decision. Owned input bytes and dependency
pins are rechecked on cold, warm, retry, readback and rejected paths.

The initial P01 output is an **assessment**, with exactly one of `bounded-positive`,
`supported-empty`, or `unsupported`. Assessment is derived from authenticated current World,
Content, Weather, prior history and current segment; it never consumes RNG or mutates World.
`unsupported` is a hard stop, never an empty candidate list. Only `bounded-positive` permits a
private selection opening by the later P02 contract. A P01 reference must not append a selection
event, spend CP/ammo, roll dice, settle a result, or invoke native ordinal-two admission.

For a positive assessment, freeze one directional attacker and defender UnitKey, one component and
independent representation per side, their current locations, the exact adjacent target hex and
its full occupant set, the current ordinal-two segment and `voluntary-adjacent` basis. Internal
candidate identity uses the existing `CMB-ID-001`–`007` domain-separated canonical base: creation
scope, authoritative cycle/position, current prefix/version, target and exact participant bindings.
The eventual outward reference must derive solely from the owner's authorized visible facts and
decision revision. No internal Source/Proof hash, hidden opponent identity, raw CP/TOE/ammo, RNG,
event count, or blocker reason may salt or order it. A future schema must freeze exact field order,
domain tag and caps before an executable reference can issue IDs; this design deliberately does
not invent an incompatible hash encoding.

## Terminal partition and complete bounded support

The retained predicate audit proposes 44 positive shapes, indices `36+6n` for `0 ≤ n < 44`,
`r012-c01` through `r055-c01`; 22 have Axis as actor and 22 Commonwealth. Each is a fresh adjacent
two-battalion state with current CP 4/0, ammo 10/10, TOE 10/10, Cohesion 0/0, normal Weather,
clear geometry, no current target use, prior attack or mandatory ZOC obligation. Original World
equals the present World except current CP. These remain **provisional**, not admitted actions.

The other 256 proposed empty rows comprise 244 with no adjacent opposing target and 12 adjacent
paid rows whose acting battalion has zero ammo. Historical Contact and Settlement in 36 paid rows
do not create a new loaded assault. Both battalions have one stacking point, below Land §10.11's
ZOC threshold. A supported-empty result requires complete authenticated participant, content,
obligation and current-segment checks; neither a missing public candidate nor an empty caller list
proves emptiness. An unexpected third unit, alternate weapon/action, mandatory obligation, hidden
blocker, malformed ancestry or unproven physical path returns `unsupported` for that input.

Every positive choice must cover the entire adopted Morale/assault/result surface without seed
filtering: 1,296 Morale pairs, 6,480 joint chart coordinates, all 360 selected role/differential
cells (357 printed and three explicit `CMB-SRC-RUL-001` amendments), and 8,840 normalized
result/refusal/capture cases. Proof must cover both capture roles, zero-loss Engaged, Retreat
priority, accepted and refused one-hex retreat, custody guard and escape, exact loss/CP/Cohesion/
RP arithmetic, all receipts, conservation, and retained future obligations. The bounded chart
branches exclude the expressly unsupported voluntary-surrender option. The sibling
result/settlement and retreat/custody audits in draft PRs #227 and #226 are review inputs;
they are not substitutes for an executable same-head certificate. The geometry report establishes
two actor orientations, both retreat dispositions, both capture roles and eight placement cases
for the selected six-hex content graph. No particular RNG seed may reduce this matrix.

## Acceptance matrix

| ID | Required executable evidence | Current state |
| --- | --- | --- |
| `P01-AC-001` | Recreate every original source from CreationRequest plus 11 independent ledgers; verify all parent bytes, R03 Source/Control/Proof, source pins and exact ordinal-two suffix before inspection. Tampered, missing, reordered or substituted ledger rejects. | Pending P01 reference. |
| `P01-AC-002` | Enumerate exactly 300 unique contiguous row keys and compare every output with authenticated R03 terminal vectors: 44 positive, 256 supported-empty only after all gates pass. | Predicate partition provisional. |
| `P01-AC-003` | Prove complete original-unit and representation binding, acting side, target membership, current versus prior segment target use, ZOC/obligation absence, Weather and no alternate selected-content action. | Provenance audit provisional; no P01 replay. |
| `P01-AC-004` | For all 44 positives, certify every adopted result coordinate, cost, loss, capture share, retreat/refusal and immediate settlement path, including source amendment, zero-loss effects and retained duties. | Sibling table audit available; same-head executable proof absent. |
| `P01-AC-005` | Certify both one-hex retreat orientations and all eight custody/reunion placement cases from current post-move World and CP. Reject any new blocker, overlong path or donor/guard shortage. | Sibling geometry audit available; same-head executable proof absent. |
| `P01-AC-006` | Enforce accepted `CMB-POL-009`: exclude voluntary surrender for the initial RC, distinguish automatic ammunitionless surrender, and fail closed on a state requiring surrender handling. Decline-only RBA and its cancellation fallback remain explicit. Never claim full source-legal action coverage. | Owner decision recorded; transition and source-condition proof pending. |
| `P01-AC-007` | Freeze canonical private candidate ID and audience-visible mapping; equal authorized histories preserve visible bytes, IDs and accept/reject behavior under hidden changes. | Exact encoding and privacy proof pending. |
| `P01-AC-008` | Test 300 row classifications, all nonterminal cuts, retries, source/ledger/field mutations, both acting sides, physical negatives, dependency faults, and exact output hashes with a frozen schema and caps. | No P01 schema, fixture or verifier yet. |
| `P01-AC-009` | Separately review P01 reference, then define P02 selection transitions and P03 native consumer with exact-head tests and hosted CI before any activation. | Not started by this design. |

## Freeze decision

Contract intent and acceptance matrix are reviewable. A follow-up may implement the private
reference against this proposed contract and use it to produce the missing evidence. Executable
P01 **freeze and admission** are not yet supported: sibling audits are independent reports on
other heads; retained-vector verification is not full ancestor replay; exact canonical
ID/schema/capacity and same-head all-result privacy tests are
absent. No provisional reference may publish a supported action, open P02 selection, or enter
native ordinal-two consumption before those gates close on one reviewed head. P02 and P03 remain
separate even after P01 passes.
