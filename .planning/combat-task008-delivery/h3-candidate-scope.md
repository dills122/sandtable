# H3 research notes — no implementation before H2 acceptance

Extend single retained stream through F1–F6 using accepted strict readers. F1 Move4 shares E1
family/version; routing must select candidate without allowing failed validation to truncate/drop
an event. Full emitted canonical bytes remain authority proof. No caller family tag.

Actual F1 inactive window allows F2 first participant move or F3 one direct exit (owner decline,
System unavailable/timeout). Actual first participant move allows F2 completion, F4 System fallback
then mandatory resolution, or F5 second move. Actual F5 admits F6 completion/resolution/closure.
Accepted readers enforce count and typed predecessor at each fork. F4/F5 require precisely one
participant; F6 precisely one second move. Preserve all legal intermediate cuts including inactive
window after stop resolution and mandatory explicit closure before phasing resumes.

Closed typed projection cases preserve actual World/members/tracks/progress/route/window/receipts.
Common SequencePosition may describe suspended Movement in family state; full-root H4 serializer
must derive actual currentPosition from typed state/window/flow, not blindly copy a common field.
H3 should not introduce full-root codec or disabled-admission claims. All selected H0 identities
and canonical family goldens plus genuine causal forgery/fork/order/ownership negatives required.

Read-only research inspected actual CampaignCombatReactionClosure/Fallback/SecondMove/Completion
Replay preconditions. H3 remains provisional until H2 gate and focused scope sizing; no code begun.

Root inventory: F1–F6 supply50H0vectors/34uniquehistories;2sharedH2,32new. CombinedH1/H2/H3
should cover all286distinctH0histories/368vectors. SameMove4 discriminator available in actual
canonicalbytes: openedReactionWindow null forE1, object forF1. This may selectcandidateonly; exact
strictF1/E1 reader must validate allbytes, including falsified null/object and duplicateproperty cases.
F1 strictreader requires exactlyoneordinaryMove and atmostonetrigger. F2 requiresoneactualtrigger
and atmostfourparticipants; afterfirstmove branch on actualnextcandidate andstrictvalidation.
No try-all-and-ignore fallback. Source-based candidate selection is implementation option, not
newauthority or mandate to trust embeddedwindow. Root plans boundedfour/fivefile H3 dispatch.
