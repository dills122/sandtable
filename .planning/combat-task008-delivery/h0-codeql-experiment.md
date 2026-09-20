# H0 one bounded post-review investigation and experiment

User policy: after3rounds ifblocker remains, one deep research/experiment then one finalreview4;
if finalreview/blocker stillfails stopforintervention. This is first use of that conditional budget.
H1 started after local3Ready gates, then immediately paused upon new remote CodeQL alert. Five H1
draftpaths preserved, workerclosed no activeprocesses; no H1 acceptance or inclusion in H0fix.

## Evidence and diagnosis
H0commit4d652d30201b2b38e380007b855513be1a04ad83, PR136 OPEN/main.
Aggregatecheck106020410356 failed:1high alert. Individual4language analysis jobs succeeded.
Python analysis1805996846 refrefs/pull/136/head has1result. SARIF retained/tmp/h0-codeql-alert.sarif.
Rule py/clear-text-logging-sensitive-data. Sink docs/specs/verify-combat-inherited-snapshot-v1.py:691
serialized result['falsification'] to print. SARIF path sources are samefile490 and500, reads of
counts['trustedHistoryMutations'] and counts['trustedCreationMutations'] in +=1 operations. Four
paths duplicate these two sources through Counter→dict→result→json.dumps→print. No request/event
payload source appears in those paths.

Official rule: https://codeql.github.com/codeql-query-help/python/py-clear-text-logging-sensitive-data/
It forbids sensitive data in logs and explicitly permits non-sensitive diagnostic objects. Rule's
source classification here disagrees with actual numeric counter semantics; no suppression applied.
AST audit of all11counterwrite sites finds only literalint assignments/increments and len(shapes).
Frozen falsification object has11nonnegative integer values. Inference: this reported path does not
expose an actual secret; the detector classifies counter names as sensitive. No blanket claim about
other repo logs or CodeQL accuracy.

## Bounded remedy and experiment
Remove generic dictionary serialization at sole reported sink; print constant
'Falsification checks match frozen counts.' only after existing complete frozen comparison succeeds.
All assertions, generation, schema,fixture,sourcepins,normalization and runtime unchanged. Detailed
counts remain in frozen fixture for review. Avoid renaming counters/source-vectorchurn or disabling
query/securitygate. No alert dismissed, no checks bypassed.

One experiment: run complete normal H0 oracle with unchanged frozenfixture, verify sole-line code
diff and primaryhashboundary, then push isolated fix to obtain actual new CodeQL result. Finalfresh
review4 evaluates remedy/evidence and fullcontractboundary. Stop if blocker remains after that.
/tmp/h0-codeql-experiment.log/session59428 running; no passingclaim yet. Primarymanifest updated;
h0-source-pre-codeql.sha256 retains round3boundary. Other primary3files unchanged.

Local experiment result: normal oracle exit0,128.583seconds, /tmp/h0-codeql-experiment.log.
All368roots and entire unchanged fixture (including detailed falsification counts) compare exactly;
169sourcepins and imported-schema immutability pass. Only production-facing delta is constant console
status instead of counterdictionary. Remote CodeQL rerun and conditional finalreview4 still pending.

Remote experiment result: check106021490989 on exact c48ce728b886de2d67d2d88164d15ca0c5d3d073
completedSUCCESS,0annotations,'No new alerts in code changed by this pull request'. All4language
jobs pass. Root records raw result in h0-final-review-checks.md. OriginalH0generalCI passed;
remedy generalverify stillrunning, finalreview4 pending. One experiment complete; no suppression.

Final outcome: independentreview4 Ready, independentnormaloracle133.309seconds passes; all remote
checks on c48ce72 pass includinggeneralverify5m6s. Lead accepts recovery, resumes H1. No remaining
blocker or fifthreview. Exact sourcehashes unchanged after finalreview; next commit records evidence.
