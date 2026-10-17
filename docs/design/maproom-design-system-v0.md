# Maproom Field Atlas design system v0

Date: 2026-10-16. Field Atlas direction accepted by the owner. Token values and component treatments
are an evolving baseline, demonstrated in the [operations workspace preview](maproom-style-study/index.html).
The owner's accepted review refinement is incorporated below. Production Angular implementation
belongs to MAP-02 and MAP-03A/B/C in the [delivery plan](local-game-server-maproom-plan.md).

Maproom should feel like a working field atlas: warm paper, legible ink, useful annotations and
restrained military symbols. Its primary job is to let a player inspect what their side knows,
understand available orders, review one, and issue it with confidence. Local playtesters and
strategy-game players are the first audience.

## Directions considered

| Direction | Character | Decision |
| --- | --- | --- |
| Field Atlas | Warm paper, printed-map hierarchy, ink rules, sober unit counters | Selected starting direction. Reads clearly during long planning sessions and suits the campaign setting. |
| Operations Room | Dark graphite, amber instruments, compact command panels | Retain as a possible future dark treatment after Field Atlas semantics are proven. |
| Modern Cartography | Pale surfaces, crisp teal, light visual chrome | Useful reference for clarity; keep its restraint without losing the atlas character. |

The preview now concentrates on a working operations layout rather than a scrolling style page.
Its controls vary compact versus comfortable density and stage order outcomes; they do not
introduce game rules. Map, inspector, roster, ledger and War Diary share one fixture selection.
All units, map geometry, routes and messages in the study are explicitly synthetic presentation
fixtures. No live server, save, legal-action or combat claim is made.

## Accepted design-review refinement

Combine Field Command's compact operational framing with Cartographic Archive's paper, ink and
printed-counter hierarchy. Keep Field Atlas as the identity and the existing Angular stack.
Use an olive-tinted command frame sparingly, readable paper records, a persistent map and
contextual controls. Avoid glossy hardware, distressed surfaces and decorative window chrome.

