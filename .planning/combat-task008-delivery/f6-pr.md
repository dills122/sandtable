# F6 publication

Branch codex/combat-task008-reaction-lifecycle; base main; PR136.
Title: Replay selected inherited Reaction paths through completion

Task008 now replays all selected inherited Reaction paths from trusted creation-rooted history:
first participant movement/completion, direct closure, active fallback, second movement and its
explicit completion. After the second move, the owner records a Reaction-completed stop, System
resolves its empty cohort, and a separate no-eligible closure resumes the original phasing route.
Neither completion nor stop resolution alone closes the window. All three preserve World and RNG.

Canonical commands/events/caches bind actual predecessor history and distinct public/authority
identities. Tests cover exact bytes at every cut, actor checks before all terminal retries, competing
forks, raw/re-signed tampering, unsupported typed World fields, buffer ownership and legacy-reader
rejection. Public activation and durable publication retain later gates.

F2–F6 each passed dev review and three sequential fresh-context independent reviews.
F6 focused24/full2,104/boundary81 passed; build zero warnings/errors and full formatting passed. Frozen manifests,
source pins, exact logs and sequential independent review reports are retained with each slice.

One main-based delivery PR. Initial H full Snapshot12 restore remains open; inherited root arms
and exact full-envelope bytes must be pinned before runtime restore. Tasks009–025 and HOST-PUB-001
retain their separate gameplay, activation, simulator and durable publication gates.
