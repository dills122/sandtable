Initial frozen A3b review: REQUEST CHANGES.
P2 terminal_view3b reads only authorityArms.members (oracle line 2609), dropping authorityArms.releaseMember used by both reserve-second-movement-completion sources. Own actors receive ownReserve:null despite actual released-I membership at CP2 with expired next-Movement exception. Retained fixture reproduces both omissions. Add exact tagged source extraction and regression asserting full actual member facts for both owners, while opposite audience remains null.
No other actionable finding in initial static/probe review.
Independent initial probes passed terminal56/history4512/policy4688/finish88/fallback32/retry648/privacy48/source696. Terminal probe used arms.members and therefore did not cover releaseMember; these successes do not negate finding.
Compatibility passed144 exact old functions/classes; accepted schema/fixture sections unchanged; five bounded extensions inspected.
Initial full61-group oracle deliberately interrupted after source finding/root instruction; exit130/KeyboardInterrupt in existing A2 admission matrix. No full-suite pass claimed.
Initial SHA256:
md 8f2ac6aca621bee6dbf0b30cd616808dba05bd33b8308946415305606b3728f4
schema fbca44fef91b806906b3287bb851e5efb92e735876af7a6b755d2873f625b664
fixture 38199b79f3d474775584ffa57d7f4efbb8455743f9ca139dc5f4e23ce1e4be44
oracle 6b190a7cebd55f4e430796da001cf64925aeb418ec0150d3a1ecdf9ee750cad3
