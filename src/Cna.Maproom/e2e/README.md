# Maproom browser coverage

Run `npm run test:e2e` from `src/Cna.Maproom`. These tests use the local synthetic fixture and
Storybook. They do not prove a connected campaign or server receipt.

| Player path         | Browser proof                                                                                                                                                           |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Navigate and select | Skip link; keyboard roster and ledger selection; map counter selection; map, roster, inspector, caption and ledger parity                                               |
| Inspect map         | Eight labeled counters; minimum 44px counter targets at 320px; keyboard zoom from 100–200% with disabled bounds; locate and fit; focus pans a clipped counter into view |
| Prepare orders      | Required destination, review, changed-draft invalidation, keyboard issue, per-formation draft retention, pending lock and cross-formation recovery                      |
| Read outcomes       | Accepted position parity; rejected and stale refresh/review; lost-reply readback; unsupported Combat phase                                                              |
| Reflow and access   | 320–2560px widths, short mobile viewport, 200% text, reachable actions, WCAG A/AA axe scan, shared Storybook controls                                                   |

Add a pointer and keyboard browser case with each new Maproom control. Application keyboard
shortcuts, drag/pan gestures, search/sort ledger, filtered empty state, live audience privacy,
network receipt/reconnect and reduced-motion transitions have no fixture implementation yet. Their
tests belong with those features; do not treat this fixture suite as their acceptance evidence.
These assertions do not verify visual consistency across views. Field Atlas is the shared visual
language; review wide and narrow screenshots when changing map, inspector, ledger or diary
presentation.
