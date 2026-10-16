import { ChangeDetectionStrategy, Component, input } from '@angular/core';
export type AtlasStatusTone = 'neutral' | 'success' | 'warning' | 'danger';
/** Always pair semantic color with visible text. Announcements belong to the caller. */
@Component({
  selector: 'fa-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[attr.data-tone]': 'tone()' },
  template: `<span aria-hidden="true">{{ symbol() }}</span
    ><ng-content />`,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--space-xs);
      color: var(--color-muted);
      font-size: var(--text-sm);
      font-weight: 500;
    }
    :host([data-tone='success']) {
      color: var(--color-success);
    }
    :host([data-tone='warning']) {
      color: var(--color-warning);
    }
    :host([data-tone='danger']) {
      color: var(--color-error);
    }
  `,
})
export class AtlasStatus {
  readonly tone = input<AtlasStatusTone>('neutral');
  symbol() {
    return { neutral: '○', success: '✓', warning: '△', danger: '!' }[this.tone()];
  }
}
