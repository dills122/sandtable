# Author explanation — F3 direct Reaction closure

## Intent and plan
Implement direct player decline and System unavailable/timeout exits from actual F1 trigger history.
Six owner/reason forks, one event per fork; source contract and old F1/F2 readers unchanged. F3 is
sequential delivery after F2 review but causal predecessor remains F1, never F2 completed lifecycle.
Main-based PR136 accumulates accepted slices; this review isolates F3 against c83d7c4.

## Approach and flow
Replay retains actual F2 zero-suffix wrapper around full F1 history, then at most one direct-close
event. Command derives kind-specific action from pure public window helper. Decline authenticates
reacting owner; unavailable/timeout authenticate System. Reason is computed from exact kind.
Actor/creation/cycle validation precedes terminal retry lookup. Exact accepted input returns original
event and reconstructed terminal state; any alternative reason/input rejects as a competing fork.
ReadState compares entire regenerated canonical state to supplied bytes. No cached state admission.

The close3 event uses compatible24-field order/irl receipt domain, sole unresolved persisted opportunity,
nullable acting side, and exact suspended phasing route. Closure advances13→14, clears Reaction and
resumes Moving without reactor stop. State retains predecessor World, tracks/progress/resources/RNG;
current prefix and receipt advance once. Phasing CP4/reactor CP0 and assault positions unchanged.

## Components and choices
Three new source files: immutable command/event/state models; bounded transition/replay API; strict
canonical codec. New tests plus single test-project fixture registration complete five primary files.
Reuse existing action types and pure F2 public-window/World helpers without weakening historical
guards. Whole-World writer checks actual expected World before bounded layout omits unsupported arms.
Own closure writer avoids broadening F2 noeligible command into a caller-controlled closure reason.
Canonical layout duplication costs maintenance, but shared refactor would widen reviewed predecessors.

## Invariants and verification
Exact golden input/event/state bytes, source pins, independent public/action/receipt/prefix identities,
literal material preservation, actor-before-retry, sequential fork rejection, raw/re-signed tamper,
history order, caller/result buffer isolation and byte/depth/item bounds form acceptance boundary.
TDD initial missing-type red /tmp/f3-red.log; first green reproduces six forks/30artifacts/12cuts.
Final focused/full build/format/results and root dev review are in f3-evidence.md. Any discovered
failures or target changes must be retained there before review acceptance.

## Costs, limits and challenge points
Private wrapper references actual immutable F1/F2 evidence and replays short history for admission and
World guard. No performance or general-envelope capacity claim. No active-participant fallback,
chosen second move, multiple opportunities, positive vehicles, clock/scheduler/public activation,
noninitial Snapshot12 restore or durable publication. F4–F6/H and HOST-PUB-001 remain open.
Challenge receipt compatibility, public versus persisted IDs, exact phasing route retention, mutually
exclusive fallback forks and whole typed-World preservation. Author explanation is not review verdict.
