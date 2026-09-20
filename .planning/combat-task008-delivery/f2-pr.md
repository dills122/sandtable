# PR metadata

Branch: codex/combat-task008-reaction-lifecycle
Base: main
Title: Replay actual Reaction participant lifecycle

Task008 F2 now reconstructs actual F1 trigger history and replays reacting movement, participant
completion, mandatory empty stop resolution and no-eligible window closure. Public capabilities
bind complete current move options and version; persisted authority IDs remain distinct. Closure
restores exact phasing route after resolution, preserving RNG/resources and adding progress only
for reactor movement. Forged history/state, wrong actors, altered capabilities and conflicting retries reject.

Validation:16 focused tests (9 F2+7 F1), two traces/eight events/ten cuts/28 frozen artifacts.
Full solution2,060 passed,0failed/skipped; build0warnings/errors; full format/diff/local links pass.
Dev review and three sequential fresh-context independent reviews passed without remaining findings.

Predecessors through F1 merged to main via #135; this PR targets main. No direct/active fallback, chosen second move, multiple opportunities, positive
vehicles, public activation, general restore or durable publication. F3–F6/H remain open.
