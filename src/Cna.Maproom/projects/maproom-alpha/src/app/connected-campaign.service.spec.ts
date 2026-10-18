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
    await expect(result).rejects.toThrow('Seat response did not match campaign');
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
});
