# Simplified Axis convoy 5% proposal: source review

**Disposition:** The cited 5% table cell and synthetic ceiling arithmetic are
correct. The six-group schema, use of upward rounding and model-allocation
handling remain explicit project choices, not printed Land-only facts. Retain
`unsupported` until grouping, cargo legality and allocation are accepted.
Clarify weather and integer/event boundaries before admission. Same-edition,
informed rereading; not a blind transcription, independent edition, or proof
of a reachable historical convoy.

## Source checks and precision

1. **The 5% cell is real, and its dice are sequential.** Common chart §41.5,
   printed/PDF p. 12, Axis Naval Convoys row, 21–40 bomb points, prints `55–64`
   for **5%**. The possible sequential d6 outcomes in this range are exactly
   `55`, `56`, `61`, `62`, `63`, `64`; `57–60` are not legal two-d6 readings.
   The 21–40 column also gives `11–54` as 0% and `65–66` as 10%. Other bomb
   columns also have 5% cells, so this is a particular test cell, not an
   exhaustive 5% locator. September errata §41.5 corrects the **barrage-points
   header** and flak label; it does not identify this bomb-points cell as a
   correction. Actual bomb-point reachability is separately unresolved.

2. **The Land gap is correctly identified.** Land §32.64 (printed/PDF p. 44)
   invokes the Air Bombardment table and describes percentages for supply,
   motorization and replacement points in the affected lane. §32.65(A) covers
   10%, and (B) covers 20% or greater, leaving no express 5% rounding branch
   in that case. Land §2.1 (p. 4) describes §§6–32 as necessary Land rules;
   invocation of the shared table does not by itself settle import of detailed
   Air procedures. This bounded examination found no general Land rounding
   directive in those cited passages, but cannot exclude an applicable rule
   elsewhere. A 5% event yielding no loss under an alternative accepted floor
   rule is different from a table 0% result; do not silently collapse them.

3. **Detailed Air groups are analogies, not the abstract cargo list.** Air
   §41.67, printed/PDF p. 12, says to round fractions up and explicitly groups
   all truck replacement points together, Italian tanks together, German tanks
   together, Italian guns together and German guns together. September errata
   §41.67, PDF p. 3, clarifies that the result percentage applies to each
   cargo type. Neither text explicitly defines Land-only Supply Units and
   Motorization Points as separate detailed-Air categories, proves the
   nationality/class grouping applies to every abstract replacement point, or
   enumerates all cargo classes. Land §32.65's nationality/not-individual-type
   wording and tank-only example do not settle combined tank/gun versus
   separate groups. The proposed six groups are a deliberately bounded schema;
   retain unknown/dummy/unclassified cargo as unsupported rather than claiming
   the source excludes it. Air §41.67's named **tanks and guns** are not
   themselves a complete Land-only replacement schema.

4. **Rounding and the chooser must remain distinct decisions.** §41.67 and its
   erratum support `ceil(5% × group size)` only within their detailed Air
   scope; importing that operation is the proposal's analogy. §32.65's printed
   10% example gives the Axis player a German tank-model choice; it does not
   expressly extend that choice to 5%, guns, supply or motorization. A group
   with multiple possible losses needs an accepted allocation rule or remains
   pending, while an actual one-item group is numerically unambiguous. The
   `min(group size, loss)` cap is a sound conservation guard; it does not prove
   the source intended a special minimum, authorize a cargo group, or create
   losses for empty groups. Errata §41.35's upward rounding is attached to
   **bombing supply dumps**, not a general convoy or Land-only rounding rule.

5. **Do not import a weather restriction.** The proposal calls for accepted
   lane, division, weather and dice rules before selecting a convoy. Land
   §32.63 explicitly permits the Commonwealth to attack all convoys regardless
   of weather on the map. Preserve that specific permission: “weather rules”
   must not become an ordinary Air weather ban or evidence that weather
   prevents this simplified attack. Other independently applicable conditions
   remain separate. §32.62 caps each lane at three Supply Units and 20
   Motorization Points; this does not prove that any particular replacement
   load, nationality, date or bomb-point column is legal.

6. **Persist the draw without exposing a second outcome.** Saving the resolved
   die result and RNG position before a gated effect, then committing losses
   and survivors atomically after accepted allocation, is a proposed event
   contract, not an original-source procedure. Bind the draw to the same convoy,
   cargo snapshot and version; a retry, cargo change or crash must not reroll,
   apply partial losses, or deliver survivors while unresolved. Review
   visibility of pending status, allocation choices, event ordering and
   response timing so hidden cargo categories do not leak. This is a design
   prerequisite, not a proof that those observations are source-equivalent.

## Arithmetic cross-check

For nonnegative integer group size `n`, exact 5% ceiling is
`n // 20 + (1 if n % 20 else 0)`. This avoids overflow from an unchecked
`n + 19`; a wider checked intermediate is also valid. It gives
`1→1`, `19→1`, `20→1`, `21→2`, and `0→0`. The candidate's mixed example has
counts `(3, 20, 6, 2)` in four proposed groups, each with loss 1, hence four
**items/points across different categories**, not four interchangeable cargo
units. The one Italian tank plus one Italian gun comparison gives 2 under
separate groups versus 1 under a combined two-point group; neither grouping
is source-settled for this Land-only 5% case. Absent groups produce zero;
unknown groups cannot be treated as absent. Test negative or invalid counts as
invalid input, not a new arithmetic case.

## Suggested acceptance vectors

- Check all six legal 5% die outcomes above, adjacent 54 and 65, result 0 and
  no attack separately. Test another chart column with a printed 5% cell;
  enforce sequence digits rather than accepting every integer in a range.
- Compare a single supply unit plus one motorization point under the proposed
  per-group ceiling and a separately versioned floor alternative. Both the
  numerator/denominator and category schema must be explicit; neither is a
  printed §32.65 5% resolution.
- Exercise each proposed nationality/class group, multiple equipment models,
  an empty group, unknown nationality/class and possible dummy cargo. Pending
  ambiguous allocation must not select a model, consume new RNG, remove cargo
  or deliver survivors; verify accepted choice does not exceed carried count.
- With the same certified convoy and bomb result, compare normal and adverse
  map weather: §32.63 alone does not disallow the attack in either. Check lane
  caps, one convoy at a time, conservation by cargo category, rule identity,
  crash/retry and side-authorized observations.

## Sources and limits

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 4 and 44, §§2.1, 32.61–.65, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), printed/PDF
pp. 11–12, §§41.35, 41.66–.67,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`;
[common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf),
printed/PDF p. 12 §41.5,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`;
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
PDF p. 3 §§41.35, 41.5, 41.67,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Did not exhaust all Land rounding rules, later errata, scenario cargo or
reachability. No runtime/data changed and no gameplay tests ran.
