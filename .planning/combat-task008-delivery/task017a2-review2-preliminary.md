# Preliminary independent review

Review instance: 2 of 3. Blind ledger recorded before opening author explanation.

Frozen base `32a038c322c561f642af2aedafe37165147f4939`; candidate `a4b4f630d1250fd00d61ebae486f18813ffedd01`. Detached clone `/tmp/sandtable-review2-a4b4f6`. Integration status clean when inspected. Diff: four source/test paths plus seven documentation/dispatch/author paths; author path not opened during blind pass. No fixture/schema changes.

Read AGENTS, Task017 refinement/child table, dispatch, frozen Reserve Release spec, oracle transition/readback, implementation and tests. Inspected tests before implementation. No CCE/memory, prior reports, author rationale or other reviewers consulted.

## Preliminary ledger

- No actionable implementation defect identified so far. Native transition matches frozen oracle ordering: actor/shape checks precede retry; retry precedes stale/terminal checks; one opening budget; fixed pending queue; mandatory conversion versus optional retention; receipt-linked projection follows event hashing.
- Trust boundary sound on inspection: Apply replays independent base/request and admitted inputs; persisted state cannot authorize transitions. ReadState compares complete canonical replay bytes. Converted-II/current-release states avoid inappropriate initial-base validation.
- Tests explicitly count 44 isolated traces, 132 event hashes, 176 full state lengths/hashes and two terminal literals; four settled Result1 rows excluded. Every cut round-trips through replay reader; retries checked after completion. Additional negatives cover tampering, shape, overflow, actor, clock, queue and owned collections.
- Plan preserves separate 017B/native adapter and positive lineage prerequisites, parent017 remains open. README/design/naming/roadmap distinguish isolated lifecycle from actual campaign provenance. Exact manifest respected.
- Open verification: focused .NET run initially failed with sandbox local-pipe SocketException (13), exit134. Approved out-of-sandbox retry and frozen Python oracle currently running. Need confirm actual test outcomes and then reconcile author claims.
- Residual limits: bounded private mechanism only; no authentic positive campaign lineage, World projection, Snapshot/durable/public integration, Movement or cycle control proof.
