# P01 private Weather carry-forward evidence

The P01 reference replays the original CreationRequest and eleven R03 ledgers before reading
Weather. It follows the fixed source ancestry to the positive-entry prefix and selects its one
`weather-determined` event. The private fact carries `kind`, game turn, operation stage, receipt ID
and SHA-256 of the exact canonical event bytes. The event must be Normal for turn 1/stage 1 and its
receipt and hash must match the authenticated R03 terminal. Missing, changed, foreign-receipt and
changed-stage cases reject before physical classification.

This bridge does not modify the private candidate preimage, assessment serialization, or outward
action surface. It proves the selected current Weather fact for the bounded same-stage profile;
AC-003 source-legal action exhaustiveness remains open. Every P01 assessment remains unsupported
and unadmitted. See `test-combat-p01-weather-bridge-v1.py` for genuine-source and mutation tests.
