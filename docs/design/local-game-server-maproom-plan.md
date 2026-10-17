# Local game server and Angular Maproom implementation plan

Date: 2026-10-16. Status: MAP-01 workspace, MAP-02 foundation and MAP-03A/B fixtures implemented;
MAP-04 browser schema, fixtures and generated client drafted; connected gameplay pending.
Audience: contributors implementing the local rules-lab host and browser client.

Build a browser client around the existing deterministic C# Umpire. First delivery launches one
local development host, creates an admitted synthetic campaign, displays a side-safe map, and
submits a current legal action. Introduce Field Atlas design tokens before building gameplay
screens. Add durable save/resume only after its contracts, provider evidence and existing production
hosting prerequisites are satisfied.

The [roadmap](../roadmap/pre-alpha-roadmap.md) remains the delivery status ledger. This plan brings
forward a bounded local testing client; it does not activate private Combat or declare the
six-turn scenario playable. The [design system](maproom-design-system-v0.md) owns presentation.

## Accepted decisions

| Concern | Decision | Boundary |
| --- | --- | --- |
| First environment | Local development, one host process and one browser application | Loopback-only development ingress; remote multiplayer is later work. |
| Simulation | Existing Cna.Core and Cna.OrleansHost | Core owns rules, legal actions, RNG and fog of war. |
| Client | Angular, TypeScript, standalone components, Angular CLI | Browser owns presentation, selection and private drafts. |
| Controls | Angular Material and CDK | Material controls and supported theming; CDK for custom interaction primitives. |
| Styling | Tailwind CSS and semantic design tokens | Layout/custom surfaces use Tailwind; Material internals use supported theme APIs. |
| Forms | Typed Reactive Forms; ngx-formly/core and ngx-formly/material for variable forms | Explicit typed submission adapters; app-owned field definitions. |
| Client state | NgRx SignalStore and Angular signals; RxJS for async work | Feature-scoped view/selection/draft/session stores; no client simulation reducer. |
| HTTP | Angular HttpClient generated with OpenAPI Generator typescript-angular | Versioned browser contracts, separate from intelligence protobuf. |
| Updates | Microsoft SignalR JavaScript client | Audience-safe notifications; HTTP readback restores current truth. |
| Icons | Lucide for Angular; original SVG unit symbols | Icons supplement labels; faction and status never depend on color alone. |
| Initial map | SVG, behind a renderer interface | Original schematic fixture; no duplicated adjudication/path-cost rules. |
| Verification | Vitest/Angular TestBed, Playwright and axe | State, interaction, keyboard, privacy and real browser coverage. |
| Tooling | Angular CLI, angular-eslint, Prettier; npm with committed package-lock | Pin compatible stable versions and supported Node in MAP-01. |
| Visual direction | Field Atlas | Warm paper, dark ink, restrained military symbols; v0 is evolvable. |
| React-only components | Web Component plus Angular adapter when needed | No React runtime until a selected component requires it; teardown and styles tested. |

Exact package versions are not frozen by this document. MAP-01 verifies Angular/Material/CDK,
Formly, NgRx, Node and TypeScript together without forced peer-dependency overrides. Angular
Signal Forms are stable in v22+, but Reactive Forms keep Formly and fixed forms on one foundation.

PostgreSQL is the first durable provider candidate to evaluate, not an accepted production provider.
The publication/restore protocol must pass against the selected provider before integration.
Intelligence remains optional; the first browser flow uses human orders and deterministic System work.
Rust/WASM is not required for this browser client. Keep the existing C# simulation on the server;
consider a renderer/worker optimization only when measured browser work warrants it.

## Existing foundations and gaps

- [OrleansHost](../../src/Cna.OrleansHost/Program.cs) starts a development silo and basic endpoints;
  campaign grain, browser ingress and durable campaign storage are not implemented.
- [AppHost](../../src/Cna.AppHost/AppHost.cs) orchestrates three service scaffolds. Add a local
  Maproom profile which can launch the client and host without requiring Intelligence.
- The [Proving Ground runbook](../runbooks/proving-ground.md) supplies a two-container Development
  preview of the present Maproom shell and Orleans host, with loopback ports and HTTP smoke probes.
  It does not implement MAP-07–17 or a browser campaign seed. Aspire client orchestration remains
  an open integration option; do not infer it from the Docker preview.
