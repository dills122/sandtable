# Progress

## Setup — 2026-09-15

- Durable goal created at02:49:17 UTC; deadline10:49:17 UTC.
- Initial detached worktree clean. `git fetch origin main` exit0;
  `git merge-base --is-ancestor ff6b5f6 origin/main` exit0.
- `env -u GH_TOKEN -u GITHUB_TOKEN gh auth status --hostname github.com` exit0 outside sandbox.
- `env -u GH_TOKEN -u GITHUB_TOKEN gh pr view 113 --repo dills122/sandtable --json number,state,mergeCommit,mergedAt`
  and matching114 command exit0, both MERGED.
- `git switch -c codex/overnight-combat-wave-01 origin/main` exit0.
- Required skills read. Broad lookup cancelled with exit130; no material changes or auth failure.
- Four planning files created before implementation. No story implementation or test gate yet.

## Task004 scoping

- Plan attested; canonical004A/B/C decomposition recorded before edits.
- Writer task004_writer owns exactly four new side packet files; task004_evidence read-only.
-004A re-split before edits into004A1 codec/selection/RBA/seal,004A2 settlement,004A3 Reserve/cycle
  and handoff. Parent004A/004/B remain open until complete integrated evidence.
- Baseline `just check` running to /private/tmp/sandtable-wave01-baseline-check.log.
- Baseline `python3 -B docs/specs/verify-combat-authority-composition-v1.py` running to
  /private/tmp/sandtable-wave01-baseline-authority.log.
- Sandbox `ps` denied; read-only outside-sandbox process check succeeded. No gate failure yet.
- Baseline authority oracle exit0:28 traces/nine families/31pins/225readbacks/203mutations/6raw/8boundaries.
- Sandbox `just check` stuck at restore; interrupt ineffective. Terminated own PIDs75418/75401 only.
  Replacement `just check` outside sandbox running to /private/tmp/sandtable-wave01-baseline-check-outside.log;
  restore succeeded, formatting underway. Environment issue, not behavior failure or dependency upgrade.
- Fresh read-only reviewer task004a1_reviewer assigned canonical-source checklist before candidate.
- Outside-sandbox baseline build/format pass,0 warnings/errors and81/81 boundary tests. Full suite running.
- `command -v lychee` returned1: pinned checker absent from PATH. No installation or dependency
  change attempted; local-link fallback needed if checker remains unavailable.
- Planning files are ignored by repository `.gitignore:87`; retain explicit force-add on story
  commit so draft PR includes durable plan/handoff. Session-catchup script exit0, no unsynced output.
- Baseline `just check` outside sandbox exit0: format pass; build0warnings/errors;81/81 boundary
  tests;1670/1670 full tests,0skipped, duration3m13.322s. Exact log evidence/baseline-just-check.log.
  Baseline authority exact output retained evidence/baseline-authority.log.
- Optional TestResults directory lookup absent; test process exit/result log remain authoritative.
- README/roadmap/plan status synchronized to004 in progress/B open. Separate navigation ownership.
- Source agent final004A audit reconciled and retained source-audit-004a.md; reused for read-only
  CON006 requirements preparation while004A1 implementation proceeds.
-004A1 writer RED: `python3 -B docs/specs/verify-combat-side-projection-v1.py` exit1, assertions
  `test_codec invalid side value admitted`, `test_selection selection needs every authenticated cut`,
  `test_seals round needs every authenticated cut`, `test_submission foreign audience admitted`.
  Writer GREEN after shared codec/replay/submission implementation: same command exit0,
  `PASS: 004A1 semantic contract tests`. Further canonical/mutation/privacy coverage in progress;
  brain has not accepted candidate or independently run new oracle.
- Local Markdown target check on8 changed/planning files passed476 targets/0errors;
  external URLs and anchors excluded, not equivalent to pinned Lychee.
