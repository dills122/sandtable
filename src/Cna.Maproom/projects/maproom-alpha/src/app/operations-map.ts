import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  ViewChild,
  inject,
  signal,
} from '@angular/core';

export interface MapPan {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Presentation-only camera and hex surface shared by fixture and connected cuts. */
@Component({
  selector: 'mr-operations-map',
  templateUrl: './operations-map.html',
  styleUrl: './operations-map.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OperationsMap {
  @Input({ required: true }) zoom = 1;
  @Input({ required: true }) transform = '';
  @Input() hint = '';
  @Input() caption = '';
  @Input() scope = '';
  @Input() expanded = false;
  @Input() ordersLabel = '';
  @Output() readonly fit = new EventEmitter<void>();
  @Output() readonly zoomBy = new EventEmitter<number>();
  @Output() readonly pan = new EventEmitter<MapPan>();
  @Output() readonly expandedChange = new EventEmitter<boolean>();
  @Output() readonly openOrders = new EventEmitter<void>();
  @ViewChild('expandToggle') private expandToggle?: ElementRef<HTMLButtonElement>;
  @ViewChild('shortcutToggle') private shortcutToggle?: ElementRef<HTMLButtonElement>;
  private readonly host = inject(ElementRef) as ElementRef<HTMLElement>;
  private readonly destroyRef = inject(DestroyRef);
  protected readonly shortcutsOpen = signal(false);
  protected readonly dragging = signal(false);
  private dragStart: { x: number; y: number; pointerId: number; moved: boolean } | null = null;

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('keydown', this.onWindowKeydown, true);
    this.destroyRef.onDestroy(() =>
      window.removeEventListener('keydown', this.onWindowKeydown, true),
    );
  }

  private readonly onWindowKeydown = (event: KeyboardEvent): void => {
    if (!this.expanded || event.key !== 'Escape') return;
    this.expanded = false;
    this.expandedChange.emit(false);
    event.preventDefault();
    event.stopImmediatePropagation();
  };

  focusExpand(): void {
    this.expandToggle?.nativeElement.focus({ preventScroll: true });
  }

  protected toggleExpanded(): void {
    this.expanded = !this.expanded;
    this.expandedChange.emit(this.expanded);
  }

  toggleShortcuts(): void {
    this.shortcutsOpen.update((open) => !open);
  }

  closeShortcuts(): boolean {
    if (!this.shortcutsOpen()) return false;
    this.shortcutsOpen.set(false);
    return true;
  }

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (event.defaultPrevented) return;
    if (event.key === 'Tab' && this.expanded) {
      const controls = Array.from(
        this.host.nativeElement.querySelectorAll<HTMLElement>(
          'button:not([disabled]), [tabindex="0"]',
        ),
      );
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
    if (event.key !== 'Escape') return;
    if (this.expanded) {
      this.expandedChange.emit(false);
      event.preventDefault();
    } else if (
      !document.querySelector('.cdk-overlay-pane:not(:empty), [role="dialog"]:not(.map-panel)') &&
      this.closeShortcuts()
    ) {
      this.shortcutToggle?.nativeElement.focus();
      event.preventDefault();
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (
      event.target !== event.currentTarget ||
      event.defaultPrevented ||
      event.isComposing ||
      event.altKey ||
      event.ctrlKey ||
      event.metaKey ||
      !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key) ||
      this.zoom <= 1
    )
      return;
    const frame = event.currentTarget as HTMLElement;
    this.pan.emit({
      x: event.key === 'ArrowLeft' ? 40 : event.key === 'ArrowRight' ? -40 : 0,
      y: event.key === 'ArrowUp' ? 40 : event.key === 'ArrowDown' ? -40 : 0,
      width: frame.clientWidth,
      height: frame.clientHeight,
    });
    event.preventDefault();
    event.stopPropagation();
  }

  protected onPointerDown(event: PointerEvent): void {
    if (
      event.button !== 0 ||
      !['mouse', 'pen', 'touch'].includes(event.pointerType) ||
      (event.target as Element).closest('fa-unit-counter, button')
    )
      return;
    const frame = event.currentTarget as HTMLElement;
    this.dragStart = {
      x: event.clientX,
      y: event.clientY,
      pointerId: event.pointerId,
      moved: false,
    };
    frame.setPointerCapture(event.pointerId);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (!this.dragStart || this.dragStart.pointerId !== event.pointerId) return;
    const frame = event.currentTarget as HTMLElement;
    const x = event.clientX - this.dragStart.x;
    const y = event.clientY - this.dragStart.y;
    if (!this.dragStart.moved && Math.hypot(x, y) < 6) return;
    if (!this.dragStart.moved) {
      this.dragStart.moved = true;
      this.dragging.set(true);
    }
    this.pan.emit({ x, y, width: frame.clientWidth, height: frame.clientHeight });
    this.dragStart = {
      x: event.clientX,
      y: event.clientY,
      pointerId: event.pointerId,
      moved: true,
    };
    event.preventDefault();
  }

  protected onPointerEnd(event: PointerEvent): void {
    if (this.dragStart?.pointerId !== event.pointerId) return;
    this.dragStart = null;
    this.dragging.set(false);
  }
}
