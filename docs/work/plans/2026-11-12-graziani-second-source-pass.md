# Graziani follow-up source pass and evidence

**Status:** same-researcher critical recheck, not an independent reviewer or
release approval. Original scenario, Commonwealth charts, Land rules and errata
were re-examined; distinct published records and an independently maintained
project were compared. At the time, the separate-reader review was open. No
historical scenario data or working ruling was admitted into Core.

**Subsequent bounded review:** a fresh-context agent independently double-entered
the 11-row/34-field pilot; see [review disposition](graziani-source-review/review-disposition.md).
This later evidence does not retroactively make this pass independent, supply a
separate human reviewer, or close the broader setup and rights gates.

| Priority | Evidence added | Remaining gate |
| --- | --- | --- |
| Matruh setup | §§4.25, 19.1–19.5 and 59.2 reveal distinct assignment/attachment, costs, initial choice and capacity questions. RUL-GRA-03 selects errata's assignment reading for planning. | Separate reader; choose and version setup/slot semantics, mobility exception and testable authority. |
| 6th NZ | Schedule and OA agree on turn 6/stage 3 for brigade; §20.11 example conflicts. RUL-GRA-04 selects schedule/OA for planning. Morale training is optional and different from replacement training. | Separate reader, source/ruling review, eligible placement and phase ordering. |
| Formation scans | Errata p. 4 has 15/14/14 candidate rows grouped by role; Land p. 45 counter addenda corrects missing Cirene HQ stripes. Matching Grognard images are the same pixels. | Separate field-by-field entry, source/rights review, setup/assignment and mode effects. |
| Source review workflow | [11-row comparator and blank template](graziani-source-review/README.md) bind source bytes and scope, identify unknown/missing/different fields and locators. | Different reviewer must perform the reading; labels and matching outputs cannot prove independence or source truth. |
| Map and calendar | Hashed five-page archival map found; Map A summary supports Benghazi A4827, and Map D track corroborates six-turn calendar and stage-3 end. RUL-GRA-05 selects A4827 for planning. | Source/rights review, map seam/terrain reconciliation, approved IDs and topology. Low-resolution Gazala coast reading was withdrawn after closer inspection. |
| Arrival execution | [Core handoff](2026-11-12-graziani-arrival-authority-handoff.md) records existing explicit-none, stage-1-only policy and outlines source-backed authority work. | Complete formal scenario/skeleton prerequisites and contract-first implementation; existing fixture cannot certify arrivals. |
| Abstract logistics | Complete archival rules package supplies printed p. 25, showing §58.42 points to §32.57/32.59. That section covers a different game mode. | Decide chart numbering, lookup month, dash and mode applicability; no source-derived runtime assumption. |
| Victory | Predicate counterexamples show printed labels do not form a simple hierarchy. | Decide location/hold/supply semantics, result precedence, both/no match and terminal replay. |

## Executed verification

- Review comparator: `python3 -B -m unittest discover -s docs/work/plans/graziani-source-review -p 'test_*.py'` — 18 passed.
- Comparator with actual four source files and blank template — expected exit 1:
  34 unknown fields, 11 locator differences, 11 placeholder locators and one
  placeholder reader; identical actual reader input —
  expected exit 2. Source digests were checked during each invocation.
- `just test-class core '*StageEntryArrivalTests*'` — Release build passed with
  zero warnings/errors; test runner failed in the sandbox on local named-pipe
  binding. Direct `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj
  --configuration Release --no-build --no-restore --filter-class
  '*StageEntryArrivalTests*'` with named-pipe restriction lifted — 4 passed,
  0 failed, 0 skipped. Scope is the existing synthetic no-obligation profile.
- Original research supply arithmetic script passed. A local script checked 79
  links across 17 research documents and all 47 unique register IDs; no issues.
  Tracked `git diff --check` passed, and 21 untracked research files had no
  trailing whitespace or missing final newline.

No complete .NET solution suite, historical six-turn game, independent reader,
legal/rights determination, exact-head hosted CI, or release acceptance is
claimed. All source PDFs and map images remain outside Git.
