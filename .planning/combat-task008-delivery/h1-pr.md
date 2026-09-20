# H1 candidate publication

Branch codex/combat-task008-reaction-lifecycle; base main; existing draft PR136.
Title: Add Reaction replay, retained snapshots, and pre-cycle routing

Task008 now replays selected inherited Reaction paths from trusted creation-rooted history, including
participant movement/completion, direct closure, active fallback and second-move completion. The
H0 contract defines368literal Snapshot12 cuts, preserving creation bytes and full inherited ledger,
route, progress and interruption evidence. Runtime full-root restore remains a later gate.

H1 adds a dormant router taking one independently retained Created11/event stream and trusted request.
It derives preamble, Weather, stage and Reserve partitions through actual strict readers, materializes
Reserve membership at state10, and retains defensive byte ownership. Invalid order, causal forgery,
missing creation and unsupported later tails reject. No caller family tag or cached snapshot grants
authority. Every legal pre-cycle prefix is covered; H2/H3 later extend supported event families.

Validation: H1 focused53, cumulative2157 and boundary81 pass; build0warnings/errors and fullformatpass.
All256H0pre-cycle vectors/208distinct histories covered, plus exact predecessor goldens and ownership/
bounds/forgery tests. H0 normaloracle and15predecessor oracles pass. H0 CodeQL counter-log alert cleared
by a one-line constantstatus remedy, one bounded experiment and final independentreview4; exact-head
CodeQL and CI passed. F2–F6/H0 earlier dev+three ordinary rounds retained.

H1 is a frozen draft candidate: devreview/localgate passed; independent acceptance pending final
required rounds and exact-head CI. Fullrootcodec/admissiondisabled H4, later009–025/publicactivation,
all28runtime traces and HOST-PUB-001 durable publication remain open. One main-based PR.
