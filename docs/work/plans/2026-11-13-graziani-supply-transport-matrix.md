# Land-only supply transport: source boundaries

**Status:** source-derived work list, not accepted rules or implemented actions.
Original Land §§32.31–32.37 were visually rechecked on printed p. 43 of the
compiled scan SHA-256 `836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`.
The individual channels, their interactions with scenario/errata and their
reachability require independent review. Values below are source observations,
not an admitted content or complete movement model.

A [blind transport review](graziani-transport-review/README.md) targets these
source statements; independent entry and interpretation remain open.

| Channel / source | Source boundary | Required future proof |
| --- | --- | --- |
| General, §32.31 | A supply unit can be transported by land, rail, sea or air. Land or rail movement can occur in its arrival operation stage. | Prove correct arrival ordering and a legal move rather than treating an arrival marker as a free movement action; apply the detailed eligibility of the selected channel. |
| Motorization, §§32.32–32.33 | The source states 30 motorization points for one real supply unit, six for a dummy, with attachment/detachment in Organization. A unit can use its owner's Movement/Combat or Truck Convoy phase, not both. Phasing Movement/Combat has a friendly-combat-unit stacking condition; the text distinguishes normal non-phasing movement. | Test 29/30 and 5/6 point boundaries, permitted attachment timing, location and stacking, the mutually exclusive phase choice, non-phasing exceptions, resource conservation and source-defined disclosure. Do not expose hidden dummy status through errors or action metadata. |
| Commonwealth rail, §32.34 | Up to two supply units, real and/or dummy, may be transported in one direction as the total load for an operation stage; normal rail rules also apply. | Test combined real/dummy count, direction, turn/stage reset, permitted owner, route/capacity and any applicable rail errata. Do not read this as two units per individual action. |
| Axis coastal shipping, §32.35 | Each supply unit requires 2,000 tons of coastal shipping; two ships may be used on one mission. | Distinguish coastal ships and their capacity from the separate simplified Axis naval-convoy rule. Verify availability, timing, destination, loss and replay before constructing a legal action. |
| Commonwealth sea, §32.36 | At most one supply unit per operation stage via Alexandria, Benghazi or Tobruk; destination port must permit at least 50% of listed capacity. This movement occurs in Naval Convoy Arrival and is in lieu of the specified combat-unit transport between the same endpoints. | Test 49%/50% eligibility after authoritative rounding is decided, both port directions, ownership, loss/control, mutual exclusion, simultaneous arrival obligations and the separate Tobruk efficiency conflict. |
| Abstract air, §32.37 | Each player may air-transport one supply unit per Game Turn under its origin/destination, enemy-distance, front-line and friendly-HQ constraints. The rule specifies 12 fuel from an eligible origin supply unit before transport and prohibits further owner movement of the transported unit that stage. | Verify retained abstract-air applicability even though the detailed Air Game is excluded. Test Game-Turn and per-side limits, origin resource sufficiency, valid HQ and geography, enemy-distance boundaries, direction, stage lock, atomic debit/replay and hidden information. |

The 12-fuel transport expenditure is **explicitly inside §32**; it must not be
confused with the candidate Land-only treatment of ordinary §22 repair costs.
No new rule should infer an abstract repair charge merely because this separate
transport case specifies one.

Before production use, check original charts and errata, determine whether each
channel is reachable within this six-turn setup, freeze side-safe command/event/
state contracts and establish actual source-backed map, port, resource and unit
identities. A theoretical positive vector is not evidence that the historical
scenario can reach it. Rejected actions must mutate neither authority nor random
state; accepted actions must replay and deduplicate through the real campaign and
Host, not just the synthetic arrival laboratory.