- Brain pre-freeze check found missing explicit submission campaign/rules/config/round/slot fields
  and cycle-domain mismatch. Writer added RED assertions `test_explicit_submission_context explicit
  protocol bindings missing` and `test_canonical_cycle_reference cycle reference bypassed frozen
  Public tuple codec`, focused command exit1. Fields corrected; cycle mapping paused then resolved
  by exact canonical sources/reviewer: use original full Rules10 hash in frozen Public codec.
  No new policy ruling or historical-byte change. Candidate not yet accepted.
- CON006 source preparation complete/read-only, retained source-audit-004b.md; recommends future
  three bounded packets terminal/continuation/schedule, strict child evidence, parent/paired evidence.
-004A1 extended writer matrix (not final acceptance):18 audience traces/146cuts,63submission and
  deadline/foreign checks,192mutations,180raw rejects,14receipt recoveries. New semantic RED
  `test_round_continues_side_history round reset authorized revision/receipt/history` exposed
  C3a→round join reset; writer preserving exact prior projection/own receipt chain before final goldens.
- Read-only source agent reused for004A1 behavioral-coverage reconciliation; no additional .NET run.
-004A1 draft source/test review complete; positive behavior/privacy/current submission/receipt/frozen
  cycle coverage present. Gaps sent to writer: explicit context-field mutants, expired unaccepted
  proposals, mandatory fixture verification, compact reorder/tag/missing/ref/limit negatives.
  No focused run on changing draft; no quality verdict yet.
- Four004A1 packet files present (~264KB including ~207KB retained fixture at first inspection).
  Ordinary reviewer completed initial tests-first/code read with no confirmed blocker; execution
  and independent probes await writer's stable-candidate signal. Brain source reconciliation agrees
  synthetic scope/typed mappings/continuity are explicit; final acceptance still pending.
-004A1 final source/test reconciliation reports all four prior gaps closed. Read-only fixture
  inspection:20pins match,18audience traces/146cuts/61literal observations, all61 lengths/digests
  agree, largest observation3686bytes, fixture207039bytes. No residual source-evidence gap in A1.
- Brain `just check` running outside sandbox to /private/tmp/sandtable-wave01-004a1-just-check.log.
  Local target check10files/491targets/0errors (anchors/external excluded); all72 canonical AC IDs
  retained uniquely; no tracked src/tests/scenarios changes. Candidate hashes retained in
  evidence/004a1-candidate-sha256.txt pending final writer/reviewer/gate reconciliation.

## Hard stop — clock/privacy conflict

- Ordinary reviewer independent probe: both genuine seal-order histories at cuts1/2 expose equal
  waiting-side bytes; identical proposal with trusted time3500 accepts before hidden seal(HWM3000)
  but system-cancels/rejects after private seal4000(HWM4000). Deadline33000 unchanged.
- Source agent independently confirms no explicit regression exception: DES002124–138/AC010274,
  DES002155–163, frozen round103–106, POL004/00625–27 conflict. Timing/traffic deferral is not a
  semantic-outcome waiver. Hard-stop condition applies; no new policy invented/authority changed.
- Brain `python3 -B .planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py`
  exit1, both orders reproduce equalObservationBytes=true, accepted→rejected, HWM3000→4000.
  Exact output evidence/clock-high-water-counterexample.log. This is failing acceptance evidence.
- Writer focused oracle exit0:14semantic groups,18traces/146cuts/63submissions/297mutations/
  180raw/21bindings. Reviewer independently reran same focused command and confirmed counts.
- Writer direct predecessor commands exit0:
  `python3 -B docs/specs/verify-combat-selection-steps-v1.py`5traces/41cuts/246mutations/164raw;
  `python3 -B docs/specs/verify-combat-sealed-round-v1.py`4traces/23cuts/276mutations/138raw;
  `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py`112positions/1interrupt/6edges/
  38mutations/3996scopeidentities/896actor mappings.
- Brain story `just check` exit0:0warnings/errors,81boundary tests,1670full tests,0skipped,
  duration3m32.443s. Exact evidence/004a1-just-check.log. These passes do not waive failed privacy case.
