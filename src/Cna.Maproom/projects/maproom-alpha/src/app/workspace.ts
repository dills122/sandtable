import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
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
}
