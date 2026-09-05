# Breakdown adjudication design review 3

Review instance: 3 of 3. Target: `0512ec2..b73883f` on `codex/breakdown-adjudication-design`.

## Preliminary Review Ledger

Recorded before opening `/tmp/brk-review-3/author.md` or retained review reports. Partial blind pass: governing research packet and local task plan themselves disclose earlier review conclusions; those summaries were exposed during requirement inspection. Earlier reports and author rationale were not consulted for these preliminary judgments.

- Numeric transcription matches all nine columns of inspected Common Charts p. 7. Checker passes 324 coordinate/band cells and 606 conditional arithmetic combinations; source PDFs match retained hashes.
- Land 21.34 explicitly identifies the `33` label with one-third; recommend DEC-004 as proposed.
- Land 21.24–26 requires stops and retained stage BP, supporting stop lifecycle rather than checkpoint-only dice. Existing event factory freezes Reaction opportunities after movement; DEC-005 preserves that ordering, but remains an owner scheduling ruling.
- DEC-006 is narrow but useful: exact ordinary/Reaction BP plumbing, strict replay, stop continuations and persistent equipment survive expansion. Existing motorized-infantry packs cannot become admitted Breakdown examples. Their exclusion must remain visible in activation/fixture migration evidence.
- DEC-007 structurally removes the 21.41 larger-than-battalion origin exception. Persisted lots are necessary once survivors can move again. Capture remains an explicit excluded rule, including cases involving battalion-size units; do not describe admitted campaigns as full Section 21 simulation.
- Task 001 is correctly placed before consumers. Freeze actual standalone-Truck movement eligibility, finite continuation states, version matrix and capability certification before code. Current normal v2 move factory accepts combat elements only, so admitting Truck content alone would not produce a playable vertical.
- No actionable defect established in this research/design target. Exact contract details and production proofs remain future work, not passing claims.

## Findings

No actionable findings in the decision packet. **Ready** applies to owner decisions and the next contract-freeze task, not production activation.

The earlier placement concerns are addressed by a structural admission invariant, not a runtime hidden-state test: design lines 114–129 excludes every qualifying larger combat unit/formation/aggregate throughout admitted histories and defers immutable route-start threat evidence to general placement. This satisfies Land 21.41's timing requirement within the proposed narrow profile. Certification remains unimplemented and must be precisely defined in Task 001.

## Plan Review

Exact reviewed HEAD is `b73883f1927926e7974286dd117d30ce5acfa3c2`; branch and twelve changed paths match bootstrap. Candidate contains documentation, numerical research data/checker, and retained review reports. User edits to `.gitignore`, `AGENTS.md`, `.claude/`, `.github/copilot-instructions.md`, and `CLAUDE.md` are excluded. This reviewer adds only this report; no source, plan, staging or commit changes.

Research plan is satisfied: source evidence, current accounting gap, concrete decisions, proposed contracts, seven dependent tasks, twelve acceptance criteria and synchronized architecture/status documents. Current `MOV-AC-017` excludes post-creation BP mutation. `CampaignElementMovedV2Factory.ProjectMoveForAuthority` preserves the existing BP record; Reaction invokes the shared movement calculator. A coupled successor is justified. No historical BP-zero trajectory can be repaired by silently interpreting its CP as BP.

Ordered dormant Rules → state/creation → accounting → stop authority → public activation → Runner evidence is credible. Avoiding transport and general placement initially is reasonable because exact BP arithmetic, RNG replay, lots and finite continuation machinery remain useful in broader support. The one-cohort restriction should remain an admission predicate, not become a hard-coded single-slot loss model.

Two details deserve explicit Task 001 treatment, within its existing schema/activation remit:

- **Playable standalone Trucks:** `src/Cna.Core/Campaigns/CampaignElementMovedV2Factory.cs:38` rejects noncombat elements. Task 001 must specify the successor Truck action/cost/eligibility path, its interaction with combat-only trigger rules, and zero-working movement rejection. A new content fixture alone is insufficient. Do not reclassify Trucks as combat units merely to reuse the current factory.
- **Capability disposition:** design lines 104–112 excludes current motorized-infantry fixtures while lines 157–162 replaces current identities at activation. Preserve current CP, Reserve, ZOC, Reaction ordering and secrecy through admitted non-cohort equivalents where possible. Keep original artifacts/tests explicitly historical and motorized BP calculations explicitly dormant. Record any capability without a valid successor fixture as deferred, rather than deleting coverage or claiming unchanged support. A second current legacy engine is unnecessary.

Those are contract-freeze outputs, not demands for production changes or new workstreams. General grouping, transport, origin placement, capture, repair and later-stage authority remain outside this delivery. No heavy pivot is required for the recommended path.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Research-only change; no production authority | Exact Git path list; packet/design status; task graph | Confirmed | No .NET gate needed for this diff |
| Numeric surface source-locked | All nine chart columns, JSON bounds, checker, three PDF hashes | Confirmed | No transcription finding; arithmetic remains proposed |
| Current moves preserve BP; shared normal/Reaction calculation | Normal factory `ProjectMoveForAuthority`, Reaction `CreateMove`, current MOV specification | Confirmed | Shared successor BP integration is necessary |
| Exact cursor evidence is required | `SandtableRandom.RollD6` lines 48–62 | Confirmed | Replay must account for rejected bytes, not assume two draws |
| Rainstorm must transform inputs | `Cna1979Breakdown.GetWeatherColumnShift` lines 229–252 | Confirmed | Neutral post-transformation shift must be deliberate |
| Capability invariant removes hidden placement failure | Design lines 89–130; Land 21.41 | Confirmed as design | Exact creation/transition predicate still required |
| Existing fixture capabilities need concrete coexistence treatment | Current combat-only normal move factory; design lines 104–112 and 157–162 | Confirmed pending work | Require explicit successor/historical/deferred fixture matrix in Task 001 |
| Seven tasks and twelve acceptance criteria are ready to freeze | Design task and acceptance tables | Confirmed | Approve decisions first; do not infer implementation approval |