The useful research contribution is workflow: collapsible panels, map-linked reports and focused
information layers. [Command's UI redesign](https://command.matrixgames.com/?p=4954) describes
collapsible inspectors, per-unit message histories and map-linked logs.
[Unity of Command's supply diary](https://unityofcommand.net/blog/2019/11/05/developer-diary-25/)
demonstrates a dedicated supply view. These are interaction references, not Sandtable rule sources.
Supply networks, logistics screens and configurable role workspaces remain future probes after
their authority contracts exist. No new game system is implied by a navigation label.

Prove the system in three representative views: operational map, order workflow and dense
formation ledger. Build a minimal token/control foundation first, then evolve it alongside those
views. Expand the component showcase from demonstrated needs rather than designing a large
generic library in advance.

## Visual language

- Let the map dominate. On desktop use a narrow formation list, broad map, and contextual order
  inspector. A compact phase/seat header answers whose decision is pending and what happens next.
- Use horizontal rules, alignment and type weight to separate content. Avoid decorative panels
  around every number, distressed paper effects, camouflage, faux hardware and dramatic lighting.
- Reserve serif display type for the Maproom name, scenario titles and atlas place labels. Use
  a plain sans for every control and dense operational detail. Numeric columns use tabular figures.
- Unit counters are original geometric marks with labels, shape and line patterns. They are not
  scans of published counters or claims of historical unit accuracy.
- Terrain is quiet enough for units, selected routes and uncertain contacts to remain distinct.
  Keep contours away from labels. Unknown information is not visually encoded as zero.
- Use short order verbs: Select unit, Review order, Issue order, Refresh orders, Hand over.
  Outcomes explain the next action. Technical hashes and request IDs belong in diagnostics.

## Token foundation

The study's [tokens.css](maproom-style-study/tokens.css) is the initial executable token proposal.
Use OKLCH colors, a 4px spacing base, named type sizes, modest corner radii and explicit motion
durations. Values may evolve; semantic names should stay stable for consumers.

| Role | Token | Meaning |
| --- | --- | --- |
| Page | --color-paper | Warm off-white canvas |
| Raised surface | --color-panel | Paper sheets and order controls |
| Map ground | --color-map | Muted sand-colored theater |
| Main text | --color-ink | Dark warm ink |
| Secondary text | --color-muted | Supporting text with readable contrast |
| Dividers | --color-rule | Quiet structural lines, not sole control boundaries |
| Control boundary | --color-rule-strong | Visible inputs, counters and interactive boundaries |
| Primary action | --color-accent / --color-accent-ink | Olive action and verified foreground |
| Command frame | --color-frame / --color-frame-ink | Dark olive-tinted navigation frame, distinct from primary-action emphasis |
| Frame interaction | --color-frame-hover / --color-focus-on-frame | Hover surface and visible keyboard outline on dark framing |
| Selection | --color-selection | Selected unit, active route and linked list row |
| Focus | --color-focus | Keyboard focus, with offset separating it from component fill |
| Own side | --color-own / --color-own-ink | Labeled own-side counters |
| Opposing contact | --color-contact / --color-contact-ink | Labeled contact with uncertainty treatment |
| Status | --color-error, --color-warning, --color-success | Text/icon plus color; never faction identity |
| Terrain | --color-sea, --color-contour, --color-road | Supporting map geometry |
| Type | --font-display, --font-body | Newsreader and IBM Plex Sans candidates, with local fallbacks |
| Spacing | --space-2xs through --space-2xl | 4, 8, 12, 16, 24, 32, 48px steps |

Faction/contact color and status/error color have separate roles even when both use earth tones.
Pair uncertain contacts with dashed boundaries and labels, and rejected orders with explicit text.
The reviewed brass candidate (#A58C59) measures 2.67:1 on document ivory (#F0E9D8) and 2.01:1 on
desert parchment (#D9CBA9); it is not adopted for text or the sole selection/focus cue. Keep dark
ink and the tested focus treatment. A mono font is optional only for a demonstrated need; tabular
numerals in the body face cover the initial ledger without adding a third font.

The portable study uses installed serif/sans fallbacks so it makes no network requests. MAP-02
packages reviewed Newsreader/IBM Plex Sans font files with their licenses and verifies actual
metrics before locking screenshots. The fallback rendering is not a test of those font binaries.

Theme implementation has one source for semantic values. Angular Material consumes them through
supported theme/override APIs; Tailwind exposes corresponding semantic utilities for layout and
custom components. Do not override private Material DOM selectors. Apply the theme to CDK overlay
containers too. Test Tailwind reset/cascade order against inputs, dialogs, menus and Formly fields.

Web Components consume a documented subset of CSS custom properties. Tailwind utilities outside
a Shadow DOM do not style its internals automatically. An adapter owns mount/unmount, inputs,
events, accessibility and, for fields, ControlValueAccessor behavior.

## Layout and information hierarchy

| Region | Content | Behavior |
| --- | --- | --- |
| Phase header | Scenario, active seat, phase, connection/persistence state | Always identifiable; temporary sessions explicitly labeled |
| Formation list | Visible units, selection and actionable state | Keyboard alternative to map selection; clears unavailable selections |
| Theater | Schematic terrain, permitted units/contacts, draft route | Bounded zoom, locate and fit in the preview; production camera/semantic zoom need separate checks |
| Inspector | Selected unit details and available order controls | Coherent with the same server view as the map |
| Order review | Exact proposed action, known cost information, current validity | Distinct review and issue steps |
| Formation ledger | Searchable/sortable name, kind, position and order state | Same selection as map; expandable records area; explicit empty result |
| War Diary | Audience-safe explanation of committed outcomes | Selected-unit/all-visible scope, locate action and linked history; derived display, not raw Chronicle |

Desktop initially targets 1280px and above. Fill the working viewport with compact phase context,
map/roster/inspector, and a ledger/diary region; no introductory hero or palette gallery in play.
The roster collapses, inspector width can be adjusted, and ledger can expand while retaining the
map. On short screens allow document scrolling rather than clipping required actions.
At intermediate widths collapse the roster by default and keep map plus inspector. On narrow
screens stack map, inspector and records, preserve accessible selection controls, and keep order
actions reachable without horizontal document scroll. Use available panel width and rem-based
container queries so enlarged text also triggers reflow. The four-field ledger becomes two-column
records with visible field labels at narrow widths; all fields remain available without sideways
scrolling. Preserve table/row/cell semantics and its labeled, keyboard-focusable scroll region.
Validate 320–2560px widths, short landscape screens, 200% root text sizing, both densities and the
widest inspector setting. Small-screen usability is not yet a commitment to full touch-first
campaign play; map interactions need their own tested alternatives.

The portable workspace keeps a 34rem minimum desktop work area and a 19.5–24rem records area;
expanded records use 27–34rem. Short viewports scroll vertically. Roster headings and the contact
note stay outside the scrolling formation list; rows retain their natural height and focus space.
Inspector facts use three columns when they fit and labeled rows when they do not. Draft/review
steps already communicate order state, so a separate outcome block appears only once a result or
boundary needs explaining. Completed/failed orders replace the inactive form with their outcome
and recovery action, avoiding empty destination values and buried controls. Narrow inspectors repeat
the full destination below the native select to keep long selected values readable. Map destination
rings remain circular across aspect ratios;
only decorative place labels drop away at the smallest map/text-size ratio.

Every displayed gameplay measure needs a defined meaning, unit, source contract and audience.
Do not infer strength, supply or readiness percentages from presentation needs. Initial ledger
fields are identity, kind, position and the UI's order status. Search filters the ledger only;
selection remains visible on the map and inspector even when its ledger row is filtered out.
Counts reflect only the permitted view. Semantic zoom may summarize disclosed data, never reveal
unseen formations. Action explanations and diary locations must likewise preserve fog boundaries.

## Interaction and state language

| State | Treatment |
| --- | --- |
| Selected | Strong counter outline, selection label and synchronized list row |
| Draft | Dashed route and visible Draft label; committed counter stays at its known position |
| Ready to issue | Exact reviewed action and explicit Issue order control |
| Sending or pending publication | Stable form geometry, busy text, duplicate issue prevented |
| Accepted | Confirmed result appears in the view/War Diary after server acceptance |
| Stale | Draft marked out of date; Refresh orders action; no automatic reissue |
| Disconnected | Last confirmed view labeled stale; commands unavailable until resynchronized |
| Unsupported phase | Explain the current rules-lab stop; never imply victory or completed campaign |
| No legal action | Explain waiting/system/unsupported state according to the server contract |
| Handoff | Opaque curtain after teardown; incoming seat explicitly enters its own view |

Maintain an immutable submitted-order identity independently of the current selection. A player
may inspect another formation while waiting; completion still updates the originating formation.
Apply accepted results coherently to map position, inspector, ledger and permitted diary. Rejected
and stale results retain the last confirmed position and require fresh review. Lost replies remain
uncertain until receipt/readback reconciliation; never blindly reissue. The standalone preview
stages this flow with an in-memory delayed result and one accepted recovery example; it does not
prove actual network, idempotency or durability behavior.

Maproom is a public-facing campaign game. Keep a visible scenario title, terrain context and
formation identity as players move from map to order review; use concise operational language and
clear consequences instead of filling the workspace with generic dashboard metrics. Field Atlas
type, color, spacing, focus and status treatments should read as one system across map, inspector,
ledger and diary. Review both wide and narrow screenshots alongside browser keyboard, camera,
order and accessibility checks in the [delivery plan](local-game-server-maproom-plan.md).

Phase context expresses actual sequential decisions. Do not introduce a generic End Turn button,
batch-order queue or simultaneous resolution without the governing contracts. Preview controls
can stage an unsupported Combat-entry boundary, but this is not a phase transition in the engine.

Local selection, map camera and draft edits can respond immediately. Authoritative orders are not
optimistically applied and do not get a generic Undo affordance. This domain rule takes precedence
over generic design advice favoring optimistic UI. Confirmation is a readable review of the exact
order, not a repeated modal for every harmless interaction.

Every production control is checked in default, hover, keyboard-focus, active, disabled, loading,
error and success states where meaningful. Labels remain visible; errors have text and association
with the field; validation begins on blur/submit and updates after a field has been touched.
Keep border thickness and geometry stable across states. Disabled actions explain why.

Text-style actions retain 44px targets with 8px vertical and 12px horizontal padding; highlighted
surfaces must not touch their text. Ledger cells leave space above and below these actions, and
diary actions have a 4px outer gap. Select indicators sit 1rem inside the trailing edge, with 2.5rem
reserved padding for the indicator and text gap. The portable preview keeps native select behavior
under a decorative, non-interactive chevron; forced-colors mode restores the native indicator.

## Accessibility and motion

- Body/control text targets 16px, dense supporting text at least 14px. Do not fit more units by
  shrinking labels indefinitely. Use a list/aggregation when the map gets crowded.
- Normal text contrast at least 4.5:1; large text, essential boundaries and focus at least 3:1.
  Compute against actual composited surfaces, then verify keyboard and assistive interaction.
- Pair faction/status colors with words, symbols or patterns. Selected and focused are distinct.
- Provide real controls, visible labels, predictable tab order, Escape/close behavior and focus
  return. Use Material/CDK behavior when available; custom rendering still needs verification.
- Touch actions target 44px; compact desktop density must not reduce target or text accessibility.
- Motion communicates a state change only: short opacity/transform transitions, no continuous
  decoration. Reduced-motion mode removes spatial movement. Focus appears immediately.
- Handoff removes prior-seat content rather than fading it behind a translucent overlay.

## MAP-02/03 workspace and component scope

Start with tokens and the field/button/selection controls needed by MAP-03A. Prove the operational
map, order workflow and formation ledger in MAP-03A/B/C, extracting shared components along the
way. Before connected gameplay, retain focused specimens for:

1. Tokens, type, spacing, dividers, panels and density settings.
2. Material action buttons, input/select, checkbox, menu, tooltip and dialog.
3. One Formly order field and one custom spatial-choice ControlValueAccessor.
4. Formation row, unit/contact counter, selection, route and status treatment.
5. Order review with ready, pending, stale, accepted and rejected examples.
6. Connection status, temporary/durable session labels and opaque handoff curtain.
7. Searchable/sortable ledger, selected rows, empty results and unit-linked War Diary entries.

Use a development-only Angular showcase route with synthetic view models. It is a repeatable
visual test surface, not an additional authority API or a requirement to adopt Storybook immediately.
Keep fixture scenarios small and named by the interaction they prove.

## Evolution policy

Version the semantic token contract and record visual changes in this file. Keep tokens and
Material/Tailwind mappings together; require screenshots for meaningful appearance changes and
interaction tests for behavior changes. Components consume semantic roles instead of raw palette
values. Evolve one representative screen and state set before rolling a change across Maproom.

Field Atlas is the accepted direction. v0 values, exact font metrics, control density and contact
symbol details can change after reviewing the study and the first Angular showcase. A dark theme
must preserve role meanings, contrast, uncertainty and handoff behavior; it is not needed for the
first connected action.

## Portable study verification — 2026-10-16

The standalone study opens directly from index.html, or through a loopback static server. It
requires no package installation, remote assets, backend or persisted state. Its CSS tokens are a
starting input for MAP-02; native HTML controls here do not prove Material/Formly integration.

Three refinement passes addressed panel spacing and ledger reflow, enlarged-text layout, then
keyboard and order-state fit. Headless Chrome through Playwright checked 24 layout combinations
from 320×568 through 2560×1440, including 640×360 landscape, both sides of the desktop breakpoint,
400px inspector width, compact density and 200% root text sizing. No horizontal document or ledger
overflow, out-of-viewport controls, overlapping checked panels or dialog width overflow remained.
The map retained at least 320px of usable width in these cases. Screenshots were inspected for
desktop, tablet, phone, enlarged text, order review/results and preview controls.

The operations revision was checked in Chrome 155 through Playwright. Keyboard review/edit/issue,
selection changes during pending results, accepted/stale/rejected outcomes, lost-reply recovery,
ledger filtering/sorting/empty state, diary locate, terrain/order layer switching, zoom/fit, roster
collapse, ledger expansion, unsupported phase, reset, dialog focus return and reduced motion passed.
Additional 320/768/1440px checks covered keyboard access to the last roster row, filtered ledger
selection, preserved table roles, expanded records, review/result layout and 44px control heights.
Seventeen representative text, boundary and focus pairs passed 4.5:1 or 3:1 thresholds, including
the new command frame; lowest tested normal-text contrast was 4.97:1. No JavaScript page errors or
external requests were observed. This is a scoped prototype check, not an Angular, screen-reader
or cross-browser certification; those checks belong to MAP-02 onward.

## Implemented Angular reference

[Field Atlas](../../src/Cna.Maproom/projects/field-atlas/README.md) now owns the live tokens,
Material theme and reusable presentation API. [Storybook](../../src/Cna.Maproom/README.md) is the
living design-system reference: overview, foundations, Material controls, panel/status/counter
states, app-owned Formly pattern and operations shell. Run `just field-atlas` locally.

The HTML style study remains historical design intent. Evolve shared styles in the Angular
library and document them in Storybook; do not maintain a second active token implementation in
the prototype. App-specific patterns stay in `maproom-alpha`; extract further components only
when real reuse emerges. MAP-03A's operational map and MAP-03B's local order workflow fixtures
are implemented; MAP-03C records and campaign integration are pending.
