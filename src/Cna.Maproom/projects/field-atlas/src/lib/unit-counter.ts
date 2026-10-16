import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
export type UnitKind = 'infantry' | 'armour' | 'recon';
/** Presentation only. Accessible label must identify the formation and position. */
@Component({
  selector: 'fa-unit-counter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<button
    type="button"
    [attr.aria-label]="label()"
    [attr.aria-pressed]="selected()"
    [disabled]="disabled()"
    (click)="chosen.emit()"
  >
    <span aria-hidden="true" class="symbol">{{ symbols[kind()] }}</span
    ><span aria-hidden="true">{{ code() }}</span>
  </button>`,
  styles: `
    :host {
      display: inline-block;
    }
    button {
      display: grid;
      place-content: center;
      width: 3.5rem;
      min-height: 3.5rem;
      padding: var(--space-2xs);
      background: var(--color-own);
      color: var(--color-own-ink);
      border: 1px solid var(--color-accent);
      border-radius: var(--radius-control);
      font-size: var(--text-sm);
      line-height: 1.25;
      transition: background-color var(--duration-short) var(--ease-out);
    }
    button:hover:enabled {
      background: var(--color-selection);
    }
    button[aria-pressed='true'] {
      outline: 2px solid var(--color-accent);
      outline-offset: 3px;
    }
    button:focus-visible {
      outline: 3px solid var(--color-focus);
      outline-offset: 5px;
    }
    button:disabled {
      opacity: 0.5;
    }
    .symbol {
      font-size: var(--text-lg);
    }
  `,
})
export class AtlasUnitCounter {
  readonly code = input.required<string>();
  readonly label = input.required<string>();
  readonly kind = input<UnitKind>('infantry');
  readonly selected = input(false);
  readonly disabled = input(false);
  readonly chosen = output<void>();
  protected readonly symbols = { infantry: '×', armour: '○', recon: '/' };
}
