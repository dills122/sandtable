# C4020 six-side source review

Reviewed [six-side audit](2026-11-17-graziani-c4020-six-sides.md) against original Map C, PDF page 3, and terrain key, PDF page 1. PDF SHA-256 matches `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; reproduced crop matches `12c4dee6eaef719c800c049db23aa15fb383d54971ae5c52cd41cfeee64afe97`. Both pinned JSON hashes match audit.

Fort Capuzzo's blue point lies inside depicted C4020 hex. Six sides and neighbor IDs agree with visible numbering and grid sequence; C4121 and C4021 labels are obscured, so their identification depends on that sequence. Side descriptions agree with visible marks, subject to overprints:

| Neighbor | Independent visual check |
| --- | --- |
| C4120 | Paired road lines cross upper-left side close to upper vertex. |
| C4121 | Dark escarpment area adjoins upper-right side; dashed track crosses it. Grid and marks partly covered. |
| C4021 | Paired road lines cross right side; blue checks and brown/dark terrain ink cover portions. Slope extent under overprint remains uncertain. |
| C3921 | Dashed track and blue crossed line intersect lower-right side near lower vertex; blue checks cover much of side. |
| C3920 | Dashed track reaches lower-left side near left vertex, where text and other lines overlap. |
| C4019 | Dashed track and blue crossed line cross left side amid overprint. |

Key depicts paired-line road, dashed track, crossed unfinished railroad, dark escarpment, and brown slope. Blue crossed route matches unfinished-railroad symbol; blue checks do not directly match gray Border sample. Geographic boundary interpretation is separate from symbol identification. Coincident grid, routes, checks, labels, and terrain ink prevent reliable exclusion of hidden features or independent determination of `up` direction. This scan cannot establish full terrain classification or movement legality.

Pinned hexside records contain exactly the six pairs and feature/`up` values quoted in audit. Pinned hex record states `clear`, `village`, and `coastal: false`. These are data checks, not independent confirmation of every terrain claim. No runtime changes or tests.
