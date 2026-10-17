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
export type FixtureOutcome = 'accepted' | 'rejected' | 'stale' | 'lost-reply';
export type OrderStatus =
  'draft' | 'reviewed' | 'pending' | 'accepted' | 'rejected' | 'stale' | 'uncertain';
export interface FormationOrder {
  readonly destination: string | null;
  readonly reviewedDestination: string | null;
  readonly status: OrderStatus;
  readonly requestId: number | null;
}
const emptyOrder: FormationOrder = {
  destination: null,
  reviewedDestination: null,
  status: 'draft',
  requestId: null,
};
const destinations = {
  'M-6': { x: 75, y: 59 },
  'J-5': { x: 49, y: 36 },
  'H-8': { x: 40, y: 73 },
} as const;
type Destination = keyof typeof destinations;
interface PendingFixture {
  readonly requestId: number;
  readonly formationId: string;
  readonly destination: Destination;
  readonly outcome: FixtureOutcome;
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
    orders: {} as Record<string, FormationOrder>,
    confirmed: {} as Record<string, Destination>,
    pending: null as PendingFixture | null,
    nextRequestId: 1,
    unsupportedPhase: false,
  }),
  withComputed((store) => ({
    visibleFormations: computed(() =>
      formations.map((f) => {
        const position = store.confirmed()[f.id];
        return position ? { ...f, position, ...destinations[position] } : f;
      }),
    ),
    selected: computed(
      () =>
        formations
          .map((f) => {
            const position = store.confirmed()[f.id];
            return position ? { ...f, position, ...destinations[position] } : f;
          })
          .find((f) => f.id === store.selectedId()) ?? formations[0],
    ),
    cameraTransform: computed(
      () =>
        `translate(${50 - store.focusX() * store.zoom()}%, ${50 - store.focusY() * store.zoom()}%) scale(${store.zoom()})`,
    ),
  })),
  withMethods((store) => ({
    orderFor(id: string): FormationOrder {
      return store.orders()[id] ?? emptyOrder;
    },
    stageDraft(id: string, destination: string | null) {
      if (!formations.some((f) => f.id === id) || store.unsupportedPhase()) return;
      const current = store.orders()[id] ?? emptyOrder;
      if (current.status === 'pending' || current.status === 'uncertain') return;
      patchState(store, {
        orders: {
          ...store.orders(),
          [id]: { destination, reviewedDestination: null, status: 'draft', requestId: null },
        },
      });
    },
    review(id: string) {
      const current = store.orders()[id] ?? emptyOrder;
      if (
        store.unsupportedPhase() ||
        current.status === 'pending' ||
        current.status === 'uncertain' ||
        !(current.destination && current.destination in destinations)
      )
        return;
      patchState(store, {
        orders: {
          ...store.orders(),
          [id]: { ...current, reviewedDestination: current.destination, status: 'reviewed' },
        },
      });
    },
    issue(id: string, outcome: FixtureOutcome): number | null {
      const current = store.orders()[id] ?? emptyOrder;
      if (
        store.unsupportedPhase() ||
        store.pending() ||
        current.status !== 'reviewed' ||
        current.reviewedDestination !== current.destination ||
        !(current.destination && current.destination in destinations)
      )
        return null;
      const requestId = store.nextRequestId();
      patchState(store, {
        nextRequestId: requestId + 1,
        pending: {
          requestId,
          formationId: id,
          destination: current.destination as Destination,
          outcome,
        },
        orders: { ...store.orders(), [id]: { ...current, status: 'pending', requestId } },
      });
      return requestId;
    },
    settle(requestId: number) {
      const pending = store.pending();
      if (!pending || pending.requestId !== requestId) return;
      const current = store.orders()[pending.formationId];
      if (!current || current.requestId !== requestId) return;
      if (pending.outcome === 'lost-reply') {
        patchState(store, {
          orders: { ...store.orders(), [pending.formationId]: { ...current, status: 'uncertain' } },
        });
        return;
      }
      patchState(store, {
        pending: null,
        confirmed:
          pending.outcome === 'accepted'
            ? { ...store.confirmed(), [pending.formationId]: pending.destination }
            : store.confirmed(),
        orders: {
          ...store.orders(),
          [pending.formationId]: {
            ...current,
            reviewedDestination: null,
            status: pending.outcome,
            requestId: null,
          },
        },
      });
    },
    reconcile() {
      const pending = store.pending();
      if (
        !pending ||
        pending.outcome !== 'lost-reply' ||
        store.orders()[pending.formationId]?.status !== 'uncertain'
      )
        return;
      const current = store.orders()[pending.formationId];
      patchState(store, {
        pending: null,
        confirmed: { ...store.confirmed(), [pending.formationId]: pending.destination },
        orders: {
          ...store.orders(),
          [pending.formationId]: {
            ...current,
            status: 'accepted',
            reviewedDestination: null,
            requestId: null,
          },
        },
      });
    },
    refresh(id: string) {
      const current = store.orders()[id];
      if (!current || !['stale', 'rejected'].includes(current.status)) return;
      patchState(store, {
        orders: {
          ...store.orders(),
          [id]: { ...current, status: 'draft', reviewedDestination: null },
        },
      });
    },
    setUnsupportedPhase(unsupported: boolean) {
      patchState(store, { unsupportedPhase: unsupported });
    },
    select(id: string) {
      if (formations.some((f) => f.id === id)) patchState(store, { selectedId: id });
    },
    locateSelected() {
      const formation = store.visibleFormations().find((f) => f.id === store.selectedId());
      if (formation)
        patchState(store, {
          zoom: Math.max(store.zoom(), 1.5),
          focusX: formation.x,
          focusY: formation.y,
        });
    },
    focusFormation(id: string) {
      if (store.zoom() <= 1) return;
      const formation = store.visibleFormations().find((f) => f.id === id);
      if (formation) patchState(store, { focusX: formation.x, focusY: formation.y });
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
