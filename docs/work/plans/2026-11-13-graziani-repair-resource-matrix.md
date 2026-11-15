# Land-only repair: source and decision matrix

**Status:** planning interpretation; no accepted ruling, executable authority or
independent source/errata review. Original Land §§22.13, 22.2–22.4 and §32.0
were visually rechecked on printed pp. 34–35 and 43–45 of the local compiled
scan SHA-256 `836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`.
This is a same-stream check. The [facility ruling](2026-11-12-graziani-ruling-decisions.md#rul-gra-01-commonwealth-temporary-repair-facility)
and [Tobruk conflict](2026-11-12-graziani-source-register.md) remain separate.

A [blind source-review packet](graziani-rules-review/README.md) now targets
the relevant printed cases; its independent entry and ruling review remain open.

## What the source says

| Source | Bounded observation | Open interaction |
| --- | --- | --- |
| §22.13 | Repair is barred in listed circumstances, including no supplies available for repairs, towing in the phase, enemy control and certain weather, with stated major-facility exceptions. | Whether and how the supply-availability restriction survives the abstract supply replacement is not specified by this paragraph alone. Do not waive the other restrictions by analogy. |
| §§22.23–22.24 | Field repair of trucks and field repair of recce/armored cars explicitly require no supply expenditure. | No-expenditure wording does not itself prove how §22.13's separate availability phrase applies. |
| §22.26 | Field repair of tank TOE points specifies fuel expenditure before rolling. | Decide how §32.11 affects this debit and which exact vehicle types the case covers; do not extend its wording to every self-propelled gun without further source. |
| Errata §§22.34, 22.8 | The errata removes a reference from §22.34 and says the chart explanation’s final die-modifier sentences are wrong; it directs readers to §22.34 for correct modifiers. | Verify exact table and source revision when implementing facility repair; this correction alone does not decide Land-only supply expenditure. |
| §22.35 | Facility repair specifies stores and fuel per eligible point before rolling; §22.38 directs newly constructed temporary facilities to another case. | Abstract supply has no separate stores balance; do not fabricate one. Check the construction-specific case and source exceptions. |
| §22.42 | Destroyed-tank repair specifies stores and fuel and explicitly labels that expenditure Logistics Game only. | Check the remainder of this case and facility/ownership rules independently; its parenthetical is not a blanket ruling on all other repair categories. |
| §§32.0, 32.11 | In the Land-only abstract mode, references to ordinary ammo, fuel, stores and water usage/expenditure are ignored. The general rule replaces expenditure with §32 costs, and §32.11 also says only its following supply rules apply. | Determine whether that broader supply-rule statement removes §22.13's availability condition or only replaces its resource accounting; check any specific repair exception. Distinguish expenditure from the separately worded condition. |
| §32.16 | A friendly **combat unit** may draw from an eligible supply unit under stated distance/path restrictions. | A broken-down vehicle is not automatically a qualifying combat unit. This rule alone does not define repair-supply availability. |

## Candidate ruling to review

For planning, the strongest text-based candidate is to **omit the ordinary
§22.26 and §22.35 fuel/stores debits in Land-only mode**, subject to finding a
specific applicable exception, because §32.11 replaces ordinary resource usage
and expenditure with the procedures in §32. This candidate does not establish
that repair is free of every condition, that an abstract replacement charge
exists, or that §22.13's availability requirement is satisfied. It does not
change detailed Logistics mode or retroactively reinterpret saves.

The broader §32.11 statement gives a second argument for treating §22.13(b)'s
ordinary availability restriction as displaced along with the ordinary repair
debits. Conversely, §22.13(b) is an eligibility condition rather than itself
a debit, and references the field and facility repair cases; a reviewer must
decide how the two provisions interact. Neither possibility establishes an
implicit abstract-supply witness or automatically displaces weather, towing,
enemy-control or other repair restrictions.

Alternatives are to retain the listed fuel costs while discarding stores, adopt a
separately sourced abstract repair charge, or hold repair unsupported. The first
two require a clear source or explicit versioned house ruling; they cannot be
silently inferred from the presence of fuel in supply units. A decision must
separately specify whether §22.13's availability condition is replaced, is
vacuously satisfied for a no-cost repair, or has a sourced abstract witness. An
independent reviewer must check the original errata, specific exceptions and
scenario reachability before this candidate is accepted.

## Acceptance tests after a decision

- Field trucks, recce/armored cars, tank field repair, broken-down facility
  repair and destroyed-tank facility repair are distinct cases; test each
  applicable source cost and eligibility, including the special construction
  rule if admitted.
- Test supply witness or its explicit absence according to the selected rule;
  never create an implicit stores balance or assume a broken-down vehicle can
  draw under the combat-unit provision. In detailed mode retain its own costs.
- Independently test weather, enemy control/ZOC, towing, facility type and
  ownership, selected number of attempts, source timing and random die use.
  Rejected commands spend nothing and draw nothing; accepted failed attempts
  follow the adopted cost rule, are idempotent and replay exactly.
- Bind the repair/abstraction ruling and resource policy to the scenario's
  ruleset identity. Validate before creation or mutation and reject mixed-version
  histories; preserve old save readers and explicit unsupported cases.