- Reviewer verdict Request changes/P1 policy clarification. All children stopped; no A1 acceptance,
  A2/004B implementation or005/006 work. Writer handoff-only doc labels blocked candidate;
  schema/fixture/oracle remain unchanged from reviewed candidate hashes.
- Latest-main refresh remains ff6b5f60219afbb12d5e06bf836f06f94de32006; no existing overnight PR.
  Preparing one coherent blocked draft/handoff, not accepted contract delivery. No merge.
- Final readback: reviewed schema/fixture/oracle SHA256 unchanged; spec-only blocker status added.
  AST/JSON/diff checks pass;10Markdown files/498local targets/2new blocker anchors/0errors;
  external URLs and other anchors excluded. No further executable implementation after hard stop.

## Blocked draft delivery

- Staged scope22files,+3265/-5, explicitly reviewed before publication. Exact list
  evidence/delivery-files.txt. Planning/evidence force-added because repository ignores .planning.
- Candidate commit `aec9a847d79d013c6bffe1f628524f83c1f56c4c`; no accepted-story claim.
- `git push -u origin codex/overnight-combat-wave-01` exit0.
- `env -u GH_TOKEN -u GITHUB_TOKEN gh pr create --repo dills122/sandtable --base main --head codex/overnight-combat-wave-01 --draft --title 'Record blocked Combat side-contract candidate and clock privacy conflict' --body-file /private/tmp/sandtable-wave01-pr-body.md`
  exit0: https://github.com/dills122/sandtable/pull/115.
- PR readback confirms OPEN/isDraft=true/base main/correct head/commit. No merge.
- `git status --short` empty after candidate push. Final metadata-only commit records PR and
  handoff status; no schema/fixture/oracle changes after reviewed hashes or repeated runtime tests.
- All three children completed/stopped; parent objective explicitly incomplete and implementation
  blocked by required owner policy disposition. Source and review evidence recorded in handoff.

## Owner privacy decision — 2026-09-15

- Owner selected strict privacy: "I like maintaining the privacy so lets go that route".
  Equal-outcome privacy remains required during clock faults; explicit versioned clock correction
  authorized in principle, with historical authority bytes preserved. No implementation accepted.
- Clock at acknowledgment11:24:28 UTC; original eight-hour deadline10:49:17 UTC expired.
  Recorded decision in CCE and local plan/handoff; implementation awaits renewed work window.
- Initial working tree clean at143b1c1 on codex/overnight-combat-wave-01. This follow-up changes
  planning metadata only; no new implementation, tests, agents, commit, push or PR change.

## Renewed eight-hour run — 2026-09-15 11:39:39 UTC

- Owner authorized resumption, same scope/no merge. New deadline19:39:39 UTC.
- Latest origin/main remainsff6b5f6; HEAD143b1c1. Only prior owner-decision planning edits present.
- Reused writer/evidence agents for separate read-only clock successor design; root owns policy,
  plan and integration. No successor implementation before bounded packet assignment.
- Goal service still blocked; no tool supports resume. Preserve full objective without false
  completion/replacement. Local execution proceeds under explicit user authorization.

- Policy/navigation packet updates policy register, combined plan, README and roadmap to owner-selected
  strict privacy with versioned correction pending. Local493targets/4files pass; diff check clean.
- Writer/evidence recommendations reconciled: use immutable public opening floor; no new public
  clock events or per-side watermark. Approved common terminal disclosure preserves Prepared race.

- Historical authority composition oracle after policy-doc changes: exit0,28traces/9families/
  31pins/225readbacks/203mutations/6raw/8boundaries. Historical bytes still validate.
- Root author check of four primary policy/navigation files: owner quote/direction exact, no
  implementation acceptance, original evidence retained, renewed deadline recorded. Diff clean.
- Assigned W01-CLOCK-CONTRACT sole writer new roundv2 spec/schema/fixture/oracle; root owns plan.
  New opening validates trusted public baseline without inherited private RBA seal-time comparison.
  Successor replay/policy identities and regression matrix required; no historical file writes.

