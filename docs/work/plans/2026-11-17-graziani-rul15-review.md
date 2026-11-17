# Independent audit of RUL-GRA-15

**Disposition:** Source review supports this as an explicitly provisional
ordering choice, not as a printed correction. No contradiction found in the
ruling's two Campaign dice or corrected deployment. Its secrecy and recovery
gates need precise contracts before execution; commitment does not itself
license revealing the secret plot.

## Source findings

1. **After repair is chosen, not sourced.** Land §5.2, printed/PDF p. 12,
   sequences Fleet Assignment then Fleet Repair then the first player's
   Reserve Designation. Land §32.73, p. 44, places the secret Axis plot during
   Fleet Assignment and execution in a Tactical Naval Movement Segment, which
   §5.2 does not name. The ruling inserts resolution after repair and before
   reserve, consistently identifying that placement as project policy. The
   separate Air outline/mission completion and the Chariot Raid do not prove
   this placement. Under the selected policy, a valid same-stage completed
   repair changes the snapshot before attack; §30.37 requires six consecutive
   OpStages at Alexandria for one Gun Rating level and says early movement
   restarts work. It does not settle precisely how a new hit interacts with
   repair progress. The attack-before-repair alternative remains meaningfully
   different; do not assert one is official.
2. **Allowance and dice are separate.** §32.72 uses one secret die for the
   scenario-wide number of attacks, +2 if Campaign. §32.73 limits execution
   to one per OpStage. §32.74(A) uses a separate one-die roll to select a
   bombardment column, again +2 if Campaign; §32.74(B) uses two sequentially
   read dice for the result with no stated Campaign addition. The ruling
   describes this distinction correctly. Which profile qualifies as Campaign,
   when the initial allowance is drawn or restored, and whether a plot that
   cannot execute consumes it are separate gates. The zero-damage debit is an
   explicitly chosen accounting policy, not a special printed case.
3. **Privacy requires more than sealed initial choices.** §32.73 expressly
   says the plot is secret; it does not prescribe a simultaneous sealed
   protocol or exact disclosure time. The ruling appropriately labels sealing
   both sides' choices as project policy. “Until both choices are committed”
   sets a minimum seal, not source authority to reveal the plot, allowance,
   future plan, harbor occupancy, or any other hidden fact at that moment.
   Specify each audience's authorized observations before/after commitment,
   attack, and damage allocation; determine legal target knowledge separately.
   §32.74(B) gives **Axis** the choice of how damage points are distributed
   among ships in Alexandria Harbor, but does not itself specify a software
   reveal protocol or empty-harbor action. Error text and timing must not make
   unobserved harbor contents discoverable.
4. **Separate committed phases survive a blocked attack.** The proposal
   selects separate Assignment, Repair and attack boundaries. Define their
   journal identities, pending obligations and crash/retry rules explicitly.
   If assignment and repair have committed, an unresolved target or blocked
   allocation must not undo or duplicate those phases. A future attack commit
   must prevent duplicate RNG, allowance debit and damage across retry, while
   allowing any required private allocation step to resume. This is an
   engineering contract, not a claim that the source mandates one transaction.
   A fully atomic stage is a different design and cannot be mixed silently
   with already published phase commits.
5. **Weather, harbor and deployment are distinct.** §32.75 bars execution in
   rainstorm or sandstorm on map section E; unknown section weather is not
   clear. §32.74(B) limits damage to ships in Alexandria Harbor. It does not
   resolve whether an empty harbor executes, rolls, refunds or consumes a
   plotted attack. Corrected §60.45 removes the later printed ship list;
   schedule §30.6 allows one of the marked cruisers and two of the marked
   destroyers at Malta. Initial legal deployment does not establish later
   actual harbor contents, and the scenario's movement restriction must not be
   bypassed to make a target. The ruling correctly leaves these as gates.

## Distinguishing vectors

- A Campaign-enabled hypothetical allowance die 1 yields 3 attacks and a
  separate column die 1 selects the third bombardment column; sequential
  damage dice 2 then 5 remain the table lookup 25, with no +2 applied to the
  digits. Campaign applicability itself remains unresolved.
- A legal Alexandria ship completes its sixth consecutive repair stage.
  Selected order applies repair, then attack; compare an independently modeled
  attack-before-repair alternative without assuming how the hit affects later
  repair. Check at-most-one attack per OpStage and scenario allowance together.
- Commit both sealed decisions, then interrupt after repair and before
  allocation. Resume exactly once without rerolling accepted RNG, duplicating
  repair, or disclosing the private plot merely because both choices exist.
  An empty harbor remains unresolved rather than an observable forced zero.
- Compare section E storm, clear and unknown; only known rainstorm/sandstorm
  establishes §32.75's prohibition. Test no-plot separately from an executed
  zero-damage result and from an invalid or unexecutable plot.

## Evidence

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 12, 42, 44, §§5.2, 30.34–.37, 32.71–.75, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 §60.45, `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 31 §30.6, `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`;
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §60.45, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Compared prior [fleet sequence review](2026-11-17-graziani-fleet-sequence-review.md).
Same-edition check; no exhaustive later-errata review, actual attack, gameplay
execution or runtime modification.
