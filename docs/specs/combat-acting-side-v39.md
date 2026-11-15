# Combat selection when initiative holder acts last

Land 7.16 lets the initiative holder choose the first-acting side. The owner of
the first Movement/Combat phase is therefore the `FirstSide` recorded for that
turn and stage; it is not necessarily the initiative holder. Contact offers,
accepted decisions and the no-selection timeout must use that recorded side.

New act-last selections use event contract 39. New act-last selection timeouts
use contract 40. Existing act-first decisions and timeouts retain contracts 12
and 38 and their byte representation. A replay of an older event must apply the
policy recorded by that event, and normal Host re-adjudication must distinguish
historical evidence from new submissions. No private opposing state is added to
the owner view or public action identity.

This corrects the existing normal campaign path. It does not admit scheduled
content, extend play to later stages, or certify the Graziani scenario.
