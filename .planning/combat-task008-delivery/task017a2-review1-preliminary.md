# Task017A2 preliminary independent review

Review instance: 1 of 3. Blind first pass completed before author packet.

Target: base `a1cd42525f0f0c055f62270306d1e8ed832054d2`, candidate `3811202ee23b3ef5c972b85953f19ca77759b36f`.
Read-only source review in detached shared clone `/tmp/task017a2-review1`; integration initially clean. Exact diff: four source/test paths plus plan and six administrative documentation/evidence paths. No fixture/schema changes.

## Evidence and preliminary concerns

- Read AGENTS, Task017 refinement/dispatch, frozen Reserve Release specification/schema, tests before implementation, complete lifecycle/models/codec, relevant oracle transition, timing maximum/configuration binding and documentation diff.
- No actionable correctness finding identified. Transition is faithful to frozen oracle: canonical queue, one timing budget, first-I conversion/later-II bulk retention, owner deadline equality rejection, system fallback preserving original admitted actor, exact command/actor receipt lookup before stale checks, explicit completion.
- Initial base validation remains byte-for-byte unchanged. Apply accepts independently retained base/request and admitted input/event history, reconstructs prior state internally; ReadState compares entire canonical replay bytes. No caller-owned state authorizes transition.
- Defensive collection copies and result-byte copies close mutation paths. Replay owns event bytes, validates all raw events before trusted base access, then compares every replay result byte.
- Native tests pin all 44 isolated rows and exact 132 event hashes/176 state hashes and lengths/two terminal literals; four historical Result1 rows explicitly excluded. Negative cases exercise clock/retry/actor/order/recovery/capacity and malformed bytes.
- Plan preserves parent017 and actual World/positive-lineage/public-admission gates. Administrative prose does not claim accepted A2. One plan status phrase still says implementation pending while other docs say implemented with acceptance pending; minor transient bookkeeping, no substantive scope ambiguity.
- Runtime verification still running. Initial `dotnet test ... --no-restore` in clean clone found no test projects; normal restore then failed NU1900 because sandbox could not resolve api.nuget.org. Approved restore succeeded. Focused native test build and frozen oracle running. `git diff --check a1cd425 3811202` passed.

Next: reconcile author claims, collect focused native/oracle outcomes, issue bounded verdict. No source edits, CCE/memory, prior review material, or additional reviewers used.
