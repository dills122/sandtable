# R03: authentic moved repeat Breakdown completion

**Status:** private reference freeze candidate; exact-head gates and native runtime open.
**Base:** `9aaeacd` (`origin/main`, October 16, 2026).
**Branch:** `codex/combat-r03-breakdown-contract`.

## Working checkpoint (October 16)

R02 verifier enumerates exactly 300 authentic terminal rows: 36 paid and 264 armed. Its complete
replay takes the original CreationRequest and ten independent ledgers. The R03 schema currently
initially pinned 106 runtime source paths: the 104 inherited R02 pins plus the R02 schema and verifier. New
limits and physical closure remain provisional (`closureFrozen=false`); no native consumer is
authorized by this checkpoint.

The initial R03 reference replays the authentic parent before its own ledger and emits one System
completion into the adjacent catalog Combat position. Four representative paid/armed zero/max
rows and a separate distributed 15-row selection across ordinals 0–275 pass. Six focused tests
cover retained vectors, exact retry/readback, actor/action/context precedence, nonterminal or
altered ancestry, event and readback forgery, dependency pin rejection and duplicate-key framing.
An uninstrumented all-300 normal run was stopped after about eight minutes without a row count;
its exit 130 is not a pass. The full catalogue, all prior-cut negatives, physical-fault matrix,
structural capacity derivation and immutable reference freeze remain open. The representative
fixture is deliberately labelled as a subset; it cannot certify the 300-row catalogue.

The next checkpoint added per-row progress and retained full canonical vectors for the long
catalogue run, structural size arithmetic, and the initial 540 physical dependency fault rejections. One paid
and three armed authentic nonterminal R02 cuts reject. The first full 300-row run then completed,
but these checks did not freeze the contract. See the
[R03 reference evidence checkpoint](../reviews/2026-10-16-actual-moved-repeat-breakdown-evidence.md)
for exact commands, observed results and open gates.

Independent review of that run found two contract defects: the R03 receipt was absent from the
inherited cumulative ledger, and the mutable pin map was not compared to its captured manifest.
The initial 300-row PASS is therefore superseded. The corrected reference appends the same receipt
to both ledgers, pins both authentic parent catalogue fixtures, and rejects pin-map mutation.
Its corrected 300-row rerun passed, with a vector audit of all 56 distinct roots and 300 unique
rows (36 paid, 264 armed). The physical pin set now contains 108 runtime source paths, including
both authentic parent catalogue fixtures; all five fault modes across those pins and two own files
reject (550/550). A later framing review found that inherited numeric out-of-range errors were
being reported as shape errors. R03 now classifies its own numeric leaves and numeric leaves in the
borrowed R02 Proof locally; 32 framing negatives pass. The final exact-verifier-hash 300-row replay
and vector audit also passed, with all canonical vectors retained. A complete 788-cut audit passed
against the corrected checkpoint vectors using the final verifier; the same audit against final
vectors passed. Six of six representative fixture tests matched retained canonical cases. The
schema now marks a freeze candidate with `closureFrozen=true`; this byte change requires a new
exact-head 300-row replay, vector and 788-cut audits, focused gates, fresh review and hosted CI
before the reference can be declared frozen or native work can begin. See the evidence checkpoint
for hashes and commands.

## Boundary

R02 ends at the canonical ordinal-two Breakdown Determination entry after authentic paid or armed
repeat Movement completion. R03 should add one private, trusted-System `complete-breakdown-segment`
transition from each supported R02 terminal to the adjacent ordinal-two Combat Position
Determination entry. The active ordinal-two cycle remains open. This transition must not consume RNG,
change resources or World, reset progress, execute Combat, or expose public actions.

Use [R02](../../specs/combat-actual-moved-repeat-movement-completion-v1.md) as the actual parent and
the older [ordinal-two Breakdown contract](../../specs/combat-actual-repeated-breakdown-completion-v1.md)
as a semantic pattern only. That older contract accepts a different lineage and seven ledgers; its
Source, proofs, capacity limits, receipts and tests are not authority for R03.

## Ordered delivery

1. **Research and inventory.** Enumerate all 300 authentic R02 terminal rows and their paid/armed,
   owner, move-cut and zero/positive partitions. Pin the exact parent Source, Control, Proof,
   receipts, current MovementEndProof, position and input ledger count. Inventory inherited physical
   dependencies and measured native memory before choosing raw caps or CI partition. Keep every R02
   cut and retry available as a negative predecessor.
2. **Contract first.** Add a closed v1 spec/schema/reference/compact fixture for one System event.
   Independently replay the original CreationRequest and every R02 ancestor input ledger before the
   new input, event or readback claim. Bind a distinct R03 Base, capability, event, receipt and proof
   to the reconstructed terminal, cycle, position, version, rules/configuration and dependency
   manifest. Specify actor/action authorization, invalid-source precedence, exact retry behavior,
   canonical bytes and error mapping. Preserve all inherited state and both Movement proofs.
3. **Reference evidence.** Show genuine missing-publication RED only after authentic R02 terminal
   preconditions. Then verify one-event GREEN for every terminal, each event cut, every accepted
   retry and all nonterminal predecessor cuts. Challenge independently delivered ledgers, re-signed
   claims, altered World/progress/proofs, forged System actions, clock ceiling, raw framing and each
   physical dependency mode. Derive structural size/depth/array bounds rather than copying B02 caps.
4. **Native consumer.** Only after reference freeze, add an additive Core codec/consumer and focused
   deterministic tests. Compare native canonical bytes with retained reference vectors for every
   terminal and event cut. Keep R01/R02 and earlier Breakdown readers intact. Measure installed test
   discovery and CI process/resource cost before assigning any new test partition.
5. **Acceptance.** Run focused predecessor/consumer tests, build and format; retain exact input
   hashes and command receipts. Fresh independent review and exact-head hosted CI are separate gates
   before merge. Update roadmap, Combat plan, README, tech design and naming summary with the exact
   achieved boundary only after the implementation evidence exists.

## Scope guard

No public Combat activation (Tasks 020–021), Exercise/Runner adoption (022–024), broader Reserve,
Reaction or vehicle profile, subsequent Combat execution, durable host publication, or all-72 AC
closure follows from R03. The next private successor would be ordinal-two Combat entry and its
supported action path; it needs its own contract and evidence.
