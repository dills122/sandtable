# Second-side Reserve Designation, event 43

After the first side's accepted Patrol pass reaches the second side's Reserve
Designation, the second side may designate an independently represented,
eligible own element as Reserve I or complete designation. Core derives the
side from the retained operation-stage order, not from initiative or a client
claim. This first capability uses the current Content6 eligibility profile:
an own independent non-vehicle element with no current Reserve designation.
Other historical eligibility and attachment rules remain outside admission.

Each designation changes only that element's Reserve status in the settled
World. Completion changes no World state and advances to the second side's
Movement position. Every V43 event binds the previous event hash, prior
authority version, acting side, choice, prior/result World hashes, position
and exact successor. Contact knowledge and RNG are unchanged. Host provides
an owner-only opening with own eligible elements and completion; submissions
are fenced and saved in the existing journal. Cold replay recomputes each
choice and World hash from the preceding accepted event, and retry returns
the retained receipt. Reject wrong side, stale, duplicate, foreign, ineligible
and forged choices without mutation.

This contract does not implement second-side Movement or admit historical
formations, deferred arrivals or a Graziani campaign. Its eligibility cannot be
used for a broader content profile without validating that profile's rules.

## Ledger rollover, event 58

The first Reserve decision after a native stage preamble (V54) instead writes
contract version 58 with the same canonical fields. Its prior World hash still
binds the settled predecessor; its result World hash commits the ledger's current
Game Turn and Operation Stage, zero stage expenditure, and removal of the prior
stage's Movement-ended markers. All other element state and World data are
preserved except the optional accepted Reserve choice. Additional choices and
the other side's Reserve opening retain V43 and do not reset the ledger again.

The accounting scope follows `MOV-REQ-004` in [Movement foundation](movement-foundation-v1.md)
and the repository's retained rule provenance (Land §§6.13–6.16). The Turn 2
Reserve opening can validate the new event without accepting it. The V11
snapshot retains its historical scenario-start scope and diagnostics; no V11
serialization or setup identity changes.