- [CampaignAuthority](../../src/Cna.Core/Campaigns/CampaignAuthority.cs),
  [CampaignObservations](../../src/Cna.Core/Observations/CampaignObservations.cs) and
  [CampaignLegalActions](../../src/Cna.Core/Actions/CampaignLegalActions.cs) supply current authority,
  audience projection and legal submission boundaries.
- [CampaignExercises](../../src/Cna.Core/Exercises/CampaignExercises.cs) exposes canonical accepted
  evidence for trusted experiments. Its reconstruction requires an existing session; checkpoint
  metadata readback is not an executable restore API. Do not make Exercise internals browser DTOs.
- The [hosting study](../research/orleans-publication-feasibility.md) proves bounded in-memory
  cases, not process durability. Its atomic batch recommendation informs MAP-05 and MAP-14.
- Current public play stops at first-side Combat entry. Private Combat tests do not supply a
  public browser action surface. The developer client must show this supported-boundary stop.

## Module and deployment shape

```text
Angular Maproom
  HTTP requests + SignalR notifications
    Cna.OrleansHost: ingress, session/seat mapping, campaign lifecycle
      campaign grain -> Cna.Core trusted host adapter -> deterministic Umpire
      off-turn publication/restore coordinator -> Archives storage

Cna.AppHost: local orchestration, health and diagnostics
```

Planned paths, introduced only by their owning slices:

| Path | Responsibility |
| --- | --- |
| src/Cna.Maproom/ | Angular application, generated HTTP client, tokens, component showcase and browser tests |
| src/Cna.Game.Contracts/ | Versioned outward HTTP DTOs; no authority handles, snapshots or hidden state |
| src/Cna.OrleansHost/Campaigns/ | One grain per campaign and lifecycle messages |
| src/Cna.OrleansHost/Api/ | HTTP/session ingress, safe errors and SignalR hub |
| src/Cna.OrleansHost/Persistence/ | Publication/restore coordination and provider adapters; Archives module |
| src/Cna.Core/Hosting/ | Narrow trusted accepted-step/projection/restore boundary, subject to MAP-05 contract |
| tests/Cna.OrleansHost.Tests/ | Hosted parity, ingress, retry, publication and restart tests |
| tests/Cna.Game.Contracts.Tests/ | Wire fixtures, compatibility and outward disclosure checks |

Use one ASP.NET Core host containing API and Orleans silo. Keep Archives and Chronicle as modules;
there is no need for separate services for each. Serve the built client from the host for local
packaged play. During development, proxy browser /api and hub paths to preserve one browser origin.
The existing site/ project website and Pages deployment remain independent.

## Contract and lifecycle rules

### Browser contract

MAP-04 freezes exact routes and schemas before handlers. The drafted
[browser contract v1](../../src/Cna.Game.Contracts/README.md) defines these operations: list admitted
fixture/scenario descriptors; create a campaign; enter a granted seat; read player view and legal
actions; submit one action; query a request receipt; relinquish a seat; subscribe to safe changes.

Bind submissions to campaign, session/seat, unique request ID, opaque audience action-set token,
exact server-issued outward action identity and its current contract revision. Bind immutable
rules/setup/content/configuration identities at creation. The creation response pins four lowercase SHA-256 identities and returns one opaque
seat grant per side; MAP-08 must enforce distinct grant IDs because OpenAPI 3.0 cannot compare
object values. Preserve state_version and existing rules identity in trusted records. Never expose
authority state_version or encode it in outward action tokens. Audience revision and diary cursor
advance only on permitted visible changes and use lossless decimal strings. Outward action IDs
remain stable across hidden-only commits; the host maps them to current Core candidates and
revalidates legality. Browser receipt/status and journal sequence are distinct concepts.

Derive actor/side from server-issued session ownership. Never trust a caller-supplied System actor.
Authorize before looking up a retained receipt. Exact retries return latest retained status;
pending may later settle, and final outcomes are immutable. Reuse of a request ID with changed
actor or canonical payload conflicts. A fresh stale request
rejects without an event, RNG advance or resource charge. Transport cancellation cannot undo a
commit; retry/status lookup reconciles an uncertain response.

