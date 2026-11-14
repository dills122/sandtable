# World6 ZOC derivation

The normal World6 movement and Combat path derives controlled locations from
the existing CNA ZOC and movement rules. Independent combat representations are
grouped by side and location. Their supported stacking values and current raw
defensive Close Assault points determine each representation's qualification;
qualified sources project only over supported, enterable adjacent edges.
Unsupported classifications, components, topology or stacking values fail
closed. A source with at most one stacking point cannot qualify and is skipped
before combat component evaluation.

For this ZOC qualification only, source-backed infantry, armor, reconnaissance
and anti-tank components with unparenthesized defensive
Close Assault ratings contribute current TOE multiplied by that rating. Land
§§10.15 and 11.15–11.34 define the raw-point threshold and calculation; the
September errata's conditional ratings remain excluded. Anti-tank is treated
as forward for this purpose under §10.15. Artillery requires its forward/back
status under §12.1 and remains unsupported until that state is represented.
Unknown classes, parenthesized ratings, anti-aircraft and unresolved attachment
or component-pool authority still reject. This arithmetic does not admit those classes for Movement,
Barrage, Anti-Armor or Close Assault.

Existing Content6 admission still forbids multiple combat counters of one side
in a location, so admitted Content6 behavior remains unchanged. This calculation
does not admit a historical setup or decide its stacking and attachment rules.
The legacy player observation and Combat owner cut fail closed on positive ZOC:
publishing it requires a contact-safe projection and must not expose hidden
opposing authority through controlled hexes or offer presence.

Land §10.16 requires disclosure when a phasing unit begins a Movement Segment
in, or enters, a hex a particular opposing unit could control. A successor
should record that scoped disclosure when it applies the authoritative segment
or move, and replay the same disclosure to the affected side. It must not expose
the entire derived control map when an owner asks for a movement offer. The
current contact journal records adjacent presence but does not retain this
per-unit ZOC disclosure, so it cannot yet justify positive-ZOC player actions.
