import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatSelect, MatSelectModule } from '@angular/material/select';

export interface AtlasChoiceOption {
  readonly value: string;
  readonly label: string;
  readonly disabled?: boolean;
}

let nextChoiceId = 0;

/** Token-backed choice control with a themed, keyboard-accessible overlay. */
@Component({
  selector: 'fa-choice-select',
  imports: [MatSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label
      class="fa-choice-label"
      [id]="labelId"
      [for]="selectId"
      role="button"
      [attr.tabindex]="disabled() ? -1 : 0"
      [attr.aria-disabled]="disabled()"
      (click)="openFromLabel(select)"
      (keydown.enter)="openFromLabel(select, $event)"
      (keydown.space)="openFromLabel(select, $event)"
    >
      {{ label() }}
    </label>
    <mat-select
      #select
      class="fa-choice-trigger"
      [id]="selectId"
      [aria-label]="label()"
      [aria-labelledby]="labelId"
      [value]="value()"
      [disabled]="disabled()"
      panelClass="fa-choice-panel"
      panelWidth="min(26rem, calc(100vw - 2rem))"
      (selectionChange)="valueChange.emit($event.value)"
    >
      @for (option of options(); track option.value) {
        <mat-option [value]="option.value" [disabled]="option.disabled ?? false">
          {{ option.label }}
        </mat-option>
      }
    </mat-select>
  `,
})
export class AtlasChoiceSelect {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly options = input.required<readonly AtlasChoiceOption[]>();
  readonly disabled = input(false);
  readonly valueChange = output<string>();
  protected readonly labelId = `fa-choice-label-${++nextChoiceId}`;
  protected readonly selectId = `fa-choice-select-${nextChoiceId}`;

  protected openFromLabel(select: MatSelect, event?: Event) {
    event?.preventDefault();
    if (this.disabled()) return;
    select.focus();
    select.open();
  }
}
