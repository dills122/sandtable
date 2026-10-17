import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { AtlasPanel, AtlasStatus, AtlasUnitCounter } from '@sandtable/field-atlas';
import { DraftForm } from './draft-form';
import { provideMaproomForms } from './forms';
import { formations, WorkspaceStore } from './workspace.store';
@Component({
  selector: 'mr-workspace',
  imports: [AtlasPanel, AtlasStatus, AtlasUnitCounter, DraftForm],
  providers: [WorkspaceStore, provideMaproomForms()],
  templateUrl: './workspace.html',
  styleUrl: './workspace.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Workspace {
  protected readonly store = inject(WorkspaceStore);
  protected readonly formations = formations;

  constructor() {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
    const intermediate = window.matchMedia('(min-width: 50.01rem) and (max-width: 75rem)');
    const applyDefault = () => this.store.setRosterOpen(!intermediate.matches);
    applyDefault();
    intermediate.addEventListener('change', applyDefault);
    inject(DestroyRef).onDestroy(() => intermediate.removeEventListener('change', applyDefault));
  }
}