Return a coherent player-view/action-set cut from one committed authority state. Map artwork and
topology may be public content, but units, contacts and legal targets come only from the permitted
projection. A legal-action list is also subject to fog boundaries. Keep raw Chronicle events,
snapshots, seeds/RNG state, debug traces and hidden identifiers out of player payloads and logs.

MAP-04 also defines receipt status variants, the SignalR hub handshake, formation-ledger fields
and bounded, audience-safe War Diary readback.
Ledger rows derive from the same permitted view as the map; diary excerpts carry only approved
audience revision/cursor metadata. Neither surface becomes a raw journal export or a separate
source of campaign truth.

### Publication and automatic progression

The grain serializes authority access. Temporary sessions use a bounded in-memory commit store,
explicitly labeled as lost on server restart. Memory receipts are not durable saves.

Keep authoritative execution synchronous and deterministic. Provider I/O runs in an off-turn
host coordinator. A grain prepares an immutable pending transition against an expected committed
head, admits no competing mutation, and receives a publication-completion message in a later
turn. Only a confirmed commit installs/exposes the successor and returns an accepted receipt.
While pending, reads expose the prior committed view or a defined unavailable response. Pending
status is never described as accepted or saved. The
[MAP-05 trusted host protocol](../specs/maproom-trusted-host-protocol-v1.md) specifies crash,
reactivation, fencing, unknown-commit reconciliation, restore provenance and bounded System
progression before this protocol is implemented. Creation dedupe retains the original operator,
request payload, campaign ID and seat grants atomically; action dedupe retains the authenticated
outward request separately from Core replay bytes. A live pending grain reconciles a lost
coordinator/callback under a bounded fence before accepting another mutation.

Advance only server-owned legal System actions, one bounded committed transition at a time.
Stop for a human decision, an admitted terminal state, an explicitly unsupported phase, or a
diagnosed step limit. An empty legal set is not automatically victory. Wall-clock/browser timing
must not choose random outcomes or resolve gameplay. A later scheduler/AI policy is separate work.

### Notifications and seat isolation

Publish notifications only after commit and only when an audience-permitted view changes.
Do not broadcast raw journal versions or hidden-event counts. Treat push as a hint: subscribe,
read a coherent current view, reconcile on reconnect and recover missed notifications through
bounded refresh. Initial connection failures, gap recovery and exhausted retries have explicit UI.

Tag requests, subscriptions and drafts with a seat/session generation. Handoff invalidates it,
cancels work, disconnects subscriptions, destroys overlays/embedded components, clears form and
view stores, and removes prior-seat DOM before granting the next seat. Late responses cannot
repopulate the screen. Avoid persistent player-state browser caching. Local hot-seat isolation
protects normal application use; it is not protection from another operator controlling the same OS.

## Implemented frontend foundation

`src/Cna.Maproom` is an Angular CLI workspace. `maproom-alpha` consumes the private
`@sandtable/field-atlas` library. Storybook documents live foundations, Material controls, custom
components and app-owned patterns from that same source. Core/utils are deferred until needed.
See the [workspace guide](../../src/Cna.Maproom/README.md) for pinned versions, local commands and CI.

MAP-01, the MAP-02 foundation and MAP-03A/B are implemented. The shell selects eight synthetic
formations, locates and fits a bounded camera, collapses its roster and adjusts its inspector.
Keyboard focus brings a clipped counter into view at zoom. Fixture keyboard help exposes
formation cycling, locate, fit and zoom; a focused map accepts arrow-key pan, and mouse/pen
drag pans while terrain covers the viewport, including after locating an edge formation.
Shortcuts stand down in form fields and open overlays.
MAP-03B's Formly draft stages a
per-formation review and delayed fixture result; lost replies reconcile once from an in-memory
receipt. No server action, legal-order validation or durable receipt exists. MAP-03C remains open
for filter/sort and records.

## Small implementation slices

Each numbered slice is a reviewable PR with its own demonstration and evidence. Split further if
it grows beyond the stated scope. Dependencies allow frontend foundation work while host contracts
are developed; no slice may silently absorb new game rules or activate private Combat.

