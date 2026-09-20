# PR metadata

Branch: codex/combat-task008-inherited-movement
Base: codex/combat-task019a-first-opening
Title: Add creation-rooted ordinary Movement replay

Task008 E1 now replays ordinary Move4 from complete creation-to-first-opening history for either owner. Each Clear move updates current World/representation locations, CP, cohesion causes, own member spending, route and actual progress together. Exact retries return original events with current state.

The closed Normal-Weather, nonmotorized NONE profile reuses existing terrain/spending/route kernels. Seven2CP moves reachCP14/Cohesion−4 with two receipt-bound DP causes; eighth rejects. Canonical replay and bounded World equality reject forged effects, caches and resources.

Validation:31 focused tests (8 new Movement +23 first-opening), twoowner traces/14moves/16cuts/48 frozen artifacts; full solution2,020 passed,0 failed/skipped; build0 warnings/errors, format/diff/local links pass. Dev review and three sequential independent reviews passed with no findings.

Stacked on #131. E2/G1 still owns actual stop, System empty-cohort resolution and Movement completion; G2 owns Combat-entry transition. No public activation, general restore or durable publication changes.
