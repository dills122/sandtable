import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActionReceipt } from './api/generated/model/actionReceipt';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { SubmitActionRequest } from './api/generated/model/submitActionRequest';
import { CampaignActionFlow } from './campaign-action-flow';
import { ConnectedCampaignService } from './connected-campaign.service';
import { HttpCampaignActionAdapter } from './http-campaign-action-adapter';

const request: SubmitActionRequest = {
  contractVersion: 1,
  requestId: 'request-1',
  sessionGeneration: 'generation-1',
  expectedPositionId: 'position-1',
  actionId: 'opaque-action-1',
  actionContractVersion: 3,
  actionSetToken: 'opaque-set-1',
};

const accepted: ActionReceipt = {
  contractVersion: 1,
  campaignId: 'campaign-1',
  requestId: 'request-1',
  status: 'accepted',
  reason: 'none',
  audienceRevision: '2',
};

const view: CoherentPlayerView = {
  contractVersion: 1,
  campaignId: 'campaign-1',
  sessionGeneration: 'generation-1',
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

describe('HttpCampaignActionAdapter', () => {
  let campaignApi: ConnectedCampaignService;
  let http: HttpTestingController;
  let adapter: HttpCampaignActionAdapter;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    campaignApi = TestBed.inject(ConnectedCampaignService);
    http = TestBed.inject(HttpTestingController);
    // Seed the live CSRF token the same way the app does on connect(), so submitAction's
    // required X-Sandtable-CSRF header is actually present -- this is exactly the credential
    // a plain DI-injected ActionsService would be missing (see HttpCampaignActionAdapter's
    // header comment and the report's wiring finding).
    const fixtures = campaignApi.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'csrf-token' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([]);
    await fixtures;
    adapter = new HttpCampaignActionAdapter(campaignApi.actionsService(), campaignApi);
  });

  afterEach(() => http.verify());

  it('submits via POST .../actions with the shared CSRF credential and resolves with the receipt', async () => {
    const result = adapter.submit('campaign-1', request);
    const req = http.expectOne('/api/v1/campaigns/campaign-1/actions');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.headers.get('X-Sandtable-CSRF')).toBe('csrf-token');
    req.flush(accepted);
    expect(await result).toEqual(accepted);
  });

  it('reads a receipt via GET .../receipts/{requestId}', async () => {
    const result = adapter.receipt('campaign-1', 'request-1');
    const req = http.expectOne('/api/v1/campaigns/campaign-1/receipts/request-1');
    expect(req.request.method).toBe('GET');
    req.flush(accepted);
    expect(await result).toEqual(accepted);
  });

  it('delegates view() to the already-authorized ConnectedCampaignService read', async () => {
    const result = adapter.view('campaign-1', 'generation-1', 'axis');
    const req = http.expectOne('/api/v1/campaigns/campaign-1/view');
    expect(req.request.method).toBe('GET');
    req.flush({ ...view, audienceRevision: '2' });
    expect((await result).audienceRevision).toBe('2');
  });

  it('propagates a submit HTTP error so CampaignActionFlow settles as uncertain, never resubmitting', async () => {
    const flow = new CampaignActionFlow(adapter, true, () => 'request-1');
    flow.observeView(view);
    const submitted = flow.submit('opaque-action-1');
    http
      .expectOne('/api/v1/campaigns/campaign-1/actions')
      .flush({ code: 'unavailable' }, { status: 503, statusText: 'Unavailable' });
    await submitted;
    expect(flow.state()).toEqual({
      phase: 'uncertain',
      requestId: 'request-1',
      actionLabel: 'Move',
    });
  });

  it('propagates a receipt HTTP error so CampaignActionFlow stays uncertain on readback', async () => {
    const flow = new CampaignActionFlow(adapter, true, () => 'request-1');
    flow.observeView(view);
    const submitted = flow.submit('opaque-action-1');
    http
      .expectOne('/api/v1/campaigns/campaign-1/actions')
      .flush({ code: 'unavailable' }, { status: 503, statusText: 'Unavailable' });
    await submitted;
    expect(flow.state().phase).toBe('uncertain');

    const read = flow.readback();
    http
      .expectOne('/api/v1/campaigns/campaign-1/receipts/request-1')
      .flush({ code: 'unavailable' }, { status: 503, statusText: 'Unavailable' });
    await read;
    expect(flow.state()).toEqual({
      phase: 'uncertain',
      requestId: 'request-1',
      actionLabel: 'Move',
    });
  });
});
