import { TestBed } from '@angular/core/testing';
import { WorkspaceStore } from './workspace.store';
describe('Workspace presentation state', () => {
  it('selects only known synthetic formations', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.select('HAR');
    expect(store.selected().name).toBe('Harrier group');
    store.select('hidden-unit');
    expect(store.selectedId()).toBe('HAR');
  });
});
