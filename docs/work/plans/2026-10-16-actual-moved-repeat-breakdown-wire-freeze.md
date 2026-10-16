# Private wire-layout freeze: moved-repeat Breakdown

Independent review instance 2 of 3 returned **Ready for private wire-layout freeze** on exact commit `099c397084c1a5603293fd45f5e1c91006d3fc3d`, base `5d0417b150a7b022bf79ea3ecfc3dfe3fa2d1c5d`, in review chat `01a14677-bac3-7062-a0ee-aae3fbd16c6f`. The reviewer found no actionable finding and independently reran focused, capacity, physical and two representative semantic groups. The author's final complete local gate passed 300 rows in 696.749 seconds, with 300 unique event hashes. The reviewer did not repeat that full run.

The private native consumer may now implement this exact wire layout and event derivation:

| Frozen source | SHA-256 |
| --- | --- |
| `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json` | `0568683ecfd1d373a4c428b2ccadf72d826b8ddfc0cc9019ba249b6eb354808a` |
| `docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py` | `952c63c56f49eccb7550a1047eb1074888401b2c781c56cd034b197d16cc6c1a` |

Freeze means schema field order, types, limits, domains, dependency pins, accepted event bytes and error behavior are fixed for the private native implementation. If any frozen source changes, stop native work, rerun affected gates and obtain another independent disposition before continuing. The explicit canonical fixture may be generated from the reviewed reference after semantic GREEN; ordinary verification must never rewrite it.

This does not set `closureFrozen=true` or authorize public readback. Native near-limit process memory, latency and semantic equivalence, fresh independent review, exact-head hosted CI, publication and merge gates remain open. No public Combat, Runner, full72 or release-candidate claim follows from this private freeze.
