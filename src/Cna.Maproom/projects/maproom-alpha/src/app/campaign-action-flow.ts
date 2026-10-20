import { signal } from '@angular/core';
import { ActionReceipt } from './api/generated/model/actionReceipt';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { SubmitActionRequest } from './api/generated/model/submitActionRequest';

/** A future admitted host adapter must supply all three operations for one authorized seat. */
export interface CampaignActionAdapter {
  submit(campaignId: string, request: SubmitActionRequest): Promise<ActionReceipt>;
  receipt(campaignId: string, requestId: string): Promise<ActionReceipt>;
  view(
    campaignId: string,
    generation: string,
    seat: CoherentPlayerView['seat'],
  ): Promise<CoherentPlayerView>;
}

export type ActionFlowState =
  | { phase: 'disabled' | 'ready' }
  | { phase: 'submitting' | 'pending' | 'uncertain'; requestId: string; actionLabel: string }
  | {
      phase: 'accepted';
      requestId: string;
      actionLabel: string;
      audienceRevision: string;
      refreshPending: boolean;
    }
  | { phase: 'rejected'; requestId: string; actionLabel: string; reason: string };

function sameAudience(left: CoherentPlayerView, right: CoherentPlayerView): boolean {
  return (
    left.campaignId === right.campaignId &&
    left.seat === right.seat &&
    left.sessionGeneration === right.sessionGeneration
  );
}

function validRevision(value: string): boolean {
  return (
    typeof value === 'string' &&
    value.length <= 20 &&
    /^(0|[1-9]\d*)$/.test(value) &&
    BigInt(value) <= 18446744073709551615n
  );
}

/**
 * Browser presentation state only. The shipped workspace constructs this without an adapter or
 * admitted capability, so its submit control is always disabled. No route is activated here.
 */
export class CampaignActionFlow {
  readonly state = signal<ActionFlowState>({ phase: 'disabled' });
  readonly currentView = signal<CoherentPlayerView | null>(null);
  private epoch = 0;
  private reading = false;
  private capabilityRevoked = false;

  constructor(
    private readonly adapter: CampaignActionAdapter | null = null,
    private readonly admittedCapability = false,
    private readonly newRequestId: () => string = () => crypto.randomUUID(),
  ) {}

  get canSubmit(): boolean {
    const view = this.currentView();
    return (
      this.admittedCapability &&
      !this.capabilityRevoked &&
      this.adapter !== null &&
      view !== null &&
      !!view.actionSetToken &&
      !!view.position.positionId &&
      view.position.decision === 'human' &&
      this.state().phase === 'ready'
    );
  }

  observeView(view: CoherentPlayerView): void {
    const previous = this.currentView();
    if (!previous) {
      this.epoch++;
      this.state.set({
        phase:
          this.admittedCapability && this.adapter && !this.capabilityRevoked ? 'ready' : 'disabled',
      });
    } else if (!sameAudience(previous, view)) {
      // An adapter/capability for the prior seat must never authorize another audience.
      this.epoch++;
      this.capabilityRevoked = true;
      this.state.set({ phase: 'disabled' });
    } else if (
      this.state().phase === 'rejected' &&
      (previous.audienceRevision !== view.audienceRevision ||
        previous.position.positionId !== view.position.positionId ||
        previous.actionSetToken !== view.actionSetToken)
    ) {
      this.state.set({
        phase:
          this.admittedCapability && this.adapter && !this.capabilityRevoked ? 'ready' : 'disabled',
      });
    }
    this.currentView.set(view);
  }

  clearAudience(): void {
    this.epoch++;
    this.capabilityRevoked = true;
    this.currentView.set(null);
    this.state.set({ phase: 'disabled' });
  }

  acknowledgeAccepted(): void {
    const state = this.state();
    if (state.phase === 'accepted' && !state.refreshPending && this.currentView()) {
      this.state.set({
        phase:
          this.admittedCapability && this.adapter && !this.capabilityRevoked ? 'ready' : 'disabled',
      });
    }
  }

