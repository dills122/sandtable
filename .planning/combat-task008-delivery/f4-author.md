# Author explanation — F4 active Reaction fallback

## Intent and plan
Add System unavailable/timeout exits after the actual F2 first participant move. Four owner/reason
forks, two events each,12cuts/32artifacts. F3 delivered first but is not causal predecessor. F4 does
not close a directly inactive window or select a second move; active route must be accounted for.
Review target is F4-only working-tree delta over f0cb5ca; cumulative main-based PR136 owns publication.

## Approach and flow
Replay requires exactly one actual retained F2 participant event, derives version14 with one active
opportunity and open reactorroute, then accepts zero to two F4 events. Each event input is extracted
from strict canonical bytes, authorized and compared with exact current command; complete emitted
bytes must match. ReadState compares full replay-derived cache bytes. No caller-state admission.

Close3 at15 clears window but records actual reactorroute in ReactorStopClosed with reason
ReactionUnavailable or ReactionTimeout, Normal Weather and empty cohorts. This retains suspended
phasing continuation. Resolve2 at16 uses that exact recorded stop, emits no checks/lots/RNG changes,
and restores original phasing Moving route. No extra noeligible close follows. Both events retain
World (phasing CP4/reactor CP2), tracks, progress/resources and prior receipts; version/prefix/receipt/
position/window/flow change as prescribed.

Commands use closed kind strings, one private handle field mapped to exact windowId or stopId wire
slot, and shared creation/cycle/version/position identity. Reason derives kind, never caller reason.
System actor and creation/cycle checked before retry lookup. Exact accepted input at either prior
version returns original event/current reconstructed terminal; cross-kind/reason reuse rejects.
Public window remains trigger13, System stop capability uses current15, both differ from persisted
IDs. Action types, Stop.Create, flow primitives and pure predecessor helpers reused; no old guard edits.

## Components and choices
New models/projector/codec plus focused tests and fixture registration retain five-primary-file cap.
Wrapper owns exact F2 first-move evidence; state delegates immutable World/tracks/progress. Writer
calls F2 whole-World guard, so absent unsupported fields cannot be silently discarded. Packet-local
canonical root/event layout duplicates existing compatible shapes to avoid widening accepted APIs.
Two-event bounded replay and predecessor reconstruction trade repeated work for strict authority;
no general capacity/performance claim. Diagnostic strings identify this active fallback adapter.

## Verification and dev review
Canonical four-fork goldens and independent public/action/stop/receipt/prefix identities; literal no-
early-resume and full material retention; both-event terminal retries and all actors; competingfork,
skipped/extra/reordered events, actual foreign/history corruption, canonical/re-signed event/cache
mutations, wholeWorld forgery/buffer ownership and byte/depth/item limits form acceptance boundary.
TDD initial missing-type red /tmp/f4-red.log. Root preliminary models/projector/codec review found no
blocker; requested accurate diagnostics, explicit per-owner case counts and terminal retry coverage.
Final test/format commands, failures and full gate results are retained in f4-evidence.md before final
acceptance. Author explanation alone is not review verdict.

## Limits and challenge points
No clocks/scheduler, player decline while active, noeligible extra close, second move, multiple
opportunities, positive vehicles, public registration, general Snapshot12 restore or durable publication.
F5/F6/H and HOST-PUB-001 stay open. Challenge recorded stop reason/route/hash/version, mandatory
resolution, public-versus-persisted capabilities, actor-before-retry, unchanged material authority and
strict input/state serialization. Code and plan reviewed independently after freeze.
