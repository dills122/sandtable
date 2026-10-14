# N3 Core-B finalization budget correction proposal

Read-only at2026-10-14~23:42UTC (19:42Toronto); reviewedhead29636b9a5bcdd14219b4fc936a9c687f147efa3c remains frozen. No retry/edit/test/branch/merge. Review3 requirednext:3of9/pass3of3currentround/recovery0of2,noreset. Rawartifactslocalonly.

## Observed cause
CorrectedCI38652809605 CoreB job116156466167 started23:17:17,nominal20mdeadline23:37:17,overallcancelled23:37:21. Actualteststep succeeded23:18:46–23:37:18 (18m32s),retentionupload succeeded23:37:18–23:37:19,cleanupcompleted23:37:20. Localstatus exit0,testedmerge94736dd5530b1a4ae52f7ac17133a7abc580c96a,SDK10.0.402/Release/attempt1;exact2511argument-bearingXMLidentities matchcurrentattemptpreparedinventory. XML2511passed,0failed/skipped/notrun/errors,assemblytime1109.600s;executionsummary18m30.794s. XMLSHAc91bc3eb4d214af2d21e23e34d1a7a29daacb3780f7228af2264d6229b86888a. No remainingclasses or hang tofix;all tests andprocess completed. BUT job/needs/aggregate failed,cannotmergeorwaivecancelleddependency.

N2 sameB2511 passedwithassembly1061.977s,job19m18s. Currentassemblyonly47.623s (4.48%)longer,pretest89s,finishretention3s =>wholejob1204s. Twentyminutes appliedtoentirejobcollideswithsetup+finiteexecution+reporting,notjusttestbudget. NewDpassedandBactualrowpartitionisunchanged fromN2. Redistributionagain wouldexpand policy and testmoreconsumerpaths unnecessarily; measuredwhole-job budget correction is smallestrootfix.

## Bounded policy
Change ONLY Bmatrixdeadline20->25minutes. Otherfiveleaves15,prepare/verify15/dependency5 remainexact. Measured1204s plus15% allowance and60sreporting/finalization reserve=1444.6s (~24m05) below1500s. Twentyfive gives296s(~4m56)rawheadroom overobservedcompletedjob; notunbounded,noskips,notatimeout-onlyguessbecausefiniteexactcompletedwork/process/timingsareverified. No runtime/test/partition/SHA/attempt/SDK/Release/process/XML/needs-success change. SDK10.0.402mustmatchnewruninventory,notassumelocal10.0.400.

## Exact proposed edit allocation afterGO
1..github/workflows/ci.yml: B20->25expressiononly.
2..github/scripts/test_partition_test.py: genuineboundedbudgetpolicyRED first,then expectedB25 fixture and controls thatallotherleaf/prepare/verify/dependency budgetsremainunchanged, sixleafmatrix+coveragecontrols preserved.
3.README.md: documentedBdeadline25,existingC/Downershipunchanged.
4.docs/design/combat-cycle-implementation-plan.md: measuredfinitecompletion/finalizationcollision andnewexactheadgates.
5.docs/work/plans/2026-10-14-n3-ci-finalization-budget-implementation.md: preeditTDD/result/time/retention.
6.docs/research/2026-10-14-n3-ci-finalization-budget-diagnosis.md: safe derivedtimings/counts only.
Separate neutralreview3bootstrap/author/selfreview/evidence text. Nohelpercode/native/tests/schema/pins/fixtures/formatter/oldreviewpacketedits;no newlysplitshards. Suggestedcodex/n3-ci-finalization-budget from29636 onlyatGO,fast-forwardoriginalPR190withoutforceafterselfreview/freeze.

## Verification and gates
Add boundedbudgetregression expecting B25andallothercurrentbudgets;oldworkflow producesgenuineassertionRED. Implementexpression/expectedpolicy;fullfastPythonsuite+actionlint+gitdiffcheck. Comparepartitionhelperbody byte-exact29636,conservepreviousactual3320rowreceipt/no changedfilters,SDK/config/process/XMLacceptance unchanged. No freshmetadataorheavy/nativeexecution neededbecauseonlyworkflowdurationandtests/docschange. Author selfreview thenfreshneutralreview3exacthead;newpublicationtriggersfreshCI,notretryfailedattempt. New3320sixleafXML/process0/SHA/currentattempt/SDK/Release/allneedsSUCCESS mandatory;Brainmerge/testedtreematch/verifiedmainCI/CodeQL/links required. CompletedBleafoldrunisdiagnosis,notpassingCIgate.

## Time/stop contingency
~23:42UTC leaves55mincloseout/115minhardstop. Author5–10min+review5–10minparallelhostedCI23–28min ->merge~00:15–00:25;main23–28min ->~00:40–00:55 uncertain andlikelyafter00:37:35closeout. Preparehonestretention/handoffatcloseoutwithmainpendingifnecessary;nonewfeaturesaftercloseout/productafter01:37:35hardstop. Ifgatesfailagain,donotstartautomaticfourthreview/materialfix;threepassround exhausted,Brainresearch/recovery/evidencebackedrework decision requiredwithin9/2globalbudget anddeadline. No downstreamactualRepeat/public/full72GO. Rawlogs/XML/binlogs/archive27exportsblocked;safe derivedtextonly.
