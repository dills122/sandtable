# RUL-GRA-09 independent source review

**Disposition:** Revise the proposal's privacy acceptance language before
admission; retain the ruling's dash and timing gates. Source observations below
support several planning choices but do not establish an official resolution.
This is a separate reading of the same scans, not another edition. No runtime
or readiness state changed.

## Findings

1. **Privacy vector is overbroad and incomplete.** The proposal's final vector
   says public observations and validation must not reveal whether *a particular
   counter* is real. Land §32.18 says a dummy is removed on discovery: a
   source-authorized discovery and resulting observable removal may disclose its
   identity. Conversely, redacting a dummy flag alone is insufficient: §32.32
   requires six motorization points for a dummy and 30 for a real unit, so
   public motorization quantities, cost-specific legal actions, failure reasons,
   event order or stable identifiers may permit inference before discovery. The
   proposal's numeric-loss event also retains side and source month, with the
   calculation's denominator and numerator potentially exposing private force
   totals if the canonical event is broadcast. RUL-GRA-09's more general
   fog-of-war instruction is consistent with this concern but does not define
   audiences or observable fields.

   **Needed:** specify owner/umpire, opponent and intelligence-plane projections;
   discovery triggers, timing and authorized audience; identity/aggregate
   visibility; and atomic private canonical event versus redacted projections.
   Before discovery, compare full authorized transcripts for states differing
   only in hidden identity, including action enumeration, rejection, event type,
   ordering, identifiers and visible costs, *where the source makes those
   histories observationally equivalent*. Do not require identical views where
   an independently authorized observation differs. After authorized discovery,
   test appropriate disclosure and removal, replay and reconnect; verify no
   unintended inference from aggregate transport or monthly loss data. Source
   §32.18 does not by itself specify every discovery trigger or field: those
   remain separate rules questions.

2. **Initial monthly check is not bounded by the October example.** Land §32.57
   schedules determination during the first Naval Convoy Stage of **each**
   month; scenario §60.1 says the scenario group begins 15 September 1940 and
   §60.22 starts this scenario on Game-Turn 1, OpStage 1. The proposal tests
   the first October stage and expressly leaves first-month treatment open,
   but does not identify or test a potentially earlier September check. Under
   its selected previous-calendar-month key, any required first September stage
   would read the August 1940 dash; it may therefore block before October.
   Whether the scenario's starting sequence includes that stage, whether a
   mid-month start invokes it, and whether a startup exception applies have not
   been established here. Add a calendar/sequence-backed September case, a
   first-October case, and a resume/retry test proving a blocked obligation
   neither advances the stage nor applies partial loss. Do not claim the actual
   first reachable monthly event until the sequence is source-checked.

3. **Month and dash choices remain interpretations, and event timing needs a
   more exact contract.** §32.57 describes determining the previous month's
   losses while asking players to read the “appropriate month”; it does not
   explicitly say whether the chart key is the elapsed month or the stage's
   month. The selected previous-month key is plausible, not source-mandated.
   The §58.5 table shows dashes January–September 1940 and no dash legend on
   that chart. `unspecified` plus blocking is a conservative project policy,
   not proof of the publisher's intended meaning; zero, no-event and unavailable
   are distinct alternatives. RUL-GRA-09 appropriately marks dash, rounding,
   arrival order and first-month treatment as gates, but an admitted contract
   must define exactly when within the Naval Convoy Stage the snapshot is
   taken, when owner selection and removal happen, and what persists if input
   or a policy is missing. A numeric rate alone does not authorize an event
   with a rounded result. Test both sides, exact versus fractional losses,
   shortfall, stage ordering and idempotent replay under the final policies.

4. **Setup placement and optionality remain conditional.** Scenario §60.92(B)
   gives two groups of three Axis dummy counters and §60.92(C) gives one
   Commonwealth group, placeable under their respective dump rules. §32.18 says
   a player *may* place a dummy for every three qualifying real units; neither
   source explicitly resolves whether the scenario list is additive to the
   general entitlement. The proposed fixed starting allotment is a defensible
   policy, not proof that all groups must actually be placed or that optional
   real units create no additional entitlement. Define whether scenario groups
   are optional, whether a group can be partially placed, and what happens when
   valid placements do not exist. §59.65's exception applies only to excess
   unavoidable under distribution instructions; test it per hex against a
   certified placement set without disclosing opposing hidden state. Do not
   infer an exception simply from a player's selected co-location.

## Source checks and limits

Visually inspected local hash-matched originals:

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF and
  printed pp. 43–44: §§32.14, 32.18, 32.32, 32.34, 32.57 and 32.65;
  SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf), PDF
  and printed p. 13, §58.5 and its legends; SHA-256
  `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`.
  October 1940 is 6/2, Commonwealth/Axis. The chart's percentage and side
  legends support its mapping to §32.57, but its title/number do not prove an
  explicit correction of §32.59's chart reference.
- [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  PDF p. 2 / printed p. 26, §§59.53, 59.65–.66 and 60.1; PDF p. 3 / printed
  p. 27, §60.22; PDF pp. 4–5 / printed pp. 28–29, §60.92(B)–(C); SHA-256
  `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.

I did not independently inspect the full turn track, stage sequence, all
fog-of-war discovery cases, or additional errata. The proposal properly labels
the on-map three-unit cap, dummy inclusion and scenario allotment as choices;
§32.34's explicit rail count is supporting analogy, not a general stacking
rule. No tests were run: these are admission vectors, not implementation
results.
