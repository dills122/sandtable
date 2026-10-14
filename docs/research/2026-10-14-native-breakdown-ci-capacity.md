# B03 bounded CI capacity proposal

Decision requested from Brain before any partition edit. Frozen native productbfc4cfe357529b4bdfe5d997b09d70367824edd4,
66newnative rows/all66passing217.248s/900; no native/reference defects found. Current compiled five-leaf
discovery3009=71A/2395B/64C/469Exercise/10Contracts; current policy puts both new full classes in B.
No new shard, deadline increase, coverage exclusion or test parallelism change is proposed.

## Measurements and method

Sources /private/tmp/b03-capacity-proposal.py/.json retain XML source SHA256, class row count/summed
cost/observed start-end spans and four-worker observed-order schedules. Parent exactPR185/run38517061509
be149head: Bjob958s/assembly865.184s; Cjob440s/assembly346.396s. Existing xUnit collection-per-class,
four hosted collection workers. Parent B model uses max(class sum, observed span), ordered by earliest
observed class start, schedules onto earliest available worker. Compare its reproduced baseline and
error in JSON before interpreting variants. Job overhead is observed job minus assembly, not invented
setup work. No heavy/broad suite was run to populate this model.

Old local frozenRMC64 gate assembly251.718s, Basicclass251.609s/Contract133.572s; hosted same classes
346.194s/164.967s. Cost uses max(sum, span); observed ratios1.375975 and1.235156. New local NBC66
classes sum215.703s and212.511s, concurrent gateassembly~215s. Forecast corresponding hosted new
collection costs296.867s and262.521s. Native class runtime has not been measured hosted; ratios from
matched predecessor family are projections and can be wrong. Local ARM versus hosted x64, .NETpatch,
worker count/resource contention/order/GC are real differences; host-specific causes unmeasured.

## Alternatives

Retain B: existing-observed-order with new full classes appended or prepended projects job1078.312s
(17m58,121.688sheadroom) or1085.868s(18m06,114.132sheadroom), under20minutes. A deliberately different
alphabetical collection order projects1215.715s(20m16), above20; this reorders old collections too,
not a claim it is the runner's current default. Native additions can alter order/GC/contention, so
one ordering's spare time is not a guarantee. Preserve all current caps if choosing actualBexperiment;
a failure requires exact progress/timing evidence, not blind rerun/cap increase.

Move the two new complete NBC classes to C: four total full completion collections on four hosted
workers. Existing maxcollection cost346.264s dominates forecasts296.867/262.521s, so observed-overhead
projection is439.868s(~7m20) under15minutes. Uniform contention sensitivity1.25x/1.5x/2x across allfour
collections gives526.434s/612.999s/786.131s; even2x retains113.869s under15minutes. This is a sensitivity
experiment, not measured four-collection execution or a memory guarantee. B remains its prior2329rows
and observed15m58 job without the new2collections; A unchanged. New C130rows = old64 + new66; union
3009=71/2329/130/469/10. Tests/future methods in four exact fully qualified classes follow C;
unrelated/similarly named/future other classes remain B. No row snapshot allowlist.

Recommendation: move both full NBC classes into existing C after Brain acceptance, rather than
risk losing B00 margin on B's longest collection tail. Preserve matrix/deadlines (B20,others15), A
predicate/eight old transfers, all exactSHA/SDK/config/attempt/process/dependency/disjoint/exhaustive/
actualXML gates. Operational cost is greater C concurrency and possible GC/fixture memory pressure,
not another VM or widened scope. Native allocation observations are cumulative1.06–1.10GB/replay,
not live heap; no peak/near-limit resource guarantee is claimed.

## Bounded implementation/verification if accepted

Only .github/scripts/test_partition.py CORE_C_CLASSES append two exact full class names, plus helper
policy/fixtures/tests to require futureclass membership, unchanged A and complementary B. Workflow
matrix/caps and verifier schema/process/dependency bindings stay unchanged. Genuine focused helper
RED before policy edit; all117+newhelper tests/actionlint/five-leaf actual compiled discovery <=120s.
Native66source/compiled final receipt remains valid if native/test/reference bytes stay frozen; no
repeat native/fullnormal/broad suite for a CI predicate-only edit. Report new exact71/2329/130/469/10
and native88fingerprints unchanged. Full combined diff/selfreview/freshreview/hostedactual3009 then
Brain merge. No other native feature, profile, pin/cap/runtime refactor or new shard.
