import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CampaignsService } from './api/generated/api/campaigns.service';
import { FixturesService } from './api/generated/api/fixtures.service';
import { SessionsService } from './api/generated/api/sessions.service';
import { Configuration } from './api/generated/configuration';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { FixtureDescriptor } from './api/generated/model/fixtureDescriptor';
import { CampaignCreated } from './api/generated/model/campaignCreated';
import { SessionInfo } from './api/generated/model/sessionInfo';

/** Same-origin local API only. The CSRF and one-use grants stay in memory, never in the URL. */
@Injectable({ providedIn: 'root' })
export class ConnectedCampaignService {
  private readonly http = inject(HttpClient);
  private csrfToken: string | undefined;
  private readonly configuration = new Configuration({
    basePath: '',
    withCredentials: true,
    credentials: { csrfHeader: () => this.csrfToken },
  });
  private readonly sessions = new SessionsService(this.http, '', this.configuration);
  private readonly campaigns = new CampaignsService(this.http, '', this.configuration);
  private readonly fixtures = new FixturesService(this.http, '', this.configuration);
  private creation:
    { fixtureId: string; requestId: string; response?: CampaignCreated } | undefined;
  private entered: SessionInfo | undefined;

  async availableFixtures(): Promise<FixtureDescriptor[]> {
    const bootstrap = await firstValueFrom(this.sessions.bootstrapLocalOperator());
    if (bootstrap.contractVersion !== 1 || !bootstrap.csrfToken)
      throw new Error('Unsupported bootstrap');
    this.csrfToken = bootstrap.csrfToken;
    return firstValueFrom(this.fixtures.listFixtures());
  }

  async createAndRead(
    fixtureId: string,
    seat: 'axis' | 'commonwealth',
  ): Promise<CoherentPlayerView> {
    if (!this.csrfToken) throw new Error('Start the local connection first');
    if (this.creation?.fixtureId !== fixtureId) {
      this.creation = { fixtureId, requestId: crypto.randomUUID() };
      this.entered = undefined;
    }
    const created =
      this.creation.response ??
      (await firstValueFrom(
        this.campaigns.createCampaign({
          contractVersion: 1,
          fixtureId,
          requestId: this.creation.requestId,
        }),
      ));
    if (created.contractVersion !== 1 || !created.campaign.campaignId) {
      throw new Error('Unsupported campaign response');
    }
    this.creation.response = created;
    const session =
      this.entered ??
      (await firstValueFrom(
        this.sessions.enterSeat(created.campaign.campaignId, {
          contractVersion: 1,
          grantId: created.seatGrants[seat],
        }),
      ));
    if (
      session.contractVersion !== 1 ||
      session.campaignId !== created.campaign.campaignId ||
      session.seat !== seat
    )
      throw new Error('Seat response did not match campaign');
    this.entered = session;
    return this.read(created.campaign.campaignId, session.sessionGeneration, seat);
  }

  async read(
    campaignId: string,
    generation: string,
    seat: 'axis' | 'commonwealth',
  ): Promise<CoherentPlayerView> {
    const view = await firstValueFrom(this.campaigns.getPlayerView(campaignId));
    if (
      view.contractVersion !== 1 ||
      view.campaignId !== campaignId ||
      view.sessionGeneration !== generation ||
      view.seat !== seat
    ) {
      throw new Error('View did not match the active seat');
    }
    return view;
  }
}
