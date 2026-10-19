import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import {
  AtlasChoiceSelect,
  AtlasPanel,
  AtlasStatus,
  AtlasUnitCounter,
} from '@sandtable/field-atlas';
import { OperationsMap, MapPan } from './operations-map';
import { ConnectedWorkspace } from './connected-workspace';
import { OrderWorkflow } from './order-workflow';
import { provideMaproomForms } from './forms';
import { formations, WorkspaceStore } from './workspace.store';
@Component({
  selector: 'mr-workspace',
  imports: [
    AtlasChoiceSelect,
    AtlasPanel,
    AtlasStatus,
    AtlasUnitCounter,
    ConnectedWorkspace,
    OperationsMap,
    OrderWorkflow,
    RouterLink,
  ],
  providers: [WorkspaceStore, provideMaproomForms()],
  templateUrl: './workspace.html',
  styleUrl: './workspace.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Workspace {
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  protected readonly connectedMode = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('mode') === 'campaign')),
    { initialValue: this.route.snapshot.queryParamMap.get('mode') === 'campaign' },
  );
  protected readonly store = inject(WorkspaceStore);
  protected readonly formations = formations;
  protected readonly mapFullscreen = signal(false);
  protected readonly connectedFullscreen = signal(false);
  protected readonly ledgerQuery = signal('');
  protected readonly ledgerSort = signal<'name' | 'kind' | 'position' | 'order'>('name');
  protected readonly ledgerDescending = signal(false);
  protected readonly recordsExpanded = signal(false);
  protected readonly diaryScope = signal<'selected' | 'all'>('selected');
  protected readonly diaryScopeOptions = [
    { value: 'selected', label: 'Selected formation' },
    { value: 'all', label: 'All visible formations' },
  ] as const;
  protected readonly ledgerRows = computed(() => {
    const query = this.ledgerQuery().trim().toLocaleLowerCase();
    const sort = this.ledgerSort();
    const rows = this.store
      .visibleFormations()
      .filter((unit) =>
        [unit.name, unit.kind, unit.position, this.store.orderFor(unit.id).status].some((value) =>
          value.toLocaleLowerCase().includes(query),
        ),
      );
    rows.sort((left, right) => {
      const value = (unit: (typeof rows)[number]) =>
        sort === 'order' ? this.store.orderFor(unit.id).status : unit[sort];
      const result = value(left).localeCompare(value(right), undefined, { numeric: true });
      return (this.ledgerDescending() ? -1 : 1) * (result || left.name.localeCompare(right.name));
    });
    return rows;
  });
  protected readonly diaryEntries = computed(() =>
    this.store
      .diary()
      .filter(
        (entry) => this.diaryScope() === 'all' || entry.formationId === this.store.selectedId(),
      ),
  );
  protected setDiaryScope(value: string) {
    if (value === 'selected' || value === 'all') this.diaryScope.set(value);
  }
  @ViewChild(OperationsMap) private operationsMap?: OperationsMap;
  @ViewChild('inspectorPanel', { read: ElementRef })
  private inspectorPanel?: ElementRef<HTMLElement>;
  private originalPageOverflow = '';

  constructor() {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
    const intermediate = window.matchMedia('(min-width: 50.01rem) and (max-width: 75rem)');
    const applyDefault = () => this.store.setRosterOpen(!intermediate.matches);
    applyDefault();
    intermediate.addEventListener('change', applyDefault);
    this.destroyRef.onDestroy(() => {
      intermediate.removeEventListener('change', applyDefault);
      if (this.mapFullscreen()) document.documentElement.style.overflow = this.originalPageOverflow;
    });
  }

  protected toggleMapFullscreen() {
    if (this.mapFullscreen()) {
      this.closeMapFullscreen();
      return;
    }
    this.originalPageOverflow = document.documentElement.style.overflow;
    document.documentElement.style.overflow = 'hidden';
    this.mapFullscreen.set(true);
    this.operationsMap?.focusExpand();
  }

  private closeMapFullscreen(returnFocus = true) {
    this.mapFullscreen.set(false);
    document.documentElement.style.overflow = this.originalPageOverflow;
    if (returnFocus) this.operationsMap?.focusExpand();
  }

  protected openOrdersFromMap() {
    this.store.setInspectorOpen(true);
    this.closeMapFullscreen(false);
    requestAnimationFrame(() => {
      const panel = this.inspectorPanel?.nativeElement;
      panel?.scrollIntoView({ block: 'nearest' });
      const target =
        panel?.querySelector<HTMLElement>('mr-order-workflow button:not([disabled])') ??
        panel?.querySelector<HTMLElement>('.inspector-actions button:not([disabled])') ??
        panel?.querySelector<HTMLElement>('.inspector-header-actions .pane-toggle');
      target?.focus({ preventScroll: true });
    });
  }

  protected sortLedger(field: 'name' | 'kind' | 'position' | 'order') {
    if (this.ledgerSort() === field) this.ledgerDescending.update((value) => !value);
    else {
      this.ledgerSort.set(field);
      this.ledgerDescending.set(false);
    }
  }

  protected locateDiary(formationId: string) {
    this.store.select(formationId);
    this.store.locateSelected();
    document.getElementById('map-title')?.scrollIntoView({ block: 'nearest' });
  }

  protected formationName(id: string): string {
    return this.formations.find((unit) => unit.id === id)?.name ?? 'Formation';
  }

  @HostListener('document:keydown', ['$event'])
  protected onShortcut(event: KeyboardEvent) {
    if (
      this.connectedMode() ||
      event.defaultPrevented ||
      event.isComposing ||
      event.altKey ||
      event.ctrlKey ||
      event.metaKey
    )
      return;
    if (
      event.target instanceof Element &&
      event.target.closest(
        'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="textbox"], [role="combobox"], [role="spinbutton"]',
      )
    )
      return;
    if (document.querySelector('.cdk-overlay-pane:not(:empty), [role="dialog"]:not(.map-panel)'))
      return;
    const units = this.store.visibleFormations();
    const selectedIndex = units.findIndex((unit) => unit.id === this.store.selectedId());
    switch (event.key.toLowerCase()) {
      case '?':
        this.operationsMap?.toggleShortcuts();
        break;
      case '[':
        this.store.select(units[(selectedIndex - 1 + units.length) % units.length].id);
        break;
      case ']':
        this.store.select(units[(selectedIndex + 1) % units.length].id);
        break;
      case 'l':
        this.store.locateSelected();
        break;
      case '0':
        this.store.fitMap();
        break;
      case '+':
      case '=':
        this.store.zoomBy(0.25);
        break;
      case '-':
      case '_':
        this.store.zoomBy(-0.25);
        break;
      default:
        return;
    }
    event.preventDefault();
  }

  protected panMap(event: MapPan) {
    this.store.panByPixels(event.x, event.y, event.width, event.height);
  }
}