  async submit(actionId: string): Promise<void> {
    if (!this.canSubmit || !this.adapter) return;
    const view = this.currentView()!;
    const matching = view.legalActions.filter((action) => action.actionId === actionId);
    if (matching.length !== 1 || !view.actionSetToken || !view.position.positionId) return;
    const action = matching[0];
    const requestId = this.newRequestId();
    const epoch = this.epoch;
    const request: SubmitActionRequest = {
      contractVersion: 1,
      requestId,
      sessionGeneration: view.sessionGeneration,
      expectedPositionId: view.position.positionId,
      actionId: action.actionId,
      actionContractVersion: action.actionContractVersion,
      actionSetToken: view.actionSetToken,
    };
    this.state.set({ phase: 'submitting', requestId, actionLabel: action.label });
    try {
      const receipt = await this.adapter.submit(view.campaignId, request);
      if (epoch !== this.epoch) return;
      await this.acceptReceipt(view, receipt, requestId, action.label, epoch);
    } catch {
      if (epoch === this.epoch) {
        // A lost reply gives no proof that the server rejected the action. Never POST again.
        this.state.set({ phase: 'uncertain', requestId, actionLabel: action.label });
      }
    }
  }

  async readback(): Promise<void> {
    const state = this.state();
    const view = this.currentView();
    if (
      this.reading ||
      !this.adapter ||
      !view ||
      !('requestId' in state) ||
      !['pending', 'uncertain', 'accepted'].includes(state.phase)
    )
      return;
    const epoch = this.epoch;
    this.reading = true;
    try {
      if (state.phase === 'accepted') {
        if (state.refreshPending) await this.refreshAfterAcceptance(view, state, epoch);
        return;
      }
      const receipt = await this.adapter.receipt(view.campaignId, state.requestId);
      if (epoch !== this.epoch) return;
      await this.acceptReceipt(view, receipt, state.requestId, state.actionLabel, epoch);
    } catch {
      if (epoch === this.epoch) {
        this.state.set({
          phase: 'uncertain',
          requestId: state.requestId,
          actionLabel: state.actionLabel,
        });
      }
    } finally {
      this.reading = false;
    }
  }

  private async acceptReceipt(
    view: CoherentPlayerView,
    receipt: ActionReceipt,
    requestId: string,
    actionLabel: string,
    epoch: number,
  ): Promise<void> {
    if (
      receipt.contractVersion !== 1 ||
      receipt.campaignId !== view.campaignId ||
      receipt.requestId !== requestId
    ) {
      this.state.set({ phase: 'uncertain', requestId, actionLabel });
      return;
    }
    switch (receipt.status) {
      case 'pending':
        if (receipt.reason !== 'none') {
          this.state.set({ phase: 'uncertain', requestId, actionLabel });
          break;
        }
        this.state.set({ phase: 'pending', requestId, actionLabel });
        break;
      case 'rejected':
        if (!['invalid-action', 'stale-view', 'unsupported-phase'].includes(receipt.reason)) {
          this.state.set({ phase: 'uncertain', requestId, actionLabel });
          break;
        }
        this.state.set({ phase: 'rejected', requestId, actionLabel, reason: receipt.reason });
        break;
      case 'accepted':
        if (receipt.reason !== 'none' || !validRevision(receipt.audienceRevision)) {
          this.state.set({ phase: 'uncertain', requestId, actionLabel });
          break;
        }
        this.state.set({
          phase: 'accepted',
          requestId,
          actionLabel,
          audienceRevision: receipt.audienceRevision,
          refreshPending: true,
        });
        await this.refreshAfterAcceptance(
          view,
          this.state() as Extract<ActionFlowState, { phase: 'accepted' }>,
          epoch,
        );
        break;
      default:
        this.state.set({ phase: 'uncertain', requestId, actionLabel });
    }
  }

  private async refreshAfterAcceptance(
    view: CoherentPlayerView,
    state: Extract<ActionFlowState, { phase: 'accepted' }>,
    epoch: number,
  ): Promise<void> {
    if (!this.adapter) return;
    try {
      const refreshed = await this.adapter.view(view.campaignId, view.sessionGeneration, view.seat);
      if (epoch !== this.epoch) return;
      if (refreshed.contractVersion !== 1 || !sameAudience(view, refreshed)) {
        this.clearAudience();
        return;
      }
      if (
        !validRevision(refreshed.audienceRevision) ||
        BigInt(refreshed.audienceRevision) < BigInt(state.audienceRevision)
      ) {
        this.state.set({ ...state, refreshPending: true });
        return;
      }
      this.currentView.set(refreshed);
      this.state.set({ ...state, refreshPending: false });
    } catch {
      if (epoch === this.epoch) this.state.set({ ...state, refreshPending: true });
    }
  }
}
