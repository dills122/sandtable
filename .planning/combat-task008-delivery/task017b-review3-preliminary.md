# Task017B preliminary independent review

Review instance: 3 of3. Blind pass recorded before author explanation.

Target: clean detached clone `/tmp/sandtable-task017b-review3`, candidate `3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67`, base `201395c3e3faf06735424a8bc70658a2af75f081`. Initial clone status clean. No CCE or prior review reports read.

Inspected AGENTS, Task017 plan refinement, canonical Result2-to-cycle-finish contract and oracle admission/derivation, tests before implementation, complete new adapter, shared codec diff, native Release kernel and Result2 replay path, documentation diff.

No actionable findings established. Implementation derives state through native replay; committed-hash catalogue and kind/choice/actor signatures restrict supported lineage; authenticated fallback rejection matches oracle. Empty Release derives one status-none acting member with exact spent CP and pinned CPA10, retains full Result2 projection, resets only new Release clock, and delegates transitions to existing Release kernel. Tests target32 base literals/64 event literals/96 recovery cuts and retained guards/entitlements; wrapper hashes are not native recovery expectations.

Remaining checks: full independent Release solution restore/rebuild; focused adapter/A1/A2 and Boundary tests; verify nested source immutability and committed hash binding; compare author claims with observed evidence. Full restore initiated but currently no output; tooling limitation not yet diagnosed. Positive Reserve provenance, public APIs and cycle advancement remain correctly excluded; parent017 stays open.
