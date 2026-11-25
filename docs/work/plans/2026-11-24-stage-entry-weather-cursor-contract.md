# Stage Entry weather random-cursor contract (2026-11-24)

**Scope:** Issue #578, synthesis proposal 1, and the adjacent Initiative Declaration replay
check (C5). Applies to the Stage 1→2 and Stage 2→3 native preambles (V49–V54). It does not
activate Host/HTTP weather or add any Stage 3 Reserve/Movement/Combat mechanics.

## Authority and draw order

A campaign has one deterministic random byte stream. The retained V11 turn snapshot supplies
its random contract version, algorithm, seed, and the starting cursor after the legacy opening
preamble. For native Stage Entry and repeat-side mechanics, the next cursor is the successor
cursor of the **last accepted random-consuming event in chronological retained history**:

- `CampaignNativeCombatAssaultResultV22`: `AfterRandomState`.
- `CampaignNativeWeatherDeterminationV50`: `ResultRandomCursor`, with the same V11 stream
  identity (contract version, algorithm and seed).
- No such event: use the V11 snapshot's random state.

`ExerciseSession.LatestCursor` resolves this rule. Non-drawing events do not advance the cursor.
Use chronological order, not the maximum cursor or a per-phase random stream. No caller may
supply a fresh seed or choose a cursor for a native weather completion. Stage Entry selection
and the second-side frame use the same rule; selection itself does not draw.

## Admission and reconstruction

Core weather completion derives its input from the predecessor's accepted history, validates
the retained initiative cut, and constructs V50 using the existing weather resolver. Its
successor appends the event; subsequent callers derive the cursor from that event. Both
preamble occurrences follow the same rule.

On replay, first verify the predecessor chain. Derive the input cursor from the **predecessor**
(not the event under verification). A mismatch with V50's `PriorRandomCursor` returns
`InvalidHistory`; regenerate the event from the derived state and compare canonical bytes and
contact history, including the recorded result cursor. A self-consistent event constructed
from a different input cursor is not accepted as valid history. Initiative Declaration replay
likewise derives the holder from the retained snapshot and the terminal predecessor from the
accepted history, rather than trusting the event's claimed holder or a stage-specific source.

## Compatibility and verification

EVT-001: V50 canonical field names, ordering, encoding, versions, serialization and
deserialization remain unchanged. This defines the as-yet-unwired native weather cursor
value, not a format migration. DET-001/DET-002: every draw is reproduced from the retained
stream identity and verified history, without wall-clock inputs, ad hoc randomness or IDs.
The first legacy preamble and pre-Stage Entry draw rules are not reinterpreted.

Focused verification: real-session Stage 2/3 weather completion and reconstruction, replay
rejection of self-consistent wrong cursors and wrong holders, and chronological cursor
continuity across a prior assault result and subsequent weather event. No full-suite soak.
