# Handoff: actual prepared-round contract work in progress

## Objective And Boundary

REL-AUD-02B authorized private executable actual prepared-round-entry contract, strict TDD.
Status: **WIP, semantic GREEN only; not REVIEW_READY or independently reviewed**.
Coordinator requested immediate checkpoint after current bounded check so continuation can
move to the existing visible author chat. No substantive policy pivot or scope blocker found.
Stop edits in this worker after writing this checkpoint. Coordinator owns dispatch and review.

Five primary owned paths remain:

- `docs/specs/combat-actual-round-entry-v1.md` (not created)
- `docs/specs/combat-actual-round-entry-v1.schema.json` (new untracked draft)
- `docs/specs/fixtures/combat-actual-round-entry-v1.json` (not created; no goldens generated)
- `docs/specs/verify-combat-actual-round-entry-v1.py` (new untracked WIP)
- `docs/design/combat-cycle-implementation-plan.md` (unchanged)

No native/host/public/UI/AI work, old spec/fixture/pin changes, synthetic relabeling,
commitment/results/positive CA completion, cost or RNG activation, commits or publication.
Administrative dated handoff/bootstrap/author packets permitted. Preserve others' edits.

## Canonical Sources

Read the full accepted `docs/research/combat-actual-round-entry-feasibility.md` and
`docs/work/handoffs/2026-10-09-actual-round-entry-research.md`; research Ready is accepted.
Read actual-selection-v1 spec/schema/fixture/oracle, sealed-round-v2, step design,
canonical Combat plan and `docs/work/plans/2026-10-05-actual-selection-dependency-disposition.md`.
The research's eight acceptance groups govern full completion; this handoff does not narrow them.
AGENTS, TDD and independent-review author skills read; Serena initial instructions read.
Codebase-memory list_projects/index_status/coverage ran on exact project
`sandtable-actual-selection-contract`: index ready but docs excluded, so focused reads used.
Serena active project was primary checkout; no Serena editing occurred there.

## Current Repository State

Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
Branch: `codex/actual-round-entry-contract`.
Base and HEAD: `0db4745a5ec631269c7751a244e1f49d84a81d6f`.
No commits, staging, publication or primary-checkout edits by this worker.
Pre-existing untracked `.serena/` belongs environment and remains untouched.
Other untracked files before this handoff: the new schema and verifier above.
This handoff itself is an additional administrative untracked file.
Research PR166 publication is coordinator work, not this contract delivery.

## Completed Work And Evidence

1. Semantic RED against unchanged predecessor established for both exact original owners.
   Exact rerunnable command: `python3 -B /private/tmp/actual-round-red.py`.
   Retained log: `/private/tmp/actual-round-red.log`.
   Exit1, tool wall0.713805958s. It verifies original state20/three step receipts and
   seed1/nextByteCursor2, expects the new Prepared CA endpoint, and observes original
   positive FA completion rejection `CMB-ASE-007` for both owners. Repro and log embedded below.
   Exploratory runs before valid RED failed on fixture keys (`actor` vs `owner`),
   artifact-wrapped ledgers, and RNG key (`cursor` vs `nextByteCursor`); those were harness
   construction errors, not counted as semantic RED. Final RED is the retained evidence.
2. Dependency capture before goldens: `/private/tmp/round-dependencies.py` instrumented
   Path reads and recursively traversed loaded local modules during original selected proof
   replay. `/private/tmp/round-dependencies.json` retains sixteen unchanged original hashes
   plus41 consumed transitive files. New draft schema contains both inventories, total57.
   Content-v7 added explicitly for new empty-AA certificate. This is provisional consumption
   closure: continue static/runtime scrutiny if later acceptance consumes further dependencies.
   Preserve all original16; freeze any additions before generating literals.
3. New mechanics implement full original selected source + independent selection ledger replay,
   derived Base with full original proof, separate supplemental opening policy/parent/budget,
   source/base/opportunity/round/slot/event/receipt domains, role-ordered full infantry slots,
   independent immutable opening floor/deadline, both seal orders, System cancellation,
   FA completion and Content/world/assignments-derived empty-AA certificate, positive CA stop,
   cancelled FA→AA→CA→Reserve Release, owned deep readbacks and original command+actor retries.
   Source/proof/control readers require original source and both independently supplied ledgers.
   A private internal transition/base helper exists; no standalone derived-base admission API.
   Cache key is full source plus both complete canonical ledgers; dependencies checked before cache.
