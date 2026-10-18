# P01 owner projection draft

**Status:** private design scaffold only. No owner observation, legal action, submission,
P01 admission, P02 selection, or P03 native consumer is enabled by this packet.

The authenticated P01 assessment currently has only `unsupported`, `admitted=false`, and
`publicActions=[]`. Its `p01.` candidate digest includes the authoritative prefix, private
participant bindings, and other hidden facts. It must remain Umpire-only.

This draft models a separate Umpire-retained `draft outward alias → private candidate` map.
The alias hashes a domain tag, one NUL byte and compact ASCII JSON with exactly four proposed
owner-visible facts, in schema order: audience side, **visible** decision revision, capability
kind and apparent target reference. No authoritative state version, Source/Proof hash, private
candidate ID, hidden opponent identity, RNG, internal event count or blocker reason participates.
The visible revision must advance only when an approved owner projection changes. The draft
mapping rejects an ambiguous capability; it never publishes an alias as a legal action.

`CampaignObservationV6DisclosureIdentity` supplies the existing pattern of audience-visible
capability grouping and duplicate rejection. `CampaignObservationV7ActionDerivation.MapSubmission`
supplies the pattern of matching a submitted visible candidate/revision before mapping back to
authority. Neither is a Combat codec or an approval of these proposed P01 fields.

The following gates remain before this draft can become a contract or runtime input:

1. Approve and type the owner observation fields, apparent target policy, campaign/window scope,
   visible revision rules, stable alias encoding, and side-safe submission rejection shape.
2. Authenticate two complete R03 histories whose *approved* owner-visible facts are byte-equal
   while hidden facts differ. No such pair is certified by this scaffold. Synthetic tests only
   prove that changing a private mapping target cannot salt the draft alias.
3. On those authentic histories, compare visible bytes, candidate membership/order/IDs,
   accepted and rejected submissions, waiting/closed output, and side-log bytes. If hidden
   differences change legality, the bounded profile must reject the pair before any owner
   candidate opens. A generic error after a differing command outcome is insufficient.
4. Review transport/timing policy separately. This pure byte model cannot claim traffic or
   timing equivalence.

The executable module is intentionally disconnected from P01 assessment. Its only outward
helper emits the existing constant denial bytes. `compare_trial_visibility` rejects synthetic
equal-visible/different-legality inputs, but is not an authenticated twin-history proof.
