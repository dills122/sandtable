# N1 engineering self-review

Status: author engineering self-review complete; final corrected focused98/98 passed,0failed/0skipped,2m27.852s. This is author engineering review; it does not consume a fresh independent review instance or give the independent readiness verdict.

## Scope and requirements

Base: f59ffcdbcbcadfbdb20b7e7f8796d477533b792f (merged I01, PR #187). Branch: codex/actual-moved-assault. The implementation plan is ../plans/2026-10-14-actual-moved-assault-implementation.md and the canonical contract is ../../specs/combat-actual-moved-assault-v1.md.

The private successor authenticates real I01 entry18, Selection/RBA, Prepared Round30, cumulative commit31 and mandatory settlement into same-cycle Reserve Release entry. The six independent ledgers remain caller authority. Paid, no-attack and cancellation/fallback endpoints are included. PAC/Release continuation, finish, public/host/Exercise/Runner and general seed admission are excluded.

## Correctness

Reviewed Replay/Apply/ReadControl/ReadProof, DeriveBoundary, staged mechanics, CumulativePaidWorld and CertifyMovedSupport against the contract and literal tests. Root/cycle/MovementEnd and both move progress references survive composition. The attacker starts at CP4, commit adds5; defender adds3; ammo10 becomes0 once and commit consumes no RNG. Source and claim authentication replays all ancestors and suffixes, with exact canonical event comparison. Duplicate command+actor recovery precedes live/admission gates after primitive and context validation. Proof lastEventHash derives from retained Source, so Apply and Replay agree even for an older command retry.

56 genuine seed1 branches retain 798 source/control/proof cut hashes and every event. 12 positives span both owners, three Reaction closures and two seal orders;44 branches exercise no-attack/cancellation/fallback. Nine additional optional handler cases are isolated arithmetic support, never Source authority. They cover actual supported catalogue combinations and player/deadline/unavailable transitions, local-window reopening below prior audit high-water, disabled fresh admission, exact retry recovery, cumulative costs/RNG, custody donor/TOE conservation and retained future duties. No defender-capture-with-required-retreat catalogue row exists; none was fabricated.

## Readability and architecture

The two new native files separate closed syntax/byte ownership from source replay and deterministic phase mechanics. New technical module names stay within Umpire. Bounded local ports are deliberately coupled to frozen Selection/Round/Paid mechanics; source-specific initialization, provenance, cost and contextual projection are owned here. Reusing pure catalogue/Facts/Resolve/Project/MakeWindow and leaf grammar avoids modifying frozen predecessors. No old Admission/Round/PositiveEntry proof is used as ingress authority. PaidAssaultFrame carries factual data only; its Base is this family's PreparedBase.

The maintenance cost is duplicated bounded phase mechanics and a literal closed grammar. Extending a frozen predecessor requires a deliberate new contract/pin review rather than silently drifting this successor. No new package, service, public endpoint or protobuf contract was introduced. README, tech-design, naming-overview and the canonical delivery plan were synchronized.

## Security and data integrity

Source/current input and all six ledgers are copied before any dependency callback. Cold/warm replay acquires83 immutable runtime dependencies and separately checks exact owned schema/verifier delivery bytes; retained copies are rehashed at use. Duplicate, extra, noncanonical, malformed and oversized source input fails before dependency acquisition. Caller claims never replace replayed facts. Returned bytes/ledgers are defensive copies and JSON views are cloned.

Physical limits remain Source8MiB, Proof2MiB, Control512KiB, Event64KiB, Envelope4KiB, own depth40 and unchanged parent1MiB/depth32. Maximum quoted structural wire shape and just-over bounds are tested separately from authenticated campaign sources; valid framing is not an ancestry proof. No credentials, raw CI archives, binlogs or diagnostic payloads are part of this branch. Blocked archive27d8293 remains excluded.

## Performance

Loops and dependencies are finite:43 own events,83 runtime pins plus2 delivery pins,16616 catalogue records and bounded selected projection/window paths. Fresh support validates the same cumulative CP9/3 World before debit. Accepted-history replay does not repeat fresh admission certification, preserving already-paid recovery when fresh admission is disabled. This remains a private reader with repeated full ancestry replay and copied JSON; focused test timings are not a public latency or peak-memory proof. Compact fixture is7425308bytes and avoids97MiB repeated ancestry.

## Findings and dispositions

1. Apply used the just-returned event for Proof.lastEventHash, unlike cold Replay. Fixed by deriving the last suffix event from Source; all798 proof cut hashes now independently derived by reference and checked natively.
2. Native typed position version4 differed from frozen catalogue version5 at downstream cuts. Corrected the composed position version; existing source/control hashes were preserved.
3. Isolated optional PaidTransition coverage was missing. Added nine supported cases and independent conservation/future-duty assertions. Missing test Content/Boundary, impossible case selection and numeric-boxing failures were harness defects, not behavioral RED.
4. Native support minima retained legacy956/26552/224827/1273983/23656/192965/10528 instead of owned4096/65536/524288/2097152/65536/524288/45056. Genuine regression RED: a valid Prepared30 context accepted PaidInput4095 when rejection was required. Corrected all seven constants, tested every13 capacity just below its bound, exact-bound and larger capacity acceptance, unchanged prepared World/source and cursor2. The first edit script was a no-op after formatting expanded initializer lines; its failed combined run is explicitly not GREEN evidence.

No remaining required author finding is identified within the frozen private scope, subject to final combined validation and independent review.

## Evidence and remaining gates

Original missing-reader RED is retained in commits4819927 and a33c944; contract/API/reference freeze is5a4482e. The later fresh-support NotSupported RED is distinct from compile/helper/fixture failures. Capacity RED selected1 genuine test:1failed,0skipped,5.560s, Assert.Throws failure because no exception was thrown. Corrected targeted GREEN:1passed,0skipped,6.965s. Earlier combined98/98 was pre-capacity-fix evidence only.

Normal reference acceptance passed56 cases,12 paid,44 fallback,798cuts with source/control/proof/events/literal costs and Reserve-entry checks. Scoped dotnet format --verify-no-changes --no-restore passed. Final corrected native combined gate passed98/98,0failed/0skipped,2m27.852s; source diff contains all seven corrected minima. The targeted corrected capacity gate passed1/1,0skipped,6.965s. Full local suite was not run under the focused lease. Fresh independent review, immutable-head hosted CI with actual row reconciliation and Brain merge/verified main remain required.
