# Graziani pre-game Axis GT2 supply planning, version 1

This bounded setup action records part of a real Axis pre-game convoy plan in
the normal campaign session and journal. It is not an admitted campaign and
cannot complete setup, advance the game clock, execute a convoy or arrival, or
claim that omitted cargo is zero. GT1, other cargo, production pools, route
constraints, and later planning remain unvalidated and block completion.
The nested source initializer's cohesion and capability expenditure are
caller-supplied operational values, not source-certified historical defaults.
In particular, zero values used in tests do not establish historical state.

The selected provisional profile binds the ordinary two-turn lead time to
September II for the GT2/September IV target (Land §32.43), uses September 1940
letter B (Axis §56.4), rolls §32.46's B column (0,1,1,2,2,3 for d6 1–6),
interprets “may bring” (§32.44) as a maximum, does not select the optional
rerouting variant (errata §56.29), and treats this roll as the first recorded
pre-game RNG draw. The date, maximum interpretation, and RNG order are explicit
provisional rulings, not source facts. Scenario §60.37 restricts these initial
plans to lanes 2, 3 and 6. Each lane/operation-stage allocation is at most
three supply units (§32.62). Axis may increment or decrement a private partial
allocation; total allocated cannot exceed the recorded B result.
Later review may establish a different pre-game RNG order. Adopting it requires
a new profile and setup identity; retained version 3 journals keep their
original first-draw interpretation and must not be reinterpreted retroactively.
The source chart §56.18 identifies lane 2 as West Sicily to Tripoli, lane 3 as
Italy to Benghazi, and lane 6 as Italy to Tobruk. These identities do not prove
current port availability or complete route legality.

The version 3 genesis object contains `contractVersion`, `eventType`,
`campaignId`, `policyId`, `sources`, `sourceCreationBase64`, `sourceSnapshotBase64` in
that order. It embeds unchanged version 2 source initialization evidence.
The setup hash binds the base source setup hash, policy ID, provisional date,
target turn, RNG ordering and the source-linked lane mapping. The checkpoint contains `contractVersion`,
`campaignId`, `stateVersion`, `rulesetHash`, `contentHash`, `setupHash`,
`positionId`, `randomAlgorithm`, `randomSeed`, `randomCursor`, `die`,
`maximumSupply`, `allocations`, and `sourceSnapshotBase64`, in that order.
Before rolling, die and maximum are null; allocations are sorted by lane and
stage and omit zeros. The fixed position is a pre-game setup cut, not a Land
turn position.

Every accepted Axis action emits exactly one canonical event containing
`contractVersion`, `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `policyId`, `actionId`, `sources`, `randomCursorBefore`,
`randomCursorAfter`, followed by `die` and `maximumSupply` for a roll or
`lane`, `operationStage`, and `delta` for an allocation. No action resolves the
other pre-game obligations or signals plan completion. Replay re-adjudicates
each retained command and compares canonical event, receipt, checkpoint and
private contact evidence. Axis receives only its own setup projection; no
pre-game plan or private action ID is disclosed to Commonwealth. The Axis
projection labels the policy provisional and explains the date, maximum,
first-draw and standard-letter assumptions without revealing the RNG seed.
The v2 user-space manifest's `dotnetTypes` list includes the two pre-game
candidate classes because it inventories all current CLR subclasses; those
entries do not authorize their disclosure under legacy observation policies.
Legacy action-set policies and codecs reject pre-game candidates, and the
Boundary-tagged tests verify Axis-only access and trusted-draft serialization.

## Development Host surface

The durable Host exposes this cut only in Development when its campaign journal
is configured. All `/api/v2/campaigns` requests require loopback and same-origin
access. Operator-authenticated `POST /api/v2/campaigns/pregame-source` accepts
the existing source-creation body: API `contractVersion` 2, campaign and ruleset
IDs, seed, scenario ID, canonical `contentBase64` with expected hash, and the
caller-supplied capability-expenditure fraction and cohesion. It returns HTTP 201
with `status: incomplete` and `policyStatus: provisional`.

Clients can instead use operator-authenticated
`POST /api/v2/campaigns/pregame-source/graziani` with API `contractVersion` 2,
`campaignId`, `seed`, `capabilityNumerator`, `capabilityDenominator`, and
`cohesion`. The Host supplies the pinned, bundled Graziani excerpt and the
canonical ruleset and scenario identities. This creates the same version 3
journal as the content-upload endpoint; identical requests are retryable and
conflicting requests for an existing campaign are rejected. The operational
inputs are still caller-supplied and are not historical defaults.

For example, an operator may submit the following JSON with the
`X-Sandtable-Operator` header. The values shown are illustrative, not certified
Graziani setup values:

```json
{
  "contractVersion": 2,
  "campaignId": "graziani-example",
  "seed": 0,
  "capabilityNumerator": 0,
  "capabilityDenominator": 1,
  "cohesion": 0
}
```

The operator can issue an Axis grant via `POST /{campaignId}/grants/axis`; the
player redeems it through `POST /{campaignId}/seats`. Commonwealth grants and
credentials are refused for this setup cut. Axis uses the existing
`POST /{campaignId}/openings` and `POST /{campaignId}/actions` routes with
`stage: pregame-axis-supply` and fixed `actingElementId: land.pregame-axis-supply`.
The opening contains an Axis-safe visible projection and opaque Host action
references; submission binds the seat, opening revision and selected reference.
The Host retains canonical command, event, receipt, checkpoint and contact
evidence in its ordinary journal, and cold replay re-adjudicates each step.
Neither the source content nor this API declares a complete plan, admits a
playable campaign, advances a turn, or executes an arrival.
