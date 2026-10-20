import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  EventEmitter,
  computed,
  inject,
  Input,
  Output,
  signal,
  ViewChild,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ConnectedCampaignService, SeatIdentityError } from './connected-campaign.service';
import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { LegalAction } from './api/generated/model/legalAction';
import { FixtureDescriptor } from './api/generated/model/fixtureDescriptor';
import { AtlasPanel, AtlasStatus } from '@sandtable/field-atlas';
import { isCameraCoordinate, MapCameraState } from './map-camera';
import { MapPan, OperationsMap } from './operations-map';
import { CampaignActionFlow } from './campaign-action-flow';
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
  imports: [AtlasPanel, AtlasStatus, OperationsMap],
  templateUrl: './connected-workspace.html',
  styleUrl: './connected-workspace.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectedWorkspace {
  @Input() embedded = false;
  @Output() readonly fullscreenChange = new EventEmitter<boolean>();
  @ViewChild(OperationsMap) private operationsMap?: OperationsMap;
  @ViewChild('mapScroll') private mapScroll?: ElementRef<HTMLElement>;
  private readonly api = inject(ConnectedCampaignService);
  private readonly destroyRef = inject(DestroyRef);
  // The host has not admitted action submission. No adapter or capability is installed.
  protected readonly actionFlow = new CampaignActionFlow();
  protected readonly camera = new MapCameraState();
  protected readonly mapFullscreen = signal(false);
  private originalPageOverflow = '';
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
  protected readonly selectedPoint = computed(() =>
    this.mapLocations().find(
      (point) =>
        point.locationId === this.selected()?.locationId &&
        isCameraCoordinate(point.left) &&
        isCameraCoordinate(point.top),
    ),
  );
  protected readonly selectedActions = computed(() => {
    const view = this.view();
    return view ? formationActions(view, this.selected()) : [];
  });
  protected readonly generalActions = computed(
    () => this.view()?.legalActions.filter((action) => !action.formationId) ?? [],
  );
  protected readonly kind = connectedKind;
  protected readonly symbol = connectedSymbol;

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.mapFullscreen()) document.documentElement.style.overflow = this.originalPageOverflow;
      this.fullscreenChange.emit(false);
    });
  }

  protected toggleMapFullscreen(): void {
    if (this.mapFullscreen()) {
      this.mapFullscreen.set(false);
      this.fullscreenChange.emit(false);
      document.documentElement.style.overflow = this.originalPageOverflow;
      this.operationsMap?.focusExpand();
    } else {
      this.originalPageOverflow = document.documentElement.style.overflow;
      document.documentElement.style.overflow = 'hidden';
      this.mapFullscreen.set(true);
      this.fullscreenChange.emit(true);
    }
  }

  protected panMap(event: MapPan): void {
    this.camera.panByPixels(event.x, event.y, event.width, event.height);
  }

  protected locateSelected(): void {
    const point = this.selectedPoint();
    if (!point || !this.camera.locate(point.left, point.top)) return;
    const scroll = this.mapScroll?.nativeElement;
    scroll?.scrollIntoView({ block: 'nearest' });
    requestAnimationFrame(() => {
      if (this.selectedPoint()?.locationId !== point.locationId) return;
      const marker = Array.from(
        scroll?.querySelectorAll<HTMLElement>('[data-location-id]') ?? [],
      ).find((element) => element.dataset['locationId'] === point.locationId);
      marker?.scrollIntoView({ block: 'nearest', inline: 'center' });
      scroll?.querySelector<HTMLElement>('.map-canvas')?.focus({ preventScroll: true });
    });
  }

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
        this.actionFlow.clearAudience();
        if (this.mapFullscreen()) this.toggleMapFullscreen();
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

  protected async requestAction(action: LegalAction): Promise<void> {
    await this.actionFlow.submit(action.actionId);
    this.syncActionAudience();
  }

  protected async readActionReceipt(): Promise<void> {
    await this.actionFlow.readback();
    this.syncActionAudience();
  }

  private syncActionAudience(): void {
    const refreshed = this.actionFlow.currentView();
    if (!refreshed) {
      this.view.set(null);
      this.selectedId.set(null);
      if (this.mapFullscreen()) this.toggleMapFullscreen();
      return;
    }
    if (refreshed !== this.view()) this.setView(refreshed);
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
    this.actionFlow.observeView(view);
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