| Slice | Depends on | Deliverable | Acceptance evidence |
| --- | --- | --- | --- |
| MAP-00 Plan and stack | None | This plan, accepted stack, Field Atlas direction and linked roadmap | Documentation links and clear accepted/proposed/implemented status |
| MAP-01 Angular workspace | 00 | Cna.Maproom workspace, lockfile, lint/format/test commands; one Material control and Formly field compatibility specimen | Clean install/build/typecheck; edit, validation and disabled-state smoke; no forced peer overrides |
| MAP-02 Minimal design foundation | 01 | Semantic tokens, Material theme, Tailwind bridge, font assets/licenses and only the controls needed by the first workspace | Representative field/button/selection states, contrast and keyboard checks; theme tested inside a compact workspace frame |
| MAP-03A Operational map fixture | 02 | Viewport-oriented phase bar, collapsible formation pane, schematic SVG map, camera controls and contextual inspector | Eight or more labeled synthetic formations; map/list selection parity; locate/fit behavior; 320–2560px layouts, short screens, 200% text sizing and widest inspector; no clipped actions |
| MAP-03B Order workflow fixture | 03A | Per-formation draft/review/result flow with staged acceptance, rejection, stale view, lost reply and unsupported phase | Pending keeps last confirmed position; switching selection cannot redirect a result; refresh requires review; simulated reconnect reconciles once |
| MAP-03C Formation ledger and War Diary | 03B | Searchable/sortable ledger, shared selection, expandable records area and linked per-unit diary | Filter/empty/sort cases, keyboard table access, diary locate, coherent accepted updates across all views; narrow records expose all four labeled fields without sideways scrolling and preserve table semantics |
| MAP-04 Browser contract v1 | 01 | DTO schemas, safe error/receipt/session semantics, coherent view cut and generated Angular client | Contract fixture roundtrip, lossless identities, forbidden-data assertions, generated client compilation |
| MAP-05 Trusted host protocol | 04 | [Design contract](../specs/maproom-trusted-host-protocol-v1.md) for accepted-step/export, pending publication/fencing, restore provenance and bounded System steps | Fixed direct-Core vectors and failure matrix; exact ownership of private vs outward bytes; no implementation claims |
| MAP-06 Core host adapter | 05 | [Core adapter](../../src/Cna.Core/Hosting/CampaignHostAdapter.cs) for creation, coherent side cuts, legal query, accepted-step evidence and private replay through existing public rules | Direct/existing Exercise parity; deterministic rejections; all exposed audiences checked; private Combat stays inaccessible; no host publication claim |
| MAP-07 Temporary campaign grain | 06 | In-memory lifecycle, bounded progression and request/receipt record; one admitted fixture | Isolated Orleans host matches direct Core; concurrent identical request accepted once; stopped/unsupported states explicit |
| MAP-08 Local ingress and reads | 04, 07 | Loopback profile, bootstrap/seat grant, authorized creation/view/action endpoints, dev proxy | Wrong campaign/seat denied; cross-origin/CSRF protections tested; no secret/raw-state logging; coherent reads |
| MAP-09 Command ingress and retries | 08 | HTTP action submission, pending/accepted/rejected status and request lookup | Duplicate, changed-ID payload/actor, stale and lost-reply cases; no duplicate effects; cancellation reconciliation |
| MAP-10 Connected workspace reads | 03C, 08 | Replace fixture adapter with generated reads for map, inspector, ledger and audience-safe diary; preserve renderer interface | Two audience sessions see only permitted units/actions/history; selection survives valid refresh and clears when no longer visible |
| MAP-11 Real order forms | 09, 10 | One admitted move action with typed draft, Formly field adapter, review and explicit issue action | Keyboard path submits once; invalid/stale states explain recovery; draft never moves authoritative counter; server decides cost |
| MAP-12 Live refresh and reconnect | 11 | SignalR subscription, safe view notification, missed-update recovery and connection UI | Subscribe/read race, initial failure, disconnect, missed push and stale response tests; no hidden-event notification leak |
| MAP-13 Private hot-seat handoff | 12 | Curtain screen, server seat-generation switch and full client teardown | DOM/storage/history/late-response negatives; overlays and any embedded React elements unmount; next seat starts with its own view |
| MAP-14 Durable storage contract | 05 | Versioned atomic commit/head/receipt/outbound-marker schemas, checkpoint binding, retention and upgrade rules | Crash matrix and trusted replay/import vectors; provider decision recorded; historical-version read/recovery policy |
| MAP-15 Durable provider proof | 14 | Bounded PostgreSQL candidate adapter and isolated process-restart harness | Real kill/restart before/after commit; stale-head fencing; unknown commit; checkpoint lag/corruption; request dedupe retention |
| MAP-16 Durable host and resume | 13, 15, hosting gate | Integrate chosen provider/off-turn coordinator; saved campaign listing and resume UI | Committed state and receipts survive full process restart; no duplicate effects; failed recovery cannot expose partial truth |
| MAP-17 Local playtest gate | 13; 16 for durable mode | One-command launch, runbook, focused CI/browser gates, retained supported-path playthrough | Clean setup; create/order/handoff/reconnect; exact replay; durable mode also quit/resume; clear unsupported-phase stop |

