# Independent Engineering Review Report

Review instance: 10 of 10

## Findings

### P2 — Clarify the circular parent003/Task004 closure gate

Evidence:

- `docs/design/combat-cycle-implementation-plan.md:213-216` assigns D2c.4 full composition and says
  parent003 closes only after `004 evidence mapping is ready for B`.
- `docs/design/combat-cycle-implementation-plan.md:128-129` defines Task004 as the owner of the
  complete72-AC evidence index and gives it a formal dependency on Task003.
- `docs/design/combat-cycle-implementation-plan.md:245-247` again says004 maps every design AC and
  that completed fragment oracles do not by themselves close parent003.
- `/tmp/check-combat-d2c1-planning.py:29-41` validates only backward numeric dependencies in the25
  top-level task rows. It does not parse D2c child prose, so its passing “backward dependencies”
  result does not resolve this cycle.

Failing scenario and impact: when D2c.4 completes, maintainers have no unambiguous legal next state.
The new child rule can be read to require a Task004 output before closing003, while the formal Task004
row prevents004 from starting or completing until003 is closed. This can either stall checkpoint B
or encourage an undocumented bypass/premature parent closure. The ambiguity is newly formalized by
the D2c.4 refinement in this review range, although related pre-existing prose already coupled004's
map to003 closeout.

Smallest credible correction: name an explicit003-owned “Task004 handoff” artifact and say that D2c.4
closes003 once that handoff is complete; Task004 then consumes it and completes the72-AC map. If
003/004 are intentionally iterative or concurrent, change the formal dependency and checkpoint rule
to say so. Correct this before D2c.4/Task004 closeout; it does not block the next D2c.2 contract slice.

### P3 — Synchronize the source inventory's current checkpoint

Evidence:

- `docs/research/combat-cycle-source-inventory.md:3-27` is written as a current status paragraph,
  but line24 still says D2b.2 repeat/finish control is next and mentions only D2b.1 as complete.
- The reviewed head's plan (`docs/design/combat-cycle-implementation-plan.md:20-32`), docs index
  (`docs/README.md:103-105`), README, roadmap, and new D2c.1 spec all say D2b.2 and D2c.1 are complete
  within their bounded scopes and D2c.2 is next.
- The author says all navigation files reflect current status. The provided planning checker includes
  the source inventory only in its link/anchor audit and does not check checkpoint wording.

Failing scenario and impact: the combined plan links the source inventory as the entry point for the
six governing designs. A maintainer following that status can select an already-completed task or
misread the current contract frontier. This is bounded documentation drift; it does not change the
contract behavior or open production gates.

Smallest credible correction: update the inventory's top status paragraph to mark D2b.2 and D2c.1
complete in their private/isolated scopes and identify D2c.2 as next. Preserve dated historical notes.

No P0 or P1 finding was found. No heavy pivot is required.

## Implementation Assessment

The two new contract packets are coherent within their stated non-production boundaries.

- D2b.2 independently reconstructs its exact bounded Release/result inputs before assessment. It
  rejects incomplete Release, altered World bindings, unsupported armed Combat, non-allowlisted
  progress, forged Movement proofs, and version/ordinal overflow. Movement witnesses reuse D2a's
  terrain plus maximum Contact/Engaged break-off rule and incremental excess-CPA DP. Repeat uses the
  result authority version and pre-event prefix, resets only the declared next-cycle surfaces, and
  preserves World/RNG/attack/release/future-obligation state. Finish enters the same-slot Truck Convoy
  successor and expires pending next-Movement exceptions without stage housekeeping.
- D2b.2 timing/retry behavior matches the contract: equality is late, exact retries read back before
  stale handling, unavailable/regressed clocks finish deterministically, old timers no-op after
  closure, and an owner-choice opening reserves authority-version room for both open and close.
