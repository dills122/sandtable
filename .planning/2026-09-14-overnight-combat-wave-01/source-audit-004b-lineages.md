# B1 full native-lineage materialization

Read-only task004_evidence map; no tests/gate/writes by source agent. composition.sources() endpoints
are comparison targets, not complete history. Retain canonical native history once plus closed
family-tagged fragment boundaries; no arbitrary objects or unrestricted codec-name dispatch.

## Shared creation root

Closed CreationHistory: request,created,preambleEvents[],weatherEvents[],stageEntryEvents[],
reserveDesignationEvents[] as exact named native canonical strings. Events embed exact trustedinput.
reserve_designation.pre.initial(request,created) authenticates Request/Created via authority-envelope
readers; replay preamble→Weather→stageentry→Reserve; reserve_designation.read_state with allsix
arguments validates root. Sources opening-preamble-v1.py79 and reserve-designation-v1.py174.

## Nine families /28cases

Aliases exported by verify-combat-authority-composition-v1.py.

- Ordinary noattack4: noattack.trace(case)→history,selectionEvents,predecessor,states,inputs,events.
  History9fields=root6+Movementevents+Movementlifecycle+Breakdowncompletion/Combatentry. Retain
  selectionboundary/control and2selectionevents, then6noattackevents/controls. Nativefinal
  noattack.read_control(data,history,selectionEvents,events), Control. Factoryline212.
- DirectReaction6: direct.trace(case)→args,input,event,before,after. Args=root6+phasingMovement+
  trigger events. Exacttriggerboundary and1closureevent. direct.read_state(data,*args,[event]);
  direct.irl.raw LifecycleState. Factoryline279.
- ActiveReactionfallback4: fallback.trace(case)→args,inputs,events,states. Args=root6+Movement+
  trigger+firstparticipantstart event. Retain2fallback/emptystopresolution events.
  fallback.read_state(data,*args,events), FallbackState. Factoryline368.
- Reactionsecondmovecompletion2: reaction_complete.trace(case)→predecessorEvent,inputs,events,states
  lacksfullargs. Resolveexactcase.predecessorCase inreaction_complete.irs.FIXTURE; irs.trace(case)
  →args,secondInputs,secondEvents,secondStates. Retainfullargs/secondmove/3completionevents.
  irs.read_state(data,*args,secondEvents), thenreaction_complete.read_state(data,case,events),
  irl LifecycleState. Comparestoredpredecessorpackexactlytopinnedirs.trace beforecase-basedreader.
  Predecessorfactoryline51.
- Reservecycleentry2: reserve_cycle.trace(case)→history,parent,states,inputs,events. History=root6.
  Retain10structuralevents,fullControl andinitialReserveState; nopositiveassaultinferred.
  reserve_cycle.read_control(data,history,events),Control. Factoryline384.
- InheritedReserverelease2: reserve_release.trace(case)→base,parent,states,inputs,events. Parentis
  completeReservecycletrace. Retainparenthistory/events,exactBase,3ReleaseInput/ReleaseEvent
  records andouterControl. read_base(data,case), read_control(data,base,inputs,events).
- Armedcontinuationproof2: armed.source_trace(actor)→completeinheritedreleasechain; retainit plus
  armed.proof_from(actor,result) canonicalProof. Exactreleasereaders first, armed.read_proof(data,
  actor). Proof addsZEROauthorityevents/acceptedsteps.
- Inheritedcyclecontrol4: cycle_control.derived(actor)→base,releaseResult,proof suppliespreceding
  completechain. trace(actor,repeat|finish)→base,states,inputs,events. Retainarmedproof,exact
  InheritedCycleControlBase,2nativeCycleControlinputs/events/states. read_base(data), then
  read_state(data,base,inputs,events); statesvia cyc.raw CycleControlState.
- ReleasedIMovementcompletion2: reserve_movement.trace(actor)→base,states,inputs,events;
  previous.trace(actor) suppliesone-movefragment; previous.previous.trace(actor,'repeat') supplies
  precedingcontrol. Also materializecycle_control.derived chain. Exactnestedpredecessorbases,
  1Movementinput/event then3completioninputs/events. previous.read_base/read_state for
  InheritedReserveMovementState; outerread_base/read_state forInheritedReserveMovementCompletionState.
  Eventreaderdispatches3nativeversions. Predecessorfactoryline111.

## Wrapper/authentication constraints

Closedfragment retainscontracttag,exactnativebase/proofifapplicable,nativeinput/eventstrings and
terminalcontrolstring. Checkpointfragmentindex+localeventcut sharesroot/earlierfragments. Repeated
predecessorbytes innestedbases arebindings, neveradditionaltranscriptsteps. Fornamedreaders that
regeneratepredecessors, compareALLretainedpredecessorbytes withdefaultfactoryresult first; stored
historymustnotbeignoredin favor ofsourceID replacement. Nevercaller-suppliedexpectedbundle bypass.

Nevercollect_receipts() tobuildtranscript: losesorder/framing andisderivedsummary. Preserveproof-only
cuts/nativeversions/canonicalbytes. Checkversion/prefix/state ateveryhandoff. ComparefinalWorld/RNG/
controls/receiptstonormalizedcompositionendpoint onlyAFTERfullreconstruction. Neverinitializefrom
RootSnapshot/currentWorld or guessordinal. Executionstart/side-surfacecapability remains separately
explicit; fullsourcehistory doesnotalonepromise publiccontroller coverageofeveryhistoricalprefix.