- Policy/navigation decision committedf8231c9 (9files,+138/-15;4primary plus planning metadata).
- Extracted all72 exact acceptance rows from6canonical designs into
  evidence/acceptance-source-inventory.json as read-only preparation; no mapping/completion claim.
- Confirmed downstream scope: corrected synthetic C3 round/result/settlement lane plus unchanged
 00328trace creation-rooted handoff. No new positive creation-rooted assault bridge required for004.

- W01-CLOCK-CONTRACT writer observed RED (`python3 -B docs/specs/verify-combat-sealed-round-v2.py`,
  exit1):5semantic failures—private-seal acceptance split, absent successor identity, private RBA
  time gating opening, disabled admission still opens, old reader accepts successor event.
- Writer initial GREEN same command exit0,5semantic groups. Full matrix/readback/fixture still in
  progress; this is not packet acceptance. ClockConfiguration2 binds parentConfig1 and explicit
  public-opening-clock.v2 domain; Rules10 bytes preserved.
- Source agent validates proposed AC inventory72unique rows/names,12per6sources, exact source text.
  Found step/seal task-number drift and missing explicit hosted-transport deferral markers; root
  awaits exact corrections before retaining public index. All rows remain planned.

- Source-agent AC map corrections applied:011ownsseals/010ownssteps, expanded causal recovery
  ownership, explicit Dispatch/provider deferrals and released-II pre-Morale extension gate.
 72exactrequirements/uniqueplannedtests remain; no runtime implementation/004Cacceptance claimed.

-005source preparation complete (read-only): dormanttables/arithmetic then exactRulesInput1codec,
  independentopticalexpecteddata, fullcoordinates and33percent distinction. No005codebegun.

- Clock writer readback RED: focused successor test rejected permissive reader with
  `FAIL: test_authenticated_readback invalid successor accepted`; GREEN after strict replay.
- Mandatoryfixture absence RED: `CMB-RND2-001 /required-retained-fixture`; retainedfixture nowexists.
  Writer reports10synthetictraces/allcuts/suffixes,16clockcases plus retry/expiry/Prepared/recovery;
  final raw/tamper/sourcechecks still pending. No packetacceptance yet.
- Fresh-context ordinary clock_v2_reviewer assigned fournewfiles, finalverdict afterfreeze; source
  agent independently reconciles fixture/policy/pins. Max4active includingroot, noformalreviewpass.

- Firstclockfreeze focusedpass:11groups/10traces/68cuts/610mutations/340raw/288clockcomparisons-
  retries/480lifecycle retries/30invalid. Reader also normalized malformedpredecessor RED into
  rejection. Writer initially invoked nonexistentverify-sequence-v1.py(exit2), corrected sequence
  commandpass. Historicalround/selection/sequence checks pass; no sourcechanges.
- Freshreviewer reproduced focusedpass and56independentSystemclock/changedretry cases; noP0/P1.
  P2found outerfixture Python equality permitsfloat/int andbool/int substitution. Root assigned
  test-first strictfixturecomparison fix; firstfreeze superseded, fullgate/acceptance held.

- Source-agent independent firstfreeze byteaudit (no oracleexecution):10cases/68states/58events/
 136literals/24clockvectors/6sourcepins match; maxliteral10033B. Independently recomputed
  Config2/Base2/eventreceipt/prefix/ledgerhashes and parentlink; bothowners/orders retain4000→3500.
 15historical predecessor/rules-input/composition files byte-equalHEAD. Awaitcorrectedfreezehashes.

- P2fix RED identifiedfloat/bool/duplicate-key fixture acceptance; exactdeterministicUTF8comparison
  GREEN. Final12groups,othercountsunchanged. Specae093dd...;oracled1091b5...;schema/fixtureunchanged.
- Freshreviewer APPROVE boundedclockpacket; noresidualP0/P1/P2. Independent sourceaudit finalhashes
  confirmed; identities/all136literals/6pinsvalid. Retained evidence/clock-v2-review.md.
- Root independentadmissionprobe exit0:88equaloutcome comparisons over2sides/2orders/11times/2flags;
  v1stillaccepted/rejected at3500. Log evidence/clock-v2-independent-admission.log.