4. Semantic GREEN command:
   `python3 -B docs/specs/verify-combat-actual-round-entry-v1.py --semantic`
   Exit0, output exactly:
   `PASS semantic: 4 Prepared CA25 traces; 30 cancelled no-attack Release traces`
   It ran asynchronously; precise total wall time/output hash was not captured. Do not invent one.
   Assertions cover both original owners, both seal orders4000→3500 after opening3000,
   versions20→21→22→23→24→25, role-ordered slots, five total step receipts, open Prepared CA,
   and all empty/attacker-only/defender-only × deadline/controller/confidence/null/below-floor
   cancellation routes ending25/26, six total step receipts/closed, retained partial seals,
   exact full World/RNG conservation including CP0/1/ammo10/TOE10/seed1/cursor2.
   First GREEN attempts exposed predecessor schema delegation collision (Idle and Control),
   now corrected: pe schema handles inherited kinds except selection-owned overrides.
5. `git diff --check` exit0. Since current primary artifacts are untracked, this checks tracked
   diff only; full text/layout/link/schema/adversarial validation remains unfinished.

## Decisions And Rationale

No Round2 synthetic transition import. New bounded mechanics are local, original selection
provides authenticated actual source. Same independent opening policy v2 with new familyv1.
Clock supplement version1 identifies its new family and preserves original configuration binding.
New schema fields are ordered closed inventory following current repository oracle patterns.
Empty-AA certificate binds actual content hash, World hash, candidate, role-ordered full
allocations, assignment receipt and component IDs; no caller-supplied empty-work admission.
These are WIP author decisions requiring canonical prose and final acceptance/review.

## Blockers And Limitations

No concrete approval/policy blocker. Full acceptance is unfinished, so no readiness claim.
Do not run normal command expecting a full pass: current __main__ accepts only `--semantic`;
normal invocation exits with `Full verification acceptance not implemented yet; use --semantic`.
No fixture exists and `fixture_case` helper has not generated one. Normal oracle must NEVER
regenerate fixtures after freeze. Need full error precedence, ownership, grammar/capacity,
privacy witness and cut/suffix/retry/mutation/systematic combined-error tests first.
Current draft error precedence/primitive checks are not yet proven against complete matrix.
`EmptyAaCertificate` is an inventory object with only hash embedded in step effect; finish
certificate field/context tamper acceptance. Check canonical external inherited identity arrays
retain original rules; new semantic arrays never sort. Do not silently widen source support.
Dependency runtime reader errors and warm cache/readback gates need per-path tamper tests.
Original four failures remain separately retained: Breakdown sequence pin, cycle-sequence
sequence pin, inherited Snapshot recursive admission, outward independent Content drift.
No new predecessor oracle runs yet; historical60s timeouts stay unverified. Use5–10min budget
for unchanged actual-selection/positive-entry/Result2 instead of false60s timeout failures.
No .NET build/test/format: contract-only and coordinator prohibited redundant full native gates.

## Immediate Next Actions

1. Visible author continuation verifies branch/status/hashes and reads this handoff and full
   accepted research. Inspect WIP schema/oracle before changing them; do not restart TDD.
2. Add adversarial acceptance tests before corresponding fixes: every cut, original source+
   independent selection/round ledgers, replay/readback/suffix, every command retry at every
   later cut and closed cancellation, disabled fresh admission recovery, primitive-first order,
   late timers/no-op, all combined errors, all16+41 dependency tamper/warm-readback paths.
3. Privacy witness: equal own history with/without opposing seal for both roles, time matrix,
   malformed/foreign proposals. Preserve own accepted outcome despite private opposite times.
   Re-sign every event/input/base/control/proof leaf, original source and causal receipts;
   forged actor/time with original ledger must fail. Do not claim coherent replacement alleged
   trusted ledgers provide ingress authentication. Mixed old families/future fields reject.
