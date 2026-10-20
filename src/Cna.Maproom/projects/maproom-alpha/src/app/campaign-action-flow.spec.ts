import { ActionReceipt } from './api/generated/model/actionReceipt';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { CampaignActionAdapter, CampaignActionFlow } from './campaign-action-flow';

const view: CoherentPlayerView = {
  contractVersion: 1,
  campaignId: 'campaign-a',
  sessionGeneration: 'seat-generation-1',
  seat: 'axis',
  audienceRevision: '1',
  rulesetHash: 'rules',
  position: {
    positionId: 'position-1',
    turn: '1',
    phaseId: 'movement',
    stageId: 'orders',
    decision: 'human',
  },
  locations: [],
  ownFormations: [],
  apparentContacts: [],
  legalActions: [
    { actionId: 'opaque-action-1', actionContractVersion: 3, kind: 'move', label: 'Move' },
  ],
  formationLedger: [],
  warDiary: [],
  nextDiaryCursor: '0',
  actionSetToken: 'opaque-set-1',
};

const accepted: ActionReceipt = {
  contractVersion: 1,
  campaignId: 'campaign-a',
  requestId: 'request-1',
  status: 'accepted',
  reason: 'none',
  audienceRevision: '2',
};

function adapter(overrides: Partial<CampaignActionAdapter> = {}): CampaignActionAdapter {
  return {
    submit: vi.fn().mockResolvedValue(accepted),
    receipt: vi.fn().mockResolvedValue(accepted),
    view: vi.fn().mockResolvedValue({ ...view, audienceRevision: '2' }),
    ...overrides,
  };
}

describe('CampaignActionFlow', () => {
  it('keeps the shipped Maproom capability closed even when a view offers legal actions', async () => {
    const flow = new CampaignActionFlow();
    flow.observeView(view);
    expect(flow.canSubmit).toBe(false);
    await flow.submit('opaque-action-1');
    expect(flow.state()).toEqual({ phase: 'disabled' });
  });

  it('binds one request to exact view-issued action, token, position, and seat; then refreshes authoritative view', async () => {
    const api = adapter();
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(api.submit).toHaveBeenCalledWith('campaign-a', {
      contractVersion: 1,
      requestId: 'request-1',
      sessionGeneration: 'seat-generation-1',
      expectedPositionId: 'position-1',
      actionId: 'opaque-action-1',
      actionContractVersion: 3,
      actionSetToken: 'opaque-set-1',
    });
    expect(api.view).toHaveBeenCalledWith('campaign-a', 'seat-generation-1', 'axis');
    expect(flow.state()).toEqual({
      phase: 'accepted',
      requestId: 'request-1',
      actionLabel: 'Move',
      audienceRevision: '2',
      refreshPending: false,
    });
    expect(flow.currentView()?.audienceRevision).toBe('2');
    expect(flow.canSubmit).toBe(false);
    flow.acknowledgeAccepted();
    expect(flow.canSubmit).toBe(true);
  });

  it('uses receipt readback after a lost reply and never resubmits an uncertain request', async () => {
    const api = adapter({
      submit: vi.fn().mockRejectedValue(new Error('reply lost')),
      receipt: vi
        .fn()
        .mockResolvedValueOnce({
          contractVersion: 1,
          campaignId: 'campaign-a',
          requestId: 'request-1',
          status: 'pending',
          reason: 'none',
        })
        .mockResolvedValueOnce(accepted),
    });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.state().phase).toBe('uncertain');
    await flow.readback();
    expect(flow.state().phase).toBe('pending');
    await flow.readback();
    expect(flow.state().phase).toBe('accepted');
    expect(api.submit).toHaveBeenCalledTimes(1);
    expect(api.receipt).toHaveBeenCalledTimes(2);
    expect(api.receipt).toHaveBeenCalledWith('campaign-a', 'request-1');
  });

  it('invalidates in-flight results and the old audience on a seat change', async () => {
    let reply!: (receipt: ActionReceipt) => void;
    const api = adapter({
      submit: vi.fn().mockImplementation(
        () =>
          new Promise<ActionReceipt>((resolve) => {
            reply = resolve;
          }),
      ),
    });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    const submitted = flow.submit('opaque-action-1');
    flow.observeView({
      ...view,
      seat: 'commonwealth',
      sessionGeneration: 'seat-generation-2',
      legalActions: [],
    });
    reply(accepted);
    await submitted;
    expect(flow.state()).toEqual({ phase: 'disabled' });
    expect(flow.canSubmit).toBe(false);
    expect(flow.currentView()?.seat).toBe('commonwealth');
    expect(api.view).not.toHaveBeenCalled();
  });

  it('does not trust a mismatched receipt or duplicate action handle', async () => {
    const api = adapter({ submit: vi.fn().mockResolvedValue({ ...accepted, requestId: 'other' }) });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.state().phase).toBe('uncertain');
    expect(api.view).not.toHaveBeenCalled();

    const duplicate = new CampaignActionFlow(api, true, () => 'request-2');
    duplicate.observeView({ ...view, legalActions: [view.legalActions[0], view.legalActions[0]] });
    await duplicate.submit('opaque-action-1');
    expect(api.submit).toHaveBeenCalledTimes(1);
  });

  it('shows a rejected receipt without retrying the action or changing the audience view', async () => {
    const api = adapter({
      submit: vi.fn().mockResolvedValue({
        contractVersion: 1,
        campaignId: 'campaign-a',
        requestId: 'request-1',
        status: 'rejected',
        reason: 'stale-view',
      }),
    });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.state()).toEqual({
      phase: 'rejected',
      requestId: 'request-1',
      actionLabel: 'Move',
      reason: 'stale-view',
    });
    expect(api.view).not.toHaveBeenCalled();
    expect(flow.canSubmit).toBe(false);
    flow.observeView({ ...view, audienceRevision: '2', actionSetToken: 'new-set' });
    expect(flow.state()).toEqual({ phase: 'ready' });
  });

  it('preserves accepted outcome when view refresh fails and retries only the read', async () => {
    const api = adapter({
      view: vi
        .fn()
        .mockRejectedValueOnce(new Error('view unavailable'))
        .mockResolvedValueOnce({ ...view, audienceRevision: '2' }),
    });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.state()).toMatchObject({ phase: 'accepted', refreshPending: true });
    await flow.readback();
    expect(flow.state()).toMatchObject({ phase: 'accepted', refreshPending: false });
    expect(api.submit).toHaveBeenCalledTimes(1);
    expect(api.receipt).not.toHaveBeenCalled();
    expect(api.view).toHaveBeenCalledTimes(2);
  });

  it('does not present an old audience revision as the view after accepted adjudication', async () => {
    const api = adapter({
      view: vi
        .fn()
        .mockResolvedValueOnce(view)
        .mockResolvedValueOnce({ ...view, audienceRevision: '2' }),
    });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.state()).toMatchObject({ phase: 'accepted', refreshPending: true });
    expect(flow.currentView()?.audienceRevision).toBe('1');
    await flow.readback();
    expect(flow.state()).toMatchObject({ phase: 'accepted', refreshPending: false });
    expect(flow.currentView()?.audienceRevision).toBe('2');
  });

  it('clears the old audience when accepted readback returns another seat', async () => {
    const api = adapter({ view: vi.fn().mockResolvedValue({ ...view, seat: 'commonwealth' }) });
    const flow = new CampaignActionFlow(api, true, () => 'request-1');
    flow.observeView(view);
    await flow.submit('opaque-action-1');
    expect(flow.currentView()).toBeNull();
    expect(flow.state()).toEqual({ phase: 'disabled' });
  });
});