- Root `just check` running outside sandbox, session10765; log evidence/clock-v2-just-check.log.
  Format/build/boundary81 alreadypass; fulltestcompletion awaited before acceptedcommit.

- Root full `just check` exit0: format/build0warnings/errors;81/81boundary,1670/1670fulltests,
 0skipped; fulltests3m01.672s. Accepted W01-CLOCK-CONTRACT, pending acceptedcommit recording.
  Spec status-only update afterreview/gate; schema/fixture/oracle remain finalreviewedhashes.

- Acceptedclockcommit3fcd822 pushed;14files,+7653/-3 (5primaryinclplan,fixturesmajority).
- PR115updatedtoAdd privacy-preserving Combat clock contracts; OPEN/drafttrue/head3fcd822verified.
- AssignedW01-CLOCK-INTEGRATE fourexistingsidefiles; preservehistoricaldiagnostic andexplicitcurrent
  correctedprofileadmission. Root renewedhandoff; all004/Band005stillincomplete.

- Side integration RED: corrected profile absent; legacy profile admitted by current helper.
  Implemented corrected source registry/current-only admission gate. Second RED: System clock
  cancellation fabricated own seal receipt; fixed projection to require choice-sealed effect.
- Writer reports all18historical audience traces byte-preserved; adds40corrected audience traces
  from10mirrored C3a sources plus10acceptedround2 sources. Publicclockpolicy binds firstframe.
  Fullfocused run and unchangedhistoricaldiagnostic checks in progress; noA1acceptance yet.

## 2026-09-15 12:32 UTC — A1 accepted; result timing bounded

Side integration passes ordinary review, source audit, non-vacuous root144 serialized outcome
checks and full81+1670 gate. See evidence/side-clock-review.md and exact gate/hash logs.
Only root status prose changed after frozen review. Original diagnostic remains historical.
Result-v2 starts next with independent fresh-window opening; earlier private accepted times are
audit-only. Writer corrected branch interpretation: retained retreat/custody paths are same-owner;
regression must prove prior-time isolation without claiming an opposing-retreat leak.

## 2026-09-15 12:36 UTC — navigation and PR synchronized

Accepted A1 commit8cb1cef and navigation2e409e5 pushed. DraftPR115 readback OPEN/draft at2e409e5;
409 local navigation links passed. Result2 writer observed three REDs and is implementing explicit
mandatory-window-clock.v2 policy in command/state/event alongside RoundClockConfigurationHash.
A2 source matrix retained separately;005 independent read-only preparation running. No merge.

## 2026-09-15 12:54 UTC — Result2 accepted

Ordinary reviewer approves frozen32-trace packet; source audit784literals/11pins/32historicalfiles;
root192independent paired outcomes and just check81+1670,0skipped,3m21.794s fulltests. All passing.
Only final spec status prose changed after freeze. Next004A2 uses explicit Observation2/Submission2/
Candidate2, source-native CP/Cohesion bounds, distinct public clock/codec profile from first frame,
actual owner-choice receipts and complete allowlisted settlement facts. Canonical plan records
version split before writes. A1 exact types/bounds/literals stay frozen. Full004/B then005 required.
Static72row contract evidence maps retained; partial coverage/runtime owners explicit.

## 2026-09-15 13:49 UTC — integration preparation

Refetched origin/main; unchanged ff6b5f60219afbb12d5e06bf836f06f94de32006. No newer-main conflict.
A2 active/unfrozen. Fresh reviewer baseline ready. Required nativeResult2→finish bridge scoped
beforeA3; four-case feasibility source retained /private/tmp/sandtable-result-cycle-feasibility.py,
SHA256aceb490318f90bf7e83e34a4eca03d7c075ee7d53d0a9cb6c9727f5fa6706484. Research only.
Full72 source/planned-test rows merged with static evidence into004c-contract-map-draft.json;
exact accepted A2/A3/B references still require004C reconciliation. New B2 semantic authority action
and actual initiator/outward rejection distinction recorded in packet-004b.md; old schemes unchanged.

