import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ConnectedCampaignService } from './connected-campaign.service';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { FixtureDescriptor } from './api/generated/model/fixtureDescriptor';
import { MapLocation } from './api/generated/model/mapLocation';

type Seat = 'axis' | 'commonwealth';

@Component({
  selector: 'mr-connected-workspace',
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

  protected async connect(): Promise<void> {
    await this.run(async () => this.fixtures.set(await this.api.availableFixtures()));
  }

  protected async create(fixtureId: string, seat: Seat): Promise<void> {
    await this.run(async () => this.setView(await this.api.createAndRead(fixtureId, seat)));
  }

  protected async refresh(): Promise<void> {
    const current = this.view();
    if (!current) return;
    await this.run(async () =>
      this.setView(
        await this.api.read(current.campaignId, current.sessionGeneration, current.seat),
      ),
    );
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

  protected location(id: string): MapLocation | undefined {
    return this.view()?.locations.find((location) => location.locationId === id);
  }

  private setView(view: CoherentPlayerView): void {
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
    this.selectedId.set(
      view.ownFormations.some((formation) => formation.formationId === this.selectedId())
        ? this.selectedId()
        : (view.ownFormations[0]?.formationId ?? null),
    );
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
              ? 'Seat access expired or was denied. Start a new local session.'
              : 'The local campaign server did not complete this request.',
        );
      } else {
        this.error.set(cause instanceof Error ? cause.message : 'Unable to read campaign');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
