# Author Explanation — E2/G1 actual route lifecycle

## Intent and plan
Joint008E2/G1 implements mandatory ownerstop2→Systememptystopresolution2→ownerMovementcompletion3
from completeacceptedE1history. Sharedownership follows frozenBreakdowninventory; no skipped event or
syntheticidle. EndsBreakdownDetermination, G2stillrequiredforCombatentry. ParentF/H/publication stayopen.
Fiveprimaryfiles:3newmodels/projector/codec,tests,fixtureprojectlink; no legacy/codecontract edits.

## Flow and components
Replay reconstructs entirecreation→019A→E1 prefix, requiresactualmoves/nonvehiclemovingroute, then
re-emits atmost3 lifecycle records and compares wholecanonicalbytes. TypedStop/Resolve/Complete
commands shareidentity; capabilityfields differ. Command derives currentversion audiencecapabilities
and retainedversion1 actionhashes. Apply shape/owner-or-System/creation/cycle validation precedes
consumedoccurrence lookup; exactoldinput returnsoriginalevent/currentstate, changedreuse rejects.
Freshinput mustequal exactcurrentCommand. ReadState compares fullreplayedcanonicalprojection.

StopusesexistingBreakdownStop.Create, deliberateNormalemptycohort; captures exactcycle/cycleId and
suspendedMovementposition whileenteringgenericinterrupt. Resolution requiresmatchingcontext/actualstop,
restoresposition, clearscontext andsetsIdle; emptychecks/lots andunchangedRNG stillreal event.
Completion derivescatalogBreakdownposition, sortsALLoriginalunits fromcurrentWorld, computesown
exclusions where noenemy lieswithin2, thenemitsrealcompletionevent. Receiptcomputedbeforeinstalling
proof prevents selfhash. Proofscope/ordinal1/receipt/endlocations/exclusions derive actualhistory.
State retainsimmutableE1World/member/track/progress, appendsreceipts/head/prefix/flow/context/proof only.

## Choices and invariants
ReuseexistingtypedBreakdownroute/stop/flow+writers, B1prefix/hash, D1member/D2cycle/E1guardedWorldwriters.
No C#cyclecontrolexclusionkernel existed; privatelyport frozenpurepredicate usingboundedtwo-layer
BFS overContentedges, emptypriorunion andfirstordinal only. This doesnotimplementgeneral019repeat.
OriginalReservecompletionReceiptId remains; MovementEndProof hasnewMovementreceipt. No addedmaterial
progress. Capability !=persistedroute/stopID. Noactorfromsymbolicnullside. 1MiB/depth32 and3suffix cap;
E1boundspriorroute to7moves. Internaltypedstates neveraccepted asadmission inputs.

## Verification and dev review
Eightowner/1,5,6,7move traces,24events32cuts88artifacts. Predgolden isarrayofrecordHASHES, notstrings.
Independentcapability/action/prefix/receipts, exacteverylatercutretry, owner/System negatives, changed
identity/capability, missing/reordered/interleaved/extra history, raw/scalar/coherentproof/context/progress
forgeries, immutablefields/buffers/legacyreader. Literalexclusions distinguishdistance2rear/3supply.
Devsource/testreview found no correctnessblocker; requested coherentcontext/progress/outputbuffer
probes beforefreeze. AnalyzerCA1822 onconstantordinalgetter corrected toimmutableinitializedproperty; CA1861testarray allocation corrected.
Executedfinalchecks/reviewevidence in e2g1-evidence.md; canonicaloracle unchangedbaseline retained.

## Costs and limits
Finiteprivatecodec repeats inheritedfieldlayout; frozenhashparity constrainsdrift. Repeatedhistory
replay acceptedboundedcost, no arbitrarycapacityclaim. No positivevehicle/Reaction/Reserve/latercycle,
Combatentry/action, generalSnapshot12/publicactivation/durablepublication. HOST-PUB-001 remainsopen.
Review capturedcontext, actioncapabilitybindings, realresolution, allunitproof/exclusions, receipt
separation and no accidentalmaterialprogressaddition; futureG2must consume fullhistory, notwrapper.