## 2026-09-15 14:14 UTC — A2 frozen review boundary

37focused groups GREEN; independent root fallback/cache probe240+24checks GREEN. Fresh ordinary
review and source audit active; full gate running. Required bridge remains next after acceptance.
Retained A3 alternate-release-clock probe rejects at actual continuation readers; no new blocker.

## 2026-09-15 14:19 UTC — A2 accepted

Frozen ordinary review APPROVE; independent source/literal audit finds no discrepancy. Root full
gate exit0:format/build0warnings/errors,81boundary/1670full tests,0skips,3m11.782s. Root240fallback
outcomes/24cache-source checks pass.37focused groups,96audience traces/2080cuts,7344clock outcomes,
4872retries,26pins/628literal observations. Spec status only changed after review. Required next
packet W01-CLOCK-CYCLE; parent004/checkpointB and005 remain incomplete.

## 2026-09-15 14:23 UTC — A2 publication and bridge start

AcceptedA2042edbc; statusdocs febec3d;411local link targets pass. Pushedfebec3d to existingdraftPR115,
readbackOPEN/drafttrue/exacthead. Initial combinedpush command rejected byautomaticreview for
unestablishedownership/authorization. Read-only GitHubcheck proved sign-in dills122/DylanSteele
owns dills122/sandtable, ADMIN, isPrivate=false, sameauthor/headbranchPR115; existingplanrecords
user-authorizedcommits/push/no merge. Explicit evidence-backed retryapproved andpushsucceeded.
No unresolvedpublicationblock. Bridgewriterstarted; freshreviewerbaseline ready. A3version/source
plan recorded beforeedits; futurewriter preparesread-only. Deadline19:39:39UTC.

## 2026-09-15 14:27 UTC — bridge first RED/GREEN

Writer captured missing-base_for RED across32lineage test before implementation. New closed schema/
spec and exact source/certificate/ReleaseBase plus wrapper command/event/receipt/state replay now
implemented; first all32GREEN run active. No historical baseconstructor/reader use or changes.
Freshreview waitsfreeze; no acceptance yet.

## 2026-09-15 14:30 UTC — bridge first GREEN / B fallback refinement

WriterfirstGREEN32lineages/160cuts/128retries, eachowner-choice/exact4events;8guard+8entitlement
casesretainfullWorld/RNG/history/targetuses. Initial31source-certificate/85state-event-command/
84raw-bound/17clock-fallback checks pass. Pairedprior-time matrix/pins/freeze remainpending.
SourceB1/B2refinement uses8exactA2fallback prefixes asStepLimitExceeded failures at16/20events;
terminalfamilysupport distinct fromactualsatisfaction, no fabricatedsuccess/proofs/continuation.

## 2026-09-15 14:48 UTC — W01-CLOCK-CYCLE accepted

11focused groups/32lineages/160cuts/128retries/192prior-time checks pass. P2sourcefallback gate
fixedafterRED16realnativeforks; final24rejects incl12sameWorld. FreshordinaryreviewAPPROVE;
sourceaudit14pins/320nestedliterals/128suffixretention/160cuthashes. Rootfinal1088clock/recovery
checks pass; finaljustcheckexit0:format/build0warnings/errors,81boundary/1670full,0skips,3m21.192s.
Initialgate/review retainedseparately. Onlyfinalspecstatus changedafterfreeze. Latestmainff6b5f6
unchanged. Acceptfiveprimaryfiles+curatedplanning/evidence; next solewriter side_cycle_writer A3a.
Full004/Bthen005objectiveincomplete; reneweddeadline19:39:39UTC,oneexistingdraftPR/no merge.

## 2026-09-15 14:52 UTC — A3a RED / completion intent source

Bridge/navigation pushed7332bca, PR115OPEN/draft/exacthead;412localnavtargets pass. A3aRED observed
missingcodec3. Sourceauditconfirms nativecomplete-release isacceptedownerintent despiteSystem
eventauthor, strictowner-complete-release reason distinguishesclockfallback. Exactpredicaterecorded
inpacket004A3/B beforewriterreceiptimplementation; no newpolicy or inheritedcapabilityexpansion.

