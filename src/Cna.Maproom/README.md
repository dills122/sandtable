# Maproom Angular workspace

Angular CLI multi-project workspace for the browser client and Field Atlas. This implements
MAP-01, the MAP-02 foundation and the MAP-03A/B operational map and order fixtures: a synthetic operations
workspace, not a connected game.
The original [HTML study](../../docs/design/maproom-style-study/index.html) remains a design reference.

## Run locally

Use Node **22.23.3** (`.nvmrc`) and npm **10.x**. Dependencies are exact-pinned in `package.json`
and `package-lock.json`; use `npm ci` after checkout. Commands below run from `src/Cna.Maproom`.

```sh
npm ci
npm start
# In a second terminal:
npm run storybook
```

- App: <http://127.0.0.1:4200>
- Field Atlas workshop: <http://127.0.0.1:6006>

Both bind to loopback. Root shortcuts: `just maproom-setup`, `just maproom`,
`just field-atlas`, `just maproom-check`, `just maproom-browser-check`.
Aspire integration, browser contracts and the campaign server remain later MAP slices.

## Project boundaries

| Path                                          | Responsibility                                                                                      |
| --------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `projects/maproom-alpha/src/app`              | Lazy route, feature-local SignalStore, synthetic formations and app-owned Formly pattern            |
| `projects/field-atlas/src/lib`                | Exported panel, status label and interactive unit counter                                           |
| `projects/field-atlas/src/styles`             | Semantic tokens, public Material theme overrides, global type/interaction rules and Tailwind bridge |
| `projects/field-atlas/stories`                | Design overview, foundations, Material controls and component states                                |
| `projects/maproom-alpha/src/app/*.stories.ts` | App-owned composition patterns shown in the same workshop                                           |
| `.storybook`                                  | Shared Storybook configuration, Angular/Vite renderer and accessibility addon                       |
| `e2e`                                         | Browser, responsive, accessibility and Storybook smoke checks                                       |

**Storybook is the living implementation reference.** Start here explains ownership; Foundations
shows live tokens and typography; Material demonstrates upstream controls; Components documents
our reusable API; Patterns and Workspace show actual app compositions. Update stories alongside
component changes. The library never imports app code. There is no speculative core/utils package;
extract shared logic only when a second consumer needs it.

Use `@sandtable/field-atlas` imports, not deep library paths. App and Storybook resolve the public
API to source for live reload. `npm run build:lib` separately verifies Angular package compilation.
The internal package is private. Its styles are copied to `dist/field-atlas/styles`; consumers need
Material and the font packages declared as peers, plus Tailwind/PostCSS when using `utilities.css`.

## Stack and compatibility

Angular/CLI/Material/CDK 22.2.3; Formly core/material 10.0.1; NgRx Signals 22.0.1;
Storybook Angular/Vite 10.6.2; Tailwind 4; TypeScript 6; zoneless standalone components.
The lockfile records all tooling versions. No `--force` or `--legacy-peer-deps` install is needed.

Formly is the official `@ngx-formly` package. Its guide still links the v6→v7 migration,
but [the official v10.0.1 release](https://github.com/ngx-formly/ngx-formly/releases/tag/v10.0.1)
and npm's `latest` tag agree. The specimen checks required validation, typed value narrowing,
initially disabled fields, re-enabling and reviewed-state invalidation. Fixed-form examples use
typed Reactive Forms; the app also provides an explicitly typed control for its Formly field.

The current Lucide package is `@lucide/angular` (standalone, signal based). The older
`lucide-angular` package's published peers do not include Angular 22. Storybook's documentation
renderer has development-only React dependencies; the app has no React runtime. Deprecated
`@angular/animations` is a Storybook framework peer, installed as a dev dependency; app motion
uses CSS and reduced-motion preferences.

[Angular compatibility](https://angular.dev/reference/versions),
[Storybook Angular/Vite](https://storybook.js.org/docs/get-started/frameworks/angular-vite),
[Formly setup](https://formly.dev/docs/guide/getting-started/).

## Verification

```sh
npm run check
npx playwright install chromium
npm run test:e2e
```

`check` runs formatting, ESLint (including Angular template accessibility rules), strict TypeScript,
Angular TestBed/Vitest tests, production app/library builds and a static Storybook build.
Playwright starts the two loopback servers when needed and checks selection parity, keyboard
operation, form validation, initial disabled state, six viewport widths, 200% text and axe WCAG A/AA.
For a machine with Chrome installed, `PLAYWRIGHT_CHANNEL=chrome npm run test:e2e` is also supported.
The CI workflow installs pinned Playwright Chromium. Reports go to ignored `playwright-report/`
and traces to `test-results/` on failure.

Build outputs: `dist/maproom-alpha/browser`, `dist/field-atlas` and `dist/storybook`.
App budgets: 500kB initial warning / 1MB error, 7kB component CSS warning / 8kB error.
Storybook may report large development-documentation chunks; those are not shipped with the app.

## Design and current limits

Newsreader and IBM Plex Sans ship locally through Fontsource; licenses live under
`projects/field-atlas/licenses`. `_tokens.scss` is the canonical implemented palette/spacing/type
source. Both consumers load the same `styles.scss` and `utilities.css`; Tailwind's preflight is
excluded so Material and Field Atlas own control styling. Use public Material Sass overrides.

The shell has eight synthetic formations, coherent map/list/ledger selection, a collapsible roster,
adjustable inspector, bounded zoom and locate/fit camera controls. MAP-03B stages per-formation
drafts, explicit review, delayed accepted/rejected/stale results, a lost-reply readback and an
unsupported Combat-entry stop. Confirmed positions move only after a staged acceptance; a pending
result remains tied to its submitted formation even if selection changes. Refresh after a stale or
rejected result requires new review. One unresolved fixture result blocks another issue; its
recovery control remains available when inspecting a different formation. The pending formation's
destination is locked until the result is known. MAP-03C adds searchable/sortable four-field ledger
rows, an expanded records view and selected/all-visible fixture War Diary entries with map locate.
Accepted examples update map, roster, inspector, ledger and diary from the same local result.
Narrow widths stack panels; keyboard focus pans clipped counters into view when zoomed. No legal
actions, authoritative turn resolution, network receipt, persistence or real reconnect exists.
Connected play remains open.