## Verification Performed

- `python3 docs/research/verify-breakdown-outcomes.py`: passed 324 coordinate/band cells, complete unique ranges, monotone columns, nine source probes and 606 proposed-loss bounds. Fixture SHA-256 matches `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401`.
- Visual review of `/tmp/brk-land-33.png`, `/tmp/brk-land-34.png`, `/tmp/brk-chart-7.png`: all nine numeric bound rows match chart; stop, stage continuity, sequential dice, rounding, placement and transport limitations match cited rules.
- `shasum -a 256 /tmp/brk-land-rules.pdf /tmp/brk-common-charts.pdf /tmp/brk-errata.pdf`: all three hashes match retained source lock.
- `pdftotext -layout /tmp/brk-errata.pdf - | rg -n -A3 -B2 '21.12'`: M13/40 BAR correction confirmed; no Truck/dice correction in that clause.
- `git diff --check 0512ec2..b73883f`: passed, no output.
- Read-only Python check against exact committed changed Markdown: 18 local Breakdown link targets exist; anchors and unrelated links not checked.
- CCE used for discovery; two attempted compressed-result expansions returned `Chunk not found`, so exact located supporting source paths were read directly. Session recall occurred only after preliminary ledger; it returned prior review summaries, treated as testimony.
- No .NET build/tests run; reviewed diff changes no runtime behavior. No production acceptance criterion is claimed satisfied by numerical research checks.

## Open Questions And Residual Risks

Owner acceptance remains necessary for every proposed decision. DEC-006 deliberately narrows public scenario support; this is a product tradeoff, not merely an internal refactor. Existing motorized-infantry Reaction loss remains unsupported. Public non-cohort Reaction tests prove lifecycle with zero rolls; they do not prove positive Reaction loss. Dormant authority tests must cover that machinery separately without claiming public support.

The conservative larger-than-battalion exclusion removes the origin-placement exception only. Land 21.52 capture can involve a single battalion, so this bound does not establish capture impossibility. Capture is deliberately absent from this first capability; setup/spec/status must retain that limitation and must not call the result full Section 21 support. Broader historical scenario fidelity would require a separately approved expansion.

Inherited terrain normalization was not re-audited. Full strict contracts, privacy permutations, rejected-byte vectors, semantic forgery admission and public fixture trajectories are future executable evidence. Do not make the continuation representation arbitrarily recursive or allocate a roll/action per secret group.

## Verdict

**Ready** for BRK-RSH-002 owner decisions and a bounded BRK-TASK-001 contract freeze after those decisions are accepted. No production approval implied. Review instance 3 of 3 is final for this delivery; no further reviewer, implementation fix or workstream was created.

## Recommended Next Actions

Engineering recommendations, not owner approvals:

| Decision | Recommended choice | Reason |
| --- | --- | --- |
| `BRK-DEC-004` | Accept exact `1/3` for printed `33`, exact fractions for other labels, upward loss rounding and one-point/10% exception at the admitted standalone check unit | Land 21.34 explicitly equates the label with one-third; 21.35 defines rounding/exception. Exact rational arithmetic avoids a 100-point discrepancy. |
| `BRK-DEC-005` | Accept explicit stops, triggered Reaction before deferred phasing resolution, reactor stop before the next participant or phasing continuation, and recorded System-close continuation | Land 21.24–26 requires stop checks. Existing post-move frozen Reaction semantics favor preserving interruption order; this remains an explicit software ruling. |
| `BRK-DEC-006` | Accept the public certified synthetic profile with at most one unladen standalone Truck cohort per side, no cargo/passengers or split/merge/create capability | Produces useful authoritative losses without inventing allocation or motorization rules. Require the capability/fixture disposition matrix; accept public motorized-infantry exclusion explicitly. |
| `BRK-DEC-007` | Accept persistent lots now and structural exclusion of larger-than-battalion combat units, represented formations and aggregates; defer general origin placement/capture/towing/repair | Land 21.41–44 requires retained equipment location and immobility. Structural exclusion removes hidden-dependent origin support without saving unnecessary secret predicates for this profile. |

**Next bounded task: BRK-TASK-001 only.** After owner acceptance, produce the governing specification and accepted decision record; enumerate existing/successor identities and hash dependencies; freeze closed stop/continuation diagrams, Truck movement/action contracts, exact profile certification/rejection predicates, disclosure changes, and fixture/capability disposition. Map all twelve acceptance IDs to planned executable vectors and owning tasks, including zero-roll versus zero-loss, deferred phasing plus reactor stop, forced close, zero survivors, survivor movement leaving lots fixed, legacy/mixed identities and hidden-state transcript equivalence. Synchronize roadmap and architecture status. Exit with no open authority ambiguity and no runtime implementation, generated consumers or activation changes.

Proceed through the existing serial graph after the contract freeze is accepted. If owner rejects narrow public support and instead requires current motorized-infantry fixtures to acquire authentic losses immediately, return a heavy-pivot gate: transport/capacity and grouped allocation alter scope and dependencies. Do not launch those streams implicitly. This final review closes the configured review loop; any materially revised delivery or review allowance requires an explicit initiating-task/human decision.
