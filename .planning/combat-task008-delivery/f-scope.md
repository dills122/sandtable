# F Reaction selected-history execution research

Researchonly, noimplementation. F1dependsE1EXACTLYfirstMove4, notG2terminal. DoingG2first is execution
orderonly. F1 fullcreation→019AemptyReserve→1E1move assault→rear, state12 CP2, Normal/NONE. Same
Movementinput2 rear→assault commitsMove4 state13 CP4, enemyadjacency discovery thenexact1eligible
opportunity, routeidentity/currentWorld/track/progress updated, Reactionwindow and reacting/resume-route
flow opened, reactorroute null. Stopunresolved. Fivefiles triggerModels/projector/codec/Tests/csproj.
Canonical inherited-reaction-trigger-v1:2traces2events4cuts2retries8artifacts (prefix,input,event,stateeach).

Selected F graph canonical .3 packets:
F1 .3f trigger afterE1firstmove:2traces4cuts.
F2 .3g participant lifecycle afterF1:2traces8events10cuts28artifacts; reactingmove3→participantcompleted3→
Systememptystopresolved2→Systemnoeligibleclosure3. Inspect5filefeasibility; causalsplitifneeded.
F3 .3n directclosure afterF1:6traces6events12cuts30artifacts; ownerdecline orSystemunavailable/timeout.
F4 .3o activefallback afterF2 FIRSTPARTICIPANTMOVECUT:4traces8events12cuts32artifacts, fallback+resolution.
F5 .3p secondreactormove fromsameF2activecut:2traces2events4cuts10artifacts.
F6 .3q secondmovecompletion afterF5:2traces6events8cuts22artifacts.

Authority-composition-v1 selected28traces explicitlyincludes directclosure6,activefallback4,secondmove
completion2. ThusF3–F6 requiredselectedReactionrestore, not arbitrarycontract expansion. EarlierSnapshot
composition firstCombatprofile lackedpositiveReaction; lateracceptedauthoritycomposition expandscoverage.
F1+F2alonecannotclose selectedF/H. Namesaboveproposedexecutionlabels, pinincanonicalplan beforeF1.

H scope mustrespectprogressiveCheckpointD: restorestateswhosehandlersexist, laterlifecycleextendscuts.
Do NOT pull017ReserveRelease/018relationawareMovement+armed/019repeatfinish or009–016Combat handlers
intoF justtoclaimall28. Authoritycompositionhandoff maps72ACs acrossowners,008codec/restoredistinct.
Hcomposescompletedowners and preservesopen dependencies/selectedtraces forlatercumulativeproof;
no silentomissionorwhole28runtimeclaim fromfirstCombat/Reaction subset. No 008parentcompletionuntil
requiredscope genuinelymet; dependencygate canreportimplementedfamilyrestore withfutureownersopen.

Ftrust: persistedwindow/opportunityIDs != publichandles, publicopportunitybindsversion+completelegal
options; exactSystemreason/action; mandatoryemptyresolution; restorecapturedPHASINGroute notreactor.
Closed1opportunityinfantry doesnotadmitmultiple/vehicle/hosttimeouts/publicregistration/simulator.
No missingcanonicalprerequisite beforeF1 onceE1accepted. F1baseline exit0: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-trigger-v1.py
/tmp/f1-reaction-baseline.log:2traces2triggers4cuts2retries270mutations60raw30boundaries11pins. RemainingFbaselinesnotyetexecuted.

## F1 concrete reuse plan
Fivefiles newCampaignCombatReactionTriggerModels/Trigger/TriggerCodec, CombatReactionTriggerTests,
csprojfixturelink. ExistingMovementinput/codec/unit/cost/track/progress/receipt helpers reusable.
Pure CampaignReactionIdentity.CreateWindow/CreateOpportunity supports moveversion4/committed13;
reuse WindowId/OpportunityId/ApparentTrigger/AdjacencyEvidence/FrozenOpportunity. ExistingRoute.AtLocation,
PhasingContinuation.ResumeRoute and BreakdownFlow.Reacting writer reusable. SourceLand8.51.
DO NOTreuse historical CampaignReactingPosition (requiresmaterializedMovement) or TriggerAuthority
(onlymove2/3), norWindowdependingonboth. Newtypedboundedsequence5nullsideposition/window/authority;
no oldconstructorrelaxation/fakeSnapshot11. Legacytrigger discoveryprivateContent6/World6; derive
selectedContent7 eligibility directly fromfrozenprofile.
F1kernel replaysE1exact1move state12 CP2, derivesownrear→assault state13 CP4cohesion0. Exact1opposing
combatadjacency THENexact1eligibleopportunity. Currentroute/trackpreserved. OwnWorldwriter rederives
F1postWorld andstructuralequalityguards hardcodedabsentfields; E1writer correctlyrejectsadjacentreturn
and MUSTremainunchanged. NewwrapperallowsReactingFlow, retainsactualE1predecessor and overrides
currentWorld/member/track/progress/receipts/prefix/window/position. No fabricatedsecondE1move.
Persistedwindow/opportunityIDs use sandtable.campaign domains; publicobservationhandlesbelonglater
commands andmustnotreplaceIDs. F1nopublicprojection scope.
CheckpointD/Task009 explicitlyallow initialH forimplementedhandlers; later017–019extendsrestore,
not prerequisiteinversion. Selected28trace composition remainscumulativereconciliationtarget.
