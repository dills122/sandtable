# Initial A3b freeze — changes required

Source auditor task004_evidence found terminal_view3b reads only authorityArms.members, dropping
actual authorityArms.releaseMember on both Reserve Movement-completion owner projections.
Native member: statusnone,baseCPA10,spentCP2/1; releaseI/ordinal1/CPA10/ceiling10; nextMovement
ordinal2 expired with actual completion receipt. Plain currentordinal2 lifecycle correct.
Normalize authenticated singleton alongside members; both owner views must preserve CP2 and
expired own release history; opposite audience remains null. This is source correspondence,
not expanded gameplay or new public fields. Sourceverify-combat-inherited-reserve-movement-
completion-v1.py288/301; initial sideoracle2609. Root accepted bounded fix; initial candidate unaccepted.

Unaffected: four freeze hashes match;44sidepins+31Task003pins match files+HEAD; A1/A2/A3a schema/
fixture encodings exact; spec onlynewinsertion.232traces/4744cuts/1208literal observations/
720candidate-submission records exact hash/length/canonical correspondence. Max9842B/history19/
receipts4/actions2. No forbidden privatefields. Native correspondence reached at least64complete
audience histories before requested stop; remaining native checks incomplete.

Root initial compatibility passes328schema leaves/6fixture sections/144unchanged AST and5bounded
extensions. First probe assumed only3aggregatechanges; inspected typed3 newdisjointenum dispatch
and test_a3_preserved fingerprint exclusion, then corrected probeallowlist. No candidate change.
Root initial justcheck exit0:81boundary+1670full,0skips,fullsuite3m24.526s. Notfinalacceptanceevidence.

Root initial fallback probe overconstrained trusted input to clockAvailable=false AND time=null.
Actual retained fallback uses nulltime with available=true, also valid unavailable-clock semantics.
Native System/clock-unavailable effect is required either way. Corrected root probe to test null
OR unavailable; this is a test-input assumption correction, not candidate defect. Added independent
native releaseMember/CP2/expired-exception assertions for revised candidate before final root run.
Author fullsession37718 could not be recovered in root or author tool context; no fullgreen claim.
Initial reviewer full61run explicitly stopped exit130 after finding; independent focused counts
retained in initial-review.md. Revised fullrun will write shared file from start.
