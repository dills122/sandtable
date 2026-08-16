# webDiplomacy Product and Architecture Spike

**Status:** Owner-reviewed; product-pattern direction accepted

**Work item:** `RSH-WDIP-001`

**Observed:** 2026-08-16

**Decision owner:** Sandtable project owner

## Decision question

Which product and operational lessons from webDiplomacy should Sandtable adopt, adapt, or avoid
when it becomes a durable browser-playable campaign game?

This question matters before Maproom and remote campaign lifecycle contracts are specified. The
costly mistake would be to reproduce a successful product's implementation shape instead of
understanding the player problems it has solved over two decades.

## Scope and stop condition

This spike inspected the public repository supplied by the project owner, its current canonical
upstream, and directly linked official webDiplomacy material. Inspection was read-only. No
upstream code was executed or deployed, no account was created, no message was sent, and no
external repository was mutated.

The packet stops at product and architecture recommendations. It does not authorize runtime code,
dependencies, deployment, public multiplayer, or reuse of AGPL-covered implementation.

**Owner decision, 2026-08-16:** webDiplomacy is retained only as evidence about successful product
ideas, player workflows, and community/operational needs. Its architecture is dated and is not a
template for Sandtable. No implementation stack, source code, schema, or asset reuse is intended.

**Follow-on:** the
[frontend and webDiplomacy success deep dive](frontend-and-webdiplomacy-success-deep-dive.md)
inspected the current signed-out board, anonymous AI onboarding, and live community forum. It adds
evidence that durable success comes from a reinforcing product/community system: immediate trial,
flexible persistent games, reliability and recovery, history and status, active recruitment and
mentorship, competition, variants, volunteer stewardship, and AI/research partnerships.

## Executive conclusion

webDiplomacy is strong evidence that a complex board game can sustain a large audience as an
asynchronous web campaign. Its reusable value is in successful product behavior, not architecture
or code. The most useful ideas are its player
workflow: persistent games, configurable phase deadlines, saved-versus-final orders, automatic
early advancement, private invitation games, visible readiness, phase history, map-centric order
entry, pause/recovery controls, reliability signals, and serious community moderation.

Sandtable should **adopt the asynchronous campaign product model**, **adapt the interaction model
to CNA's sequential and hidden-information decisions**, and **treat the upstream runtime and
persistence architecture as dated, non-reusable implementation history**. In particular:

- Maproom should mature into a responsive, installable web client backed by the authoritative
  Sandtable host.
- The first remote mode should be an invite-only, asynchronous two-player campaign, not public
  matchmaking.
- Player drafts, authoritative submissions, decision deadlines, notifications, and host recovery
  need separate contracts.
- Chronicle should power replay and history; moderator recovery must not rewrite mutable tables as
  though the earlier history never occurred.
- Every browser payload must be a side-safe observation plus legal actions, never the complete
  campaign state.
- webDiplomacy code and visual assets should not be copied into Sandtable. The upstream is
  AGPL-3.0, while Sandtable has not selected a license, and the architectures are materially
  different.

The linked `jmo1121109/webDiplomacy` repository must not be treated as the current upstream. It is
a fork whose default branch stops at commit `b82573998a43eea95a29e18ecf0eddb803cdbe03` from
2020-11-01. Its own README points to `kestasjk/webDiplomacy`, whose default branch was inspected at
`f5dffbe944c2d2c7b143946362cacbf486d03860` from 2026-07-19. The supplied fork remains valuable as
evidence of the project's earlier full-stack shape and the contributor's involvement, but current
architecture and activity claims in this packet use the canonical repository.

## Method and source index

Two shallow, read-only clones were inspected outside the Sandtable repository. Exact commit IDs,
commit timestamps, file layout, and selected implementation boundaries were recorded. The live
site was inspected as a signed-out visitor. Facts about live counts are a point-in-time
observation, not a claim about concurrent users or future activity.

### Repository sources

