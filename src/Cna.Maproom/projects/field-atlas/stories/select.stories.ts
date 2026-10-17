import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { AtlasChoiceSelect } from '../src/public-api';
import type { Meta, StoryObj } from '@storybook/angular-vite';

@Component({
  selector: 'fa-select-example',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AtlasChoiceSelect],
  template: `<main class="select-examples fa-stack">
    <h1>Choice select</h1>
    <p>
      Field Atlas controls both trigger and open menu. Tab to focus; Enter or Space opens; arrow
      keys move; Enter selects; Escape closes. The same menu works with touch and small viewports.
    </p>
    <div class="field">
      <fa-choice-select
        label="Show entries for"
        [options]="scopeOptions"
        [value]="scope()"
        (valueChange)="scope.set($event)"
      />
    </div>
    <div class="field narrow">
      <fa-choice-select
        label="Narrow container"
        [options]="narrowOptions"
        [value]="narrow()"
        (valueChange)="narrow.set($event)"
      />
    </div>
    <div class="field">
      <fa-choice-select
        label="Unavailable scope"
        [options]="disabledOptions"
        value="unavailable"
        [disabled]="true"
      />
    </div>
  </main>`,
  styles: `
    .select-examples {
      grid-template-columns: minmax(0, 1fr);
      max-width: 42rem;
      padding: var(--space-md);
    }
    .field {
      min-width: 0;
      width: 100%;
    }
    .narrow {
      width: 16rem;
      max-width: 100%;
    }
  `,
})
class SelectExample {
  readonly scope = signal('selected');
  readonly narrow = signal('all');
  readonly scopeOptions = [
    { value: 'selected', label: 'Selected formation' },
    { value: 'all', label: 'All visible formations' },
  ];
  readonly narrowOptions = [
    { value: 'all', label: 'All formations in the current operations area' },
    { value: 'selected', label: 'Selected formation' },
  ];
  readonly disabledOptions = [{ value: 'unavailable', label: 'Pending campaign activation' }];
}

export default {
  title: 'Foundations/Choice select',
  component: SelectExample,
} satisfies Meta<SelectExample>;

export const States: StoryObj<SelectExample> = {};
