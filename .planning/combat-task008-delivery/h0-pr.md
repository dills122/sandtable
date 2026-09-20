# H0 publication draft

Branch codex/combat-task008-reaction-lifecycle; base main; existing draft PR136.
Proposed title: Replay inherited Reaction paths and define retained Snapshot12

Task008 replays all selected inherited Reaction paths from trusted creation-rooted history: first
participant movement/completion, direct closure, active fallback, second movement and explicit
completion. Completion records a stop, System resolves its empty cohort, and a separate closure
resumes the original phasing route. World and RNG remain intact across completion/resolve/close.

The additive H0 contract now defines literal Snapshot12 roots for every selected inherited-history
cut and prefix:368roots,286distinct histories,62shared-cut groups. Preserve exact creation bytes and
nineteen-field root; typed Reserve/cycle arms retain complete ledger, route, progress and interrupt
proofs. Full roots must match independently trusted creation and history, including rejection of
internally consistent but different legal event tails. Existing creation and synthetic Combat readers
remain unchanged. H0 does not implement runtime restore.

Validation: F6 cumulative2104solution/81boundary/focused24, build0warnings/errors, formatpass.
H0 normal oracle and15direct predecessor oracles pass;6992root edits,6992omissions,2576raw negatives,
2247new-arm nested edits and trusted-history/creation/recomputed-binding checks pass. H0 is contract
only; prior .NET results are the unchanged runtime checkpoint, not a new H0 test run.

F2–F6 and H0 each completed dev review and three sequential independent rounds with no remaining
findings. Frozen source manifests, exact logs
and reports retained per slice. One main-based PR, no manually chained PR targets.

Initial H1–H4 runtime routing/full-root codec/disabled-admission restore remains open. Later009–025
and HOST-PUB-001 retain gameplay, public activation, simulator and durable-publication gates.
