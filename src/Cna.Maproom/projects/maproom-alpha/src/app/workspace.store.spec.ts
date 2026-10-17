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
    expect(store.zoom()).toBe(1.5);
    expect(store.focusX()).toBeCloseTo(100 - 50 / 1.5);
    expect(store.focusY()).toBeCloseTo(100 - 50 / 1.5);
    for (let index = 0; index < 10; index++) store.zoomBy(0.25);
    expect(store.zoom()).toBe(2);
    store.zoomBy(-0.75);
    expect([store.zoom(), store.focusX(), store.focusY()]).toEqual([1.25, 60, 60]);
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
      75,
      49,
      'FOX',
    ]);
  });
  it('holds a submitted identity across selection changes and moves only after acceptance', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.stageDraft('FOX', 'M-6');
    store.review('FOX');
    const id = store.issue('FOX', 'accepted');
    expect(id).not.toBeNull();
    store.select('HAR');
    expect(store.visibleFormations().find((f) => f.id === 'FOX')?.position).toBe('K-7');
    expect(store.issue('HAR', 'accepted')).toBeNull();
    store.settle(id!);
    expect(store.visibleFormations().find((f) => f.id === 'FOX')?.position).toBe('M-6');
    expect(store.selected().position).toBe('G-5');
    expect(store.orderFor('FOX').status).toBe('accepted');
    store.settle(id!);
    expect(store.visibleFormations().find((f) => f.id === 'FOX')?.position).toBe('M-6');
  });
  it('keeps separate formation drafts and invalidates review after an edit', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.stageDraft('FOX', 'M-6');
    store.review('FOX');
    store.select('HAR');
    store.stageDraft('HAR', 'H-8');
    expect(store.orderFor('FOX').reviewedDestination).toBe('M-6');
    expect(store.orderFor('HAR').destination).toBe('H-8');
    store.stageDraft('FOX', 'J-5');
    expect(store.orderFor('FOX').reviewedDestination).toBeNull();
    expect(store.issue('FOX', 'accepted')).toBeNull();
  });
  it('keeps rejected and stale positions and requires fresh review', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    for (const outcome of ['rejected', 'stale'] as const) {
      store.stageDraft('FOX', 'J-5');
      store.review('FOX');
      const id = store.issue('FOX', outcome)!;
      store.settle(id);
      expect(store.orderFor('FOX').status).toBe(outcome);
      expect(store.selected().position).toBe('K-7');
      expect(store.issue('FOX', 'accepted')).toBeNull();
      store.refresh('FOX');
      expect(store.orderFor('FOX').status).toBe('draft');
      expect(store.issue('FOX', 'accepted')).toBeNull();
    }
  });
  it('reconciles a lost reply once without a second issue and stops unsupported phase', () => {
    TestBed.configureTestingModule({ providers: [WorkspaceStore] });
    const store = TestBed.inject(WorkspaceStore);
    store.stageDraft('FOX', 'H-8');
    store.review('FOX');
    const id = store.issue('FOX', 'lost-reply')!;
    store.settle(id);
    expect(store.orderFor('FOX').status).toBe('uncertain');
    expect(store.selected().position).toBe('K-7');
    expect(store.issue('FOX', 'accepted')).toBeNull();
    store.reconcile();
    expect(store.orderFor('FOX').status).toBe('accepted');
    expect(store.selected().position).toBe('H-8');
    store.reconcile();
    expect(store.selected().position).toBe('H-8');
    store.setUnsupportedPhase(true);
    store.stageDraft('HAR', 'M-6');
    store.review('HAR');
    expect(store.orderFor('HAR').status).toBe('draft');
    expect(store.issue('HAR', 'accepted')).toBeNull();
  });
});