- D2c.1 accurately inventories20 currently admitted event types/version pairs from the pinned current
  dispatch/codec sources. Every declared successor is exactly current+1. Created11 remains C2-owned;
  only Reserve-completion2 is frozen here, and only for the named isolated first-stage/first-slot
  profile. The other18 payloads remain declarations rather than falsely claimed codecs.
- The isolated opening transition is atomic and avoids self-hashing: cycle1 binds the pre-event prefix
  and result version, the receipt hashes the unsigned canonical event, and the Chronicle prefix hashes
  the final event bytes. Replay derives the state from a separately admitted base and at most one
  event; exact retry authenticates the actor and full accepted input before returning the prior
  receipt. World, RNG, CP, ammunition, TOE, Reserve membership, and creation bindings are preserved.
- The implementation-owner graph in the D2c.1 inventory is internally acyclic, bounded to five primary
  files per child, places019A after008D, and makes shared008H restoration depend on all codec families
  plus019A. Its parent003/004 closure wording is the separate P2 above.

The Python oracles share helpers and are prospective contract models. Their exact goldens and
mutation/replay checks are useful compatibility evidence, but they do not prove future C# serializer
parity, production publication, actual restart, reachable creation-to-Reserve provenance, public
privacy, or simulator behavior. The specs state those limits consistently.

## Plan Review

The plan remains conservative about capability and production activation:

- D2b.2 and D2c.1 close only bounded private/isolated contract checkpoints. D2c.2-4, parent003,
  Task004, checkpoint B, and runtime Tasks005-025 remain open.
- D2c.2 retains actual creation/preamble/Weather/Reserve provenance; D2c.3 retains inherited
  Movement/Reaction/Breakdown, positive Reserve movement, progress provenance, and armed Combat
  continuation; D2c.4 retains complete World/Snapshot/history/capacity composition and CON-002-004
  reconciliation.
- Public side projection, privacy/error behavior, complete AC mapping, production codecs/readers,
  runtime replay, admission, and Exercise/Runner evidence remain assigned to004 and later tasks.
- The five-primary-file slice rule,25 top-level task IDs,72 design AC IDs, and8 policy IDs are retained.
- No plan requirement authorizes a second assault, general resupply, real RBA, released-Reserve
  offensive Combat, hosted decisions, or a complete playable campaign.

The one plan defect is sequencing, not architecture: the P2 must be made explicit before parent003
and Task004 can close, but it does not require splitting workstreams or replacing the agreed design.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| D2b.2 freezes guarded repeat/finish and Movement expiry only for a private exhausted-ammunition boundary. | Cycle spec lines8-48 and106-139; schema; fixture cases; oracle `assess`, `transition`, `expire_movement`; passing run. | Confirmed | Verdict is limited to the declared bounded contract. |
| Material progress is commitment or material Reserve disposition, not receipt count. | Cycle oracle lines107-116 and425-431; design allowlist lines207-223. | Confirmed for the admitted profiles | Other inherited progress remains D2c.3. |
| Repeat/finish preserve identity, resources/history, timing, and retry invariants. | Cycle oracle lines231-316 and boundary checks;64 traces/164 cuts. | Confirmed within the oracle model | Production atomic publication/restart remains unimplemented. |
| D2c.1 inventories20 source-backed current/successor declarations;18 payloads remain unfrozen. | Schema rows37-197; oracle inventory checks lines175-203; independent current+1 probe. | Confirmed | No declaration is mistaken for a codec. |
| First opening is one atomic Reserve-completion event using the pre-event prefix/result version. | Inherited spec lines59-117; oracle `authority`, `generate`, `replay`, `apply`; four actor/status traces. | Confirmed | Actual predecessor trust/provenance remains D2c.2. |
| The009-child implementation graph is acyclic and bounded, with first opening before shared restore. | Schema owners lines199-277; oracle topological check; independent owner count/max-size probe. | Confirmed internally | Parent003/004 sequencing still has the P2 ambiguity. |
| README/docs/roadmap/tech-design/naming/Orleans navigation reflect current status. | All changed navigation docs plus linked source inventory. | Contradicted in part | Produces the P3 finding; main plan/README/roadmap frontier is correct. |
| All17 Python contract/research oracles pass. | Independently executed14 spec and3 research commands. | Confirmed | Evidence is reproducible at the frozen head. |
| D2c.1 used five primary files and a separate six-file navigation commit; existing contract bytes were not silently rewritten. | Commit stats for `84a049f` and `cef6cb9`; exact range diff and manifest. | Confirmed | Slice/accounting claim is honest. |
| RED failures and one-time fixture construction occurred as described. | Current code, fixtures, and GREEN results only. | Unverified historical process; no adverse current evidence | Final verdict does not rely on the claimed RED chronology. |
| No .NET/runtime/simulator behavior changed or is claimed. | Name-status/stat for the exact range; no `src/`, `tests/`, or `scenarios/` changes. | Confirmed | Python and documentation checks are proportionate for this range. |

