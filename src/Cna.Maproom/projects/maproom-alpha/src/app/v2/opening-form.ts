import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { AtlasPanel, AtlasStatus } from '@sandtable/field-atlas';
import { Fact, describeOpening } from './opening-model';
import { ActionOutcome, OpeningView } from './types';

export interface OpeningChoice {
  readonly opening: OpeningView;
  readonly publicActionRef: string;
}

let nextId = 0;

/**
 * Generic, accessible form for any owner opening. It reads only `visibleCut` and
 * `publicActionRefs` (via `describeOpening`), so a new opening kind needs no new screen.
 * It emits intent; the caller submits. The selection is keyed to the opening it was made on,
 * so a refreshed opening (new revision or refs) never carries a stale pick forward.
 */
@Component({
  selector: 'mr-opening-form',
  imports: [AtlasPanel, AtlasStatus, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './opening-form.html',
  styleUrl: './opening-form.scss',
})
export class OpeningForm {
  readonly opening = input.required<OpeningView>();
  readonly heading = input.required<string>();
  readonly eyebrow = input('');
  /** True while a submission is in flight; locks the choices and the submit control. */
  readonly busy = input(false);
  readonly outcome = input<ActionOutcome | null>(null);
  readonly error = input('');
  readonly chosen = output<OpeningChoice>();

  protected readonly uid = `mr-opening-${nextId++}`;
  protected readonly description = computed(() => describeOpening(this.opening()));
  private readonly pick = signal<{ key: string; ref: string } | null>(null);
  private readonly key = computed(() => {
    const o = this.opening();
    return [o.campaignId, o.audience, o.stage, o.revision, o.publicActionRefs.join('|')].join('::');
  });
  protected readonly selected = computed(() => {
    const pick = this.pick();
    return pick && pick.key === this.key() ? pick.ref : null;
  });
  protected readonly canSubmit = computed(() => this.selected() !== null && !this.busy());
  protected readonly accepted = computed(() => this.outcome()?.kind === 'accepted');
  protected readonly rejectedText = computed(() => {
    const o = this.outcome();
    if (o?.kind !== 'rejected') return '';
    return o.code === 'rule-not-implemented'
      ? 'This choice is legal under the source rules but is not built yet. Pick another.'
      : 'This opening is no longer current. Refresh to see the current choices.';
  });

  protected select(ref: string): void {
    this.pick.set({ key: this.key(), ref });
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const ref = this.selected();
    if (ref === null || this.busy()) return;
    this.chosen.emit({ opening: this.opening(), publicActionRef: ref });
  }

  protected hasFacts(facts: readonly Fact[]): boolean {
    return facts.length > 0;
  }
}
