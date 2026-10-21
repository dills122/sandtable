# P01 Combat RC contact disclosure and owner opening

**Status:** candidate under fog-of-war research; not approved for implementation.
Read [the fog-of-war research spike](../research/combat-fog-of-war-architecture-spike.md)
before selecting any contact rule. No action, observation, grant, or public
route is enabled by this document. Original R03 roots retain their 44
provisional positive / 256 absent / zero admitted classification.

## Candidate to unblock the first playable Combat slice

Create one explicitly labelled **open-contact RC scenario profile**. At the
Combat selection cut, Umpire records a side-specific contact disclosure before
opening an owner decision. That disclosure is an authoritative game event,
not a value inferred by Maproom, supplied by a player, or copied from an
unlabelled private World field. The profile makes an apparent target hex and
the fact that it is currently offerable public to that side. It does not make
the defender's identity, composition, CP, ammunition, retreat choices, RNG,
or other private state public. Ordinary fog-of-war campaigns remain outside
this bounded RC profile until a separate visibility policy is proven.

The original 300 R03 cuts contain no contact-disclosure event or approved
same-cut side-visibility record. They cannot be retroactively labelled as
open-contact. Continue using them to test private combat replay and negative
admission; capture a new pinned RC cut for this profile.

## Source and ordering

1. Core validates creation, content, rules, Control, World, prior inputs, and
   the exact current position. Only the acting side at the ordinal-two Combat
   selection boundary may receive this disclosure.
2. Core applies the named open-contact profile to the authoritative state and
   derives an audience-specific list of apparent adjacent target hexes. For
   the first RC slice, the list has at most one hex. Core also derives the
   public `offerable` bit under the **complete** source-legal eligibility
   rules for this profile. Any unresolved rule branch closes the offer.
3. Core emits canonical `combat-contact-disclosed` bytes that bind campaign,
   cycle, position, audience, profile/rules hash, predecessor state version
   and journal prefix, source-cut digest, visible hex or null, and `offerable`.
   Chronicle/Archives conditionally appends and reads those exact bytes back
   before any owner opening or alias is exposed. Duplicate or stale cut
   returns the original disclosure; changed source cannot replace it.
4. The approved A2 owner context and refs are derived only from the persisted
   disclosure plus authenticated own-side facts and a valid local seat grant.
   The host fences and persists the opening with its private mapping, then
   returns the exact read-back visible bytes. An absent or non-offerable
   disclosure yields no selection opening. Timeout never becomes a decline.
5. Selection or explicit decline follows the approved A2 acceptance, receipt,
   retry, and cold-restart rules. Core adjudicates the selected assault;
   Maproom only shows the opening and submits the typed choice.

No caller-supplied apparent hex, `pub.` ref, offerability bit, seat, clock,
or source hash is accepted as authority. No raw defender record enters the
visible disclosure or generic rejection. A contact event does not itself
admit Combat or authorize public activation.

## Proof gates for one integrated private vertical

- One new source capture must carry the profile ID and exact disclosure event
  from the same validated cut. Physical source pins, process-boundary readback,
  cold restart, and source mutation tests must pass.
- A pair of authentic histories with equal disclosed contact/offerability and
  different hidden defender facts must produce byte-equal visible opening,
  aliases, membership, and generic rejection. If the source rules cannot
  provide such a pair, no AC-007 claim or public action is made.
- The offered candidate must be source-legal for every history in its
  equal-visible class. If legality depends on an undisclosed defender fact,
  either disclose the single offerability result by this named RC policy or
  withhold the offer. Never use hidden-history-dependent offer presence under
  a policy that promises that fact is hidden.
- Same-seat retry returns the committed receipt after restart. Wrong seat,
  stale cut/revision, changed visibility event, revoked grant, changed clock
  fence, duplicate acceptance, and missing or tampered journal bytes fail
  closed without a second game event.
- A new bounded support certificate classifies every row and every
  source-legal mode for this **new** RC profile. The original 300-root P01
  certificate stays open and is reported separately.

## Shipping boundary

First merge one reviewed private Core/host vertical containing the disclosure,
owner opening, accepted action, adjudication, durable receipt, and readback.
Then prove the new source capture and support certificate. Public activation
requires a separately reviewed exact profile scope and an all-green
exact-head gate. Label the RC scenario as open-contact in the product; do not
claim general fog-safe Combat from this bounded profile.
