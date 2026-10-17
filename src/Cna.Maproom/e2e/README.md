# Maproom browser coverage

Run `npm run test:e2e` from `src/Cna.Maproom`. These tests use the local synthetic fixture and
Storybook. They do not prove a connected campaign or server receipt.

| Player path         | Browser proof                                                                                                                                                                                                                                                       |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Navigate and select | Skip link; keyboard roster and ledger selection; map counter selection; map, roster, inspector, caption and ledger parity                                                                                                                                           |
| Inspect map         | Eight labeled counters; minimum 44px counter targets at 320px; keyboard zoom from 100–200% with disabled bounds; locate and fit; focus pans a clipped counter into view; shortcut help, selection, zoom, fit and locate; focused arrow-key pan and pointer drag pan |
| Prepare orders      | Required destination, review, changed-draft invalidation, keyboard issue, per-formation draft retention, pending lock and cross-formation recovery                                                                                                                  |
| Read outcomes       | Accepted position parity; rejected and stale refresh/review; lost-reply readback; unsupported Combat phase; selected/all-visible diary scope and formation-specific locate actions                                                                                  |
| Reflow and access   | 320–2560px widths, short mobile viewport, 200% text, reachable actions, WCAG A/AA axe scan, shared Storybook controls                                                                                                                                               |

Add a pointer and keyboard browser case with each new Maproom control. Ledger search/sort,
filtered empty state, keyboard selection, narrow labeled records and accepted fixture diary updates
have browser tests. Live audience privacy, network receipt/reconnect and reduced-motion transitions
have no fixture implementation yet. Their tests belong with those features; do not treat this fixture
suite as their acceptance evidence.
These assertions do not verify visual consistency across views. Field Atlas is the shared visual
language; review wide and narrow screenshots when changing map, inspector, ledger or diary
presentation.

The 1440px and 320px fixture screenshots from this slice were inspected: map, inspector, ledger
and diary use consistent paper/ink surfaces, type hierarchy and selected-state treatment. This
review covers those two default layouts, not hover, loading or connected-play states.
