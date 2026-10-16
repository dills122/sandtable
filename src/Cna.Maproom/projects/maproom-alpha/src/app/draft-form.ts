import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FormlyFieldConfig, FormlyForm } from '@ngx-formly/core';
import { MatButtonModule } from '@angular/material/button';
import { AtlasStatus } from '@sandtable/field-atlas';
export const destinations = [
  { value: 'M-6', label: 'East crossing · M-6' },
  { value: 'J-5', label: 'Coast road · J-5' },
  { value: 'H-8', label: 'Inland ridge · H-8' },
] as const;
export type Destination = (typeof destinations)[number]['value'];
/** Narrow the dynamic form value before it enters typed application state. */
export function readDestination(value: unknown): Destination | null {
  return destinations.find((item) => item.value === value)?.value ?? null;
}
@Component({
  selector: 'mr-draft-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FormlyForm, MatButtonModule, AtlasStatus],
  template: `<form [formGroup]="form" (ngSubmit)="review()" class="fa-stack">
    <formly-form
      [form]="form"
      [fields]="fields()"
      [model]="model"
      (modelChange)="reviewed.set(null)"
    />
    <button matButton="filled" type="submit" [disabled]="disabled()">Review draft</button>
    <p class="fa-muted">Presentation example. No order is sent and no formation moves.</p>
    <div role="status">
      @if (reviewed(); as destination) {
        <fa-status tone="success">Draft reviewed</fa-status>
        <p>{{ formation() }} → {{ destination }}. No order issued.</p>
      }
    </div>
  </form>`,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    formly-form {
      display: block;
      min-width: 0;
    }
    p {
      font-size: var(--text-sm);
    }
  `,
})
export class DraftForm {
  readonly formation = input('Fox group');
  readonly disabled = input(false);
  readonly form = new FormGroup({ destination: new FormControl<string | null>(null) });
  readonly model = { destination: '' };
  readonly reviewed = signal<Destination | null>(null);
  readonly fields = computed<FormlyFieldConfig[]>(() => [
    {
      key: 'destination',
      type: 'select',
      props: {
        label: 'Move to',
        required: true,
        disabled: this.disabled(),
        options: [...destinations],
        appearance: 'outline',
      },
    },
  ]);
  constructor() {
    effect(() => {
      this.formation();
      this.reviewed.set(null);
    });
  }
  review() {
    this.form.markAllAsTouched();
    const destination = readDestination(this.form.get('destination')?.value);
    if (!this.disabled() && this.form.valid && destination) this.reviewed.set(destination);
  }
}
