import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { CampaignV2Client } from '../v2/campaign-v2.client';
import { CampaignV2Error, OpeningView, SeatCredential } from '../v2/types';
import { HotSeatSession } from './hot-seat.session';

const cred = (audience: 'axis' | 'commonwealth'): SeatCredential => ({
  campaignId: 'c1',
  audience,
  epoch: 1,
  secret: `SECRET-${audience}`,
  expiresUnixMs: 1,
});
const opening = (audience: 'axis' | 'commonwealth', stage: string, label: string): OpeningView => ({
  campaignId: 'c1',
  audience,
  stage,
  actingElementId: stage,
  revision: 1,
  visibleCut: { choices: [label] },
  publicActionRefs: [`ref-${audience}-${stage}`],
});

class FakeClient {
  queries: { audience: string; stage: string }[] = [];
  gate: Promise<void> | undefined;
  open: Record<string, OpeningView> = {};
  submitted: string[] = [];
  discoveries: string[] = [];
  async discoverOpenings(c: SeatCredential) {
    this.discoveries.push(c.audience);
    await this.gate;
    return Object.values(this.open)
      .filter((o) => o.audience === c.audience)
      .map((o) => ({
        openingStage: o.stage,
        actionStage: o.stage,
        actingElementIds: [o.actingElementId],
        ruleNotImplemented: false,
      }));
  }
  async queryOpening(c: SeatCredential, stage: string): Promise<OpeningView> {
    this.queries.push({ audience: c.audience, stage });
    await this.gate;
    const hit = this.open[`${c.audience}:${stage}`];
    if (!hit) throw new CampaignV2Error('no', 409, 'unavailable');
    return hit;
  }
  async submitAction(c: SeatCredential, o: OpeningView, ref: string) {
    await this.gate;
    this.submitted.push(`${c.audience}:${ref}`);
    return { kind: 'accepted' as const, openingRevision: o.revision, publicActionRef: ref };
  }
}

describe('HotSeatSession', () => {
  let session: HotSeatSession;
  let client: FakeClient;
  beforeEach(() => {
    client = new FakeClient();
    TestBed.configureTestingModule({
      providers: [HotSeatSession, { provide: CampaignV2Client, useValue: client }],
    });
    session = TestBed.inject(HotSeatSession);
  });

  it('starts at a neutral hand-off to Axis and reveals nothing until confirmed', async () => {
    session.configure(cred('axis'), cred('commonwealth'));
    expect(session.phase()).toBe('handoff');
    expect(session.seat()).toBe('axis');
    await session.findDecisions();
    expect(client.queries).toEqual([]);
    session.reveal();
    expect(session.phase()).toBe('seat');
  });

  it('rejects mismatched credentials', () => {
    expect(() => session.configure(cred('commonwealth'), cred('axis'))).toThrow(CampaignV2Error);
    expect(() =>
      session.configure(cred('axis'), { ...cred('commonwealth'), campaignId: 'c2' }),
    ).toThrow(CampaignV2Error);
    expect(session.phase()).toBe('setup');
  });

  it('uses only advertised elements of the active seat', async () => {
    client.open['axis:initiative-order'] = opening('axis', 'initiative-order', 'act-first');
    client.open['commonwealth:movement'] = opening('commonwealth', 'movement', 'choice');
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    expect(session.openings().map((f) => f.opening.stage)).toEqual(['initiative-order']);
    expect(client.discoveries).toEqual(['axis']);
    expect(client.queries).toEqual([{ audience: 'axis', stage: 'initiative-order' }]);
  });

  it('does not probe unsupported entries and clears their notice on hand-off', async () => {
    client.discoverOpenings = async () => [
      {
        openingStage: 'future-opening',
        actionStage: 'future-action',
        actingElementIds: [],
        ruleNotImplemented: true,
      },
    ];
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    expect(session.unsupported()).toEqual(['future-opening']);
    expect(client.queries).toEqual([]);
    session.handoffTo('commonwealth');
    expect(session.unsupported()).toEqual([]);
  });

  it('uses the host action-stage mapping for an advertised element', async () => {
    const seatOpening = opening('axis', 'example', 'choice');
    client.discoverOpenings = async () => [
      {
        openingStage: 'example',
        actionStage: 'example-submit',
        actingElementIds: ['example'],
        ruleNotImplemented: false,
      },
    ];
    client.open['axis:example'] = seatOpening;
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    expect(session.openings()[0].entry.actionStage).toBe('example-submit');
  });

  it('destroys the prior seat decisions, outcome and error on hand-off', async () => {
    client.open['axis:initiative-order'] = opening('axis', 'initiative-order', 'act-first');
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    await session.choose(session.openings()[0], 'ref-axis-initiative-order');
    expect(session.outcome()).not.toBeNull();
    session.handoffTo('commonwealth');
    expect(session.phase()).toBe('handoff');
    expect(session.openings()).toEqual([]);
    expect(session.outcome()).toBeNull();
    expect(session.error()).toBe('');
    expect(session.searched()).toBe(false);
    session.reveal();
    await session.findDecisions();
    expect(session.openings()).toEqual([]);
    expect(client.discoveries).toContain('commonwealth');
  });

  it('discards a lookup reply that lands after the hand-off', async () => {
    client.open['axis:initiative-order'] = opening('axis', 'initiative-order', 'act-first');
    let release!: () => void;
    client.gate = new Promise<void>((r) => (release = r));
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    const pending = session.findDecisions();
    session.handoffTo('commonwealth');
    release();
    await pending;
    expect(session.openings()).toEqual([]);
    expect(session.searched()).toBe(false);
    expect(session.busy()).toBe(false);
  });

  it('discards a submit reply that lands after the hand-off', async () => {
    client.open['axis:initiative-order'] = opening('axis', 'initiative-order', 'act-first');
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    let release!: () => void;
    client.gate = new Promise<void>((r) => (release = r));
    const pending = session.choose(session.openings()[0], 'ref-axis-initiative-order');
    session.handoffTo('commonwealth');
    release();
    await pending;
    expect(session.outcome()).toBeNull();
  });

  it('cannot act while on the hand-off screen, and never as the other seat', async () => {
    client.open['axis:initiative-order'] = opening('axis', 'initiative-order', 'act-first');
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    const found = session.openings()[0];
    session.handoffTo('commonwealth');
    await session.choose(found, 'ref-axis-initiative-order');
    expect(client.submitted).toEqual([]);
  });

  it('keeps credentials out of every public property and forgets them on reset', () => {
    session.configure(cred('axis'), cred('commonwealth'));
    expect(JSON.stringify(session)).not.toContain('SECRET');
    expect(Object.keys(session).join()).not.toMatch(/credential|secret/i);
    session.reset();
    expect(session.phase()).toBe('setup');
    expect(session.campaignId()).toBeNull();
    expect(session.seat()).toBeNull();
  });

  it('turns a host failure into a short audience-safe error', async () => {
    client.discoverOpenings = async () => {
      throw new CampaignV2Error(
        'Host rejected the request (401 unauthorized).',
        401,
        'unauthorized',
      );
    };
    session.configure(cred('axis'), cred('commonwealth'));
    session.reveal();
    await session.findDecisions();
    expect(session.error()).toContain('401');
    expect(session.error()).not.toContain('SECRET');
  });
});
