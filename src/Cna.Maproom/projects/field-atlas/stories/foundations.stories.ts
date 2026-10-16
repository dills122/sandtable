import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';
@Component({
  selector: 'fa-foundations-example',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<main class="overview">
    <p class="fa-eyebrow">Field Atlas / Foundations</p>
    <h1>Paper, ink, and a clear situation.</h1>
    <p class="lead">
      A working atlas for deliberate decisions. Information first; decoration in service of
      orientation.
    </p>
    <hr class="fa-rule" />
    <h2>Semantic color</h2>
    <div class="swatches">
      @for (token of tokens; track token) {
        <div class="swatch">
          <span [style.background]="'var(--color-' + token + ')'"></span><code>{{ token }}</code>
        </div>
      }
    </div>
    <h2>Type with a job to do</h2>
    <h3>The coastal exercise — Newsreader</h3>
    <p>Fox group · Infantry · K-7 — IBM Plex Sans</p>
    <p class="fa-muted">Supporting text stays at 14px or larger. Body and control text use 16px.</p>
    <h2>Space to read and act</h2>
    <div class="scale">
      @for (size of spaces; track size) {
        <div>
          <span [style.width.px]="size"></span><code>{{ size }}px</code>
        </div>
      }
    </div>
    <h2>Interaction rules</h2>
    <p>
      44px actions. Visible keyboard focus. Selected states use a boundary and text, alongside
      color. Motion is brief and respects reduced-motion preferences.
    </p>
  </main>`,
  styles: `
    .overview {
      max-width: 68rem;
      padding: clamp(1rem, 4vw, 3rem);
    }
    h1 {
      max-width: 18ch;
      font-size: clamp(2rem, 5vw, 3.5rem);
    }
    .lead {
      max-width: 58ch;
    }
    h2 {
      margin-top: var(--space-xl);
    }
    .swatches {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(8rem, 1fr));
      gap: var(--space-md);
    }
    .swatch {
      border: 1px solid var(--color-rule);
    }
    .swatch > span {
      display: block;
      height: 5rem;
    }
    code {
      display: block;
      padding: var(--space-xs);
      font-size: var(--text-sm);
      overflow-wrap: anywhere;
    }
    .scale {
      display: flex;
      gap: var(--space-lg);
      flex-wrap: wrap;
    }
    .scale span {
      display: block;
      height: 24px;
      background: var(--color-accent);
    }
  `,
})
class FoundationsExample {
  readonly tokens = [
    'paper',
    'panel',
    'map',
    'ink',
    'muted',
    'accent',
    'selection',
    'own',
    'contact',
    'focus',
    'error',
    'warning',
  ];
  readonly spaces = [4, 8, 12, 16, 24, 32, 48];
}
const meta: Meta<FoundationsExample> = {
  title: 'Foundations/Overview',
  component: FoundationsExample,
  parameters: { layout: 'fullscreen' },
};
export default meta;
export const Overview: StoryObj<FoundationsExample> = {};
