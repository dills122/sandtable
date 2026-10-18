#!/usr/bin/env python3
"""Private, fail-closed AC-006 pretrigger diagnostic; never admits ordinal-two Combat."""

import argparse
import json
from collections import Counter
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / 'combat-p01-surrender-rba-boundary-v0.schema.json').read_text())


def load(name, filename):
    spec = spec_from_file_location(name, ROOT / filename)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


source = load('p01_surrender_source', 'verify-combat-p01-source-conditions-v1.py')
evidence = source.evidence
p01 = evidence.p01


def require(condition, label):
    if not condition:
        raise ValueError('P01-AC-006-BOUNDARY ' + label)


def diagnose(value):
    """Classify typed trusted facts; callers must separately authenticate their origin."""
    require(type(value) is dict and set(value) == set(SCHEMA['inputPropertyOrder']),
            'closed input shape')
    require(value['contractVersion'] == SCHEMA['contractVersion'], 'contract version')
    require(type(value['cycleOrdinal']) is int and value['cycleOrdinal'] == 2,
            'ordinal-two scope')
    trigger = value['sourceTrigger']
    require(type(trigger) is str and trigger in SCHEMA['sourceTriggers'], 'source trigger')
    before = value['targetAmmunitionBefore']
    after = value['targetAmmunitionAfter']
    cohesion = value['targetCohesionBefore']
    require(type(before) is int and before >= 0 and type(after) is int and after >= 0,
            'typed pre/post ammunition')
    require(type(cohesion) is int, 'typed pre-trigger Cohesion')
    rba = value['rbaOutcome']
    require(type(rba) is str and rba in SCHEMA['rbaOutcomes'], 'RBA outcome')
    decline = value['declineReceiptId']
    cancellation = value['cancellationReceiptId']
    for name, receipt in (('decline', decline), ('cancellation', cancellation)):
        require(receipt is None or (type(receipt) is str and receipt.startswith('cmb.')
                and len(receipt) > 4), name + ' receipt shape')
    require(value['surrenderSettlementReceiptId'] is None,
            'unproved automatic surrender settlement')
    if rba == 'unopened':
        require(decline is None and cancellation is None, 'unopened has no receipt')
    elif rba == 'declined':
        require(decline is not None and cancellation is None, 'decline excludes cancellation')
    else:
        require(cancellation is not None and decline is None, 'cancel excludes decline')
        require(trigger not in source.ASSAULT_TRIGGERS, 'cancelled RBA cannot assault')
    if trigger == 'combat-position':
        require(rba == 'unopened' and before == after, 'pre-assault terminal shape')
    surrender = source.automatic_surrender(
        cohesion_at_trigger=cohesion, ammunition_at_trigger=before, trigger=trigger)
    return dict(sourceCondition='requires-typed-settlement' if surrender else
                'no-automatic-surrender-at-trigger',
                rbaCondition=rba, automaticSurrenderSettlementProved=False,
                ordinalTwoRbaProofProved=False, admitted=False, publicActions=[])


def audit(evidence_path):
    """Reauthenticate all original R03 ancestors, then inspect only the pretrigger terminal."""
    counts = Counter()
    physical_shape = p01.physical_shape

    def inspect(state, selected_content):
        require(state['activeCycle']['ordinal'] == 2 and
                state['sequencePosition']['positionId'].endswith(
                    '.combat.position-determination'), 'authenticated terminal scope')
        require(len(state['world']['elements']) == 2, 'exact bounded unit count')
        for unit in state['world']['elements']:
            ammunition = unit['ammunition']['points']
            report = diagnose(dict(
                contractVersion=SCHEMA['contractVersion'], cycleOrdinal=2,
                sourceTrigger='combat-position',
                targetCohesionBefore=unit['operationalState']['cohesionLevel'],
                targetAmmunitionBefore=ammunition, targetAmmunitionAfter=ammunition,
                rbaOutcome='unopened', declineReceiptId=None, cancellationReceiptId=None,
                surrenderSettlementReceiptId=None))
            require(report['sourceCondition'] == 'no-automatic-surrender-at-trigger' and
                    report['admitted'] is False and report['publicActions'] == [],
                    'authenticated pretrigger diagnosis')
            counts['units'] += 1
        counts['rows'] += 1
        return physical_shape(state, selected_content)

    p01.physical_shape = inspect
    try:
        ancestry = evidence.audit(Path(evidence_path))
    finally:
        p01.physical_shape = physical_shape
    require(ancestry == dict(verifiedRows=300, provisionalPositive=44,
                             provisionalEmpty=256, admittedRows=0), 'ancestor partition')
    require(counts == {'rows': 300, 'units': 600}, 'terminal census')
    return dict(**ancestry, inspectedUnits=counts['units'],
                priorSelectionRbaTraces=source.check_rba_prior_contract(),
                ordinalTwoRbaProofProved=False,
                automaticSurrenderSettlementProved=False, publicActions=[])


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, required=True)
    print(json.dumps(audit(parser.parse_args().evidence), sort_keys=True))
