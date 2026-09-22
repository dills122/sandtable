# Task017B implementation report

Owned source paths only:
- `src/Cna.Core/Campaigns/CampaignCombatResultRelease.cs`
- `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs`

Root confirmed compiling behavioral RED in `/tmp/017b-red.log` before full implementation.
Skeleton Core build passed; completed implementation Core build passed zero warnings/errors in
5.87s (`/tmp/task017b-build.log`). Root owns focused behavioral tests, full gates and fresh reviews.

## Authority and ownership

Owned source envelope copies Created and every event byte array at construction and access; typed
input arrays copied and exposed read-only. Nested records/World/context values use existing owned
immutable contracts. No cache and no imported arbitrary predecessor metadata envelope exists.
Complete native Result2 replay reexecutes trusted Created/boundary/selection/Round2/Result2 tuple.
Static compatibility catalogue pins 32 named cases, 28 distinct replay-derived committed hashes,
and exact Result2 command kind/choice/actor signatures. Canonical Base/receipt chains transitively
bind upstream inputs and events; this proves typed canonical authority, not raw external metadata.
Catalog literals extracted from frozen Result2 fixture at implementation time; no runtime fixture IO.

Checks require completed immediate settlement/CA/round receipts and no owner window, reject every
fallback reason beyond owner-choice/not-required/null, and check every choose author's actual
owner and accepted payload kind. Reliable retiming remains possible; Result2 native replay validates
clock semantics. Future obligations remain untouched.

Release base derives actual settled selected attacker membership, creation/content side binding,
spent CP, CPA10 within pinned profile, full canonical World hash, RNG, final version/prefix, actual
CA receipt, exact same-slot Release position and committed attack history. New high-water null.
Codec adds settled-empty structural profile only (one own none-status member, entirely empty
history, null high-water). Existing isolated validation continues for both profiles; generic codec
expected-value trust boundary unchanged. Adapter authenticates source independently.

Only native untimed System open/complete admitted (at most two retained events); native kernel
owns receipts, versions, prefixes and retry behavior. Duplicate returns unchanged state/original
receipt with null EventBytes, matching A2. Source World stays intact; adapter advances no cycle.
Raw base/state/suffix syntax checked before source derivation, then complete byte comparison/replay.

## Scope held

No kernel/models/schema/fixture changes, no Movement proof/cycle control/public activation or
creation-rooted positive provenance. Root owns all tests/docs/gates. No commits made by worker.