## Verification Performed

All commands ran read-only from `/private/tmp/sandtable-combat-creation-snapshot` with a non-login
shell unless stated otherwise.

Scope and integrity:

- `git status --short --branch`, `git rev-parse HEAD`, `git rev-parse --abbrev-ref HEAD`, and exact
  base/head resolution: correct branch/head and clean worktree.
- `git diff --stat BASE HEAD`, `git diff --name-status BASE HEAD`, and `git log --oneline BASE..HEAD`:
  five expected commits;17 files; `+1963/-41`; no runtime/test/scenario paths.
- SHA-256 comparison of every manifest entry: all17 matched before inspection.
- Exact byte comparison of `/private/tmp/sandtable-review-10/diff.patch` with
  `git diff --binary BASE HEAD`: matched; SHA-256
  `d3dd0ec9402e7c2a8fa094e5ec0786c04f9910b709c5acd4393334e3ccafd8e8`.
- `git diff --check BASE HEAD`: passed.
- `python3 /tmp/check-combat-d2c1-planning.py`: passed621 local link targets,17 anchors,25 stable
  top-level tasks with backward row dependencies,72 stable AC IDs, and8 policy IDs. Its top-level-only
  dependency parsing is noted in P2.
- Independent successor probe:20 rows, zero non-incrementing successor versions;9 owners, maximum
  five primary files.

Contract oracles, all passed:

- `python3 docs/specs/verify-combat-content-v7.py`
- `python3 docs/specs/verify-combat-creation-ledger-v1.py`
- `python3 docs/specs/verify-combat-world-settlement-v1.py`
- `python3 docs/specs/verify-combat-rules-inputs-v1.py`
- `python3 docs/specs/verify-combat-cycle-sequence-v1.py`
- `python3 docs/specs/verify-combat-authority-envelope-v1.py`
- `python3 docs/specs/verify-combat-selection-steps-v1.py`
- `python3 docs/specs/verify-combat-sealed-round-v1.py`
- `python3 docs/specs/verify-combat-result-settlement-v1.py`
- `python3 docs/specs/verify-combat-snapshot-composition-v1.py`
- `python3 docs/specs/verify-combat-ordinary-movement-v1.py`
- `python3 docs/specs/verify-combat-reserve-release-v1.py`
- `python3 docs/specs/verify-combat-cycle-control-v1.py` —19 cases,64 traces,164 cuts,1748
  mutations,700 raw rejects,43 boundary checks,216 cost coordinates.
- `python3 docs/specs/verify-combat-inherited-successors-v1.py` —20 declarations,9 owners,4 opening
  traces,8 replay cuts,216 event mutations,32 raw rejects,88 boundary checks.

Research oracles, all passed:

- `python3 docs/research/verify-combat-source-freeze.py`
- `python3 docs/research/verify-combat-rng.py`
- `python3 docs/research/verify-reserve-release.py`

No .NET build/test, Orleans probe, or simulator run was repeated: the range changes no production or
test source, and review9 already retained the focused Orleans evidence. This review does not convert
that prior evidence into a claim about the new prospective contracts.

