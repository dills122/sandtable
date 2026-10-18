import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ConnectedCampaignService, SeatIdentityError } from './connected-campaign.service';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { FixtureDescriptor } from './api/generated/model/fixtureDescriptor';
import { AtlasPanel, AtlasStatus } from '@sandtable/field-atlas';
import {
  connectedKind,
  connectedSymbol,
  formationActions,
  operationalLocations,
  selectedFormationId,
} from './connected-read-model';

type Seat = 'axis' | 'commonwealth';

@Component({
  selector: 'mr-connected-workspace',
  imports: [AtlasPanel, AtlasStatus],
  templateUrl: './connected-workspace.html',
  styleUrl: './connected-workspace.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectedWorkspace {
  private readonly api = inject(ConnectedCampaignService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly fixtures = signal<FixtureDescriptor[]>([]);
  protected readonly view = signal<CoherentPlayerView | null>(null);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly selected = computed(() =>
    this.view()?.ownFormations.find((formation) => formation.formationId === this.selectedId()),
  );
  protected readonly selectedLocation = computed(() =>
    this.view()?.locations.find((location) => location.locationId === this.selected()?.locationId),
  );
  protected readonly mapLocations = computed(() => {
    const view = this.view();
    return view ? operationalLocations(view) : [];
  });
  protected readonly selectedActions = computed(() => {
    const view = this.view();
    return view ? formationActions(view, this.selected()) : [];
  });
  protected readonly generalActions = computed(
    () => this.view()?.legalActions.filter((action) => !action.formationId) ?? [],
  );
  protected readonly kind = connectedKind;
  protected readonly symbol = connectedSymbol;

  protected async connect(): Promise<void> {
    await this.run(async () => this.fixtures.set(await this.api.availableFixtures()));
  }

  protected async create(fixtureId: string, seat: Seat): Promise<void> {
    await this.run(async () => this.setView(await this.api.createAndRead(fixtureId, seat), true));
  }

  protected async refresh(): Promise<void> {
    const current = this.view();
    if (!current) return;
    await this.run(async () => {
      try {
        this.setView(
          await this.api.refreshAuthorizedView(
            current.campaignId,
            current.sessionGeneration,
            current.seat,
          ),
        );
      } catch (cause) {
        // A failed read cannot retain the old audience's map or selected formation.
        this.view.set(null);
        this.selectedId.set(null);
        throw cause;
      }
    });
  }

  protected select(id: string): void {
    if (this.view()?.ownFormations.some((formation) => formation.formationId === id)) {
      this.selectedId.set(id);
    }
  }

  protected locationLabel(id: string): string {
    return (
      this.view()?.locations.find((location) => location.locationId === id)?.label ??
      'Unknown location'
    );
  }

  private setView(view: CoherentPlayerView, initial = false): void {
    if (
      !Array.isArray(view.ownFormations) ||
      !Array.isArray(view.locations) ||
      !Array.isArray(view.apparentContacts) ||
      !Array.isArray(view.formationLedger) ||
      !Array.isArray(view.warDiary) ||
      !Array.isArray(view.legalActions)
    ) {
      throw new Error('Incomplete audience view');
    }
    this.selectedId.set(selectedFormationId(view, this.selectedId(), initial));
    this.view.set(view);
  }

  private async run(action: () => Promise<void>): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await action();
    } catch (cause) {
      if (cause instanceof HttpErrorResponse) {
        this.error.set(
          cause.status === 503
            ? 'The local campaign view is unavailable at this phase. No order was issued.'
            : cause.status === 401 || cause.status === 403
              ? 'Seat access expired or was denied. The previous view was removed.'
              : cause.status === 409
                ? 'This host already holds a campaign for a different operator or request. Reuse the original tab while its operator session is active, or restart the local host to create a new campaign.'
                : 'The local campaign server did not complete this request.',
        );
      } else if (cause instanceof SeatIdentityError) {
        this.error.set(cause.message);
      } else {
        this.error.set(cause instanceof Error ? cause.message : 'Unable to read campaign');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
