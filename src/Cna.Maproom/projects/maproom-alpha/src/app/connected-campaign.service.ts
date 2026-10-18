import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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
import { Bootstrap } from './api/generated/model/bootstrap';

export class SeatIdentityError extends Error {
  constructor(message = 'Seat identity changed. The previous audience view was removed.') {
    super(message);
  }
}

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
    this.acceptBootstrap(await firstValueFrom(this.sessions.bootstrapLocalOperator()));
    return firstValueFrom(this.fixtures.listFixtures());
  }

  private acceptBootstrap(bootstrap: Bootstrap): void {
    if (bootstrap.contractVersion !== 1 || !bootstrap.csrfToken)
      throw new Error('Unsupported bootstrap');
    // A restarted host or replaced cookie cannot reuse the prior request/grant/seat identity.
    if (this.csrfToken !== bootstrap.csrfToken) {
      this.creation = undefined;
      this.entered = undefined;
    }
    this.csrfToken = bootstrap.csrfToken;
  }

  private async refreshBootstrap(): Promise<void> {
    this.acceptBootstrap(await firstValueFrom(this.sessions.bootstrapLocalOperator()));
  }

  async createAndRead(
    fixtureId: string,
    seat: 'axis' | 'commonwealth',
  ): Promise<CoherentPlayerView> {
    if (!this.csrfToken) throw new Error('Start the local connection first');
    if (this.creation || this.entered) await this.refreshBootstrap();
    if (this.entered?.campaignId === fixtureId) {
      if (this.entered.seat !== seat) return this.resumeSeat(fixtureId, seat);
      return this.read(fixtureId, this.entered.sessionGeneration, seat);
    }
    if (this.creation?.fixtureId !== fixtureId) {
      this.creation = { fixtureId, requestId: crypto.randomUUID() };
      this.entered = undefined;
    }
    let created: CampaignCreated;
    try {
      created =
        this.creation.response ??
        (await firstValueFrom(
          this.campaigns.createCampaign({
            contractVersion: 1,
            fixtureId,
            requestId: this.creation.requestId,
          }),
        ));
    } catch (cause) {
      // A retained seat cookie cannot create another campaign. Read its authorized seat instead.
      if (cause instanceof HttpErrorResponse && cause.status === 401)
        return this.resumeSeat(fixtureId, seat);
      throw cause;
    }
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
      throw new SeatIdentityError();
    this.entered = session;
    return this.read(created.campaign.campaignId, session.sessionGeneration, seat);
  }

  private async resumeSeat(
    fixtureId: string,
    seat: 'axis' | 'commonwealth',
  ): Promise<CoherentPlayerView> {
    const session = await firstValueFrom(this.sessions.getSession(fixtureId));
    if (session.contractVersion !== 1 || session.campaignId !== fixtureId)
      throw new SeatIdentityError();
    if (session.seat !== seat) {
      if (session.seat !== 'axis' && session.seat !== 'commonwealth') throw new SeatIdentityError();
      const current = session.seat === 'axis' ? 'Axis' : 'Commonwealth';
      throw new SeatIdentityError(
        `This browser is already seated as ${current}. Choose ${current} to reopen its view.`,
      );
    }
    this.creation = undefined;
    this.entered = session;
    return this.read(fixtureId, session.sessionGeneration, seat);
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
      throw new SeatIdentityError();
    }
    return view;
  }

  async refreshAuthorizedView(
    campaignId: string,
    generation: string,
    seat: 'axis' | 'commonwealth',
  ): Promise<CoherentPlayerView> {
    try {
      return await this.read(campaignId, generation, seat);
    } catch (cause) {
      if (!(cause instanceof SeatIdentityError)) throw cause;
      // A second local tab can rotate the cookie for the same seat. Recheck
      // server-owned identity before accepting the replacement view.
      const session = await firstValueFrom(this.sessions.getSession(campaignId));
      if (
        session.contractVersion !== 1 ||
        session.campaignId !== campaignId ||
        session.seat !== seat
      )
        throw cause;
      this.entered = session;
      return this.read(campaignId, session.sessionGeneration, seat);
    }
  }
}
