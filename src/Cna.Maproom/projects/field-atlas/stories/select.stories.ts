import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

@Component({
  selector: 'fa-select-example',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<main class="select-examples fa-stack">
    <h1>Native select</h1>
    <p>
      Use <code>fa-select</code> for a small native choice control outside an Angular Material form.
      The indicator sits one rem inside the edge, with room reserved for long labels. Tab to inspect
      focus, then use arrow keys to choose an option.
    </p>
    <div class="field">
      <label for="scope">Show entries for</label>
      <select class="fa-select" id="scope">
        <option value="selected">Selected formation</option>
        <option value="all">All visible formations</option>
      </select>
    </div>
    <div class="field narrow">
      <label for="narrow-scope">Narrow container</label>
      <select class="fa-select" id="narrow-scope">
        <option>All formations in the current operations area</option>
        <option>Selected formation</option>
      </select>
    </div>
    <div class="field">
      <label for="disabled-scope">Unavailable scope</label>
      <select class="fa-select" id="disabled-scope" disabled>
        <option>Pending campaign activation</option>
      </select>
    </div>
  </main>`,
  styles: `
    .select-examples {
      grid-template-columns: minmax(0, 1fr);
      max-width: 42rem;
      padding: var(--space-md);
    }
    .field {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: var(--space-xs);
      min-width: 0;
      width: 100%;
    }
    .narrow {
      width: 16rem;
      max-width: 100%;
    }
    .narrow .fa-select {
      width: 100%;
    }
    code {
      font-size: 0.9em;
    }
  `,
})
class SelectExample {}

export default {
  title: 'Foundations/Native select',
  component: SelectExample,
} satisfies Meta<SelectExample>;

export const States: StoryObj<SelectExample> = {};
