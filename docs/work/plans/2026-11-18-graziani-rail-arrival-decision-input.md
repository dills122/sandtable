# Graziani rail arrival: decision input

**Decision needed:** Decide how §8.73's rail-start requirement interacts with
§32.31's express arrival-stage permission for an abstract Supply Unit placed
on rail after the Operations Stage starts. No option below is an established
source correction or a certified Graziani journey.

## Strongest printed evidence

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
PDF/printed p. 43 §32.31 specifically permits a Supply Unit to move by land or
rail during the Operations Stage in which it arrives. On that page §32.34
expressly applies normal rail movement rules to Commonwealth Supply Units.
Section 8.73 (p. 15) requires a rail traveler to start the Operations Stage on
a rail hex, among other conditions. Section 8.72 permits Commonwealth rail in
its designated phase; §8.77 allows pickup/dropoff with restrictions.

The most direct argument for an exception is §32.31's specific, express
arrival-stage rail permission. The strongest argument for retaining the start
requirement is §32.34's explicit incorporation of normal rail rules, including
§8.73. A qualified permission and a condition can coexist where an arriving
unit was already on rail at Stage start; their actual conflict depends on the
arrival and placement convention. Neither inspected clause explicitly states
that it overrides the other. In particular, §32.31 does not say to ignore
track, enemy ZOC, CP or other restrictions. Section 32.37 separately bars
owner movement of an air-transported Supply Unit for that Stage; do not treat
all transport as a newly received arrival.

Land-only §5.2 (pp. 11–12) puts Naval Convoy Arrival Phase D after the start
of an Operations Stage and before Commonwealth Rail Movement Phase J. It names
reinforcements, replacement points and ammunition points, not explicitly
Commonwealth abstract Supply Units under §32.42. The
[witness check](2026-11-18-graziani-rail-arrival-witness-next.md) identifies
this timing gap, the Alexandria placement threshold and the period-I table's
maximum of three in one result. Initial scenario stock is not an established
mid-stage receipt. Thus the competing options have a clear semantic difference
without a source-certified reachable instance.

## Finite policy choices

| Choice | On a new unit placed on rail after Stage start | Principal consequence |
| --- | --- | --- |
| **Apply start condition literally** | Reject same-stage rail for that unit; it was not on rail at Stage start. | Preserves §8.73's literal condition and §32.34's normal-rule reference. Arrival-stage rail remains possible only under a separately justified convention where the condition is met; §32.31 is not unconditional. |
| **Arrival exception** | Treat placement on eligible rail in the arrival Stage as satisfying only the start-location requirement; evaluate all other restrictions. | Gives §32.31's specific rail permission effect for after-start arrivals. Requires an explicit, versioned exception; does not prove a concrete arrival or route. |
| **Admit only established cases** | Defer or mark unsupported whenever after-start placement creates this conflict; allow other cases only when each prerequisite is established. | Avoids deciding precedence now. Must distinguish unsupported/unknown from an adjudicated illegal order and preserve state and capacity. |

A separate timing choice determines when an abstract supply counter is
received and placed. Merely declaring it present at Stage start to avoid the
conflict changes that timing model and needs independent source support; it is
not evidence that Phase D occurred earlier. Arrival-stage permission also does
not settle cumulative versus simultaneous rail load or establish the phase in
which a decision engine may accept an order.

## Minimal distinguishing fixture

Assume, without claiming source reachability, a newly received Commonwealth
regular Supply Unit `U` does not exist on the map at Stage start, is placed on
a certified rail hex after that start and before the Commonwealth rail phase,
and is proposed for one actual rail movement that Stage. Assume independent
validation establishes every other predicate, including placement entitlement,
route, direction, capacity, ownership/control, ZOC, CP and destination.

- Literal start condition rejects `U`; arrival exception passes that one
  condition; deferred policy returns unsupported. No outcome alone establishes
  a legal real game journey.
- Control: an otherwise identical unit already on eligible rail at Stage start
  passes this particular condition under either substantive reading. Its actual
  arrival history and other legality still require proof.
- Separate negative control: an air-transported Supply Unit proposed for owner
  rail movement in the same Stage remains subject to §32.37's restriction;
  the proposed arrival exception does not authorize it.

Before adopting either substantive option, record the meaning and timing of
receipt/placement, which counters qualify as arrivals, applicable state and
source policy version, and behavior for unknown placement facts. Prove an
actual scenario sequence separately; no six-turn witness is asserted.

Original Land scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
the linked witness note identifies and hashes the scenario and chart scans.
This is a bounded interpretation input, not an exhaustive corrections search.
No runtime/data changes or gameplay tests.