## Open Questions And Residual Risks

- D2c.2 must replace the isolated prefix/designation receipt with an accepted
  creation-to-preamble/Weather/Reserve chain and must prove exact RNG/header/receipt continuity.
- D2c.3 must authenticate Movement-end/progress evidence, admit positive Reserve movement, and prove
  armed Combat continuation without converting unsupported capability into “none legal.”
- D2c.4/004 must prove complete Snapshot12/World/history/obligation capacity and resolve the P2
  handoff order before closing parent003/checkpoint B.
- The20 successor declarations are not codecs for the18 declared-only families. Future family packets
  may reveal field, version, or capacity constraints that require explicit contract revision.
- Public privacy equivalence, safe error mapping, stable audience/action references, production C#
  parity, actual restart/recovery, atomic Chronicle publication, and simulator evidence remain open.
- The source inventory status drift should be corrected during the next navigation update.

## Verdict

**Ready with non-blocking follow-ups.**

The frozen range is ready to continue to D2c.2 within its explicit non-production contract scope.
The two bounded transition packets are internally coherent, their claims are appropriately limited,
and all proportionate checks passed. The P2 is a real plan defect that must be corrected before
D2c.4/Task004 closeout, but it does not require an architecture change and does not prevent the next
already-scoped provenance checkpoint. The P3 is bounded documentation drift.

Review10 is the user-authorized final instance. No further review instance, agent, or workstream is
authorized or recommended.

## Recommended Next Actions

1. Clarify the003-owned handoff versus004-owned complete AC map before D2c.4/Task004 closure.
2. Update the source inventory's current-status paragraph to D2c.2 next.
3. Proceed only with D2c.2's already-planned creation/preamble/Weather/Reserve provenance split;
   retain every synthetic-trust limitation until independently derived evidence replaces it.
4. Keep parent003, Task004, checkpoint B, and Tasks005-025 open until their stated evidence exists.
5. Stop the independent-review loop at10of10 and return any later heavy pivot to the human decision
   gate rather than creating another review instance.

## Author Disposition — after the frozen review

The report above is retained from the fresh reviewer without alteration. Its verdict applies to
`a96d2a1474536cb6cc7ec9e261ebf45fedcacc47..cef6cb94986922aceb359bb055f9c3c951e17c70`, five commits,
17 files, +1963/−41. Review task `01a07dd4-83cf-7203-8f45-5158625f3a11` completed; all17 manifest
hashes and clean target HEAD were independently checked before applying the following corrections.

| Finding | Disposition | Correction |
| --- | --- | --- |
| P2: circular003/004 closeout wording | Accept; fixed | [Combined plan](../design/combat-cycle-implementation-plan.md) names a003-owned Task004 handoff section in the planned D2c.4 composition packet.003 closes after its own integrated checks/handoff;004 then consumes those outputs and completes the72-AC map. B requires both. The formal004 dependency on003 is unchanged. |
| P3: stale source-inventory frontier | Accept; fixed | [Source inventory](../research/combat-cycle-source-inventory.md) now marks D2b.2/private control and D2c.1/isolated opening complete, with D2c.2 actual provenance next. Historical notes remain intact. |

These are bounded documentation corrections. They change no contract/schema/fixture/oracle or
runtime bytes and introduce no new gameplay policy, workstream or architecture. The reviewer found
no heavy pivot. Author accepts the remaining limits: D2c.2–4,004/B and all production/simulator
consumers remain required. Review10of10 exhausts the user-authorized review flow; no additional
independent pass was launched for these corrections.

Post-review verification: local link/anchor and stable25-task/72-AC/8-policy checks, explicit manual
inspection of003→004→B handoff order, whitespace check, and byte comparison of all eight D2b.2/D2c.1
contract artifacts against the frozen head. The full17-oracle sweep passed in both author and
independent review; it was not repeated for documentation-only corrections. This author disposition
is not a new independent verdict.
