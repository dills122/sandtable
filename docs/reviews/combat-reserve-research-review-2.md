# Combat and Reserve Research — Independent Review 2

**Review instance:** 2 of 3. **Verdict:** Ready for research review and owner decisions.
Production readiness remains unestablished.

**Reviewed range:** `5cc55769224c0b73f27dd7bc41f8ea500371fc9e` through
`029c09e6a8e9a7da9a100f171540c666443814ca`; all twelve changed paths reviewed.
Branch `codex/cmb-rsh-003-mutable-state` and clean worktree were verified. This report retains
the fresh reviewer's returned findings and qualification; no implementation changed during review.

## Findings

No actionable findings remain. Both [round 1](combat-reserve-research-review-1.md) P2s are closed:

- Seed 1296 now produces Engaged, no Retreat, and no victory RP. Source §15.79 places two-hex
  Retreat beyond the selected −2…+2 surface; predecessor requirements were corrected too.
- Moving guard capacity uses five Prisoner Points per guard under errata §28.17; the five/six
  boundary probes require one/two guards.

## Plan Review

Research sequence and authority boundaries are sound. Current TOE, CP/Cohesion, and RNG authority
are reused conceptually. Reserve release retains cumulative spending, offensive-use limits, and
exact next-Movement scope. Owner decisions and reviewed Combat/cycle designs remain prerequisites.
No production approval or heavy pivot follows from this review.

## Author-Claim Reconciliation

| Claim | Independent result |
| --- | --- |
| Research only; no runtime/schema changes | Confirmed against complete diff |
| Twelve source-backed seeded outcomes | Confirmed against source renders and arithmetic |
| Existing RNG stream compatibility | Confirmed against Core and independent digest calculations |
| Corrected Retreat and guard regressions | Confirmed; restoring either error fails probes |
| Bounded research coverage | Confirmed; sampled losses/domain counts do not prove full tables or runtime |
| Historical mutation executions | Not reconstructed in full; current two correction regressions independently checked |

Independence qualification: CCE recall/search exposed brief implementation summaries before the
preliminary ledger. The author packet and prior report remained unread until that ledger was sent;
source and repository evidence were reconstructed independently. Reviewer inherited no author
conversation and made no repository edits.

## Verification Performed

All three Python commands passed: mutable-state 8 loss vectors/108 combinations/3 guard probes;
RNG 12 seeded vectors/6,480 joint coordinates/44,208 capture paths/1,900,656 expanded leaves;
Reserve release 26 logical cases. Range diff check passed.

An independent subprocess probe using `openssl dgst -sha256 -binary` matched all thirteen used
digest blocks. In-memory mutations restoring the wrong guard capacity and +2 Retreat each failed.
Land pages 13/14/24/25/28/40, full-rules pages 67–69, common charts pages 3–5, and applicable errata
were checked. Worktree remained clean. No .NET gate ran because runtime code was unchanged.

## Open Questions and Residual Risks

Complete typed loss tables, route/custody settlement, effective-CPA changes, Reserve II Morale
expansion, full lifecycle activation, and runtime replay/privacy remain explicit downstream work.
Research probes do not establish those production properties.

## Recommended Next Actions and Author Response

Author accepts the reconciliation and residual limits. Close this review loop at instance 2 of 3;
no further instance is needed for unchanged work. Submit proposed research decisions to the owner,
then compose approved inputs into the Combat/cycle design and implementation plan.
