import { computed } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { UnitKind } from '@sandtable/field-atlas';
export interface Formation {
  readonly id: string;
  readonly name: string;
  readonly kind: UnitKind;
  readonly position: string;
  readonly x: number;
  readonly y: number;
}
export const formations: readonly Formation[] = [
  { id: 'FOX', name: 'Fox group', kind: 'infantry', position: 'K-7', x: 54, y: 47 },
  { id: 'HAR', name: 'Harrier group', kind: 'armour', position: 'G-5', x: 25, y: 32 },
  { id: 'HER', name: 'Heron group', kind: 'armour', position: 'L-4', x: 70, y: 23 },
  { id: 'KES', name: 'Kestrel group', kind: 'infantry', position: 'E-8', x: 19, y: 72 },
  { id: 'MER', name: 'Merlin group', kind: 'infantry', position: 'H-7', x: 40, y: 64 },
  { id: 'OSP', name: 'Osprey group', kind: 'recon', position: 'M-8', x: 73, y: 78 },
  { id: 'SWI', name: 'Swift group', kind: 'recon', position: 'J-3', x: 44, y: 16 },
  { id: 'TER', name: 'Tern group', kind: 'infantry', position: 'O-6', x: 88, y: 49 },
];
/** Local presentation state only; never a simulation reducer. */
export const WorkspaceStore = signalStore(
  withState({
    selectedId: 'FOX',
    zoom: 1,
    focusX: 50,
    focusY: 50,
    rosterOpen: true,
    wideInspector: false,
  }),
  withComputed((store) => ({
    selected: computed(() => formations.find((f) => f.id === store.selectedId()) ?? formations[0]),
    cameraTransform: computed(
      () =>
        `translate(${50 - store.focusX() * store.zoom()}%, ${50 - store.focusY() * store.zoom()}%) scale(${store.zoom()})`,
    ),
  })),
  withMethods((store) => ({
    select(id: string) {
      if (formations.some((f) => f.id === id)) patchState(store, { selectedId: id });
    },
    locateSelected() {
      const formation = formations.find((f) => f.id === store.selectedId());
      if (formation)
        patchState(store, {
          zoom: Math.max(store.zoom(), 1.5),
          focusX: formation.x,
          focusY: formation.y,
        });
    },
    fitMap() {
      patchState(store, { zoom: 1, focusX: 50, focusY: 50 });
    },
    zoomBy(step: number) {
      patchState(store, {
        zoom: Math.min(2, Math.max(1, Math.round((store.zoom() + step) * 4) / 4)),
      });
    },
    toggleRoster() {
      patchState(store, { rosterOpen: !store.rosterOpen() });
    },
    setRosterOpen(open: boolean) {
      patchState(store, { rosterOpen: open });
    },
    toggleInspectorWidth() {
      patchState(store, { wideInspector: !store.wideInspector() });
    },
  })),
);
