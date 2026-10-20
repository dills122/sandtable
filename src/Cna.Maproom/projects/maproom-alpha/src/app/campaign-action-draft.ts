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
  private view: CoherentPlayerView | null = null;
  private observedAudience: AudienceStamp | null = null;
  private current: CampaignActionDraftState = { phase: 'empty' };

  constructor(
    private readonly admittedCapability = false,
    private readonly adapterAvailable = false,
  ) {}

  get state(): CampaignActionDraftState {
    return this.current;
  }

  observeView(view: CoherentPlayerView): boolean {
    if (
      view.contractVersion !== 1 ||
      !view.campaignId ||
      !view.sessionGeneration ||
      !['axis', 'commonwealth'].includes(view.seat) ||
      revision(view.audienceRevision) === null ||
      !view.position?.positionId ||
      !Array.isArray(view.legalActions)
    ) {
      this.clear();
      return false;
    }

    const previous = this.observedAudience;
    const incoming = stamp(view);
    if (this.view === view) {
      if (previous && sameStamp(previous, incoming)) return true;
      this.clear();
      return false;
    }
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
    this.view = view;
    this.observedAudience = incoming;
    this.current = { phase: 'empty' };
    return true;
  }

  clear(): void {
    this.view = null;
    this.observedAudience = null;
    this.current = { phase: 'empty' };
  }

  stage(actionId: string): boolean {
    const view = this.view;
    if (
      !view ||
      !this.observedAudience ||
      !sameStamp(this.observedAudience, stamp(view)) ||
      !this.actionable(view) ||
      this.current.phase === 'issue-intent'
    ) {
      this.current = { phase: 'empty' };
      return false;
    }
    const matching = view.legalActions.filter((action) => action.actionId === actionId);
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
    this.current = { phase: 'draft', audience: stamp(view), action: copyAction(action) };
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

  private actionable(view: CoherentPlayerView): boolean {
    return (
      view.position.decision === 'human' &&
      !!view.position.positionId &&
      !!view.actionSetToken &&
      revision(view.audienceRevision) !== null
    );
  }

  private currentActionStillLegal(): boolean {
    const view = this.view;
    const current = this.current;
    if (!view || current.phase === 'empty') return false;
    if (
      !this.observedAudience ||
      !this.actionable(view) ||
      !sameStamp(this.observedAudience, stamp(view)) ||
      !sameStamp(current.audience, stamp(view))
    ) {
      this.current = { phase: 'empty' };
      return false;
    }
    const matching = view.legalActions.filter(
      (action) => action.actionId === current.action.actionId,
    );
    if (matching.length !== 1 || !sameAction(matching[0], current.action)) {
      this.current = { phase: 'empty' };
      return false;
    }
    return true;
  }
}
