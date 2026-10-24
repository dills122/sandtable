# Native Reserve Release timeout v1

Native event contract 35 (`reserve-release-timeout`) is an explicit System disposition batch. Host accepted-step contract 29 uses `native-reserve-release-timeout`. Existing owner choice/completion contract 27 bytes remain unchanged.

At Reserve Release entry, bind a durable deadline to the entry event hash, current cycle and timing configuration. Accepted member choices advance authority but never restart that deadline. Deadline or controller/clock unavailability converts each still-pending Reserve I member to Reserve II in cycle 1; later cycles retain each still-pending Reserve II member. Prior owner dispositions and movement allowances survive. The fallback emits no owner choice, adds no movement allowance, and changes no location, TOE, ammunition or RNG state. It completes Reserve Release in one state transition, leaving cycle control next.

Event fields: campaign ID, prior state version/hash, stable `releaseEventHash`, cycle ordinal, sorted unique `convertedElementIds` and `retainedElementIds`, result world hash, next position, fallback `convert-unresolved-i-retain-ii`, configuration hash, opened/deadline/high-water timestamps, optional cancellation timestamp and reason. Cycle 1 records only converted IDs; later cycles record only retained IDs. Replay recomputes the exact unresolved membership, resulting World and canonical bytes. Duplicate/stale/foreign or changed-member events reject.

Reasons: `deadline`, `clock-unavailable`, `clock-regression`, `controller-unavailable`, `reserve-release-unavailable`. Unavailable entry carries all three stored timestamps as -1 and no cancellation timestamp; absent timing uses the all-zero SHA256 sentinel. Other reasons require a valid opening and high-water timestamp strictly before deadline. Deadline requires observed time at/after deadline; regression requires observed time below high-water. Core binds configured budget, ruleset and content; Host binds persisted opening/history.

No public certificate admission or original-root support is implied.
