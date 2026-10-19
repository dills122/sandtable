import { CoherentPlayerView } from './api/generated/model/coherentPlayerView';
import {
  connectedKind,
  connectedSymbol,
  formationActions,
  operationalLocations,
  selectedFormationId,
} from './connected-read-model';

const view: CoherentPlayerView = {
  contractVersion: 1,
  campaignId: 'campaign',
  sessionGeneration: 'generation',
  seat: 'axis',
  audienceRevision: '1',
  rulesetHash: 'hash',
  position: {
    positionId: 'position',
    turn: '1',
    phaseId: 'movement',
    stageId: 'initial',
    decision: 'human',
  },
  locations: [
    { locationId: 'west', label: 'West', x: -1, y: 0 },
    { locationId: 'east', label: 'East', x: 1, y: 0 },
  ],
  ownFormations: [
    {
      formationId: 'outward-infantry',
      label: 'Axis infantry',
      kindLabel: 'Infantry',
      locationId: 'west',
      orderStatusLabel: 'No reserve designation',
    },
    {
      formationId: 'outward-truck',
      label: 'Axis truck',
      kindLabel: 'Truck convoy',
      locationId: 'east',
      orderStatusLabel: 'Reserve I',
    },
  ],
  apparentContacts: [{ contactId: 'contact', label: 'Unidentified presence', locationId: 'east' }],
  legalActions: [
    {
      actionId: 'move',
      actionContractVersion: 1,
      kind: 'move',
      label: 'Move truck',
      formationId: 'outward-truck',
    },
    { actionId: 'first', actionContractVersion: 1, kind: 'act-first', label: 'Act first' },
  ],
  formationLedger: [],
  warDiary: [],
  nextDiaryCursor: '0',
  actionSetToken: 'opaque',
};

describe('connected read model', () => {
  it('uses outward IDs and audience locations for map content', () => {
    const [west, east] = operationalLocations(view);
    expect([west.left, east.left]).toEqual([16, 84]);
    expect(west.formations.map((formation) => formation.formationId)).toEqual(['outward-infantry']);
    expect(east.formations.map((formation) => formation.formationId)).toEqual(['outward-truck']);
    expect(east.contacts.map((contact) => contact.contactId)).toEqual(['contact']);
  });

  it('retains selection only while outward formation remains visible', () => {
    expect(selectedFormationId(view, null, true)).toBe('outward-infantry');
    expect(selectedFormationId(view, 'outward-truck', false)).toBe('outward-truck');
    expect(
      selectedFormationId(
        { ...view, ownFormations: [view.ownFormations[0]] },
        'outward-truck',
        false,
      ),
    ).toBeNull();
    expect(selectedFormationId(view, null, false)).toBeNull();
  });

  it('maps Truck convoy explicitly and keeps action handles as data', () => {
    expect(connectedKind('Truck convoy')).toBe('truck');
    expect(connectedSymbol(connectedKind('Truck convoy'))).toBe('T');
    expect(connectedKind('unrecognized')).toBe('unknown');
    expect(formationActions(view, view.ownFormations[1]).map((action) => action.label)).toEqual([
      'Move truck',
    ]);
  });
});
