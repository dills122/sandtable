# R02 feasibility checkpoint — stop before generation/native

Timestamp approximately18:36UTC. Main R01 gates fully reconciled; conditionalR02 branch codex/combat-repeat-movement-completion created clean from3a7b62645b377326ed2318dfa81cfe66107f4445. Original R01 branch retained. No repository file modifications, tests, fixtures, generation, or native work. Heavy reference lease released to Brain after measurements.

## Actual measurements

Two genuine R01 ancestry samples per profile (first root and largest initialSource root), publication-only and max moves, three independent full replays each:24replays total. Every call authenticates original request and all nine ledger inputs; no warm replay cache. Python module import/canonical fixture reconstruction is outside measured replay timer; physical dependency checks remain inside replay.

Largest-root medians/maxima seconds:
- paidzero0.341558/0.343633 (42ancestryinputs)
- paid2moves0.352089/0.354378 (44inputs)
- armedzero0.187111/0.203457 (35inputs)
- armed5moves0.188392/0.197375 (40inputs)

These are representative full ancestry observations, not full300-rowR02 verification or guaranteed worst-case runtime. Python timing does not establish native hosted runtime.

## Native hosted cost forecast

Read actual PR38779337962attempt1 core-b XML, not shard wall-time. R01 contract69-row collection about659s; its exhaustive parity56rows consumed624.391s. Method performs initial replay, two ancestry replays per accepted Apply, one fresh terminal rejection, and two prior retries:4+2N replays per root.12paid*N3 +44armed*N6 =>824calls. Observed blended0.757756s/call, including own method overhead; not an isolated microbenchmark.

R02 required300roots,788events,1088cuts. Initial plus two replays per accepted event =>300+2*788=1876full ancestry calls. Straight R01 blended projection1421.55s=23.69min before extra negatives and own-family deeper work. Exhaustive all-prior retry at owncuts adds56*1+244*(1+2+3)=1520calls, total3396; projected2573.34s=42.89min. No uniform-speed assertion; this is conservative planning evidence that existing placements lack credible headroom, rather than proof of exact future duration.

Existing caps:core-b25min shared with existing suite; other collections15min. No existing allocation credibly fits one complete requiredR02collection. Valid isolation groupings must be explicitly owner-reviewed and measured; compact fixture references solve bytes, not CPU. No hidden row split, cache, sampling, timeout expansion, or weakened coverage.

## Next gate

Brain decision required: deferR02; explicitly design separate valid independent collections/shards and measure allocation before full implementation; or explicitly revise accepted semantic scope. This checkpoint does not claim referenceRED/GREEN, native tests, contract freeze, or delivery acceptance. Proposed scope remains a local planning artifact; reconstruct and obtain accepted allocation design before implementation. Raw measurement scripts/logs/JSON stay local.
