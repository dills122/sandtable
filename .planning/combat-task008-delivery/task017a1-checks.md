# Task017A1 verification facts — in progress

Base afdcd9e. Four source/test paths only; canonical plan and administrative docs/evidence separate. Root development review complete with no remaining findings after raw canonical-order and attack cycle-hash fixes. Worker focused/shared20passed,0failed/skipped22.774s; six new Release facts plus existing seals/custody. Final worker evidence/pins pending.

Root unchanged contract oracle: `python3 -B docs/specs/verify-combat-reserve-release-v1.py` passed13literal cases/48side-slot traces,188cuts,2368mutations,840raw rejects,20timing and27boundary checks. Log `/tmp/task017a1-root-oracle.log`; process closed. This is complete historical contract evidence, not C#A1 transition coverage. RuntimeA1 proves44isolated base hashes, explicitly excludes4historical settled rows and claims zero event/state transitions.

Root full build/tests/Boundary/format, immutable-candidate CI and three fresh sequential independent reviews pending. Third independently rebuilds full solution. No acceptance yet.

19:20UTC worker handoff complete: post-format20passed/0failed/skipped26.284s; scoped format/diff clean, owned processes closed. Four source pins retained and checked. Root full gates starting; no further source edits planned. Whitespace/escaping/-0 regression added with fix, no separate RED claimed; first golden mismatch and attack-cycle regression have actual executed RED evidence.
