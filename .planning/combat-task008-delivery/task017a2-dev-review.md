# Task017A2 development review

Root read frozen spec/schema/oracle transitions and tests, then full implementation before freeze.
A1 base validation methods remain unchanged. Lifecycle owns copied collections; private Transition
is reachable only through independently validated base and full trusted-history replay. No supplied
Control object bypasses replay. State reader validates malformed raw bytes before context and
compares complete derived bytes; no hash-only campaign provenance or World projection claimed.

Checked first-I release/convert, later-II release/retain/bulk complete, canonical queue and no revisit;
Config-pinned budget/deadline and high-water; equality rejection; clock-failure System author with
original input actor; fallback lock; exact retry before stale/terminal; no-op old timers; completion
after last choice; one-consumed conversion; preserved CP/CPA/RNG/World hash/attack history and prior
exceptions. Receipt is unsigned-event digest before linked history and prefix includes full event.
Member32/event34 capacity and checked ordinal/version overflow reject before result publication.

Behavioral RED first opening confirmed against compiling no-op kernel. Negative raw test exposed
numeric effect tag throwing InvalidOperationException; ID validation now supplies JsonException
before touching null context. Replay event byte bound moved before copy to avoid copying oversized
candidates. Final focused lifecycle10+A1six passed16/0/0; tests cover all44isolated fixture traces,
132event hashes,176state hashes+lengths,two terminal literals and explicitly exclude historical4.

No remaining source defect identified by root self-review. This is not an independent verdict.
Reviewers and full gates own separate acceptance evidence. Parent017 and actual integration remain
open. Ancestry-only rebase3811202→a4b4f63 changed no file bytes; main32a038c tree equals oldbasea1cd425.
