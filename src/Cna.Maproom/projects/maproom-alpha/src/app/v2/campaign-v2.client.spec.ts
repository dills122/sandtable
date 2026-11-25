import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { CampaignV2Client } from './campaign-v2.client';
import { CampaignV2Error, OpeningView, SeatCredential } from './types';

const credential: SeatCredential = {
  campaignId: 'c1',
  audience: 'axis',
  epoch: 1,
  secret: 'TOP-SECRET-VALUE',
  expiresUnixMs: 10,
};
const openingBody = {
  contractVersion: 2,
  campaignId: 'c1',
  audience: 'axis',
  stage: 'initiative-order',
  actingElementId: 'initiative-order',
  revision: 4,
  visibleCutBase64: btoa(JSON.stringify({ choices: ['act-first', 'act-last'] })),
  publicActionRefs: ['r1', 'r2'],
};
const opening: OpeningView = {
  campaignId: 'c1',
  audience: 'axis',
  stage: 'initiative-order',
  actingElementId: 'initiative-order',
  revision: 4,
  visibleCut: { choices: ['act-first', 'act-last'] },
  publicActionRefs: ['r1', 'r2'],
};

describe('CampaignV2Client', () => {
  let client: CampaignV2Client;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    client = TestBed.inject(CampaignV2Client);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('queries an opening with the v2 envelope', async () => {
    const pending = client.queryOpening(credential, 'initiative-order', 'initiative-order');
    const req = http.expectOne('/api/v2/campaigns/c1/openings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      contractVersion: 2,
      credential,
      stage: 'initiative-order',
      actingElementId: 'initiative-order',
    });
    req.flush(openingBody);
    const view = await pending;
    expect(view.visibleCut).toEqual({ choices: ['act-first', 'act-last'] });
  });

  it('refuses an opening served for the other side', async () => {
    const pending = client.queryOpening(credential, 'initiative-order', 'initiative-order');
    http
      .expectOne('/api/v2/campaigns/c1/openings')
      .flush({ ...openingBody, audience: 'commonwealth' });
    await expect(pending).rejects.toBeInstanceOf(CampaignV2Error);
  });

  it('submits a choice and verifies the receipt', async () => {
    const pending = client.submitAction(credential, opening, 'r2', 'initiative-order');
    const req = http.expectOne('/api/v2/campaigns/c1/actions');
    expect(req.request.body).toMatchObject({
      openingRevision: 4,
      publicActionRef: 'r2',
      stage: 'initiative-order',
    });
    req.flush({ contractVersion: 2, accepted: true, openingRevision: 4, publicActionRef: 'r2' });
    expect(await pending).toEqual({ kind: 'accepted', openingRevision: 4, publicActionRef: 'r2' });
  });

  it('turns a 409 into a rejected outcome, not an exception', async () => {
    const pending = client.submitAction(credential, opening, 'r1');
    http
      .expectOne('/api/v2/campaigns/c1/actions')
      .flush(
        { contractVersion: 2, code: 'rule-not-implemented', requestId: 'rq' },
        { status: 409, statusText: 'Conflict' },
      );
    expect(await pending).toEqual({
      kind: 'rejected',
      code: 'rule-not-implemented',
      requestId: 'rq',
    });
  });

  it('never sends a ref outside the opening or for another seat', async () => {
    await expect(client.submitAction(credential, opening, 'forged')).rejects.toBeInstanceOf(
      CampaignV2Error,
    );
    await expect(
      client.submitAction({ ...credential, audience: 'commonwealth' }, opening, 'r1'),
    ).rejects.toBeInstanceOf(CampaignV2Error);
  });

  it('keeps the credential out of error messages', async () => {
    const pending = client.queryOpening(credential, 'initiative-order', 'initiative-order');
    http
      .expectOne('/api/v2/campaigns/c1/openings')
      .flush(
        { contractVersion: 2, code: 'unauthorized' },
        { status: 401, statusText: 'Unauthorized' },
      );
    const error = (await pending.catch((e: unknown) => e)) as CampaignV2Error;
    expect(error).toBeInstanceOf(CampaignV2Error);
    expect(error.status).toBe(401);
    expect(JSON.stringify([error.message, error.code, error.requestId])).not.toContain(
      credential.secret,
    );
  });

  it('sends the operator secret only on the grant call', async () => {
    const pending = client.issueGrant('OP', 'c1', 'axis');
    const req = http.expectOne('/api/v2/campaigns/c1/grants/axis');
    expect(req.request.headers.get('X-Sandtable-Operator')).toBe('OP');
    req.flush({ contractVersion: 2, grant: credential });
    const grant = await pending;
    const redeem = client.redeemSeat(grant);
    const seat = http.expectOne('/api/v2/campaigns/c1/seats');
    expect(seat.request.headers.has('X-Sandtable-Operator')).toBe(false);
    seat.flush({ contractVersion: 2, credential });
    expect((await redeem).audience).toBe('axis');
  });

  it('rejects a seat redemption that changes identity', async () => {
    const redeem = client.redeemSeat(credential);
    http.expectOne('/api/v2/campaigns/c1/seats').flush({
      contractVersion: 2,
      credential: { ...credential, audience: 'commonwealth' },
    });
    await expect(redeem).rejects.toBeInstanceOf(CampaignV2Error);
  });

  it('lists opening kinds', async () => {
    const pending = client.openingKinds();
    http
      .expectOne('/api/v2/campaigns/opening-kinds')
      .flush({ contractVersion: 2, kinds: ['Movement'] });
    expect((await pending).kinds).toEqual(['Movement']);
  });
});
