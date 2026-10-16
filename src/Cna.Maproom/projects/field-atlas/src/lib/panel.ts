import { ChangeDetectionStrategy, Component, input } from '@angular/core';
/** Bordered workspace section. Use the actions slot for secondary actions. */
@Component({
  selector: 'fa-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section>
    <header>
      <div>
        @if (eyebrow()) {
          <p class="fa-eyebrow">{{ eyebrow() }}</p>
        }
        <h2>{{ heading() }}</h2>
      </div>
      <ng-content select="[panel-actions]" />
    </header>
    <div class="body"><ng-content /></div>
  </section>`,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    section {
      background: var(--color-panel);
      border: 1px solid var(--color-rule);
      height: 100%;
    }
    header {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-sm);
      padding: var(--space-md) var(--space-lg);
      border-bottom: 1px solid var(--color-rule);
    }
    h2,
    p {
      margin: 0;
    }
    h2 {
      font-size: var(--text-xl);
    }
    .body {
      padding: var(--space-lg);
    }
    @media (max-width: 400px) {
      header,
      .body {
        padding: var(--space-md);
      }
    }
  `,
})
export class AtlasPanel {
  readonly heading = input.required<string>();
  readonly eyebrow = input('');
}
