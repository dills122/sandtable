# Actual assault admission assessment v1

Private admission-only checkpoint of DAY-C1. The intended later paid actual-assault reader will
consume this authenticated assessment as a prerequisite, then independently certify complete
actual result/settlement support before any debit. That consumer is planned, not implemented.
This contract does not grant paid, native, public or settlement authority. Complete paid C1,
Reserve completion, repeated Movement, later-II/consumed lineage, Snapshot/Archives recovery,
public audience authentication, Exercise/Runner and full Combat-loop completion remain open.

## Authority and callable boundary

Only the two original creation-rooted seed1/cursor2 prepared ARE25 sources and both genuine
seal orders are admitted. Require full original Request/Created, twelve entry events, seven
actual-selection events and five round events, accepted defender decline, two full allocations,
FA/empty-AA receipts, five retained step receipts and unchanged original World/RNG. Status must
be Prepared, stateVersion25, stepIndex5, not cancelled/closed/paid; all original independent
ledgers authenticate their own ordered causal inputs. The selected laboratory roots remain
laboratory content, not validated historical scenarios.

The private reference APIs are:

```
read_source(data, selection_ledger, round_ledger, assault_ledger)
proof(data, selection_ledger, round_ledger, assault_ledger)
read_proof(claim, data, selection_ledger, round_ledger, assault_ledger)
```

`data` and `claim` are full canonical ASCII bytes. All three ledger arguments are mandatory.
Selection and round ledgers are independently retained caller inputs; event-embedded actor/time
cannot authenticate them. The assault ledger must have strict list type and be empty. This
proves exclusion of new assault inputs, not nonempty assault actor/time authentication,
deduplication or recovery. Caller authentication of independently supplied context/ledgers is
an ingress precondition, not public seat authentication. The unchanged fixed creation context
is obtained from enforced original dependency bytes, as in the predecessor oracle.

Use ARE's public read_source/proof admission, never its fixture trace/start helpers or old
synthetic RoundState/context admission. A new source is neither a digest-only proof nor a
caller-derived Base/terminal World. Every source/proof readback authenticates full history;
proof claims are compared against a freshly derived complete proof and arithmetic assessment.
Return owned objects. No new cache, event, command API, receipt, cost, draw, commitment, result,
settlement, window, clock policy supplement or authority permission is added. Closed source or
proof readers reject supplied paid/old-family arms without mutation. Existing ARE commit-attack
rejection remains a regression; it is not the candidate's semantic RED.

## Closed bytes, limits and evidence identity

The [ordered inventory](combat-actual-assault-v1.schema.json) is the executable grammar, not a
JSON Schema implementation. AdmissionSource is ordered contractVersion,
actualRoundSourceCanonicalUtf8. AdmissionProof is ordered contractVersion,sourceId,sourceHash,
full actualRoundProof,arithmeticAssessment. ArithmeticAssessment retains exact ordered fields
from the inventory; scopeKind is `pure-selected-arithmetic-v1` and paidAdmissionEnabled,
nativeSettlementCertified,authenticSettlementCertified are invariant false. Missing/changed
capability fields reject; no switch can enable payment. Inherited ActualRoundProof/World/Result
records retain their exact grammars and original identities. The inventory lists each reachable
record version: inherited Position is5, World is7, other admitted record versions are1.
Inherited PE records keep their signed int/long domains and original grammar namespace;
ARE records retain nonnegative int/long domains. UTC, unsigned and future-turn primitives
retain their original bounds. The candidate checks these fields before claim equality.

Reject duplicate/missing/unknown keys, wrong primitives, booleans as integers, floats/exponents,
non-ASCII/BOM, whitespace, alternate escapes, reordered raw keys, trailing documents and
out-of-bounds values. Canonical encoding is compact ASCII JSON in declared field order,
explicit nulls, and unchanged inherited array ordering. Individual source/proof/input carriers
are at most1MiB, recursive depth32 and ordinary arrays512. Original selection and round input
capacities16 apply independently, no global16 truncation. New assault capacity0 is a trust
rule: any nonempty strict list rejects004, regardless of length. Missing Python ledger argument
is invocation misuse TypeError outside the encoded error contract. Candidate maximum accepted
shape/type/ledger size checks remain mandatory before readiness; nominal limits alone are not
representability proof. No paid maximum-history or suffix-ID capacity is certified.

