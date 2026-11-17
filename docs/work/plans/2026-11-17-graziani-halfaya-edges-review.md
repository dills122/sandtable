# Halfaya local edges: source review

**Disposition:** The reproduced Map C crop supports the local adjacency and
escarpment reading; §10.21(b) then blocks ZOC across that shared edge in both
directions. Keep the proposed map/state admission gates. Qualify the statement
that §8.42 **alone** establishes vehicles may go down *only* by track: its
express text permits a crossing track for downhill travel and forbids vehicle
uphill travel; the full terrain chart and any other applicable provisions must
also be checked before asserting a universal no-track prohibition.

## Reproduction and scope

Reproduced the [candidate's](2026-11-17-graziani-halfaya-edges-next.md) exact
300-dpi crop from [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf)
PDF p. 3, using its stated `pdftoppm` parameters. The source package SHA-256
is `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
the rendered crop hash is
`61217cc97a0b079f9d186a17d48bb2cca6619e085566aa5808aa116f32c1571d`.
Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14–15 and 17–18 §§8.35, 8.42 and 10.11–.15, 10.21; SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
The September errata file matches
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
this review did not independently repeat the candidate's errata search.

Map-key PDF p. 1 was rendered, but its visual output was not available for
inspection in this review. Thus the match to the printed terrain-key sample and
chart-specific downhill prohibition are **not independently verified here**;
the candidate's prior visual key reading remains evidence, not a second check.
This is a prompted recheck using the same source edition, not independent
publisher material or a complete map transcription.

## Observations and qualifications

- The crop visibly labels 3920 and 3921 in neighboring cells, with a blue
  checkered line following their shared vertical side. No dark stippled band
  matching the nearby escarpment artwork is visible on that side. Its blue
  feature is not classified here; do not call the side featureless or prove
  movement/positive ZOC from absence of the dark band. Nearby track and blue
  cross-tied linework crowd the upper vertex; no particular side crossing is
  certified by this review.
- The next cell east of 3921 contains the Halfaya Pass label; a dark band with
  splash edge runs along their shared vertical side, consistent with the
  candidate's escarpment reading. The cell number is obscured by artwork. Local
  sequence plus the scenario's Halfaya C3922 locator supports the identity,
  but does not turn an obscured digit into an independently legible label. The
  stippling lies on the Halfaya/east side: assuming the feature is an
  escarpment, §8.35 identifies that as down, so C3921 is up and C3922 down.
- C3920 and inferred C3922 have C3921 between them and do not share a hexside.
  Direct ZOC between those cells is therefore absent; this does not rule out
  ZOC from other cells or demonstrate any setup is globally contact-free.
- Land §10.21(b) expressly excludes ZOC through an escarpment hexside without
  a direction or track exception. Thus, **if** this shared side and cell
  identity are accepted, both directed projections are false even for a
  hypothetical movement crossing. That is a ZOC result, not a finding that
  units cannot be adjacent or attack by other means. On the blue-marked
  3920/3921 side, positive ZOC needs all other rules and source evidence:
  §10.11's qualifying units/stack, §10.14 cohesion, §10.15's ten raw defensive
  close-assault point threshold and §10.21(c)'s unit-specific entry condition.
  Assess each projecting side independently. Do not conflate an enemy ZOC's
  projection with its effect on movement in a friendly-occupied hex (§10.26).
- §8.42 explicitly forbids vehicles moving **up** and permits vehicles to go
  **down** using a track that crosses the escarpment, with +8 CP and six
  breakdown points. §8.35 defines splash direction. Its text does not itself
  expressly say that no vehicle may go down without a track. The terrain chart
  might supply that restriction; its relevant cells were not visually checked
  here. Retain an unknown for a specific downhill, nonmotorized or road/track
  move until the complete chart and crossing are verified. Track movement does
  not override §10.21(b).

## Proposed vectors and remaining work

Assert bilateral ZOC false across the **accepted** escarpment even with a
synthetic traversable crossing. For C3920/C3921, preserve the unclassified
blue mark and test a qualifying projector, an under-threshold stack, prohibited
entry and each direction separately only after the relevant feature/rules are
accepted. Assert C3920/C3922 nonadjacency; test unrelated neighboring ZOCs
and combat separately. Independently inspect key/chart at readable resolution,
verify the obscured C3922 label or coordinate identity, classify the blue mark,
review errata and certify per-edge traversal before admission. These are
unexecuted vectors. No code, runtime state, map content or readiness changed.