### Browser and Field Atlas acceptance

Playwright is the standing Maproom interaction gate. Add cases with each slice, keeping the
fixture and connected suites distinct. Review screenshots at a desktop map scale and a narrow
screen; axe, type checks and unit tests complement interaction checks but do not replace them.

| Surface | Current MAP-03A/B browser proof | Next acceptance as slices land |
| --- | --- | --- |
| Map and camera | Eight counters; map/list/ledger selection; locate, fit, zoom bounds; keyboard focus pans a clipped counter into view; discoverable shortcuts and bounded keyboard/pointer pan; accepted position updates counter, inspector, roster and ledger | Test pan and zoom controls, selection retention, counter hit targets and responsive overlays with the connected audience view; avoid hidden-unit disclosure at every zoom |
| Order path | Keyboard selection, Formly destination, review and issue; pending holds confirmed position and locks submitted draft; origin survives selection switch; one unresolved order blocks another issue with visible recovery; accepted, rejected, stale, lost reply and unsupported fixtures | Add server-backed invalid/stale/rejected receipts, duplicate submit prevention, real readback/reconnect and explicit no-legal-action copy |
| Layout and accessibility | 320–2560px, 200% text, short viewport, WCAG A/AA axe and reachable controls | Cover each new panel in default/focus/loading/error/success states; check tab order, essential shortcuts when introduced, map keyboard alternatives and reduced motion |
| Design coherence | Field Atlas tokens/components and Material Formly controls appear in Storybook and the workspace | Compare map, inspector, ledger and diary using the same type, color, spacing, status and focus language; keep scenario/terrain context readable and inviting rather than a dense administrative dashboard |
| Privacy and sessions | Synthetic own-side fixture only | Check each audience, safe notifications, stale subscriptions, seat handoff DOM/storage clearance and reconnect without hidden-event or hidden-count leaks |

Production map controls or shortcuts require a discoverable keyboard equivalent and a browser test
for both the pointer and keyboard path. The staged MAP-03B reconnect is an in-memory demonstration,
not evidence for MAP-12 network recovery.

MAP-14 contract work and MAP-15 isolated provider research can proceed without a production
hosting claim. MAP-16 requires accepted public Core activation020–021, one verified Combat Runner
trace023, and the accepted publication/storage decision, as recorded by HOST-PUB-001. The separate
024/025 and full six-turn MVP gates remain open. This plan does not reinterpret passing synthetic
tests as passing those gates.

MAP-07 through MAP-13 constitute a Development-only rules-lab harness. Production startup continues
to fail closed until production clustering/storage are explicitly configured. No remote exposure,
account platform, matchmaking, inference provider or public content import is part of these slices.

### Delivery checkpoints

1. **Visible design foundation:** 01–03C. Prove the Angular theme through an operational map,
   order workflow and dense formation ledger before API integration.
2. **First connected action:** 04–11. Create an admitted campaign, select a visible unit, issue a
   legal move and display its authoritative result. In-memory lifetime is explicit.
3. **Local two-seat test:** 12–13 plus temporary-mode17. Reconnect and handoff are proven.
4. **Durable local playtest:** 14–17 after hosting prerequisites. Restart reproduces committed
   truth. The complete six-turn scenario remains a separate product milestone.

## Early design implementation

