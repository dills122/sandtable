# Combat delivery plan: author documentation and planning review

**Input:** `f17581deddb582bcd7cc55396d53ebd46ac1dc7d`, PR93. Merged during review as
`50da769`; the merged tree matches the reviewed input. Corrections use a fresh feature branch.
**Scope:** current roadmap, combined Combat plan, governing contract boundaries and supporting
implementation evidence. Author review requested after the planning sync; not a fresh-context
independent review, and not another verdict on C3a code. Independent-review use remains8of8.

## Owner disposition

After reviewing commit `b8be39a`, owner requested incorporating the new information into documentation
and planning and opening a PR. Accepted direction: preserve003C3b as next Combat task, explicitly
allocate the inherited pre-Combat path and first opening at B, retain progressive Core integration
evidence, and run a bounded Orleans investigation during remaining contract work.

The governing [roadmap](../roadmap/pre-alpha-roadmap.md#bounded-hosting-investigation--host-rsh-001)
now records `HOST-RSH-001`, scheduled after003C3b. Investigation is authorized and unstarted.
The recommendation/proposal labels in the original review below retain the review-time status;
this disposition supersedes them for the accepted investigation and planning direction.
Exact runtime child plans still require B acceptance; production hosting still needs a host
contract/storage decision. No provider, deployment, model dispatch or new independent review round
was approved. This acceptance is planning evidence, not an executed experiment. Follow-up checks
passed403 local link targets,11 Markdown anchors,25 stable tasks/backward dependencies,72 unchanged
design ACs,8 policy IDs and whitespace. No runtime/scenario/frozen-spec files changed.

## Conclusion

Keep the accepted contract → dormant Core → public authority → Runner sequence. It protects the
existing fidelity, privacy and replay requirements, but the plan needed clearer ownership of the
inherited campaign path and more precise recovery/status language. Those gaps are corrected or
made explicit checkpoint-B decisions in the accompanying documentation changes.003C3b remains next.

The best-supported improvement is earlier integration feedback and a separate, bounded Orleans
feasibility probe on today's Rules9 engine. There is no demonstrated need for a wholesale rewrite,
a new gameplay policy or full hosting before Combat contracts. There is also no technical reason
to wait for all Combat work before learning about storage and retry behavior. This recommendation
is an inference from current boundaries and official documentation, not a measured delivery estimate.

## Findings and disposition

| ID / severity | Evidence and consequence | Disposition |
| --- | --- | --- |
| PLAN-001 / P2 | [Current action execution](../../src/Cna.Core/Actions/CampaignCurrentActionExecution.cs) accepts Snapshot11/Content6; [event/snapshot dispatch](../../src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs) selects Created10 and current predecessor codecs. [C3a](../specs/combat-selection-steps-v1.md#trusted-opening-boundary) instead requires validated World7/current-history input, and uses synthetic hashes only in isolated probes. Tasks008/019/021 did not explicitly allocate all inherited-path adapters; first-cycle opening was assigned019, after the first Combat lifecycle tests. Leaving this implicit risks discovering an unbudgeted integration block at activation. | B now requires003C3c/D2 to inventory inherited families and allocate bounded runtime owners. Proposed008 split/first-opening move below must be reconciled before B acceptance. This does not show an existing runtime bug or invalidate the disclosed C3a fragment. |
| DOC-001 / P2 | Roadmap's “current parallel execution window” still told readers to continue completed research. Sprint5 introduction requested plan approval already recorded later on the same page. C2's ordered-slice row still said author-only despite review8 in the current status. | Corrected active-lane/status prose and the C2 row. Historical review records remain unchanged. |
| DOC-002 / P2 | Checkpoint D said stored obligations survive “restart” while008 defines Core codecs and later handlers do not yet exist. No campaign grain or durable provider is implemented. This wording could be mistaken for process/storage recovery evidence. | D now limits its claim to serialization/fresh Core reconstruction for implemented states; durable storage, publication and silo/process restart remain separate hosting acceptance. |

Existing E/G/H already require lifecycle, settlement and cycle tests. The review does **not** claim
that all testing was postponed to024. The new progressive-evidence table makes composition and
provenance requirements explicit so independent unit/fragment successes cannot stand in for a
joined execution. Early test fixtures retain their stated trust limits; H must derive a real
pre-Combat boundary and close that gap before public activation.

## Recommended sequence

1. Continue003C3b,003C3c,003D2 and004; accept checkpoint B only with the inherited-path map,
   composed prospective contract evidence, full capacity/identity checks and named AC evidence.
2. At B, size008 as codec/restore work plus separate inherited-path adapters. Bring first-cycle
   opening forward from019 as needed for composed dormant tests; keep repeat/finish in019.
   This is a proposed child decomposition, not an executed task or silent change to accepted order.
   New campaigns use their explicit compatible contract set; no inferred Snapshot11 upgrade.
3. Implement the existing dormant Core sequence. Show a joined assault at016 and a joined
   inherited-path/cycle trace at019. Expose only the complete supported surface at020–021.
4. Deliver first verified public Combat Exercise/Runner trace through022–023, then024's full
   bounded regression/paired evidence.025 reconciles closeout; broad statistical balance or timing
   studies are driven by a question and executable coverage, not by documentation churn.
5. Investigate Orleans independently of Combat completion using the bounded proposal below.
   Earliest proposed production-host handoff:020–021 plus one verified023 trace and an accepted
   host contract/storage decision.024 remains mandatory for Combat closeout. Sprint8 remains the
   accepted lifecycle milestone until the owner accepts a revised schedule.

### Proposed Orleans feasibility probe

Decision owner: repository owner. Status: proposed, experiment not run. Time/scope bound: one
focused investigation, at most three primary files; report unresolved issues rather than expand
into a platform implementation. It can follow this review or interleave with contract work without
becoming a prerequisite for003C3b. It must not mutate shared authority registrations.

Use an isolated test host and one current synthetic Rules9 campaign. Compare a direct-Core accepted
command trace with the hosted equivalent. Inventory trusted creation/query/submission/checkpoint
seams first; identify any missing adapter instead of serializing an opaque handle by assumption.
Test duplicate/stale submissions, a lost reply after a write, write failure and reactivation.
Use a provider/test double with stated failure semantics for the first bounded probe; do not claim
durability from memory storage or select a deployment database without evidence.

Output: a concrete host contract and publication/storage decision proposal covering the accepted
command/event/checkpoint/receipt relationship, audience authentication, source/config identity,
acknowledgment point, retry deduplication and failed-write recovery. Chronicle remains authoritative;
independently saving an event and snapshot is not presumed atomic. Compare at least an atomic record
approach with a journal plus reconstructable checkpoint before recommending the durable design.
Actual process restart against the selected durable provider is a later implementation acceptance
check. Clustering configuration, campaign storage and model dispatch are distinct concerns.

### Alternatives considered

| Option | Benefit | Cost / decision |
| --- | --- | --- |
| Keep all hosting discovery until after full Combat closeout | Least immediate context switching | Leaves storage/publication uncertainty late; retain as fallback if no separate investigation capacity. |
| Build production Orleans before completing Combat contracts | Earlier hosted product path | Adds transport/storage compatibility work around changing contracts; not recommended without a concrete external-host requirement. |
| Keep Combat order, add progressive composition and an isolated Rules9 hosting probe | Earlier failure evidence without public partial Combat or an immediate provider commitment | Recommended; requires explicit experiment scope and later owner acceptance of host design/scheduling. |

## Evidence and sources

**Repository observations:** current host [Program](../../src/Cna.OrleansHost/Program.cs) configures a
Development-only local silo, fails outside Development and exposes a basic endpoint. The
[worker](../../src/Cna.DecisionWorker/Program.cs) registers a gRPC client and runs a host; it has no
decision-dispatch loop. [Legal actions](../../src/Cna.Core/Actions/CampaignLegalActions.cs) already
provide an opaque-handle query/submission boundary, returning a successor handle on acceptance.
This is a candidate integration seam, not proof it supplies every needed persistence operation.
[Technical design](../../tech-design.md) and [naming](../../naming-overview.md) already identify service
scaffolds and distinguish Runner from an Orleans workload; no architectural rewrite was needed.

**Documented facts, consulted2026-09-07 UTC:**

- Orleans loads configured persistent state on activation; writes are explicit and storage errors
  surface through the operation. The framework does not choose Sandtable's event/checkpoint
  publication contract. [Microsoft: grain persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/).
- Application/runtime retries can deliver a message more than once; Orleans does not durably
  suppress duplicates for the application. Retry acceptance therefore needs application evidence.
  [Microsoft: delivery guarantees](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/messaging-delivery-guarantees).
- Orleans supplies test-cluster infrastructure, allowing a bounded host experiment without cloud
  deployment. Select APIs against the repository's pinned package version when implementing.
  [Microsoft: grain testing](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/testing).

**Existing execution evidence:** [Rules9 simulator checkpoint](../research/simulator-post-merge-checkin.md)
retains the786-execution study and the separate12-execution smoke at clean `a15a0a9`. Neither
executes Combat or Orleans. No simulation, benchmark, .NET test or provider experiment was rerun
for this documentation review. Published source documentation does not certify this repository's
provider behavior or choose an optimal schedule.

## Verification and limits

Author checked current task/contract ownership, source-type bindings and active versus historical
status. Validation passed:219 local link targets,5 Markdown anchors,25 stable ordered task IDs
with backward dependencies,72 unchanged design ACs,8 policy IDs and `git diff --check`. No gameplay
policy or frozen contract/fixture bytes change. The check script is retained locally at
`/tmp/check-combat-plan-audit.py`; structural checks do not replace semantic review.

Confidence is high in the observed status/type-boundary gaps; moderate in scheduling recommendations.
Task counts are not duration estimates. Provider choice, hosted crash behavior, exact008 child
sizes and renewed independent-review scope remain unresolved. A blocking host use case, failed
prototype or new evidence about inherited adapters would change the recommended sequence.
