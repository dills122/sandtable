import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Icon-only controls are for familiar, reversible actions with an unambiguous short name. */
@Component({
  selector: 'fa-icon-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<button
    type="button"
    [attr.aria-label]="label()"
    [attr.title]="label()"
    [attr.aria-pressed]="pressed()"
    [disabled]="disabled()"
    (click)="activated.emit()"
  >
    <span aria-hidden="true"><ng-content /></span>
  </button>`,
  styles: `
    :host {
      display: inline-flex;
    }
    button {
      display: inline-grid;
      place-items: center;
      min-width: 2.75rem;
      min-height: 2.75rem;
      padding: 0;
      border: 1px solid var(--color-rule);
      border-radius: var(--radius-control);
      background: var(--color-panel);
      color: var(--color-ink);
      cursor: pointer;
    }
    button:hover:enabled {
      background: var(--color-selection);
    }
    button:focus-visible {
      outline: 3px solid var(--color-focus);
      outline-offset: 2px;
    }
    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    button[aria-pressed='true'] {
      border-color: var(--color-accent);
      background: var(--color-selection);
    }
    span {
      display: inline-flex;
      width: 1.25rem;
      height: 1.25rem;
      align-items: center;
      justify-content: center;
    }
  `,
})
export class AtlasIconButton {
  readonly label = input.required<string>();
  readonly disabled = input(false);
  readonly pressed = input<boolean | null>(null);
  readonly activated = output<void>();
}
