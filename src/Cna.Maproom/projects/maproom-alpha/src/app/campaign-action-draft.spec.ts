import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { CampaignActionDraft } from './campaign-action-draft';

const view: CoherentPlayerView = {
  contractVersion: 1,
  campaignId: 'campaign-a',
  sessionGeneration: 'generation-a',
  seat: 'axis',
  audienceRevision: '18446744073709551614',
  rulesetHash: 'rules',
  position: {
    positionId: 'position-a',
    turn: '1',
    phaseId: 'land.phase.movement',
    stageId: 'land.stage.movement',
    decision: 'human',
  },
  locations: [],
  ownFormations: [],
  apparentContacts: [],
  legalActions: [
    {
      actionId: 'opaque-a',
      actionContractVersion: 3,
      kind: 'move',
      label: 'Move infantry',
      formationId: 'own-a',
      targetLocationId: 'public-b',
    },
  ],
  formationLedger: [],
  warDiary: [],
  nextDiaryCursor: '0',
  actionSetToken: 'opaque-set-a',
};

describe('CampaignActionDraft', () => {
  it('stages only a unique outward legal action and requires explicit review', () => {
    const draft = new CampaignActionDraft();
    expect(draft.observeView(view)).toBe(true);
    expect(draft.review()).toBe(false);
    expect(draft.stage('invented-action')).toBe(false);
    expect(draft.stage('opaque-a')).toBe(true);
    expect(draft.state).toEqual({
      phase: 'draft',
      audience: {
        campaignId: 'campaign-a',
        sessionGeneration: 'generation-a',
        seat: 'axis',
        audienceRevision: '18446744073709551614',
        positionId: 'position-a',
        actionSetToken: 'opaque-set-a',
      },
      action: view.legalActions[0],
    });
    expect(draft.review()).toBe(true);
    expect(draft.state.phase).toBe('reviewed');
    expect(draft.issueIntent()).toBe(false);
    expect(draft.state.phase).toBe('reviewed');
  });

  it('keeps issue intent disabled unless both admission and adapter capability exist', () => {
    for (const [admitted, adapter] of [
      [false, false],
      [true, false],
      [false, true],
    ]) {
      const draft = new CampaignActionDraft(admitted, adapter);
      draft.observeView(view);
      draft.stage('opaque-a');
      draft.review();
      expect(draft.issueIntent()).toBe(false);
    }
    const enabled = new CampaignActionDraft(true, true);
    enabled.observeView(view);
    expect(enabled.issueIntent()).toBe(false);
    enabled.stage('opaque-a');
    expect(enabled.issueIntent()).toBe(false);
    enabled.review();
    expect(enabled.issueIntent()).toBe(true);
    expect(enabled.state.phase).toBe('issue-intent');
  });

  it('invalidates review on a newer audience cut, seat, generation, position, or action-set change', () => {
    const updates: CoherentPlayerView[] = [
      { ...view, audienceRevision: '18446744073709551615' },
      { ...view, seat: 'commonwealth' },
      { ...view, sessionGeneration: 'generation-b' },
      { ...view, campaignId: 'campaign-b' },
    ];
    for (const update of updates) {
      const draft = new CampaignActionDraft(true, true);
      draft.observeView(view);
      draft.stage('opaque-a');
      draft.review();
      expect(draft.observeView(update)).toBe(true);
      expect(draft.state).toEqual({ phase: 'empty' });
      expect(draft.issueIntent()).toBe(false);
    }
  });

  it('rejects older and conflicting equal-revision views without restoring a discarded cut', () => {
    const draft = new CampaignActionDraft();
    draft.observeView(view);
    expect(draft.observeView({ ...view, audienceRevision: '2' })).toBe(false);
    expect(draft.observeView({ ...view, actionSetToken: 'other-set' })).toBe(false);
    expect(draft.stage('opaque-a')).toBe(true);
    expect(draft.state.phase).toBe('draft');
  });

  it('refuses duplicate handles and non-human decisions', () => {
    const duplicate = new CampaignActionDraft();
    duplicate.observeView({ ...view, legalActions: [view.legalActions[0], view.legalActions[0]] });
    expect(duplicate.stage('opaque-a')).toBe(false);
    const unsupported = new CampaignActionDraft();
    unsupported.observeView({
      ...view,
      position: { ...view.position, decision: 'unsupported-phase' },
    });
    expect(unsupported.stage('opaque-a')).toBe(false);
  });

  it('snapshots legal actions and audience metadata before a caller mutates the source view', () => {
    const altered: CoherentPlayerView = {
      ...view,
      position: { ...view.position },
      legalActions: [{ ...view.legalActions[0] }],
    };
    const draft = new CampaignActionDraft(true, true);
    draft.observeView(altered);
    altered.legalActions.push({
      actionId: 'invented',
      actionContractVersion: 3,
      kind: 'move',
      label: 'Invented move',
    });
    altered.legalActions[0].targetLocationId = 'other-public-location';
    altered.actionSetToken = 'new-set';
    altered.position.positionId = 'other-position';
    expect(draft.stage('invented')).toBe(false);
    expect(draft.stage('opaque-a')).toBe(true);
    expect(draft.review()).toBe(true);
    expect(draft.state.phase).toBe('reviewed');
    expect(draft.state.phase === 'reviewed' && draft.state.action.targetLocationId).toBe(
      'public-b',
    );
  });

  it('revokes the constructor admission after A to B to late A identity changes', () => {
    const draft = new CampaignActionDraft(true, true);
    draft.observeView(view);
    draft.stage('opaque-a');
    draft.review();
    expect(draft.observeView({ ...view, seat: 'commonwealth' })).toBe(true);
    expect(draft.state).toEqual({ phase: 'empty' });
    expect(draft.observeView({ ...view })).toBe(true);
    expect(draft.stage('opaque-a')).toBe(false);
    expect(draft.review()).toBe(false);
    expect(draft.issueIntent()).toBe(false);
    expect(draft.state).toEqual({ phase: 'empty' });
  });

  it('does not expose mutable internal draft or review state to callers', () => {
    const draft = new CampaignActionDraft(true, true);
    draft.observeView(view);
    draft.stage('opaque-a');
    const exposed = draft.state as Record<string, unknown>;
    exposed['phase'] = 'reviewed';
    (exposed['action'] as { actionId: string }).actionId = 'invented';
    (exposed['audience'] as { actionSetToken: string }).actionSetToken = 'invented';
    expect(draft.state.phase).toBe('draft');
    expect(draft.issueIntent()).toBe(false);
    expect(draft.review()).toBe(true);
    expect(draft.state.phase).toBe('reviewed');
    expect(draft.issueIntent()).toBe(true);
    expect(draft.state.phase).toBe('issue-intent');
  });

  it('fails closed on invalid audience revision and malformed legal-action metadata', () => {
    const draft = new CampaignActionDraft();
    for (const invalid of ['01', '-1', '18446744073709551616', '1e2']) {
      expect(draft.observeView({ ...view, audienceRevision: invalid })).toBe(false);
      expect(draft.state).toEqual({ phase: 'empty' });
    }
    draft.observeView({
      ...view,
      legalActions: [{ ...view.legalActions[0], actionContractVersion: 0 }],
    });
    expect(draft.stage('opaque-a')).toBe(false);
  });
});
