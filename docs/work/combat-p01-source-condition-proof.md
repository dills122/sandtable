# P01 AC-006 source-condition boundary

**Status:** private, read-only bounded proof. P01 remains `unsupported` with zero admitted
actions. This artifact does not implement surrender settlement, an ordinal-two selection or
RBA transition, or a native/public consumer.

## Source and policy

The pinned Land Rules PDF (`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`)
at printed §15.88, PDF page 25, allows voluntary surrender and separately says a unit out of
ammunition or at Cohesion −17 or worse automatically surrenders **when assaulted**. The pinned
September errata PDF (`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`)
clarifies §15.88: Cohesion −17 does not surrender merely because an enemy moves adjacent, while
−26 does. Errata §50.12 further requires an anti-armor or close assault for an ammunitionless
unit; enemy ZOC or Barrage is insufficient. These printed and corrected source conditions are
distinct from Sandtable's `CMB-POL-009`: the owner excludes the **voluntary choice** from the
initial RC bounded exercise. That exclusion does not erase an automatic source obligation, and
the exercise cannot claim every source-legal Combat action.

## Read-only checks

Run:

```text
PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/test-combat-p01-source-conditions-v1.py
PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-p01-source-conditions-v1.py --evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```

The verifier checks the two physical source hashes before replay, then uses the frozen P01
evidence audit to authenticate all 300 original CreationRequests plus eleven separate parent
ledgers, exact R03 suffixes, the summary/manifest/predicate pins, and eleven compressed chunks.
The observed 44 provisional positives and 256 provisional empties remain `unsupported`,
`admitted=false`, with no public actions. It independently inspects each authenticated
ordinal-two position state at its **pre-assault** timing. Twelve adjacent paid rows have zero
carried ammo after the previous assault and no current target use; their existing adjacency is
not a new assault and cannot cause an automatic-surrender classification. The 44 fresh positive
rows have both units' carried ammo 10 and Cohesion 0 before any prospective second assault.

The literal source matrix distinguishes pre-event ammunition from later payment: a supplied
10-TOE defender that pays 10→0 is not reclassified from that post-cost balance, while an already
ammunitionless unit **being assaulted** is a source-triggered surrender case. It tests the
−17/−26 Cohesion boundary, actual assault versus movement adjacency, Barrage and existing
adjacency. A detected automatic case raises as unsupported. Malformed event timing or resource
values reject. This diagnostic receives an
explicit source-event trigger; it neither fabricates an assault from a position snapshot nor
publishes a transition.

For RBA, the verifier reruns the existing actual-selection predecessor contract for both sides:
an explicit defender decline alone records a decline; missing response before opening, at
deadline or after controller unavailability cancels the selected attempt without one. Those
eight traces authenticate the older selection contract, **not** a P01 ordinal-two transition.
No P01 selection, RBA deadline, cancellation event or replay ledger exists yet.

Focused local evidence: seven tests passed. Full pinned replay returned 300 authenticated rows,
44 provisional positives, 256 provisional empties, zero admitted, twelve adjacent paid rows
with post-cost zero ammo, fourteen source-boundary cases and eight prior-selection RBA traces.

## Remaining AC-006 gate

The read-only diagnostic and prior-contract regression close only source interpretation and
counterexample checks. A reviewed P01 transition must still carry typed event-time evidence
from authenticated authority: which unit was actually assaulted or saw a new enemy move
adjacent, its Cohesion and **pre-assault** accessible ammunition, the active decision and
selection context, and the proper automatic-surrender settlement. That transition must fail
closed whenever it cannot prove or execute the source obligation. It must separately enforce
the versioned voluntary-surrender exclusion and decline-only RBA cancellation through its own
R03-derived ledger and outward privacy tests. No state here is admitted until that work and all
other P01 acceptance gates pass on a reviewed head.
