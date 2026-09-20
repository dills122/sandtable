# F5 publication

Branch codex/combat-task008-reaction-lifecycle; base main; PR136.
Title: Replay Reaction lifecycle, fallback and second movement

Task008 now replays the selected Reaction lifecycle, direct closures, active fallback and a second
reactor move from trusted creation-rooted history. The second move advances rear to supply at CP2→4,
appends the existing track and preserves the same open route and suspended phasing continuation.
Active fallback still requires an explicit stop resolution before phasing resumes; direct closure
creates no stop.

Canonical commands/events/caches bind actual predecessor history, reason-specific public capabilities
and authenticated actors before retry. Tests reject re-signed effects, competing forks, altered
history/cache, unsupported typed World fields and caller-buffer mutation. Historical readers and
public activation retain their existing gates.

F2–F5 each passed dev review and three sequential fresh-context independent reviews. F5 verification:
22 focused/predecessor tests, 2,093 full tests and 81 boundary tests passed, zero failures/skips.
Build zero warnings/errors; full format passed. Exact logs, frozen manifests and reports retained.

One main-based delivery PR. F6 second-move completion and initial H full Snapshot12 restore remain
open. Later gameplay/public activation and HOST-PUB-001 durable publication retain their gates.
