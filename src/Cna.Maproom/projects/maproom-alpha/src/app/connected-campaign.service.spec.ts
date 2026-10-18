import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ConnectedCampaignService } from './connected-campaign.service';

describe('ConnectedCampaignService', () => {
  let service: ConnectedCampaignService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConnectedCampaignService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('bootstraps, creates with CSRF, redeems only chosen grant, then reads authorized view', async () => {
    const fixturePromise = service.availableFixtures();
    const bootstrap = http.expectOne('/api/v1/bootstrap');
    expect(bootstrap.request.method).toBe('GET');
    bootstrap.flush({ contractVersion: 1, csrfToken: 'csrf-from-host' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([{ fixtureId: 'rules9', displayName: 'Lab' }]);
    expect((await fixturePromise)[0].fixtureId).toBe('rules9');

    const result = service.createAndRead('rules9', 'axis');
    const create = http.expectOne('/api/v1/campaigns');
    expect(create.request.method).toBe('POST');
    expect(create.request.headers.get('X-Sandtable-CSRF')).toBe('csrf-from-host');
    expect(create.request.body).toEqual({
      contractVersion: 1,
      fixtureId: 'rules9',
      requestId: expect.stringMatching(/^[0-9a-f-]{36}$/),
    });
    create.flush({
      contractVersion: 1,
      campaign: { campaignId: 'campaign-1' },
      seatGrants: {
        axis: 'axis-secret',
        commonwealth: 'opposing-secret',
      },
    });
    await Promise.resolve();
    const enter = http.expectOne('/api/v1/campaigns/campaign-1/seat');
    expect(enter.request.body).toEqual({ contractVersion: 1, grantId: 'axis-secret' });
    enter.flush({
      contractVersion: 1,
      campaignId: 'campaign-1',
      seat: 'axis',
      sessionGeneration: 'generation-1',
      lifetime: 'temporary-memory',
    });
    await Promise.resolve();
    const view = http.expectOne('/api/v1/campaigns/campaign-1/view');
    expect(view.request.method).toBe('GET');
    view.flush({
      contractVersion: 1,
      campaignId: 'campaign-1',
      seat: 'axis',
      sessionGeneration: 'generation-1',
      ownFormations: [],
      apparentContacts: [],
      locations: [],
      formationLedger: [],
      warDiary: [],
      legalActions: [],
    });
    expect((await result).seat).toBe('axis');
  });

  it('rejects a mismatched seat before reading a view', async () => {
    const fixtures = service.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'csrf' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([]);
    await fixtures;
    const result = service.createAndRead('rules9', 'axis');
    http.expectOne('/api/v1/campaigns').flush({
      contractVersion: 1,
      campaign: { campaignId: 'campaign-1' },
      seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/campaign-1/seat').flush({
      contractVersion: 1,
      campaignId: 'campaign-1',
      seat: 'commonwealth',
      sessionGeneration: 'wrong',
    });
    await expect(result).rejects.toThrow('Seat identity changed');
  });

  it('reuses the exact creation request ID after an unavailable reply', async () => {
    const fixtures = service.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'csrf' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([]);
    await fixtures;

    const first = service.createAndRead('rules9', 'axis');
    const failed = http.expectOne('/api/v1/campaigns');
    const requestId = failed.request.body.requestId;
    failed.flush(
      { contractVersion: 1, code: 'unavailable' },
      { status: 503, statusText: 'Unavailable' },
    );
    await expect(first).rejects.toBeTruthy();

    const retry = service.createAndRead('rules9', 'axis');
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'csrf' });
    await Promise.resolve();
    await Promise.resolve();
    const next = http.expectOne('/api/v1/campaigns');
    expect(next.request.body.requestId).toBe(requestId);
    next.flush({
      contractVersion: 1,
      campaign: { campaignId: 'campaign-1' },
      seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/campaign-1/seat').flush({
      contractVersion: 1,
      campaignId: 'campaign-1',
      seat: 'axis',
      sessionGeneration: 'g-1',
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/campaign-1/view').flush({
      contractVersion: 1,
      campaignId: 'campaign-1',
      seat: 'axis',
      sessionGeneration: 'g-1',
    });
    expect((await retry).campaignId).toBe('campaign-1');
  });

  it('resumes an already seated browser after reload without redeeming another grant', async () => {
    const fixtures = service.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'seat-csrf' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([{ fixtureId: 'rules9', displayName: 'Lab' }]);
    await fixtures;

    const resumed = service.createAndRead('rules9', 'axis');
    http
      .expectOne('/api/v1/campaigns')
      .flush(
        { contractVersion: 1, code: 'unauthorized' },
        { status: 401, statusText: 'Unauthorized' },
      );
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/session').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'live-generation',
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/view').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'live-generation',
      ownFormations: [],
      apparentContacts: [],
      locations: [],
      formationLedger: [],
      warDiary: [],
      legalActions: [],
    });
    expect((await resumed).sessionGeneration).toBe('live-generation');
    http.expectNone('/api/v1/campaigns/rules9/seat');
  });

  it('drops cached creation and seat identity when bootstrap changes after host restart', async () => {
    const fixtures = service.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'old-csrf' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([{ fixtureId: 'rules9', displayName: 'Lab' }]);
    await fixtures;

    const first = service.createAndRead('rules9', 'axis');
    const oldCreate = http.expectOne('/api/v1/campaigns');
    const oldRequestId = oldCreate.request.body.requestId;
    oldCreate.flush({
      contractVersion: 1,
      campaign: { campaignId: 'rules9' },
      seatGrants: { axis: 'old-grant', commonwealth: 'old-opposing-grant' },
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/seat').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'old-generation',
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/view').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'old-generation',
    });
    await first;

    const refreshed = service.availableFixtures();
    http.expectOne('/api/v1/bootstrap').flush({ contractVersion: 1, csrfToken: 'new-csrf' });
    await Promise.resolve();
    http.expectOne('/api/v1/fixtures').flush([{ fixtureId: 'rules9', displayName: 'Lab' }]);
    await refreshed;

    const next = service.createAndRead('rules9', 'axis');
    const newCreate = http.expectOne('/api/v1/campaigns');
    expect(newCreate.request.body.requestId).not.toBe(oldRequestId);
    expect(newCreate.request.headers.get('X-Sandtable-CSRF')).toBe('new-csrf');
    newCreate.flush({
      contractVersion: 1,
      campaign: { campaignId: 'rules9' },
      seatGrants: { axis: 'new-grant', commonwealth: 'new-opposing-grant' },
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/seat').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'new-generation',
    });
    await Promise.resolve();
    http.expectOne('/api/v1/campaigns/rules9/view').flush({
      contractVersion: 1,
      campaignId: 'rules9',
      seat: 'axis',
      sessionGeneration: 'new-generation',
    });
    expect((await next).sessionGeneration).toBe('new-generation');
  });
});
