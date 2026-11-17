# Independent review: Matruh and Benghazi working rulings

**Scope:** review of RUL-GRA-03 and RUL-GRA-05, not approval or source admission. This reader saw the existing rulings before examining the sources; this is not blind double entry. It does not review the other rulings or establish complete errata, setup, rights, map topology, or ruleset coverage.

## Findings

### Matruh: capacity remains an explicit conflict

The selected assigned-unit reading is defensible, but the ruling's assertion that the two assignees do not consume the six attachment slots is a choice between conflicting texts, not an automatic consequence of §19.5. Commonwealth OA p. 10 says no units may be assigned and **up to six units may be attached**, referencing the chart. Common chart p. 6 §19.5 labels the row maximum attachments, with six units for Matruh in turns 1–12, no tanks and one gun class. Its key and company modification qualify what counts; they do not expressly discuss the newly assigned Matruh units. September errata §19.5 says chart attachments are additional to normally assigned units, even when those assigned are elsewhere. The same errata §4.44b specifically assigns the Buffs and Hampshires to Matruh. Read together, the later general clarification supports six *additional* units; the OA's specific six-attachment language supports six *total attached* units. Neither text expressly resolves this exceptional combination.

**Action:** label the six-additional calculation explicitly as a selected precedence ruling against the OA's literal total-attachment wording, retain six-total as an equal-status candidate for release review, and avoid declaring source proof. Example discriminator, assuming full-strength eligible battalions, co-location, and no other restrictions: two Matruh assignees attached plus five non-assigned attached gives seven total; the selected extra-only reading allows the count while the literal six-total reading rejects it. Both still require chart type, company, stacking, and legality checks. With the two assignees detached, six additional attachments fits either count; their assignment persists under the proposed relationship model. These are proposed review vectors, not established legal scenario states.

### Matruh: setup choice is supported, but conditional on placement

Scenario §60.41 lists the garrison at D3714, with Essex, Durham and South Staffordshires after `Att`; §59.2 defines that indicator as attached but not assigned. The erratum names the other two assignees but does not itself state a hex or attachment. Section §59.2 also permits owner-selected attachment and detachment among units listed in the same starting hex, subject to formation and stacking limits. Thus scenario-listed attachments are source disposition and can be changed under that setup provision; it does not independently locate the errata-named battalions or grant a general choice across different hexes. The actual composition expansion, default attachment under §4.25, current positions, stacking and any special exception still need a complete cross-check. Section §59.2 also contains an over-limit setup provision, with restrictions on adding and replacing subordinates: account for it if a particular source-listed formation exceeds limits; do not simply reject all such source setups.

**Action:** retain separately (1) printed disposition, (2) inferred initial locations and default attachments, and (3) final legal owner choices. Establish an actual source-supported starting state before turning either illustrative capacity vector into a positive historical fixture. Explicitly review whether §59.2's over-limit provision applies before defining validation. The OA permits voluntary HQ movement in the operations stage of a close assault as well as after conversion; do not model an unconditional movement ban or assume duration of the exception beyond its words.

### Benghazi: A4827 is well-supported; preserve B4827 as conflicting text

Scenario p. 27 §60.31 prints Benghazi `(B4827)` for the garrison; p. 28 §60.92(B) repeats `(B 4827)` for the land-only supply row. The map A scan depicts the named coastal city, and its location summary identifies Benghazi as A4827; the printed city mark obscures the hex number, so its exact identity rests on the summary and adjoining numbering. The §37.42 land-distance chart also identifies Benghazi as A4827. These are consistent records in one edition, not independent editions, and the repeated B is still genuine scenario text. The A-coordinate choice preserves the named city, and is a persuasive planning interpretation; the text does not prove *why* both scenario rows say B. The literal B interpretation and deferral remain viable alternatives pending clarification.

**Action:** phrase the repeated section-letter mistake as an inference, preserve both raw B values, and bind any A-coordinate behavior to an explicit versioned choice. Require a test that rejects accidental section aliasing, plus city identity, control/port, setup and replay checks. Do not infer roads, seam transforms or supply routes from the city label. A separate location summary marks no assigned garrison at Benghazi while the Axis OA explicitly titles a Benghazi Garrison at A4827: treat that summary column discrepancy as a separate source issue rather than using it to erase the scenario garrison.

## Evidence and limits

Direct visual comparisons used these retained PDF bytes, verified with SHA-256 in this review:

- Scenario scan `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`: printed pp. 25–28, especially §§59.2, 60.31, 60.41 and 60.92(B).
- Commonwealth OA `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`: PDF p. 10, Matruh Garrison.
- Common chart `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`: PDF p. 6, §19.5 and modifications.
- September errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`: §§4.44b and 19.5; these passages were checked in extracted text. This is not a comprehensive visual search of all corrections.
- Map package `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`: Map A overview and location summary; §37.42 distance chart reviewed via its rendered chart. The prior independent map review covers Map B separately; this review does not independently certify its topology.

Scenario and OA images were checked against renders already present in the working environment; their corresponding source PDFs matched the listed hashes. The chart was newly rendered from its matched PDF. Hashes identify bytes, not provenance or permission. The Axis OA and secondary location summary observations above were read from retained renders; their source package hashes were not independently checked in this bounded review. No claim of official correction, approval, complete setup, or exhaustive errata search follows.