4. Capacity/depth/arrays/Int64/UTC/canonical spellings and caller/returned buffer ownership.
   Full source aggregate12 entry+7 selection+up to6 new events must stay retained independently.
5. After semantic/adversarial GREEN and frozen consumed dependency closure, generate one-time
   literal fixture using `fixture_case`; then normal verifier only compares immutable bytes.
   Write spec prose and canonical plan status, exact bounded original-oracle results (including
   four independent failures) and measured timings/output hashes. No native/public claim.
6. Freeze five primary hashes, neutral reviewer bootstrap BEFORE separate author rationale,
   dated handoff. Return REVIEW_READY to coordinator only. Coordinator dispatches fresh review
   GPT6.1medium, counter0of9, bounded3sets×3, recovery max2; do not self-review or spawn agents.

## Verification Commands

```sh
cd /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable
git status --short
git branch --show-current
git rev-parse HEAD
python3 -B docs/specs/verify-combat-actual-round-entry-v1.py --semantic
python3 -B /private/tmp/actual-round-red.py
```

Last command deliberately exits1 at original immutable unsupported boundary. First semantic
command already passed once on current bytes; do not repeat just for reassurance. Run proportionate
new acceptance after modifications. Do not erase source logs when measuring future evidence.

## Delivery Metadata

Date2026-10-09. Contract review counter0of9, no review dispatched, no goldens frozen.
Draft SHA256s:

- `docs/specs/combat-actual-round-entry-v1.schema.json`: `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8`
- `docs/specs/verify-combat-actual-round-entry-v1.py`: `17bd314dd122ef1f3a962ea4ceb8d6c943ed20079e0bce89be242c62152175b0`

RED script SHA256: `2a6567e6a5d9344ac6d07e8fd4ff19202b4e7c95b3e9fc2f206d62fdeaa46d85`.
RED log SHA256: `2554479915010ce2e501cbb114c74e156354f8cb1ce50121de68ec21a75ccd97`.

## Exact RED Reproduction

```python
import importlib.util,json
from pathlib import Path
root=Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/specs')
s=importlib.util.spec_from_file_location('selection',root/'verify-combat-actual-selection-v1.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
failures=[]
for side in ('axis','commonwealth'):
 row=next(x for x in json.loads(m.FIXTURE.read_text())['cases'] if x['owner']==side and x['variant']=='selected')
 source=m.parse(row['source']['canonicalUtf8'].encode(),'ActualSelectionSource');ledger=[m.parse(x['canonicalUtf8'].encode(),'Input') for x in row['trustedInputs']];state=m.replay(source,ledger)
 assert state['stateVersion']==20 and state['stepIndex']==3 and len(state['stepReceipts'])==3
 b=m.boundary(source);assert b['randomState']['seed']==1 and b['randomState']['nextByteCursor']==2
 inp=m.trusted(m.command(state,'complete-step',expectedPriorVersion=20,fromPositionId=m.edge(b)['combatPositionIds'][3]),'system',None,True)
 print('EXPECT GREEN: actual owner',side,'must reach prepared CA25 without charges; predecessor positive FA currently rejects ASE007',flush=True)
 try: m.apply(source,ledger,inp)
 except m.Invalid as error:
  assert error.code=='CMB-ASE-007'
  failures.append((side,error.code));print('RED:',side,error.code,flush=True)
assert not failures, f'{len(failures)} actual-owner prepared-entry semantic expectations unmet'
```

## Exact RED Log

```text
EXPECT GREEN: actual owner axis must reach prepared CA25 without charges; predecessor positive FA currently rejects ASE007
RED: axis CMB-ASE-007
EXPECT GREEN: actual owner commonwealth must reach prepared CA25 without charges; predecessor positive FA currently rejects ASE007
RED: commonwealth CMB-ASE-007
Traceback (most recent call last):
  File "/private/tmp/actual-round-red.py", line 17, in <module>
    assert not failures, f'{len(failures)} actual-owner prepared-entry semantic expectations unmet'
           ^^^^^^^^^^^^
AssertionError: 2 actual-owner prepared-entry semantic expectations unmet
```
