# Combat fog-of-war architecture spike

**Status:** research and recommendation, 2026-10-21. No visibility rule or
public action is approved by this document. The adjacent
[open-contact RC proposal](../design/p01-rc-contact-disclosure-opening.md)
is one option, not the selected rule.

## Question and evidence

P01's authentic R03 source records contain a private World and replay inputs,
but no same-cut, side-specific observation/contact disclosure. World presence
is insufficient proof that an attacker saw a target. The current Observation 7
projector requires valid Snapshot 11 authority and accepts caller-supplied
authority facts. It also projects opposing representation positions from the
snapshot World. That existing projection is not a P01 contact source or a
general fog policy. CampaignLegalActions likewise requires admitted snapshot
authority before offering actions. The original 300 P01 roots remain private
regression evidence: 44 provisional positive, 256 absent, zero admitted.

Established games distinguish terrain knowledge from current sight. Wesnoth
separates unexplored shroud from returning fog and scopes unit/hex visibility to
a viewing side. Its input handling looks up visible units for a viewing team.
0 A.D. retains last-seen map objects under fog while enemy units disappear.
Freeciv tracks seen state per player, updates last-seen tile information, and
sends out-of-sight unit notifications when vision changes. These are useful
architecture examples, not Sandtable rule authority.

Primary references:

- Wesnoth [fog rules](https://wiki.wesnoth.org/Fog_of_war),
  [viewing-side filter](https://wiki.wesnoth.org/FilterWML), and
  [visible-unit input code](https://github.com/wesnoth/wesnoth/blob/master/src/mouse_events.cpp).
- 0 A.D. [exploration manual](https://play0ad.com/6-exploring-the-map/).
- Freeciv [server vision and map handling](https://files.freeciv.org/nightly/weekly/main/doxygen/html/d1/d14/maphand_8c_source.html).

## Proposed rule model

Core remains the only authority for both truth and what each side knows.
Represent these separately at every decision cut:

1. **Ground truth:** actual units, hexes, resources and hidden combat state.
2. **Side knowledge:** known terrain and last-seen contact records with
   observation time and staleness. Last-known position is never silently
   treated as current presence.
3. **Current observation:** side-scoped contacts and apparent hexes supported
   by a named visibility rule and a specific source cut.
4. **Action offer:** a visible alias and permitted intent derived from current
   observation and public/own-side rules. An offer may disclose that a target
   is actionable; that disclosure must be an explicit rule choice.

The side observation should carry campaign, side, position, state version,
source-cut hash, visibility-policy ID and version, and a canonical digest.
Current contacts must identify when and why they were seen. Contact loss must
be an explicit transition; a remembered contact remains marked stale. The
contact alias should not be a stable opposing unit ID unless the rule explicitly
reveals identity.
Chronicle must durably append and read back the exact observation before Host
opens a decision. Host binds its seat grant, public aliases, deadline and
accepted-input fence to that observation digest. Core resolves the alias
against the private mapping, rechecks the current authoritative cut, then
adjudicates. Maproom renders only the side observation and owner opening.
Neither Maproom nor Intelligence may calculate visibility or receive hidden
opposing state.

Do not derive offerability from a hidden defender fact while claiming that
fact remains secret. The presence, absence, count, order, identity, rejection
class, and timing of offers can reveal it. Decide explicitly which facts the
rule discloses. Offer only intents whose *offer status* is constant across
histories with the same approved observation; otherwise change the rule or
withhold that intent for the entire observation class. Core can still resolve
private outcome details after acceptance. Use a generic stale/invalid
rejection for submitted aliases.

## RC choices

| Choice | Player sees | Source needed | Assessment |
| --- | --- | --- | --- |
| Contact-gated assault | Current apparent contact at a hex; no hidden identity or composition | Deterministic side visibility/contact event at same cut | Recommended first Combat RC rule, if Land rules and scenario scope support it. Preserves ordinary fog and gives a clear action. |
| Named open-contact scenario | At most one adjacent apparent target hex plus offerability bit | Explicit profile-specific disclosure event | Valid bounded test scenario if intentionally approved; does not establish general fog safety. |
| Blind hex assault | A chosen hex with no confirmed current contact | Explicit blind-attack rule and outcome disclosure policy | Defer until source rules justify intent, cost, and miss behavior. |

The current recommendation is to build the shared side-observation mechanism
first, then use **contact-gated assault** for the first RC scenario. This is a
design recommendation, not a claim that the existing P01 source already
contains contact or that Land rules permit every suggested attack. The
open-contact profile can remain an opt-in test scenario. Do not implement it
as the default or convert the original 300 roots to it.

## Smallest safe proof slice

1. Select a written, bounded visibility rule for one scenario: sensor/LOS
   inputs, terrain and weather effects, contact creation/loss, last-known
   retention, and exact allowed disclosure. A missing rule branch closes the
   offer. Pin its ID, source version and fixtures.
2. Capture a **new authentic** same-cut side-contact event from Core. Preserve
   the original P01 archive and its separate support status. No target alias
   comes from private World position alone.
3. In one integrated private Core/Host vertical, persist the observation,
   issue a fenced opening, accept selected assault or explicit decline,
   adjudicate, persist receipt, and prove cold-restart readback. No public
   route until this vertical and its source certificate pass.
4. Prove fog noninterference: equal side-visible histories with different
   hidden defender data must yield byte-equal observation and offers, and the
   same generic rejection surface. Test stale sightings, disappearing
   contacts, hidden changes, different seats, retry/restart, and cut races.
5. Run a new profile-specific support certificate across the pinned scenario
   set. Keep original 300-root AC-003/006/007/008 certificate open.

The remaining scope decision is the exact visibility rule and disclosure
fields for that bounded scenario. Until they are pinned to source evidence,
the owner-action vertical must fail closed rather than infer a target.