New source identity is `aaadm.` plus SHA256 over ASCII
`sandtable.combat.actual-assault.admission-source.v1`, one NUL byte, then complete canonical
AdmissionSource. sourceHash is the same digest with `sha256:` prefix. ActualRoundSourceHash
within the assessment is the original ARE proof sourceHash, not the new wrapper sourceHash.
There is no standalone assessment hash, self identity, or canonical-plan digest dependency.
The closed admission-only grammar must remain distinguishable from any future paid family;
adding paid arms later is not inferred wire compatibility.

## Streamed arithmetic evidence

Canonical RulesInput is independently recomputed from pinned current source ranges and accepted
CMB-SRC-RUL-001 using the unchanged normalizer. Validate the complete grammar and canonical
ordering. Its30395-byte encoding at the current freeze hashes to
`sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029`, exactly the
original Config1 rulesInputHash. Tuple-key internal tables are never serialized as a new
normalization grammar. RulesInput carries36 Morale cells,360 loss cells,five effects and six
capture shares; its arrays retain inherited≤512 constraints.

The catalogue contains16616 indexed records:1296 ordered Morale pairs,6480 ordered assault
cells across all five differentials,8840 conditional capture/refusal variants. These are pure
isolated combinations, not16616 actual campaigns/native settlements or637 deduplicated semantic
summaries. Counts alone are insufficient. Every expected index, exact record key, facts and
independent rounding/conservation must match; missing/duplicate/reordered/foreign/changed
records reject even when nominal counts match.

Stream header order is kind,catalogueVersion,rulesInputHash,dependencyManifestHash,moralePairs,
assaultPairs,captureRefusalVariants,totalRecords. kind is
`pure-selected-arithmetic-catalogue-v1`, version1. Exact record-arm keys are in the inventory.
Ordinal is global0..16615. Morale section orders attacker then defender two-die coordinate11..66
(valid digits1..6) ascending. Assault section orders differential−2..2,attacker coordinate,
defender coordinate. Variant section uses that same ordering, then captureDie null singleton
when no role triggers or die1..6 when triggered, then refusalHexes0..requiredRetreat ascending.
Capture triggers are role-specific sum sets; simultaneous role triggers are invalid. The
conditional ninth-die variants are isolated enumeration, not actual RNG advancement here.

Attacker loss uses ceiling of TOE10×tablepercent/100. Defender loss uses floor of
TOE10×(tablepercent+10×refusalhexes)/100. Captured TOE is ceiling of rounded role loss×share/100
for the captured role only. Other loss=loss−captured; remaining=10−loss; lossDP3 iff loss≥3.
Check0≤captured≤loss≤3 and each original10=remaining+captured+otherloss. Compare existing pure
result facts and independently rounded Fraction arithmetic; representative retained literals
also guard source drift. Geometry, actual retreat/CP/RP, guard transfer, custody routes,
mandatory windows, future duties and authentic settlement publication remain unproved.

Catalogue hash streams ASCII `sandtable.combat.actual-assault.catalogue.v1`, NUL, compact
canonicalASCII header,LF,then each compact canonicalASCII record followed byLF. No BOM/CRLF,
spaces or omitted finalLF. Each record≤4096bytes; never embed6480/8840 records in a512-item
array or retain a required full6.8MB buffer. Proof contains only the independently recomputed
hash/counts; supplied catalogue hash/assessment is not trusted. Optional retained JSONL is test
evidence, not source admission. Recompute for readback, no new catalogue cache.

## Physical dependencies and error order

