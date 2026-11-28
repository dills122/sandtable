import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  CONTRACT_VERSION,
  parseAcceptedAction,
  parseErrorBody,
  parseDiscoveredOpenings,
  parseGrantResponse,
  parseOpeningKinds,
  parseOpeningResponse,
  parseSeatResponse,
} from './codec';
import {
  ActionOutcome,
  Audience,
  CampaignV2Error,
  DiscoveredOpening,
  OpeningKindsView,
  OpeningView,
  SeatCredential,
  SeatGrant,
} from './types';

const BASE = '/api/v2/campaigns';

/**
 * Typed client for the durable Host's v2 owner API. Stateless: it holds no credential, no cache and
 * no operator secret, so seat hand-off cannot leak a prior seat through this service. Same-origin
 * only (the Host rejects anything else); the browser supplies Sec-Fetch-Site.
 */
@Injectable({ providedIn: 'root' })
export class CampaignV2Client {
  private readonly http = inject(HttpClient);

  async openingKinds(): Promise<OpeningKindsView> {
    return parseOpeningKinds(await this.send('GET', `${BASE}/opening-kinds`));
  }

  /** Operator-only: mint one grant for a seat. The operator secret is passed through, never kept. */
  async issueGrant(
    operatorSecret: string,
    campaignId: string,
    audience: Audience,
  ): Promise<SeatGrant> {
    return parseGrantResponse(
      await this.send(
        'POST',
        `${BASE}/${encodeURIComponent(campaignId)}/grants/${audience}`,
        {},
        operatorSecret,
      ),
    );
  }

  async redeemSeat(grant: SeatGrant): Promise<SeatCredential> {
    const credential = parseSeatResponse(
      await this.send('POST', `${BASE}/${encodeURIComponent(grant.campaignId)}/seats`, {
        contractVersion: CONTRACT_VERSION,
        grant,
      }),
    );
    if (credential.campaignId !== grant.campaignId || credential.audience !== grant.audience)
      throw new CampaignV2Error('Unsupported v2 response: seat identity');
    return credential;
  }

  async discoverOpenings(credential: SeatCredential): Promise<readonly DiscoveredOpening[]> {
    return parseDiscoveredOpenings(
      await this.send(
        'POST',
        `${BASE}/${encodeURIComponent(credential.campaignId)}/openings/open`,
        {
          contractVersion: CONTRACT_VERSION,
          credential,
        },
      ),
      credential,
    );
  }

  async queryOpening(
    credential: SeatCredential,
    stage: string,
    actingElementId: string,
  ): Promise<OpeningView> {
    const raw = await this.send(
      'POST',
      `${BASE}/${encodeURIComponent(credential.campaignId)}/openings`,
      {
        contractVersion: CONTRACT_VERSION,
        credential,
        stage,
        actingElementId,
      },
    );
    const opening = parseOpeningResponse(raw, { campaignId: credential.campaignId, stage });
    // A cut for the other side must never reach a seat that did not ask for it.
    if (opening.audience !== credential.audience)
      throw new CampaignV2Error('Unsupported v2 response: audience');
    return opening;
  }

  /**
   * Submits one choice. A 409 is a normal outcome (`unavailable` after a stale opening,
   * `rule-not-implemented` for a source-legal choice Core has not built); everything else throws.
   * Retrying the same ref is idempotent on the Host, so callers may retry after a lost reply.
   */
  async submitAction(
    credential: SeatCredential,
    opening: OpeningView,
    publicActionRef: string,
    actionStage: string = opening.stage,
  ): Promise<ActionOutcome> {
    if (!opening.publicActionRefs.includes(publicActionRef))
      throw new CampaignV2Error('That choice is not part of this opening.');
    if (opening.audience !== credential.audience || opening.campaignId !== credential.campaignId)
      throw new CampaignV2Error('This opening belongs to a different seat.');
    try {
      const raw = await this.send(
        'POST',
        `${BASE}/${encodeURIComponent(credential.campaignId)}/actions`,
        {
          contractVersion: CONTRACT_VERSION,
          credential,
          stage: actionStage,
          actingElementId: opening.actingElementId,
          openingRevision: opening.revision,
          publicActionRef,
        },
      );
      return parseAcceptedAction(raw, { openingRevision: opening.revision, publicActionRef });
    } catch (cause) {
      if (cause instanceof CampaignV2Error && cause.status === 409)
        return { kind: 'rejected', code: cause.code ?? 'rejected', requestId: cause.requestId };
      throw cause;
    }
  }

  private async send(
    method: 'GET' | 'POST',
    url: string,
    body?: unknown,
    operatorSecret?: string,
  ): Promise<unknown> {
    const headers: Record<string, string> = operatorSecret
      ? { 'X-Sandtable-Operator': operatorSecret }
      : {};
    try {
      return await firstValueFrom(
        this.http.request<unknown>(method, url, { body, headers, withCredentials: false }),
      );
    } catch (cause) {
      if (cause instanceof HttpErrorResponse) {
        const { code, requestId } = parseErrorBody(cause.error);
        // Deliberately omit cause.message/url/body: they can carry the credential or grant.
        throw new CampaignV2Error(
          `Host rejected the request (${cause.status}${code ? ` ${code}` : ''}).`,
          cause.status,
          code,
          requestId,
        );
      }
      throw cause;
    }
  }
}
