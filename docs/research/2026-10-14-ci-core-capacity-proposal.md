# B00 measured CI capacity proposal

Decision owner Brain. Baselineed443bdf2dfb5e6d8e3333364f52b49b9d939c83; featurebranch
codex/ci-core-partition-capacity. Actualauthorstart03:56:49UTC. No substantiveimplementation yet.
New six-hour scope closeout09:10:17UTC/stop09:55:17UTC. B00review0of9/recovery0of2.

## Observed failure risk and causal evidence

Authenticated successful PR183 CI38503950210 attempt1 head3491be4/testedmerge1eb26347695f21c9e9393d5a7c34581457b062e1
and postmerge mainCI38506203076 attempt1 headed443bdf. ActualAPIjobmetadata:

| Run | Core-A job | Core-B job | Core-B cap margin |
| --- | --- | --- | --- |
| PR183 |13m23s |19m36s |24s |
| mained443bd |9m10s |19m57s |3s |

No assertion/skip/process failure; bothcurrent2393row B shards complete. XML counters/status
exit0 and exactsameA71/B2393rowsets independently parsed acrossruns. MTP.Net10/xunit4.0.2 reports
collection-per-class,4parallelcollectionthreads. XMLtesttime sums are NOT walltime.
Matched diagnostic node starts/ends exactlycover everyXMLrow; headers/status/sdk10.0.402/attempt1
andsourceSHA hashes retained in /private/tmp/b00-analysis.json. OriginalsPR /private/tmp/nrc-ci-final;
main downloaded A/B/inventory in /private/tmp/b00-main-ci. PriorCIreceipts/archives alreadypreserved
byBrain onhandoffbranch2f40230; no binaryarchives copiedinto productPR.

| Core-B class | Rows | PR sum/span seconds | main sum/span seconds |
| --- | --- | --- | --- |
| ActualRoundEntryTests |151 |727.683/732.354 |743.211/739.007 |
| ActualRepeatedMovementCompletionTests |48 |649.947/649.984 |671.693/671.733 |
| ActualRepeatedMovementCompletionContractTests |16 |267.768/267.781 |281.959/281.965 |
| ActualSelectionTests |44 |227.543/227.537 |231.257/231.273 |

Existing Core-B tail is Selection: starts850.894/874.490sec afterfirsttest and completes1078.431/
1105.764. Completioncollections occupythe earlier4threadschedule anddelaythat tail. MainB assembly
wall1105.957s vsPR1078.624; sums3943.125/3828.276. Effectivehostvariation remainsunmeasured, not
anassertedCPU/I/O rootcause. Core-A slowest26mutationrows710.199s PR vs488.159main; shifting
newworkintoA risksitsalready13m23slowrun/15min cap. No product/runtime/profile changes justified.

## Recommended bounded change

Addone Core-C shard dedicated to exactlythese2fullqualified testclasses:

- Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionTests
- Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionContractTests

Core-A predicate/12selectedmethods and8mutation-row transfer remain unchanged. Let old B=Core−A,
new C=oldB∩selectedclassset, new B=oldB−C. Expectedcurrentactualrowcounts71A/2329B/64C/469Exercise/
10Contracts=2943. Core-C includes future rows/methods inthoseclasses throughclassmembership;
Core-B remainscomplement forunselectedfuturecases. No snapshotrowallowlist; fixednames define
partition policy only. CurrentnewB rowset shouldexactlymatchpriorPR1822329B controlinventory.

Core-C usesexisting15min fallback; Core-B remains20, Core-A15, Exercise15, Contracts15,
prepare/verify15/dependency5. No blindtimeoutincrease. Workflowmatrix addscore-c; helperMODULES,
corepartition/filter/discovery/load/execute/verify logic andfixturetests include5leafshards.
Afilterunchanged; Baddsclass exclusions; Cincludesexact2classes. Verify actualMTPfilterdiscovery
matchespredicates, all3Corepartitions disjoint/exhaustive, currentattempt/status/SDK/config/SHA and
allowningprocesses/dependencies/aggregatezero failure/skip/notrun/errors. No acceptance-gate weakening.

## Projection, uncertainty and acceptance experiment

Read-only model: sortclassjobs byobservedfirststart, scheduleeachon earliestof4workers, duration
=max(XMLclasssum, diagnosticspan). FulloriginalB simulation differsfromactualassemblywall only
+1.130% PR/+1.244% main, so schedulingmodel is a usefulboundedestimate, not hostedvalidation.
Removeonly2chosenclasses fromB andsimulateC separately withsameclassdurations; addobserved
jobminusassembly overhead97.376s PR/91.043s main:

| Sample | Projected B job | Projected C job | C15 margin |
| --- | --- | --- | --- |
| PR |932.707s (15m33) |747.360s (12m27) |152.640s |
| main |945.163s (15m45) |762.776s (12m43) |137.224s |

Cclasscount2 canrunconcurrently atcurrent4threads. OldBnew2329sum2910.561/2989.473 removes
917.715/953.652summedseconds. Scheduling/CPU/I/O/GC/cache andsetup canchange onfreshVMs; estimates
assume retainedcosts andordering. NewC15 hasobserved2m17–2m33headroom, not guaranteedfuturecapacity.
ExtraVMsetup/billing cost andanotherrequired leafartifact are acceptedmaintenancecosts. No claim
about productionperformance or fixedfuturelatency. Ifnewshardtimes out/stalls, diagnoseactual
progress ratherthanraisecap orrerununchangedhead blindly.

Authorvalidation: genuine focusedRED tests thirdpartition/realrowconservation/filtersemantics/
5shardmissing-leaf/attempt/process/futurecase invariants, thenminimalGREEN. RunexistingPython
coverage-helper suite/actionlint; compileRelease rowhelper/current.Core ifneeded and perform
actualMTPdiscovery only forCore/A/B/C withproviderexpansion. No broadlocaltest oroldPythonnormal;
heavytest leaseidle. Engineering5axis selfreview thenfrozen draftPR, freshneutralfirstreview and
Brainexactheadhostedfull2943execution experiment beforemerge. Needactualsuccessfulnew5leaf run,
notprojections/discovery/partialrows. Preservefulltiming/row/XML/status receipts fromthat experiment.

Alternatives rejected: justincrease20cap repeatscapacityriskwithoutbalance; transferintoA ignores
measured slow13m23 sample; changeclass testparallelism/diagnostics/physicalhashcache altersworkload
orsemantics; broadgenericbinpacking/rowallowlist largerthan64class split. NewRoundEntry shard
moves151rows/739s criticalclass, offersless15min headroom andcomplicatesalreadysharedmutationfilter.
Acceptednarrow roadmapP3 patch767fc37 remainsseparate: applyonlywhenrelevantroadmapdocs updated,
neverpullpriorbinaryarchives/producthistory wholesale.

Evidence scripts: /private/tmp/b00-analyze.py (XML/diagidentity/timingextractor) andsmallmodel in
b00-analysis.json.models. CLIinitialrunAttempt fieldname correctedtoattempt, read-onlyerror.
Initialregexmatcheddiagnosticmetadata DisplayName ratherthanTestNode, failedanalysisbeforeclaims;
correctedtoexactTestNode pattern andallXML/starts/ends identities nowequal. No test/runtimeredcredit.
No substantiveimplementation, testexecution, reviewdispatch, merge orhostedrerun inthisinvestigation.
