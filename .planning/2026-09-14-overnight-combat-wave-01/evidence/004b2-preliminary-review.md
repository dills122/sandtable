# B2 preliminary review — before freeze

Fresh ordinary reviewer result_v2_reviewer plus root identified three required contract fixes:

1. Native provenance: synthetic Root.boundaryJson.cycle supplies setupId/setupHash/contentPackId/
contentHash/scenarioId; creation Root.requestJson supplies setupId/setupHash/content pack/hash/
scenario. Aggregate oracle pins are build identity, not content identity. Initial RNG hash needs
explicit seed-state kind. Sources: selection oracle84–92 and envelope read_request.
2. Prospective publication inventory: distinct ArtifactManifest envelope inventories run-manifest
and evidence files, excludes its own artifact-manifest.json. This gives manifest-last an explicit
self-free identity; no actual publisher in B2.
3. Attempted failed reconstruction/readjudication retains unverified proof and observed mismatch/
nullable unavailable observations; only unattempted proof is absent. Baseline validator483–505/
555–622. No fabricated empty observed hashes.

Author confirmed fixes in progress before freeze. No acceptance or green evidence claimed here.

4. Exact B2 build binding: predecessor-only source-pin digest gives different B2 executors,
controllers and schemas the same build identity. Reviewer confirmed P2. Closed ordered build
descriptor must include B2 oracle and schema byte hashes plus predecessor pins; exclude fixture
and Child bytes to avoid self-cycle. Initial three-file freeze superseded before fixture freeze.
Root .NET gate unaffected because src/tests remain unchanged.

5. Supported reference configuration: admit only source-mapped public Preferences and effective
dual-slot role order before execution. Otherwise an actually offered choice could be mislabeled
failed membership when its successor merely differs from closed retained reference. B1spec102–124
requires exact supported suffix; matching alternate references preserve B3 comparison coverage.
Source auditor confirmed bounded admission fix, not gameplay/source expansion.

Root accepted bounded fixture adjustment to avoid stale build-derived child hashes: retain exact
authenticated source/reference/event summaries plus freshly generated representative actual Child
goldens (including failure modes). Programmatic main still covers all134 sources. Full native run
on3730 freeze finishes before edits; final all134 canonical-config admissions prove narrowing
leaves those paths unchanged. Root compares native/controller/reconstruction ASTs against saved
pre-admission source. Final focused changed-admission/build/fixture proof checks and source/review
applicability are required. Report chronology exactly; no claim of repeated full final-code run.
