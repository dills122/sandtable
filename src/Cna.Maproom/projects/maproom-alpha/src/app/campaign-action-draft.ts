import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { LegalAction } from './api/generated/model/legalAction';

type AudienceStamp = Readonly<{
  campaignId: string;
  sessionGeneration: string;
  seat: CoherentPlayerView['seat'];
  audienceRevision: string;
  positionId: string;
  actionSetToken: string;
}>;

type ObservedCut = Readonly<{
  audience: AudienceStamp;
  decision: CoherentPlayerView['position']['decision'];
  legalActions: readonly Readonly<LegalAction>[];
}>;

export type CampaignActionDraftState =
  | { phase: 'empty' }
  | { phase: 'draft'; audience: AudienceStamp; action: Readonly<LegalAction> }
  | { phase: 'reviewed'; audience: AudienceStamp; action: Readonly<LegalAction> }
  | { phase: 'issue-intent'; audience: AudienceStamp; action: Readonly<LegalAction> };

const MAX_REVISION = 18446744073709551615n;

function revision(value: string): bigint | null {
  if (typeof value !== 'string' || value.length > 20 || !/^(0|[1-9]\d*)$/.test(value)) return null;
  const parsed = BigInt(value);
  return parsed <= MAX_REVISION ? parsed : null;
}

function stamp(view: CoherentPlayerView): AudienceStamp {
  return {
    campaignId: view.campaignId,
    sessionGeneration: view.sessionGeneration,
    seat: view.seat,
    audienceRevision: view.audienceRevision,
    positionId: view.position.positionId,
    actionSetToken: view.actionSetToken,
  };
}

function sameStamp(left: AudienceStamp, right: AudienceStamp): boolean {
  return (
    left.campaignId === right.campaignId &&
    left.sessionGeneration === right.sessionGeneration &&
    left.seat === right.seat &&
    left.audienceRevision === right.audienceRevision &&
    left.positionId === right.positionId &&
    left.actionSetToken === right.actionSetToken
  );
}

function copyAction(action: LegalAction): Readonly<LegalAction> {
  return {
    actionId: action.actionId,
    actionContractVersion: action.actionContractVersion,
    kind: action.kind,
    label: action.label,
    ...(action.formationId === undefined ? {} : { formationId: action.formationId }),
    ...(action.targetLocationId === undefined ? {} : { targetLocationId: action.targetLocationId }),
  };
}

function validAction(action: LegalAction): boolean {
  return (
    !!action &&
    typeof action.actionId === 'string' &&
    !!action.actionId &&
    Number.isSafeInteger(action.actionContractVersion) &&
    action.actionContractVersion >= 1 &&
    typeof action.kind === 'string' &&
    !!action.kind &&
    typeof action.label === 'string' &&
    !!action.label &&
    (action.formationId === undefined || typeof action.formationId === 'string') &&
    (action.targetLocationId === undefined || typeof action.targetLocationId === 'string')
  );
}

function sameAction(left: LegalAction, right: LegalAction): boolean {
  return (
    left.actionId === right.actionId &&
    left.actionContractVersion === right.actionContractVersion &&
    left.kind === right.kind &&
    left.label === right.label &&
    left.formationId === right.formationId &&
    left.targetLocationId === right.targetLocationId
  );
}

/**
 * Presentation-only selection of one authority-issued legal action. The shipped browser passes
 * neither an admitted capability nor an action adapter, so it can draft and review but cannot
 * enter issue intent. This model never creates an action, request ID, receipt, or HTTP request.
 */
export class CampaignActionDraft {
  private observed: ObservedCut | null = null;
  private current: CampaignActionDraftState = { phase: 'empty' };
  private authorityRevoked = false;

  constructor(
    private readonly admittedCapability = false,
    private readonly adapterAvailable = false,
  ) {}

  get state(): CampaignActionDraftState {
    const state = this.current;
    return state.phase === 'empty'
      ? { phase: 'empty' }
      : {
          phase: state.phase,
          audience: { ...state.audience },
          action: copyAction(state.action),
        };
  }

