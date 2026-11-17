# Patrol and fog: blind source review

**November 17 independent review:** [entry](reading-independent.json) and
[raw comparison](comparison-independent.json) are retained; see the
[independent review disposition](../2026-11-17-graziani-independent-reviews.md)
for source-specific differences and limits.

**Status:** one reader and a blank second entry. Ten grouped records cover
combat-history wording, patrol limits, weather, route, abstract supply,
survival, information, the eligibility erratum and dummy reports. They do not
transcribe all tables or resolve report disclosure, applicability or reachability.

An independent reader should inspect the original provisions and addenda without
the first entry, verify hashes, fill the template and retain exact locators.
`null` means uncertain or unreadable. The general and specific combat-history
wording are separate entries because they may lead to different interpretations.
Report fields are short descriptions, not proof of an accepted privacy policy.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-patrol-review/manifest.json \
  docs/work/plans/graziani-patrol-review/reading-a.json \
  /private/tmp/independent-patrol-reading.json \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

Agreement is scoped transcription evidence, not a source ruling, permission
to redistribute, approved contact model or playable-release claim. The
[patrol matrix](../2026-11-13-graziani-patrol-fog-matrix.md) records open cases.
