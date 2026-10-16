import { LucideMapPin } from '@lucide/angular';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import type { Meta, StoryObj } from '@storybook/angular-vite';
@Component({
  selector: 'fa-material-example',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideMapPin,
  ],
  template: `<main class="fa-stack controls">
    <h1>Controls that belong on the atlas.</h1>
    <p>
      Angular Material, themed through public APIs. Use Tab to inspect focus and open the select
      with the keyboard.
    </p>
    <div class="flex flex-wrap gap-4">
      <button matButton="filled">Review order</button
      ><button matButton="outlined">Cancel draft</button
      ><button matButton="text">
        <svg lucideMapPin aria-hidden="true" width="18" height="18"></svg>Locate</button
      ><button matButton="filled" disabled>Unavailable</button>
    </div>
    <mat-form-field appearance="outline"
      ><mat-label>Formation name</mat-label><input matInput [formControl]="name" /><mat-hint
        >Typed Reactive Forms example</mat-hint
      ></mat-form-field
    ><mat-form-field appearance="outline"
      ><mat-label>Record scope</mat-label
      ><mat-select [formControl]="scope"
        ><mat-option value="selected">Selected formation</mat-option
        ><mat-option value="all">All visible formations</mat-option></mat-select
      ></mat-form-field
    ><mat-form-field appearance="outline"
      ><mat-label>Unavailable field</mat-label
      ><input matInput [formControl]="locked" /></mat-form-field
    ><mat-form-field appearance="outline"
      ><mat-label>Required reference</mat-label
      ><input matInput [formControl]="required" /><mat-error
        >Enter a reference.</mat-error
      ></mat-form-field
    >
  </main>`,
  styles: `
    .controls {
      max-width: 42rem;
      padding: var(--space-md);
    }
    h1 {
      margin-bottom: 0;
    }
  `,
})
class MaterialExample {
  readonly name = new FormControl('Fox group', { nonNullable: true });
  readonly scope = new FormControl('selected', { nonNullable: true });
  readonly locked = new FormControl({ value: 'Server unavailable', disabled: true });
  readonly required = new FormControl('', Validators.required);
  constructor() {
    this.required.markAsTouched();
  }
}
export default {
  title: 'Material/Controls',
  component: MaterialExample,
} satisfies Meta<MaterialExample>;
export const Overview: StoryObj<MaterialExample> = {};