  observeView(view: CoherentPlayerView): boolean {
    if (
      view.contractVersion !== 1 ||
      !view.campaignId ||
      !view.sessionGeneration ||
      !['axis', 'commonwealth'].includes(view.seat) ||
      revision(view.audienceRevision) === null ||
      !view.position?.positionId ||
      !Array.isArray(view.legalActions) ||
      !view.legalActions.every(validAction)
    ) {
      this.clear();
      return false;
    }

    const previous = this.observed?.audience;
    const incoming = stamp(view);
    if (
      previous &&
      previous.campaignId === view.campaignId &&
      previous.sessionGeneration === view.sessionGeneration &&
      previous.seat === view.seat &&
      revision(view.audienceRevision)! <= revision(previous.audienceRevision)!
    ) {
      // A late or conflicting read cannot restore a draft from an older/equal audience cut.
      return false;
    }
    if (
      previous &&
      (previous.campaignId !== incoming.campaignId ||
        previous.sessionGeneration !== incoming.sessionGeneration ||
        previous.seat !== incoming.seat)
    ) {
      // Constructor capability belongs to the initial audience. A new model/admission is needed.
      this.authorityRevoked = true;
    }
    this.observed = {
      audience: incoming,
      decision: view.position.decision,
      legalActions: view.legalActions.map(copyAction),
    };
    this.current = { phase: 'empty' };
    return true;
  }

  clear(): void {
    this.observed = null;
    this.current = { phase: 'empty' };
    this.authorityRevoked = true;
  }

  stage(actionId: string): boolean {
    const observed = this.observed;
    if (
      !observed ||
      this.authorityRevoked ||
      !this.actionable(observed) ||
      this.current.phase === 'issue-intent'
    ) {
      this.current = { phase: 'empty' };
      return false;
    }
    const matching = observed.legalActions.filter((action) => action.actionId === actionId);
    if (matching.length !== 1) {
      this.current = { phase: 'empty' };
      return false;
    }
    const action = matching[0];
    if (
      !action.actionId ||
      !action.kind ||
      !action.label ||
      !Number.isSafeInteger(action.actionContractVersion) ||
      action.actionContractVersion < 1
    ) {
      this.current = { phase: 'empty' };
      return false;
    }
    this.current = {
      phase: 'draft',
      audience: { ...observed.audience },
      action: copyAction(action),
    };
    return true;
  }

  review(): boolean {
    if (this.current.phase !== 'draft' || !this.currentActionStillLegal()) return false;
    this.current = { ...this.current, phase: 'reviewed' };
    return true;
  }

  issueIntent(): boolean {
    if (
      this.current.phase !== 'reviewed' ||
      this.authorityRevoked ||
      !this.admittedCapability ||
      !this.adapterAvailable ||
      !this.currentActionStillLegal()
    )
      return false;
    this.current = { ...this.current, phase: 'issue-intent' };
    return true;
  }

  cancel(): void {
    this.current = { phase: 'empty' };
  }

  private actionable(observed: ObservedCut): boolean {
    return (
      observed.decision === 'human' &&
      !!observed.audience.positionId &&
      !!observed.audience.actionSetToken &&
      revision(observed.audience.audienceRevision) !== null
    );
  }

  private currentActionStillLegal(): boolean {
    const observed = this.observed;
    const current = this.current;
    if (!observed || current.phase === 'empty') return false;
    if (
      this.authorityRevoked ||
      !this.actionable(observed) ||
      !sameStamp(current.audience, observed.audience)
    ) {
      this.current = { phase: 'empty' };
      return false;
    }
    const matching = observed.legalActions.filter(
      (action) => action.actionId === current.action.actionId,
    );
    if (matching.length !== 1 || !sameAction(matching[0], current.action)) {
      this.current = { phase: 'empty' };
      return false;
    }
    return true;
  }
}
