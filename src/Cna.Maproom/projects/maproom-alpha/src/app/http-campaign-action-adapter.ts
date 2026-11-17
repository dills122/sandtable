import { firstValueFrom } from 'rxjs';
import { ActionsService } from './api/generated/api/actions.service';
import { ActionReceipt } from './api/generated/model/actionReceipt';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { SubmitActionRequest } from './api/generated/model/submitActionRequest';
import { CampaignActionAdapter } from './campaign-action-flow';
import { ConnectedCampaignService } from './connected-campaign.service';

/**
 * The real CampaignActionAdapter: submit/receipt delegate to the generated ActionsService
 * (POST /api/v1/campaigns/{campaignId}/actions, GET .../receipts/{requestId}), converted from
 * Observable to Promise with `firstValueFrom`, matching this codebase's existing convention
 * (see ConnectedCampaignService). view() delegates to ConnectedCampaignService.read(), which
 * already performs the admitted-view read (GET .../view) and revalidates the returned view's
 * campaignId/sessionGeneration/seat against what was requested -- the same authorized-view path
 * ConnectedCampaignService.createAndRead()/refreshAuthorizedView() already use. There is no
 * separate "admitted view" endpoint on ActionsService itself, and none is invented here.
 *
 * `actionsApi` must come from `ConnectedCampaignService.actionsService()`, not from Angular DI
 * (`inject(ActionsService)`), because submitAction requires the live CSRF credential that only
 * ConnectedCampaignService's Configuration carries.
 */
export class HttpCampaignActionAdapter implements CampaignActionAdapter {
  constructor(
    private readonly actionsApi: ActionsService,
    private readonly campaignApi: ConnectedCampaignService,
  ) {}

  submit(campaignId: string, request: SubmitActionRequest): Promise<ActionReceipt> {
    return firstValueFrom(this.actionsApi.submitAction(campaignId, request));
  }

  receipt(campaignId: string, requestId: string): Promise<ActionReceipt> {
    return firstValueFrom(this.actionsApi.getActionReceipt(campaignId, requestId));
  }

  view(
    campaignId: string,
    generation: string,
    seat: CoherentPlayerView['seat'],
  ): Promise<CoherentPlayerView> {
    return this.campaignApi.read(campaignId, generation, seat);
  }
}