| Source | Revision inspected | Purpose |
| --- | --- | --- |
| [Supplied fork](https://github.com/jmo1121109/webDiplomacy/tree/b82573998a43eea95a29e18ecf0eddb803cdbe03) | `master` at `b82573998a43eea95a29e18ecf0eddb803cdbe03`, authored 2020-11-01 | Verify the exact owner-supplied reference and its historical shape. |
| [Canonical upstream](https://github.com/kestasjk/webDiplomacy/tree/f5dffbe944c2d2c7b143946362cacbf486d03860) | `master` at `f5dffbe944c2d2c7b143946362cacbf486d03860`, authored 2026-07-19 | Current codebase, deployment assumptions, and product mechanics. |
| [Supplied-fork metadata](https://api.github.com/repos/jmo1121109/webDiplomacy) and [languages](https://api.github.com/repos/jmo1121109/webDiplomacy/languages) | Queried 2026-08-16 | Fork relationship, activity timestamps, counts, and language bytes. |
| [Canonical metadata](https://api.github.com/repos/kestasjk/webDiplomacy) and [languages](https://api.github.com/repos/kestasjk/webDiplomacy/languages) | Queried 2026-08-16 | Current activity counts, license declaration, and language bytes. |
| [Canonical README](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/README.md) | Same revision | Official server/source relationship and contribution posture. |
| [AGPL license](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/AGPL.txt) | Same revision | Code-reuse obligations. |
| [Container topology](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/docker-compose.yml) | Same revision | MariaDB, PHP-FPM, nginx, Redis, Node SSE, React development, and bot assumptions. |
| [Server configuration template](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/config.sample.php) | Same revision | Database, gamemaster, SSE, web-push, email, moderation, variant, and bot configuration. |
| [Composer manifest](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/composer.json) | Same revision | Current root PHP dependencies. |
| [React package manifest](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/beta-src/package.json) | Same revision | Current client framework and library versions. |
| [Game creation](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/gamecreate.php) | Same revision | Phase length, privacy, press, anonymity, missing-player, draw, and reliability options. |
| [Order interface](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/board/orders/orderinterface.php) | Same revision | Save/ready workflow and stale phase checks. |
| [Game processor](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/gamemaster/game.php) | Same revision | Phase processing, mutable archives, recovery, pausing, and outcome handling. |
| [Gamemaster scheduler](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/gamemaster.php) | Same revision | Deadline/ready processing and crash-attempt handling. |
| [JSON API documentation](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/api/README.md) | Same revision | Bot/game-state and order-submission boundaries. |
| [Game-state response](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/api/responses/game_state.php) | Same revision | Whole-game/history response and acknowledged polling cost. |
| [React client](https://github.com/kestasjk/webDiplomacy/tree/f5dffbe944c2d2c7b143946362cacbf486d03860/beta-src) | Same revision | TypeScript/React map, orders, phase history, responsive UI, and state management. |
| [SSE client](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/beta-src/src/lib/sselistener.ts) | Same revision | Redis-backed, per-game/per-country live update design. |
| [Moderation tooling](https://github.com/kestasjk/webDiplomacy/tree/f5dffbe944c2d2c7b143946362cacbf486d03860/admin) | Same revision | Pauses, recovery, account investigation, and community operations. |
| [Notification panel](https://github.com/kestasjk/webDiplomacy/blob/f5dffbe944c2d2c7b143946362cacbf486d03860/usernotifications.php) | Same revision | Desired notification categories and incomplete user-facing implementation. |
| [DATC cases](https://github.com/kestasjk/webDiplomacy/tree/f5dffbe944c2d2c7b143946362cacbf486d03860/datc) | Same revision | Executable adjudication conformance corpus. |

### Official product sources

| Source | Observation |
| --- | --- |
| [Live play home and introduction](https://play.webdiplomacy.net/) | On 2026-08-16 it reported version 1.81, 7,420 users “playing,” 411 active games, and 1,400,431 finished games. These are site-defined counters, not verified concurrent-user analytics. |
| [Official FAQ](https://www.webdiplomacy.net/faq.php) | Describes game creation, phase timing, saved versus ready orders, private invite codes, takeovers, messaging modes, ratings, bots, and DATC-based adjudication. |
| [Official site rules](https://www.webdiplomacy.net/rules.php) | Describes moderation, anti-collusion expectations, private games for known associates, and director controls for managed groups. |
| [Official introduction](https://www.webdiplomacy.net/intro.php) | Shows the onboarding path and explains reliability consequences for missed phases. |

## Evidence discipline

The rest of this packet uses these labels:

- **Documented fact:** stated in an upstream file or official product page.
- **Repository observation:** directly observed at one of the fixed commits.
- **Live observation:** observed on the public site on 2026-08-16.
- **Inference:** a conclusion drawn for Sandtable; it is not an upstream claim.
- **Unknown:** evidence was not available within this bounded spike.

## Repository relationship and activity

### Supplied fork

- **Repository observation:** `jmo1121109/webDiplomacy` is a GitHub fork of
  `kestasjk/webDiplomacy`.
- **Repository observation:** its default branch is `master` at
  `b82573998a43eea95a29e18ecf0eddb803cdbe03`, a 2020 merge commit.
- **Repository observation:** GitHub reported no fork stars and no fork forks on 2026-08-16; its
  repository `pushed_at` timestamp was 2021-12-29.
- **Documented fact:** the fork README names `kestasjk/webDiplomacy` as the source repository.
- **Inference:** the supplied fork is not suitable evidence for the current deployment stack or
  current maintenance activity. It remains useful for historical comparison and for identifying
  durable concepts that were already present by 2020.

### Canonical upstream and live product

- **Repository observation:** the canonical default branch was at
  `f5dffbe944c2d2c7b143946362cacbf486d03860`, a dependency-maintenance commit authored
  2026-07-19.
- **Repository observation:** GitHub reported 209 stars, 115 forks, 111 open issues, and a latest
  repository push on 2026-08-12. These values are time-sensitive and do not measure code quality.
- **Live observation:** the public play site reported 411 active games and more than 1.4 million
  finished games on 2026-08-16.
- **Inference:** the product has strong longevity and meaningful ongoing usage. That validates the
  asynchronous web-campaign category, not the suitability of its implementation stack for a new
  system.

## Architecture map

```text
Browser
  |-- legacy PHP-rendered pages + JavaScript
  |-- newer React/TypeScript board
  |       |-- JSON/HTTP requests to PHP
  |       `-- authenticated Server-Sent Events
  |
nginx
  |
PHP-FPM monolith
  |-- accounts, lobby, game creation, forum, moderation
  |-- game/order API
  |-- variant loading
  `-- gamemaster/adjudicator
  |
MariaDB
  |-- mutable current game/member/order/unit state
  |-- move and territory archives
  |-- messages, ratings, access/moderation data
  `-- backup tables

PHP publishes update hints -> Redis -> Node SSE server -> browser

Periodic gamemaster loop
  `-- finds due/all-ready games, locks/processes them, records archives,
      updates ratings, retries or marks crashes, and advances deadlines
```

### Languages and frameworks

- **Repository observation:** the supplied fork's GitHub language endpoint reported approximately
  3.17 MB PHP, 495 KB CSS, 380 KB JavaScript, 75 KB HTML, 19 KB Hack, and 1 KB shell.
- **Repository observation:** the canonical repository remains PHP-led but now includes a
  React 17/TypeScript board using Redux Toolkit, Material UI, D3, Tailwind-related tooling, Axios,
  and Create React App.
- **Repository observation:** its GitHub language endpoint reported approximately 3.48 MB PHP,
  1.73 MB TypeScript, 478 KB CSS, 468 KB JavaScript, 80 KB HTML, 14 KB Hack, 4 KB shell, and a
  small Dockerfile contribution.
- **Repository observation:** Composer dependencies are small and focused on Twilio and web push;
  much of the server is repository-owned PHP rather than framework-based application code.
- **Inference:** this is an evolved modular monolith with a newer client grafted onto a mature PHP
  core, not a clean separation between an immutable adjudicator, event history, and presentation.

### Storage and deployment assumptions

- **Repository observation:** the development composition expects nginx, PHP-FPM, MariaDB 10.6,
  Redis, and a Node SSE process. Mailhog, phpMyAdmin, the React development server, and bot images
  are optional profiles.
- **Repository observation:** the same MariaDB schema owns live game state, order state, messages,
  ratings, moderation data, phase archives, and operational status.
- **Repository observation:** a periodic gamemaster process queries games that are due or ready,
  mutates phase state, archives selected tables, and tracks processing/crash attempts.
- **Repository observation:** admin tooling can pause/unpause, back up/restore, and move a game
  backward for reprocessing.
- **Inference:** the deployment is operationally understandable but strongly coupled to a mutable
  relational model and scheduled processor. Sandtable should not inherit that coupling.

## Product mechanics map

### Accounts, lobby, creation, and invitations

- **Documented fact:** players can create games, join open pre-game lobbies, or take over abandoned
  positions.
- **Repository observation:** game creation includes variant, name, optional password, stake and
  scoring mode, phase duration, retreat/build duration, anonymous play, press mode,
  missing-player policy, draw visibility, minimum reliability rating, and excused missed turns.
- **Documented fact:** an invite code makes a game private; the official rules require private
  games when participants know one another outside the site.
- **Inference:** Sandtable should begin with a much smaller campaign-creation contract: scenario,
  ruleset/content identity, side assignment, invite, decision-window policy, visibility policy,
  and timeout policy. Ranking, stakes, variants, and public discovery are later concerns.

### Deadlines, drafts, submission, and adjudication cadence

- **Documented fact:** a player may save orders and continue editing, or mark them ready/final.
  When all players are ready, webDiplomacy processes early; otherwise it processes at the phase
  deadline.
- **Repository observation:** the order interface validates completeness, refuses ready status for
  incomplete/invalid orders, locks member/order rows for updates, and rejects an update if the
  persisted turn/phase has moved on.
- **Repository observation:** game phases can range from short live-play minutes to multi-day
  asynchronous windows.
- **Inference:** the saved-draft/final-submission distinction is one of the highest-value product
  lessons. Sandtable must implement drafts outside Umpire authority and submit versioned commands
  against an exact campaign state. A successful command becomes Chronicle history; a draft does
  not.
- **Inference:** CNA does not share Diplomacy's single simultaneous-order cadence. Sandtable needs
  typed **decision windows** attached to the current legal actor and mechanic, rather than one
  generic phase timer.

### Map interaction and mobile use

- **Repository observation:** the newer board is a React/TypeScript map client with explicit
  controllers for map/unit interaction, phase navigation, order panels, countdowns, messages, and
  responsive device/orientation handling.
- **Repository observation:** the UI supports saved/unsaved visual states, current and historical
  phases, keyboard shortcuts, map overlays, and separate layouts for mobile, landscape, tablet,
  and desktop dimensions.
- **Inference:** Maproom should be map-first but task-oriented: select a unit or decision, see only
  legal actions, preview consequences that rules permit, submit, and receive an explainable
  Chronicle result. Responsive design must be a first-class acceptance criterion rather than a
  desktop page with a mobile toggle.

### History, spectating, and analysis

- **Repository observation:** webDiplomacy retains move and territory archives, provides order/map
  and message archive links, lets the newer UI navigate historical phases, and has sandbox tooling
  capable of copying a game or moving a sandbox backward.
- **Repository observation:** current live viewing is treated differently for an active player and
  a spectator.
- **Inference:** persistent history is central to player trust and learning. Sandtable has a
  stronger foundation: Chronicle can produce exact replay, War Diary explanations, and a forked
  analysis sandbox without rewriting the authoritative campaign.
- **Inference:** spectating cannot reuse webDiplomacy's visibility assumptions. Sandtable must
  define viewer-specific, time-specific observation policies, such as participant view,
  finished-game omniscient replay, or delayed/redacted spectator view.

### Notifications

- **Repository observation:** the repository contains email infrastructure, web-push dependencies,
  VAPID configuration, Redis/SSE updates, and a detailed list of desired game/moderation
  notification events.
- **Repository observation:** the user notification configuration page at the inspected revision
  still renders “Coming soon,” so this spike cannot claim the full notification matrix is a
  deployed capability.
- **Inference:** Sandtable needs a small initial matrix: invitation received, decision available,
  decision approaching deadline, campaign advanced, campaign paused, and campaign completed.
  Notification text must contain no hidden game state.

### Moderation, reliability, and anti-abuse

- **Documented fact:** webDiplomacy uses a Reliability Rating, game-level minimum reliability,
  missed-phase consequences, position takeover, player muting, and active moderator support.
- **Repository observation:** moderator tooling includes pauses, account bans, access logs,
  multi-account analysis, relationship analysis, game recovery, and support forums.
- **Documented fact:** managed groups can have a game director who can pause games and replace
  absent players.
- **Inference:** asynchronous multiplayer is partly a community-operations product. Public
  matchmaking without absence, harassment, collusion, replacement, appeals, and audit workflows
  would be incomplete.
- **Inference:** Sandtable's first invite-only friend mode can defer most public-community systems,
  but it still needs owner-controlled pause, resignation, replacement/AI takeover policy, and an
  auditable timeout outcome.

### Recovery and operations

- **Repository observation:** the gamemaster records processing status and attempts, marks repeated
  failures as crashed, supports backups/restores, and exposes pause/reprocess tools.
- **Inference:** these controls reflect real long-running campaign failure modes: stuck jobs,
  missing players, deadline changes, disputed adjudication, and partial processing.
- **Inference:** Sandtable should solve them through idempotent command handling, a durable pending
  action scheduler, exact state versions, immutable Chronicle events, replay checkpoints, and
  audited administrative commands. Direct database rollback should be an emergency storage
  operation, not the normal game-management model.

### Community administration

- **Repository observation:** forums, moderator forums, user profiles, ratings, tournaments,
  relationship declarations, mute/silence controls, and admin dashboards coexist with gameplay in
  the same product and repository.
- **Inference:** the community layer is a major reason a durable browser game retains players, but
  it also creates a separate safety and staffing obligation. Sandtable should integrate with a
  smaller external community surface initially instead of building forums and tournament
  administration before the game is playable.

## Particularly reusable product ideas

1. **Campaign inbox rather than only a live board.** Show games needing this viewer's attention,
   their authorized deadline/status, and their last visible result. Do not expose an opponent
   actor or readiness summary unless a named projection policy proves it public.
2. **Draft versus submit.** Autosave private drafts; make authoritative submission explicit and
   version-bound.
3. **Early advancement when safely complete.** Advance immediately only when all required current
   decisions are final and the Umpire can accept them.
4. **Configurable pacing.** Support friendly no-deadline play and bounded asynchronous decision
   windows before attempting live timed play.
5. **Private invitations first.** A direct invite link/code is sufficient for friends and avoids
   premature matchmaking complexity.
6. **Visible, viewer-specific workflow status.** Indicate `waiting on you`, `processing`, `paused`,
   or `up to date` where authorized. Opponent readiness, controller, delegation, deadline,
   attention conditions, and Engagement participation remain hidden unless the game rules make a
   particular fact public.
7. **Historical map navigation.** Let players step through campaign state and explanations from
   Chronicle.
8. **Pause and replacement policy.** Long games need humane recovery from vacations, resignation,
   and absence.
9. **Map plus side panel.** Preserve geographic context while exposing legal actions, reports,
   logistics, and phase status.
10. **Adjudication conformance corpus.** webDiplomacy's DATC practice maps especially well to
    Sandtable's source-cited rule cases, golden Chronicle streams, and replay checks.
11. **System status and clear failure states.** Players need to know whether a campaign is waiting,
    processing, paused, or genuinely failed.
12. **Analysis sandbox.** Fork a historical checkpoint for experimentation without changing the
    real campaign.

## Ideas that conflict with Sandtable boundaries

| webDiplomacy observation | Conflict | Sandtable response |
| --- | --- | --- |
| A game-state endpoint assembles the full game and history; an upstream comment calls repeated whole-state polling “obscenely wasteful.” | Sandtable has stricter fog of war and potentially much larger state. | Return compact side-safe observations, diffs, and legal actions. Never hydrate the browser or Intelligence with the authoritative world. |
| Current state, orders, messages, ratings, moderation, and archives share a mutable relational model. | Chronicle events must remain authoritative and replayable. | Store immutable events as truth, snapshots as checkpoints, and projections/read models separately. |
| Admin recovery can restore table backups or move a game backward and delete/rebuild archives. | Rewriting history weakens auditability and deterministic replay. | Prefer audited pause, supersession, correction, or fork commands; reserve storage restoration for disaster recovery. |
| The server gamemaster owns scheduling and adjudication in one legacy processing path. | Sandtable separates host lifecycle from pure Umpire adjudication. | Scheduler/Orleans activates due decisions; `Cna.Core` validates and emits events without clocks or remote I/O. |
| The client has broad interdependent state; a current code comment says it fetches “the whole world” and is hard to untangle. | Broad client state raises leakage and maintainability risk. | Design Maproom DTOs around viewer, campaign version, decision, map projection, and Chronicle explanation. |
| Variant mechanisms make one engine support many Diplomacy boards. | Sandtable explicitly targets CNA and rejects premature generic architecture. | Build CNA content/version boundaries; extract generality only after demonstrated duplication. |
| Points, pots, public ratings, tournaments, press, and forums are core platform features. | They add incentives, abuse cases, privacy work, and scope before CNA is playable. | Defer them. Begin with private two-player campaigns and optional private notes. |
| webDiplomacy's visible-state game chiefly hides current orders and messages. | CNA can hide opposing formations, condition, supply, intentions, and more. | Make observation projection and negative disclosure tests an API prerequisite, not a UI convention. |

## Adopt, adapt, or avoid

| Capability or idea | Decision | Rationale for Sandtable |
| --- | --- | --- |
| Browser-based persistent campaigns | **Adopt** | Removes installation friction and fits long CNA sessions. |
| Asynchronous play with deadlines | **Adapt** | Use mechanic-specific decision windows and explicit timeout policies, not a generic Diplomacy phase timer. |
| Save draft / mark final | **Adopt** | Excellent separation of planning from commitment; drafts stay non-authoritative. |
| Process early when everyone is ready | **Adapt** | Advance only when all required actors/mechanics are complete and the exact state version still matches. |
| Invite code/private game | **Adopt** | Best first remote experience for two friends. Use expiring, revocable invitation tokens rather than treating a shared code as campaign authorization forever. |
| Joinable public lobby | **Defer** | Requires identity, trust, absence handling, moderation, and enough player liquidity. |
| Reliability rating | **Defer/adapt** | Useful only with public matchmaking; measure completion/deadline behavior transparently and permit appeals. |
| Position takeover | **Adapt** | Support resignation and explicit human/scripted/AI replacement policy with Chronicle audit. |
| Map/order/history single workspace | **Adopt** | Directly aligns with Maproom. |
| Responsive mobile/desktop board | **Adopt** | Players need to inspect and answer decisions away from a desktop, though dense planning may remain desktop-preferred. |
| SSE/live update hints | **Adapt** | SignalR or SSE can invalidate/refetch player projections; updates must be authenticated and carry no hidden payload. |
| Whole-game JSON payload | **Avoid** | Inefficient and incompatible with strict fog-of-war boundaries. |
| Mutable archive tables as authoritative history | **Avoid** | Chronicle already provides the correct event/replay model. |
| Admin rewind/reprocess as ordinary recovery | **Avoid** | Use auditable commands, replay, and campaign forks instead. |
| DATC-style executable rule corpus | **Adopt** | High-value precedent for proving tricky adjudication cases. |
| PHP monolith and current React dependency set | **Avoid copying** | A successful product can outlive an aging stack; Sandtable's C# Umpire and TypeScript Maproom shape is cleaner for its requirements. |
| Variant/plugin generality | **Avoid before evidence** | Current roadmap deliberately implements CNA, not a generic board-game platform. |
| Built-in forum/tournament/scoring platform | **Defer** | Community value is real, but not required for the first credible campaign. |
| webDiplomacy code or art | **Avoid by default** | AGPL and separate game/art rights require deliberate owner and legal decisions. Product ideas can be reimplemented independently. |

## Code reuse and license implications

- **Documented fact:** canonical webDiplomacy is distributed under GNU AGPL version 3 or later.
- **Documented fact:** AGPL is specifically designed to require corresponding source availability
  for modified covered software used over a network.
- **Repository observation:** the repository also contains third-party libraries and game-specific
  images/assets with their own notices or potential underlying rights concerns.
- **Current Sandtable fact:** Sandtable has not selected a repository license.
- **Inference:** copying server, client, adjudicator, or tightly adapted source into Sandtable could
  impose AGPL obligations and create license compatibility decisions for the combined work.
- **Recommendation:** use clean, idea-level product study only. Do not copy code, CSS, maps, icons,
  text, schema, or assets. If direct reuse is ever proposed, first choose Sandtable's license,
  inventory every relevant file's provenance, and obtain qualified legal review. This packet is
  technical research, not legal advice.

## Operational lessons for Sandtable

1. **A campaign scheduler is product infrastructure.** It needs idempotent due-work claiming,
   retries, a dead-letter/failed status, and an operator view.
2. **Deadlines are policy, not domain time.** The host decides when to submit a timeout command;
   the Umpire records the resulting authoritative decision/event.
3. **Long campaigns need explicit lifecycle states.** Proposed minimum: inviting, active, waiting
   for player, adjudicating, paused, completed, resigned/abandoned, and operator-attention-needed.
4. **Never put secrets in push channels.** A notification says “decision available,” then Maproom
   retrieves a freshly authorized side-safe projection.
5. **Recovery must preserve history.** Replay from Chronicle plus immutable checkpoints should be
   the normal repair tool. Administrative interventions should be recorded.
6. **Public multiplayer multiplies responsibilities.** Account recovery, abuse reports, player
   replacement, privacy, rate limits, audit retention, and moderator staffing arrive together.
7. **The live product can be a modular monolith.** Sandtable does not need separate matchmaking,
   notifications, moderation, and API microservices. Clear modules in an ASP.NET host are enough
   until scaling evidence says otherwise.
8. **Conformance tests build player trust.** Each disputed rule should point to a source/ruling and
   a reproducible test or Chronicle example.

## Concrete implications for a future Sandtable web-play specification

The governing spec should include at least these requirements:

### Campaign access and identity

- A campaign records exact ruleset, content, scenario, and contract identities.
- Every request is authorized for a campaign role: owner, Axis player, Commonwealth player,
  operator, or permitted spectator.
- Invitations are single-campaign, expiring, revocable, and become a durable membership only after
  acceptance.
- The initial remote product supports invite-only campaigns; public discovery and matchmaking are
  explicit later capabilities.

### Player projection and commands

- Maproom receives only a viewer-specific observation, legal-action surface, current state
  version, and public Chronicle projection.
- Draft orders/preferences are private, mutable, and non-authoritative.
- Submission includes the exact state version and legal-action identity; stale or no-longer-legal
  submissions reject without an event.
- No browser API, realtime channel, log, telemetry field, notification, or error body leaks hidden
  opposing state.
- Workflow metadata is projected for the viewer just like military state. The responsible actor,
  readiness, deadline, controller, delegation scope, attention condition, and Engagement
  participation are not inherently public.

### Asynchronous lifecycle

- Each pending decision declares responsible actor, opened state version, deadline policy,
  finalization rules, and deterministic timeout/fallback policy.
- The host may schedule and retry work, but only Umpire commands/events advance the campaign.
- The UI distinguishes saved draft, final submission, viewer-authorized waiting, adjudicating,
  paused, optional Intelligence pending, and operator attention without identifying a hidden actor.
- Deadline extension, pause, resignation, replacement, and timeout are authorized and audited.

### Maproom UX

- The primary screen combines pan/zoom map, selected entity/decision, legal actions, phase/status,
  logistics/report context, and Chronicle explanation.
- A campaign inbox identifies exactly which campaigns require attention.
- Desktop, tablet, and phone layouts are acceptance-tested; color is never the sole carrier of
  ownership, status, or legality.
- Players can navigate their side-safe historical views and explanations without mutating the
  campaign.

### History, replay, and spectating

- Chronicle remains machine truth; War Diary and map history are projections.
- Replay can rebuild the same state from exact ruleset/content/runtime identities.
- Analysis mode forks a checkpoint into a non-authoritative sandbox.
- Spectator visibility is an explicit policy. The safe default is no live spectating and an
  omniscient view only after campaign completion; any delayed/redacted mode requires disclosure
  tests.

### Operations and community

- Due-work processing is idempotent and observable, with retry limits and operator attention
  state.
- Campaign owners can pause and propose a replacement; public operators can intervene only
  through audited capabilities.
- Notification preferences and delivery attempts are non-authoritative projections/jobs.
- Public matchmaking cannot launch until reporting, blocking, absence/replacement, anti-abuse,
  audit, privacy, and moderator workflows have a reviewed specification.

### Verification

- Add source-cited conformance cases for every adopted ruling and tricky mechanic, analogous in
  purpose—not copied content—to DATC.
- Add negative fog-of-war tests for game state and workflow metadata at every
  browser/realtime/inbox/email/push boundary.
- Add deterministic timeout, retry, duplicate submission, stale state, pause/resume, resignation,
  replay, and crash-recovery tests.
- Add responsive and accessible Maproom interaction tests for dense maps and decision panels.

## Risks and unknowns

| Item | Classification | Consequence or follow-up |
| --- | --- | --- |
| Exact production topology, backup cadence, SLOs, and incident history | **Unknown** | Public source shows a development composition, not how the live service is operated. A maintainer interview could provide valuable operational lessons. |
| Whether the full notification matrix is deployed outside the incomplete settings page | **Unknown** | Do not rely on webDiplomacy as proof of a specific notification design. |
| Mobile usage, retention, campaign completion rate, and deadline preference analytics | **Unknown** | Public counters establish scale but not which UX choices cause retention. |
| Exact relationship between live version 1.81 and public commit `f5dffbe9` | **Unknown** | Treat source/live observations as adjacent evidence, not a proven production artifact hash. |
| Full licensing provenance of bundled map/art assets | **Unknown/high risk** | Reinforces the no-copy recommendation. |
| CNA decision cadence under multi-day asynchronous play | **Sandtable validation gate** | `WEB-PACE-EVAL-001` measures the six-turn local MVP before Cruise/Engagement assumptions are generalized; insufficient coverage remains explicit. |
| Player replacement with hidden prior knowledge | **Sandtable design risk** | Replacement observation and access history need explicit policy; an AI replacement must receive only the side-safe state. |
| Deadline outcomes for a two-player simulation | **Versioned campaign policy** | Private alpha defaults to reminder/grace/pause; simulation-first play may choose deterministic Staff. Exact durations need evidence. |

## Recorded decision

The project owner accepted the following direction for later specification work:

1. Sandtable's mature player experience is a responsive web Maproom built around persistent,
   asynchronous campaigns.
2. The first networked milestone after local MVP is invite-only two-player play with private
   drafts, explicit final submission, side-safe projections, Chronicle history, pause/resume, and
   small notification support.
3. Public lobbies, reliability, matchmaking, community messaging, tournaments, and live
   spectating remain later product lanes.
4. Existing C# Umpire/Orleans/service boundaries remain authoritative. A TypeScript web client may
   consume player-specific HTTP APIs and authenticated realtime invalidation/update signals.
5. webDiplomacy informs product requirements, success factors, and failure cases only; its dated
   architecture is not a target, and no code or assets are reused.

**Confidence:** High for the product direction, repository relationship, license, and broad
architecture. Medium for current production operations and deployed notification behavior because
those details are not established by public source.

## Remaining owner and evidence questions

1. Which numeric Cruise deadlines and Engagement durations feel humane? The lifecycle and
   no-surprise rules are accepted, but values need local playtest evidence.
2. Which timeout policy should a campaign select? Private alpha defaults to reminder, grace, then
   pause; simulation-first play may explicitly select deterministic Staff.
3. Should completed campaigns be shareable as omniscient replays by default, or remain private
   unless both players opt in?
4. Is a future public community/matchmaking product a real goal, or should Sandtable stay centered
   on private campaigns and War College automation?

## Next gate

The project owner accepted the product-pattern direction. Write a scoped Maproom/web campaign
specification only after the local pre-alpha skeleton has exposed the real action cadence. The
first spec should cover campaign access, player projection, draft/final submission, asynchronous
lifecycle, Chronicle history, and invite-only play—not public community infrastructure. The
Vue 3 + Vite baseline is accepted; its later rules-laboratory prototype validates generated DTO,
map, accessibility, and stale-state behavior rather than reopening framework selection.
