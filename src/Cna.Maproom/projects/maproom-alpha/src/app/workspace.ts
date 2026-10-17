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
import { AtlasPanel, AtlasStatus, AtlasUnitCounter } from '@sandtable/field-atlas';
import { OrderWorkflow } from './order-workflow';
import { provideMaproomForms } from './forms';
import { formations, WorkspaceStore } from './workspace.store';
@Component({
  selector: 'mr-workspace',
  imports: [AtlasPanel, AtlasStatus, AtlasUnitCounter, OrderWorkflow],
  providers: [WorkspaceStore, provideMaproomForms()],
  templateUrl: './workspace.html',
  styleUrl: './workspace.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Workspace {
  private readonly destroyRef = inject(DestroyRef);
  protected readonly store = inject(WorkspaceStore);
  protected readonly formations = formations;
  protected readonly shortcutsOpen = signal(false);
  protected readonly dragging = signal(false);
  protected readonly mapFullscreen = signal(false);
  protected readonly ledgerQuery = signal('');
  protected readonly ledgerSort = signal<'name' | 'kind' | 'position' | 'order'>('name');
  protected readonly ledgerDescending = signal(false);
  protected readonly recordsExpanded = signal(false);
  protected readonly diaryScope = signal<'selected' | 'all'>('selected');
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
  @ViewChild('shortcutToggle') private shortcutToggle?: ElementRef<HTMLButtonElement>;
  @ViewChild('mapExpandToggle') private mapExpandToggle?: ElementRef<HTMLButtonElement>;
  @ViewChild('mapPanel') private mapPanel?: ElementRef<HTMLElement>;
  @ViewChild('inspectorPanel', { read: ElementRef })
  private inspectorPanel?: ElementRef<HTMLElement>;
  private dragStart: { x: number; y: number; pointerId: number; moved: boolean } | null = null;
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

  protected toggleShortcuts() {
    this.shortcutsOpen.update((open) => !open);
  }

  protected toggleMapFullscreen() {
    if (this.mapFullscreen()) {
      this.closeMapFullscreen();
      return;
    }
    this.originalPageOverflow = document.documentElement.style.overflow;
    document.documentElement.style.overflow = 'hidden';
    this.mapFullscreen.set(true);
    this.mapExpandToggle?.nativeElement.focus({ preventScroll: true });
  }

  private closeMapFullscreen(returnFocus = true) {
    this.mapFullscreen.set(false);
    document.documentElement.style.overflow = this.originalPageOverflow;
    this.dragStart = null;
    this.dragging.set(false);
    if (returnFocus) this.mapExpandToggle?.nativeElement.focus({ preventScroll: true });
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
    if (event.key === 'Tab' && this.mapFullscreen()) {
      const controls = Array.from(
        this.mapPanel?.nativeElement.querySelectorAll<HTMLElement>(
          'button:not([disabled]), [tabindex="0"]',
        ) ?? [],
      ).filter((control) => !control.closest('[hidden]'));
      if (controls.length) {
        const current = controls.indexOf(document.activeElement as HTMLElement);
        if (event.shiftKey && current <= 0) {
          controls[controls.length - 1].focus();
          event.preventDefault();
        } else if (!event.shiftKey && current === controls.length - 1) {
          controls[0].focus();
          event.preventDefault();
        }
      }
      return;
    }
    if (event.key === 'Escape' && this.mapFullscreen()) {
      this.closeMapFullscreen();
      event.preventDefault();
      return;
    }
    if (
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
    if (event.key === 'Escape' && this.shortcutsOpen()) {
      this.shortcutsOpen.set(false);
      this.shortcutToggle?.nativeElement.focus();
      event.preventDefault();
      return;
    }

    const units = this.store.visibleFormations();
    const selectedIndex = units.findIndex((unit) => unit.id === this.store.selectedId());
    switch (event.key.toLowerCase()) {
      case '?':
        this.toggleShortcuts();
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

  protected onMapKeydown(event: KeyboardEvent) {
    if (event.target !== event.currentTarget) return;
    if (
      event.defaultPrevented ||
      event.isComposing ||
      event.altKey ||
      event.ctrlKey ||
      event.metaKey
    )
      return;
    if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return;
    if (this.store.zoom() <= 1) return;
    const frame = event.currentTarget as HTMLElement;
    const step = 40;
    const x = event.key === 'ArrowLeft' ? step : event.key === 'ArrowRight' ? -step : 0;
    const y = event.key === 'ArrowUp' ? step : event.key === 'ArrowDown' ? -step : 0;
    this.store.panByPixels(x, y, frame.clientWidth, frame.clientHeight);
    event.preventDefault();
    event.stopPropagation();
  }

  protected onMapPointerDown(event: PointerEvent) {
    if (event.button !== 0 || !['mouse', 'pen', 'touch'].includes(event.pointerType)) return;
    if ((event.target as Element).closest('fa-unit-counter, button')) return;
    const frame = event.currentTarget as HTMLElement;
    this.dragStart = {
      x: event.clientX,
      y: event.clientY,
      pointerId: event.pointerId,
      moved: false,
    };
    frame.setPointerCapture(event.pointerId);
  }

  protected onMapPointerMove(event: PointerEvent) {
    if (!this.dragStart || this.dragStart.pointerId !== event.pointerId) return;
    const frame = event.currentTarget as HTMLElement;
    const deltaX = event.clientX - this.dragStart.x;
    const deltaY = event.clientY - this.dragStart.y;
    if (!this.dragStart.moved && Math.hypot(deltaX, deltaY) < 6) return;
    if (!this.dragStart.moved) {
      this.dragStart.moved = true;
      if (this.store.zoom() === 1) this.store.zoomBy(0.25);
      this.dragging.set(true);
    }
    this.store.panByPixels(deltaX, deltaY, frame.clientWidth, frame.clientHeight);
    this.dragStart = {
      x: event.clientX,
      y: event.clientY,
      pointerId: event.pointerId,
      moved: true,
    };
    event.preventDefault();
  }

  protected onMapPointerEnd(event: PointerEvent) {
    if (this.dragStart?.pointerId !== event.pointerId) return;
    this.dragStart = null;
    this.dragging.set(false);
  }
}
