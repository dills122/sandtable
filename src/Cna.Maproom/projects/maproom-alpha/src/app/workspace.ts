import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  ViewChild,
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
  protected readonly store = inject(WorkspaceStore);
  protected readonly formations = formations;
  protected readonly shortcutsOpen = signal(false);
  protected readonly dragging = signal(false);
  @ViewChild('shortcutToggle') private shortcutToggle?: ElementRef<HTMLButtonElement>;
  private dragStart: { x: number; y: number; pointerId: number } | null = null;

  constructor() {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
    const intermediate = window.matchMedia('(min-width: 50.01rem) and (max-width: 75rem)');
    const applyDefault = () => this.store.setRosterOpen(!intermediate.matches);
    applyDefault();
    intermediate.addEventListener('change', applyDefault);
    inject(DestroyRef).onDestroy(() => intermediate.removeEventListener('change', applyDefault));
  }

  protected toggleShortcuts() {
    this.shortcutsOpen.update((open) => !open);
  }

  @HostListener('document:keydown', ['$event'])
  protected onShortcut(event: KeyboardEvent) {
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
    if (document.querySelector('.cdk-overlay-pane:not(:empty), [role="dialog"]')) return;
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
    if (event.button !== 0 || !['mouse', 'pen'].includes(event.pointerType)) return;
    if ((event.target as Element).closest('fa-unit-counter, button')) return;
    const frame = event.currentTarget as HTMLElement;
    if (this.store.zoom() <= 1) return;
    this.dragStart = { x: event.clientX, y: event.clientY, pointerId: event.pointerId };
    frame.setPointerCapture(event.pointerId);
    this.dragging.set(true);
  }

  protected onMapPointerMove(event: PointerEvent) {
    if (!this.dragStart || this.dragStart.pointerId !== event.pointerId) return;
    const frame = event.currentTarget as HTMLElement;
    this.store.panByPixels(
      event.clientX - this.dragStart.x,
      event.clientY - this.dragStart.y,
      frame.clientWidth,
      frame.clientHeight,
    );
    this.dragStart = { x: event.clientX, y: event.clientY, pointerId: event.pointerId };
  }

  protected onMapPointerEnd(event: PointerEvent) {
    if (this.dragStart?.pointerId !== event.pointerId) return;
    this.dragStart = null;
    this.dragging.set(false);
  }
}
