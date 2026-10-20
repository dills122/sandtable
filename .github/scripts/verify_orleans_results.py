"""Check the dedicated temporary Orleans host MTP report without altering Core shards."""
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


REQUIRED = {
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.InProcessHostMatchesDirectCoreAndDeduplicatesConcurrentRequests',
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign',
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.TwelveStepTraceStopsAtUnsupportedCombatWithoutGuessingTerminal',
}


def main(directory):
    reports = list(Path(directory).glob('*.xml'))
    if len(reports) != 1:
        raise ValueError(f'Expected one Orleans test XML, found {len(reports)}')
    assembly = ET.parse(reports[0]).getroot().find('assembly')
    if assembly is None or Path(assembly.attrib.get('name', '')).stem != 'Cna.OrleansHost.Tests':
        raise ValueError('Wrong Orleans test assembly')
    rows = assembly.findall('.//test')
    names = [row.attrib.get('type', '') + '.' + row.attrib.get('method', '') for row in rows]
    cases = [row.attrib.get('name', '') for row in rows]
    total = int(assembly.attrib['total'])
    passed = int(assembly.attrib['passed'])
    if (total != len(rows) or passed != total or int(assembly.attrib['errors']) != 0
            or int(assembly.attrib['failed']) != 0
            or int(assembly.attrib['skipped']) != 0 or int(assembly.attrib['not-run']) != 0
            or not all(cases) or len(set(cases)) != len(cases) or not REQUIRED.issubset(names)
            or any(row.attrib.get('result') != 'Pass' for row in rows)):
        raise ValueError('Incomplete or failed Orleans test rows')
    print(json.dumps({'module': 'Cna.OrleansHost.Tests', 'total': total,
                      'passed': passed, 'required_rows': len(REQUIRED)}, sort_keys=True))


if __name__ == '__main__':
    main(sys.argv[1])