MAP-02 starts [Field Atlas v0](maproom-design-system-v0.md) with a small token/control foundation.
MAP-03A/B/C then develop that system alongside three representative views: operational map,
individual order workflow and formation ledger. Extract reusable components as these views prove
their need; a comprehensive component catalog is not a prerequisite for a usable workspace.
Keep an internal development-only state route for focused regressions, including overlays and
seat handoff. The current portable operations preview is a standalone design artifact, not an
Angular implementation or executable campaign.

Use the browser viewport for campaign work: compact phase/decision context above the map,
collapsible roster, contextual inspector, and expandable ledger/War Diary below. Records remain
linked to map selection; reports do not require abandoning the theater. At small widths stack
panels and reflow the four-field ledger into labeled records within a keyboard-accessible vertical
scroll region. Base layout changes on available width and text sizing; test short screens and 200%
text alongside the default viewport matrix. Adapt inspector width without shrinking map controls
below usable sizes. Keep design-study explanations out of the play surface.

Field Atlas combines archival paper/cartography and printed-counter hierarchy with compact
operational panels. Borrow interaction lessons from historical strategy games, not their rules
or asset files. Broad logistics overlays and role-specific workspaces remain later design probes.
The first dense screen is a formation ledger using defined fields, not invented supply/readiness
percentages. Each displayed measure must name its meaning, unit, audience and source contract.
Order counts and turn controls must reflect actual sequential phase/action semantics; no implicit
batch-order or simultaneous-resolution model is introduced by a mockup.

Define primitives once, semantic roles once, and bind Material/Tailwind to those roles. Separate
selection/focus from faction colors; distinguish planned movement from committed movement by line
style and labels. Build a few shared components around domain meaning, not wrappers around every
Material component. A React-backed form field gets a ControlValueAccessor adapter and deterministic
teardown; its own Shadow DOM styles consume the documented CSS custom properties.

Explanations, diary links and future semantic zoom consume only audience-permitted facts. An
unavailable-action explanation must not disclose a hidden cause, and aggregated counters must
not imply unseen formations. Status colors and faction/contact colors have separate semantic
roles. Brass may be decorative; it cannot be the only selection/focus cue or low-contrast label.

The existing Player Intent Composer remains future work. This plan implements exact current legal
orders. Language parsing, Staff-plan generation and batch-order authority need their own contracts.

## Verification and operations

- Backend slices add focused deterministic contract/host tests, then run repository-required gates.
  Use dotnet test --project or --solution with Microsoft.Testing.Platform; retain exact commands,
  exits, counts and fixture/build identities. Existing Core/contract checks remain required.
- Frontend slices run clean install, lint, typecheck/build, focused Vitest and Playwright cases.
  Add the frontend lane to CI rather than quietly changing what the existing just check means.
- axe supports accessibility checks; also exercise keyboard focus, map/list parity, zoom and real
  browsers. Component-library defaults alone do not establish accessibility.
- Plan a just playtest recipe when the local host and client are connected. It must start the
  supported profile, report readiness/URLs and stop children cleanly. It is not available today.
- Default development flow needs Node and .NET. Durable-provider research additionally needs the
  selected database/container runtime; add that prerequisite only when its slice lands.
- Keep secrets local, generated artifacts ignored, and diagnostic identifiers out of player-facing
  copy. A debug evidence export is a trusted developer operation, separate from player War Diary.
- Each PR updates roadmap status and relevant runbook/README. No timing estimates are asserted;
  measure slice effort and representative scenario interaction counts before estimating a release.

## Reference decisions

Selected library capabilities were checked against primary documentation during planning:
[Material/CDK](https://github.com/angular/components),
[Tailwind Angular integration](https://tailwindcss.com/docs/installation/framework-guides/angular),
[Formly](https://formly.dev/docs/guide/getting-started/),
[Angular form approaches](https://angular.dev/guide/forms/signals/comparison),
[SignalStore](https://ngrx.io/guide/signals),
[Angular OpenAPI generator](https://openapi-generator.tech/docs/generators/typescript-angular/),
[SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-10.0),
[Lucide Angular](https://lucide.dev/guide/angular),
[Angular testing](https://angular.dev/guide/testing), and
[Playwright accessibility testing](https://playwright.dev/docs/accessibility-testing).
