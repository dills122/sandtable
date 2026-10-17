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
  it('locates the selected formation, clamps zoom and restores the full map', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.select('OSP');
    store.locateSelected();
    expect([store.zoom(), store.focusX(), store.focusY()]).toEqual([1.5, 73, 78]);
    for (let index = 0; index < 10; index++) store.zoomBy(0.25);
    expect(store.zoom()).toBe(2);
    store.fitMap();
    expect([store.zoom(), store.focusX(), store.focusY()]).toEqual([1, 50, 50]);
    store.zoomBy(-0.25);
    expect(store.zoom()).toBe(1);
  });
  it('pans a zoomed map to the keyboard-focused formation without changing selection', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.zoomBy(1);
    store.focusFormation('TER');
    expect([store.zoom(), store.focusX(), store.focusY(), store.selectedId()]).toEqual([
      2,
      88,
      49,
      'FOX',
    ]);
  });
});