## 2026-09-15 15:04 UTC — A3a first GREEN / B1 scope

Writerinitialmatrix104liveaudiencecuts/12ownreceipts,112standalonetraces/76offers/all7arms/4owner
completiontraces.1140clockoutcomes/472retries/1710bindingmutants/80sourcemutants/37hiddenstate
comparisons pass. Nooprawtestvector repaired; fixtureRED/generation/finalchecks pending.
B1full9familylineagemap retainedsource-audit-004b-lineages.md. Source-backedB1/B2scopeclarification
separatesfullauthenticatedprefix fromexecutionstart/profile: nativeC3live, admittedinheritedlive,
terminal-onlyzeroexecution. Explicitcheckpoint-scopedproofs; runtime022fullpublicCoreparitydeferred.

## 2026-09-15 15:06 UTC — A3a fixture RED

WriterconfirmedmissingA3fixtureRED; deterministicsuccessor3 generationactivewithpriorA1/A2full
fingerprint preservation. Addedrealnamedreleasefallback/controlbranch samepublicprefix tests:
fullobservations/submissions/outcomes/acceptedrelease retries; ledgerrelabel/mutationisolation.
Finalfullsideoracle+fourdirectpredecessors pendingfreeze. Handwrittenchangeabout1150lines within
samefourfiles; nohistorical/runtimeedit.

## 2026-09-15 15:26 UTC — A3a acceptance

52focused groups; fresh ordinary review APPROVE; root independent92binary sets/190actions;
sourceaudit38pins/456cuts/344literals/190candidate pairs. Root just check exit0,81boundary+1670full,
zero skipped,3m16.682s. Four-file frozen schema/fixture/oracle exact; status-only spec acceptance.
A1/A2 preserved. Next separate A3b packet; full004/B and005 incomplete. No merge.

## 2026-09-15 15:34 UTC — C-map successor reconciliation

Read-only source audit reviewed72rows, supplied55accepted-successor evidence/coverage amendments;
17unchanged. Root applied exact amendment artifact after base SHA check and verified all IDs,
requirements, planned tests, verification status and runtime owner fields unchanged. Draft remains
unaccepted: A3b/B and finalC closeout pending. Historical first/last36 maps retained as provenance;
updated JSON references successor amendment report and explicit aliases/scope notes. No runtime
coverage claim or tests run for this planning update.

# 004A3b accepted evidence — 2026-09-15

Revised frozen candidate approved by fresh ordinary reviewer and native source auditor.
Root full side oracle: exit 0, 62 semantic groups (session 47348).
Root just check: exit 0; format/build clean; 81 boundary + 1670 full tests, zero skipped;
full suite 3m31.596s. Logs retained beside this report.

Root compatibility: 328 schema leaves, six original fixture sections, 144 unchanged ASTs,
five bounded aggregate extensions. Native fallback probe: 32 owner/fallback pairs, 64 binary
identity checks, 64 equal public prefixes, 320 clock outcomes, 64 retries, 56 historical
admission rejects, two direct native releaseMember positives. All exit 0.

Source audit: 116 sources, 4744 native-backed cuts, 1208 literal observations, 720 candidate
pairs; 56 historical projections and 28 ownReserve records; 176 corrected World/RNG/policy
histories, 4512 receipt/history pairs, 44 side pins and 31 Task003 pins.
Review initial P2 omitted singleton releaseMember; corrected only two owner terminal vectors.
Opposing and corrected traces preserved. Final review has no unresolved findings.
Initial gate/review artifacts remain clearly labeled initial, not final acceptance.

Reviewed hashes are in 004a3b-reviewed-sha256.json. Final spec changes only acceptance status;
schema, fixture and oracle hashes remain frozen. Full004/checkpointB and005 are incomplete.

