# Task011 worker evidence — frozen handoff

Authorized after010C9b999a7/8108ccd, parent010 dormant gate complete. Own only SealedRoundModels.cs, SealedRound.cs, SealedRoundCodec.cs, narrow SelectionStepsCodec bridge and CombatSealsTests.cs, plus this evidence. No commits/push/full suite/agents/reviews or012 implementation; root docs preserved.

Implementation: independently authenticate retained Created and trusted010B Boundary plus separate predecessor input/event histories; require selected decline/step3/three receipts; derive exact supplemental policy/budget from original Config1. Base owns canonical Boundary/World buffers from validated typed serialization, with get-only immutable provenance. Local accepted RoundInputs/events are captured separately; event.input never supplies trust. Private transition reproduces active v2 opening floor, own-slot check before clock cancellation, Command-only digest plus actor recovery before status/time, system cancellation author vs player receipt actor, Prepared3→4→5 and cancelled3→4→5→6. Fresh disabled admission blocks opening only. Entire RoundState world/RNG remain original Base; no cost/history/target/commit mutation. Full frozen commit grammar recognized but semantic commit and committed roots rejected pending012. Narrow bridge reuses010B World/Boundary/Control grammar and semantic array order, including forbidden Route/Legacy primitive behavior.

## Verification history and exact commands

All dotnet commands run native .NET10 MTP/xUnit v3 from repository root, login:false, authorized local IPC. stdout/stderr redirected to corresponding `/tmp/011-STEM.log`. Unique MSBuild logs captured by literal `{}` expansion.

Command pattern (substitute exact STEM below):
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatSealsTests' '-bl:/tmp/011-STEM-{}.binlog'`

- `red`: exit2,1failed/0passed,1.171s. Meaningful missing authenticated Round runtime API test anchored to retained ten-case fixture. No production implementation at RED. `/tmp/011-red.log`; binaries `/tmp/011-red-20260920-101656--24605--BxpWbB.binlog`, `/tmp/011-red-20260920-101703--24605--PrHljU-dotnet-test.binlog`.
- `literals`: build exit1, CA1859 private Shapes field should use concrete Dictionary rather than IReadOnlyDictionary. No runtime test failure. Fixed field declaration only; `/tmp/011-literals.log`, `/tmp/011-literals-20260920-102310--24964--LPxqoJ.binlog` retained.
- `literals2`: exit0,1passed,3.036s. Exact10Bases54precommit events64states, four final commits excluded. `/tmp/011-literals2.log` and matching unique logs.
- `expanded`: exit2,7passed/1failed,5.568s. Fixture pretty subtree GetRawText differed from canonical state subtree whitespace. Fixed comparison by serializing both parsed subtrees to canonical bytes; whole literal state byte assertions unchanged. `/tmp/011-expanded.log` retained.
- `green`: exit2,7passed/1failed,5.575s. Test expected NoOp for cancellation callback already accepted earlier; runtime correctly returned Duplicate because exact command/actor retry precedes status/time. Test corrected to assert retained command digest determines Duplicate vs fresh NoOp and independently assert foreign-round callback NoOp. No production order change. `/tmp/011-green.log` retained.
- `green2`: exit0,8passed/0failed/0skipped,8.281s. `/tmp/011-green2.log`.

Scoped formatting command:
`dotnet format Sandtable.slnx --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs`
Exit0, `/tmp/011-format.log` empty. Final focused/shared/verify results appended below.

## Coverage and limits

Eight focused tests: all10Base occurrences with independently supplied historical predecessor;54literal precommit events64states including initial readback, explicit4commit/committed-root rejection; both acting sides and first roles across24literal clock rows gives192fresh outcomes+96exact retries with equal own-visible witness;174additional retained-command retries and120suffix transitions across all cuts with admission disabled; independent opening1999 after RBA2001, opposite4000/own3500, deadline equality, unavailable/null/belowfloor, fresh structural time rejection, cancelled/Prepared callback ordering and post-close changed-choice rejection. Counter174 is sum n(n+1)/2 and120 sum n(n-1)/2 for retained event lengths [5,5,5,6,6] for each acting side. No claim of oracle480 retry coverage by worker tests.

