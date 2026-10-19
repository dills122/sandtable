# MAP-08B2 private accepted-outcome head binding v1

Status: host-private contract. No diary copy, public successor `/view`, HTTP submission,
or durable receipt is authorized by this binding.

For each accepted temporary campaign step, the grain holds the predecessor `ExerciseSession`
and the one `ExerciseStepResult` returned by Umpire. Before installing its successor, it creates
`CampaignHostAcceptedStep` from that result's canonical command, ordered current event bytes,
canonical acceptance receipt and snapshot checkpoint. It asks the Core hosting
`CampaignAudienceOutcomeSource` for one paired Axis/Commonwealth decision. The host must not
adjudicate a second turn; Core's in-memory proof replay does not mutate or publish authority.

The process-local audience tracker binds the paired decisions and two successor side cuts to
one exact committed head. Its initial head is the SHA-256 of the canonical creation snapshot.
For an accepted step, the tracker requires:

- The prior head hash equals its retained committed head; the successor head hash equals the
  accepted checkpoint hash. The receipt and canonical command agree on campaign, actor,
  action, prior and successor versions, and resulting position.
  The receipt has no snapshot-hash field: the host binds its canonical receipt to the
  checkpoint and paired head hashes through the same accepted-step record and Core
  in-memory replay. A journal event hash alone is not proof of tracker publication.
- Each decision has contract version 1, its own distinct authorized audience, matching
  campaign/source binding, command/event hash, actor, event identity, index, prior/successor
  heads and consecutive versions. Source binding uses the Core proof's exact ordered
  ruleset/setup/content/scenario preimage, not the tracker's presentation pin string.
- Both side cuts agree on campaign, source, version, position, turn, phase and stage, and
  pass existing action/visibility continuity checks. The pair and both cuts are prepared
  before either side's tracked head changes. An absent or inconsistent proof/cut invalidates
  private audience reads and leaves the previous authority session uncommitted.

The grain publishes the tracker and committed session in one serialized, synchronous turn,
with no provider call or `await` between them. Its existing request record retains the accepted
reply, so an exact retry returns that result before any second source or tracker advance.
Session generation still scopes read handles and outward request admission; revocation checks
remain at the ingress boundary. A typed `Unavailable` diary disposition is retained privately
as an explicit unapproved-copy state, not silently converted to a public entry. The browser
projector remains initial-only and returns neutral unavailable for all successor cuts.

Acceptance: focused Core/Orleans tests prove exact head, source, event, command, receipt,
version and both-side binding; wrong-side/missing proof fails closed; retry does not advance
again; hidden-only contact/action continuity survives; a revoked seat cannot access the
retained private step through outward ingress. No raw Core evidence enters browser JSON.