A3b accepted commit b33f69c, navigation commit 3f1ef9d, pushed to existing draft PR115.
277 local navigation links pass. PR description update initially rejected by auto-review; read-only
checks verified PUBLIC repository dills122/sandtable, user ownership/ADMIN and same-user draft PR.
Evidence-backed retry approved; readback OPEN/draft, head3f1ef9d, A3b summary present. No merge.
B1 sole writer side_cycle_writer active; source auditor prepares independent checkpoint checks.

004C draft mapping reconciled against accepted A3b: 24 evidence/coverage amendments, all 72 original
IDs, requirements, planned tests/tasks, verification status and runtime owners preserved. Referenced
oracle functions exist. Prior amendment provenance retained; B and final C acceptance still pending.

B1 prefix identity also binds closed capability family. Inherited Reserve-only and inherited
cycle-control profiles expose different capabilities from first frame despite identical native
starting World. Distinguish those families; continue excluding catalog names/future transcript and
preserve corrected same-start seal/time/choice pairs. This implements existing closed-family/profile
requirement rather than expanding admission. Writer has corrected initial/intermediate/final probes,
inherited CP and fallback cut16 open-state probes green; full corpus acceptance still pending.

B1 initial run authenticated all134 sources in268.6s:126 satisfied terminals/eight exact unsatisfied
fallback prefixes/28 historical zero-execution; max checkpoint52567B;74 equal initial pairs/two
unequal CP/RNG pairs;59 mutations. Schedule test then found wrong cross-audience roundRef equality
assumption. Writer fixed same authenticated occurrence/position check while retaining scoped refs,
added two capability-family difference pairs and safe full-reference replay/view memoization.
Fresh complete seven-group run pending; initial partial run is not acceptance.

B1 intermediate run36344 stopped exit130 after root found local native_state helper shadowing;
writer renamed local state_body. Separate source-specific RED showed early bridge ReleaseState
omits World, so generic fallback restored initial C3 World incorrectly. Writer now retains exact
authenticated completed Result2 World and adds352 bridge-cut World/RNG comparisons. Fresh initial/
release/finish probes then eight-group run pending. No intermediate full-pass claim.

B1 ordinary static review found two required fixes: authenticate_source returned cached catalog
authority, and test_fixture used parsed equality accepting noncanonical/numeric variants. Writer
accepted both, plans defensive copy plus exact bytes and focused RED/GREEN. Other static source,
World/occurrence/schedule/identity boundaries consistent; no acceptance yet. Native source auditor
started independent134-source/588-event comparisons before final fixture, will verify final hashes.
Root just check93399 running; src/tests unchanged throughout B1 Python fixes.

Root B1 repository gate exit0: format/build clean,81 boundary and1670 full tests,zero skips;
full suite3m13.738s. Runtime/tests diff empty throughout later Python review fixes. Independent
literal framing probe passes33 checks across three scope goldens,2576 cut bindings and nine pins.
Initial full eight-group author command exit0 (827.59s post-import/group timer only); log header
UTC not reliable whole-run instrumentation. Final two review fixes run has true UTC17:20:02.417418
start and4.305s import. Final native focused results/review/sourceaudit still pending.

# B1 ordinary final review — APPROVE

cycle_finish_reviewer: no unresolved findings. Fresh native process27318 exit0,245.6s.
134-source catalog authenticated;18 targeted checks pass: returned-authority copy/mutation rejection
on four surfaces, defender guard/escape cross-seal equal starts on both sides, guarded bridge
suffix/full World/obligation retention and forged-obligation rejection, historical source1/active2
Request distinction. Six independent malformed fixture-byte variants rejected. All four frozen
hashes rechecked unchanged inside final process.

Reviewed full original eight-group evidence (2576cuts/192dual schedules/52live cuts/352bridgeWorld)
and revised67rejection/exact1542785Bfixture evidence. Required cache-escape and parsed-fixture-equality
findings fixed. Read-only ordinary review; no full suite, Git mutation or formal independent review.

B1 accepted after all gates. Root commits packet, then starts sole-writer B2.
