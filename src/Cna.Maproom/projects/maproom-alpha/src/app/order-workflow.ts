import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FormlyFieldConfig, FormlyForm } from '@ngx-formly/core';
import { MatButtonModule } from '@angular/material/button';
import { AtlasStatus } from '@sandtable/field-atlas';
import { destinations, readDestination } from './draft-form';
import { FixtureOutcome, formations, WorkspaceStore } from './workspace.store';

/** In-memory interaction specimen. No request leaves this browser. */
@Component({
  selector: 'mr-order-workflow',
  imports: [ReactiveFormsModule, FormlyForm, MatButtonModule, AtlasStatus],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="fa-stack" aria-label="Order workflow fixture">
      @if (store.unsupportedPhase()) {
        <fa-status tone="warning">Unsupported phase</fa-status>
        <p>Combat entry stops in this rules-lab fixture. No campaign turn is resolved.</p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="review()" class="fa-stack">
          <formly-form
            [form]="form"
            [fields]="fields()"
            [model]="model"
            (modelChange)="draftChanged()"
          />
          <button matButton="filled" type="submit" [disabled]="locked()">Review draft</button>
        </form>
        @if (order().status === 'reviewed') {
          <div class="order-review" role="group" aria-label="Reviewed order">
            <fa-status tone="success">Ready to issue</fa-status>
            <p>{{ formationName() }}: move to {{ order().reviewedDestination }}.</p>
            <label for="fixture-outcome">Fixture result</label>
            <select id="fixture-outcome" class="fa-select" #outcome>
              <option value="accepted">Accepted</option>
              <option value="rejected">Rejected</option>
              <option value="stale">Stale view</option>
              <option value="lost-reply">Lost reply</option>
            </select>
            <button
              matButton="filled"
              type="button"
              [disabled]="!!store.pending()"
              (click)="issue(outcome.value)"
            >
              Issue order
            </button>
          </div>
        }
        @if (otherPending(); as pending) {
          <div role="status" aria-live="polite">
            <p>
              {{ pending.name }} has an unresolved order. You can prepare this draft, but issue
              another order only after its result is known.
            </p>
            @if (pending.uncertain) {
              <p>Reply lost. Reconcile the submitted order before issuing another.</p>
              <button matButton type="button" (click)="store.reconcile()">
                Simulate reconnect and reconcile
              </button>
            }
          </div>
        }
        <div role="status" aria-live="polite">
          @switch (order().status) {
            @case ('pending') {
              <p>Sending fixture order. Last confirmed position stays on map.</p>
            }
            @case ('uncertain') {
              <p>Reply lost. Outcome unknown until receipt readback. Do not issue again.</p>
              <button matButton type="button" (click)="store.reconcile()">
                Simulate reconnect and reconcile
              </button>
            }
            @case ('accepted') {
              <p>Accepted fixture result. Confirmed position updated.</p>
            }
            @case ('rejected') {
              <p>Rejected fixture result. Position unchanged.</p>
            }
            @case ('stale') {
              <p>Stale view. Position unchanged.</p>
            }
          }
        </div>
        @if (order().status === 'rejected' || order().status === 'stale') {
          <button matButton type="button" (click)="store.refresh(formationId())">
            Refresh orders
          </button>
        }
      }
      <p class="fa-muted">
        Local staged results only. No campaign server, legal-action check or persistence.
      </p>
    </section>
  `,
  styles: `
    :host,
    formly-form {
      display: block;
      min-width: 0;
    }
    .order-review {
      display: grid;
      gap: var(--space-sm);
      min-width: 0;
    }
    .fa-select {
      width: 100%;
    }
    button {
      min-height: 44px;
    }
  `,
})
export class OrderWorkflow {
  readonly formationId = input.required<string>();
  readonly formationName = input.required<string>();
  protected readonly store = inject(WorkspaceStore);
  protected readonly form = new FormGroup({ destination: new FormControl<string | null>(null) });
  protected model = { destination: '' };
  private readonly fieldLocked = computed(() => this.locked());
  protected readonly fields = computed<FormlyFieldConfig[]>(() => [
    {
      key: 'destination',
      type: 'select',
      props: {
        label: 'Move to',
        required: true,
        disabled: this.fieldLocked(),
        options: [...destinations],
        appearance: 'outline',
      },
    },
  ]);

  constructor() {
    effect(() => {
      const id = this.formationId();
      const destination = untracked(() => this.store.orderFor(id).destination) ?? '';
      this.model = { destination };
      this.form.controls.destination.setValue(destination, { emitEvent: false });
    });
  }
  protected order() {
    return this.store.orderFor(this.formationId());
  }
  protected locked() {
    return this.store.unsupportedPhase() || ['pending', 'uncertain'].includes(this.order().status);
  }
  protected otherPending() {
    const pending = this.store.pending();
    if (!pending || pending.formationId === this.formationId()) return null;
    return {
      name:
        formations.find((formation) => formation.id === pending.formationId)?.name ??
        'Another formation',
      uncertain: this.store.orderFor(pending.formationId).status === 'uncertain',
    };
  }
  protected draftChanged() {
    this.store.stageDraft(
      this.formationId(),
      readDestination(this.form.controls.destination.value),
    );
  }
  protected review() {
    this.form.markAllAsTouched();
    const destination = readDestination(this.form.controls.destination.value);
    if (!this.form.valid || !destination) return;
    this.store.stageDraft(this.formationId(), destination);
    this.store.review(this.formationId());
  }
  protected issue(value: string) {
    if (!['accepted', 'rejected', 'stale', 'lost-reply'].includes(value)) return;
    const requestId = this.store.issue(this.formationId(), value as FixtureOutcome);
    if (requestId !== null) window.setTimeout(() => this.store.settle(requestId), 400);
  }
}
