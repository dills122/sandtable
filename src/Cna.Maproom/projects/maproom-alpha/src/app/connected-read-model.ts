import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import { OwnFormation } from './api/generated/model/ownFormation';

export type ConnectedKind = 'infantry' | 'armour' | 'recon' | 'truck' | 'unknown';

/** The public kind is a label, not a Field Atlas UnitKind. Trucks need their own symbol. */
export function connectedKind(label: string): ConnectedKind {
  switch (label.toLocaleLowerCase()) {
    case 'infantry':
      return 'infantry';
    case 'armour':
      return 'armour';
    case 'recon':
      return 'recon';
    case 'truck convoy':
      return 'truck';
    default:
      return 'unknown';
  }
}

export function connectedSymbol(kind: ConnectedKind): string {
  return { infantry: '×', armour: '○', recon: '/', truck: 'T', unknown: '?' }[kind];
}

export function selectedFormationId(
  view: CoherentPlayerView,
  previousId: string | null,
  initial: boolean,
): string | null {
  if (initial) return view.ownFormations[0]?.formationId ?? null;
  return view.ownFormations.some((formation) => formation.formationId === previousId)
    ? previousId
    : null;
}

export function operationalLocations(view: CoherentPlayerView) {
  if (!view.locations.length) return [];
  const xs = view.locations.map((location) => location.x);
  const ys = view.locations.map((location) => location.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);
  const position = (value: number, min: number, max: number) =>
    min === max ? 50 : 16 + ((value - min) / (max - min)) * 68;

  return view.locations.map((location) => ({
    ...location,
    left: position(location.x, minX, maxX),
    top: position(location.y, minY, maxY),
    formations: view.ownFormations.filter(
      (formation) => formation.locationId === location.locationId,
    ),
    contacts: view.apparentContacts.filter((contact) => contact.locationId === location.locationId),
  }));
}

export function formationActions(view: CoherentPlayerView, formation: OwnFormation | undefined) {
  return formation
    ? view.legalActions.filter((action) => action.formationId === formation.formationId)
    : [];
}