Malformed/duplicate/missing/unknown/reordered/escaped/raw whitespace, root size/depth/array limits before sentry context, local Role/owner/slot/allocation primitive mutations, acceptedsequence17 rejection, unsigned RNG cursor syntax and canonical causal rejection, semantic World array order, separate actor/time mismatch, foreign/missing predecessor, modified v2 supplement, canonical rehashed event effects and state forgeries, reverse roles/proof order, duplicate appended seal, Created-before-list mutation, owned buffers/collections and immutable Base serialization tested. Fixture commitments are literal UTF8 here, not reconstructed hash-only goldens. Existing010B syntax/grammar remains shared regression authority.

Read-only local descriptor audit reproduced by Python: regex collect `["name"] = "descriptor"` from SealedRoundCodec, compare dictionary exactly to JSON `objects` in combat-sealed-round-v2.schema.json. Output `Round2 local descriptor audit:17/17 exact ordered field/type definitions`. Bridge delegates existing C3a primitives; full-root CheckBounds runs before delegation, preserving nested depth32 and all arrays512; no H4 cohesion4096 exception. Schema descriptor parity alone does not prove primitive behavior, hence raw sentries and shared010B tests. Maintenance cost: frozen17 local descriptors must stay reviewed against future intentional version changes.

No actual positive-history provenance/public projection/host clock monitor/publication/Snapshot12/result/commit claim. Production is internal dormant synthetic-C3a mechanism with independent trusted Boundary; retained creation validates compatible request, not invented actual positive path. Root owns independent oracle, fullgates and fresh reviews.

## Source contract pins (SHA256)

- combat-sealed-round-v2.md:9fb337bdbfeebf0db367f860aafeb6a37b72af0d602036aec41da82678332bc5
- combat-sealed-round-v2.schema.json:365283c1c51b9e155aa1651c253759b271bbda75f613f52f511ceeb0dec12c7c
- fixtures/combat-sealed-round-v2.json:8200354f49bef2fd4a976dd9c24e4e485f8c06ad903d8c3deb06d5a9db509a95
- verify-combat-sealed-round-v2.py:d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9
- verify-combat-sealed-round-v1.py:497e295e71c4b4da43539ee9b6c30de6d33f409a2f14b6cc24d9030390525ae0
- combat-selection-steps-v1.schema.json:ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d

## Frozen final results and primary manifest

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatSealsTests' '-bl:/tmp/011-final-{}.binlog'`
Exit0,8passed/0failed/0skipped,8.289s; `/tmp/011-final.log`.

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStepsTests' --filter-class '*CombatIdentityTests' '-bl:/tmp/011-shared-{}.binlog'`
Exit0,26passed/0failed/0skipped,12.987s; `/tmp/011-shared.log`. This covers existing C3a semantic arrays, forbidden Route/Legacy arms, input ownership and strict reader precedence, plus Identity grammar/provenance.

SHA256 frozen primary files:
- `src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs`:701099acad250aae1adb111bf6ed31fca8be2a61000858cbbd6415186c6b60d2
- `src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs`:7b9bc22f5d158c35550b9efa5b07516c31662d673614c97b43facae1151f5b57
- `src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs`:8c6c98de4b9d9cea1cc77382eda725784384bdb48473b2aaa91024fac05aa496
- `src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs`:90ef43ea5e10be1c8473c0991c591304c16df218ef3f593117ea64b436e09d30
- `tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs`:53e4237c9d6d2e924e6cce33f234995fc8e30570726758c167b1093191cc7346


Final scoped verification command:
`dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs`
Exit0, empty `/tmp/011-format-verify.log`; scoped `git diff --check` exit0. All worker processes closed, parent notified fullgate/reviewer .NET may start. No source changes after final focused run; only evidence text completed afterward.

Final binary logs: `/tmp/011-final-20260920-103037--25404--BoE+SY.binlog`, `/tmp/011-final-20260920-103045--25404--5gPPp+-dotnet-test.binlog`.
Shared binary logs: `/tmp/011-shared-20260920-103147--25463--+rRYr2.binlog`, `/tmp/011-shared-20260920-103147--25463--7Et7pD-dotnet-test.binlog`.
Root fullgate/independent reviews pending at this worker handoff; no acceptance claim.