The inventory freezes59 runtime physical paths: all57 unchanged ARE enforcing paths plus ARE
oracle and schema. Preserve all original57 even when a particular helper is only hashed rather
than imported. One separately pinned ARE fixture is test-only original source/ledger/literal
input; it is not a runtime whitelist or alternate caller ledger. Own schema/fixture delivery
hashes are independently frozen for review without a self-hash cycle. Runtime manifest hash is
SHA256 over canonical path→digest object sorted by ASCII repository-relative path, excluding
test-only input. Actual candidate import/read/branch/test tracing must confirm this closure
before new literals. Unknown material files return to freeze; no pin refresh is allowed.

Check exact physical bytes before cold/warm ingress/retry/readback, including inherited caches,
and again at material Content/rules use and before publication of derived evidence. Missing,
changed or unreadable bytes reject009. Current matching physical hashes do not imply passing
all owning oracles. The original four owning failures remain unwaived hard activation/completion
blockers under the accepted private dependency disposition; no blocked check_fixture/read-root/
outward admission may authenticate this checkpoint.

Dependency009 precedes grammar/cache access. Encoded source shape/type001,primitive bounds002,
version/family003,canonical spelling008 precede independent ledger/source admission. Wrong
selection/round type/capacity001 and assault wrongtype001/nonempty004 are distinct. Full original
trust/prepared-history mismatch maps004. Supplied proof is parsed only after authenticating
its ingress; valid coherent claim/assessment/catalogue divergence maps006. Fixed-false capability
change002,missing key001. Initial deliberate rejecting stub007 was semantic RED only; it confers
no paid API. Competing-error tests and every leaf must verify these paths.

## Verification and remaining gates

Commands select focused admission, independent indexed mechanics, recovery, or complete normal
verification; normal operation never regenerates goldens:

```
python3 -B docs/specs/verify-combat-actual-assault-v1.py --check-admission
python3 -B docs/specs/verify-combat-actual-assault-v1.py --check-mechanics
python3 -B docs/specs/verify-combat-actual-assault-v1.py --check-recovery
python3 -B docs/specs/verify-combat-actual-assault-v1.py
```

Four genuine proofs, original immutable World/RNG, full inherited claim/input/event mutations,
coherent local re-signatures, all earlier/cancelled cuts, malformed/capacity/ownership/error
matrices, cold/warm/at-use physical dependency probes and exact fixture identities are required.
Full original ARE owning oracle remains separate (~31m38s observed,≥35min budget), followed by
selected direct affected owning regressions under measured budget. Focused ingress is not that
gate. No.NET/native/public/loop acceptance is inferred. Author engineering self-review, immutable
five-primary evidence freeze, Brain-dispatched fresh implementation review and exact-head CI
remain required before any readiness/merge claim. Exact verification boundaries and results
are retained in the dated review evidence, separately from author rationale.

Paid identity compatibility is deferred: existing CombatAssaultResult uses assault-result.v2 and
CreateResolvedResultV2 enforces settlement.v2, not arbitrary new actual domains. Matching cmt/res/
set prefixes cannot solve that constraint. Existing historical World projector reconstructs
INITIAL; matching arithmetic does not authenticate actual World projection. No shared type,
constructor, predecessor grammar, profile, seed, rule pin or old golden changes here.

### Declared union diagnostics after implementation review2

Closed inherited claim validation includes every declared LifecycleFlow alternative, including
`moving`, `phasing-stop` and `idle`, in the original PE/lifecycle grammar namespace. Signed
integer limits apply inside alternate arms before canonical spelling008 or coherent-claim
equality006; unknown string tags are family003 and malformed tag types are shape001. Empty
arrays and nullable records retain their original accepted shapes. The declared-grammar audit
includes records hidden by baseline nulls/empty arrays and checks every terminal special delegate.
Alternate-arm tests use authentic ingress with independent ledgers and complete shaped claims;
accepting their grammar does not authenticate their semantics or enable an assault.
