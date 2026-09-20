# Author explanation — F5 second Reaction move

## Intent and plan
Implement one exact second move for each owner after actual F2 first-participant authority14.
Rear→supply costs2, cumulative reactor CP2→4. Two traces/two events/four cuts/10 artifacts.
Delivery base26ff137 accepted F4, but F4 terminal is not causal predecessor. Main-based PR136.
No participant completion, stop resolution, window closure or third move in this slice.

## Approach and flow
Replay accepts complete creation/preamble/Weather/stage/Reserve/Movement/trigger history, exactly
one F2 participant move and zero or one F5 suffix event. It reconstructs F2 authority before deriving
the current command and entire successor event; retained bytes must match the canonical result.
ReadState compares the whole history-derived cache. No cached caller state supplies authority.

Stable public window binds trigger13; opportunity rotates using current14 and complete sole move
inventory. Action binds both public handles, rear/supply and exact cost. Persisted window/opportunity
IDs remain separate. Actor and creation/cycle authorization precede retry lookup. Exact authenticated
retry returns original event and current terminal; conflicting command or extra event rejects.

World element and representation move together, CP becomes4, existing track appends supply, and
the same reactor route retains identity/start14/assault origin. One receipt/progress ref and Chronicle
prefix advance. Active opportunity/window and suspended phasing continuation remain open; phasing
CP4, RNG, resources and all unspecified authority remain unchanged.

## Components and choices
Three new production files own state/projector/codec; one test file and fixture link complete five
primary paths. Reuse F2 command shape, pure identity and route helpers, but do not broaden its
completion-at14 command policy or its historical World guard. F5 computes expected World from actual
F2 evidence and its own suffix before writing the bounded World projection, preserving omitted-field
rejection. Packet-local ordered writers retain canonical bytes without accepted predecessor edits.

## Verification and limits
Five primary files frozen in f5-source.sha256. Focused22 tests and clean full build passed;
lead dev review completed. Exact results/failures live in f5-evidence.md and f5-worker.md. This packet
does not itself assert readiness. Full gate and three fresh reviews must finish.
Challenge route/track continuity, public-versus-private handles, actor-before-retry, re-signed tamper,
strict full-cache/history rejection, capacity and whole typed World preservation.
No multiple opportunities, positive vehicle accounting, public activation, general Snapshot12 restore
or durable publication. F6/H and HOST-PUB-001 remain open. Repeated bounded replay favors explicit
authority over broad runtime performance claims.
