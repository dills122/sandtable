# RUL-GRA-18: anti-armor ammunition review

**Disposition:** bounded 1/2-ammo policy has direct source support and states
its scope appropriately. It does not define which anti-armor involvement counts
as participation, so do not silently convert allocation, being targeted, or
an aborted action into an expenditure. This is a fresh check of the final
ruling by the author of earlier ammunition research, not a fresh-author or
independent-reader audit; the separately attributed
[independent review](2026-11-17-graziani-ammo-combat-independent.md) remains
distinct.

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 5, 19–21, 43 §§3.1, 11.21–.22, 12.15–.18, 14.0–.26,
32.11, 32.21–.22; scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Read the independent review and visually revisited its cited September errata
§50.12, PDF p. 3, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Other editions, full errata and scenario reachability not exhaustively checked.

## Findings

* §3.1 explicitly describes assault as ground combat with anti-armor combat
  followed by close assault. This materially strengthens RUL-GRA-18 over a
  mere terminological analogy. §11.21 also uses “Anti-Tank, Close” in a CP
  discussion; errata §50.12 refers to assault as anti-armor or close in a
  separate no-ammunition context. Neither specifies a separate abstract charge
  for every step of an assault.
* §32.21 prints one ammo point for a non-phasing battalion-equivalent
  participating in assault, twice that for the phasing unit. It counts
  individual component battalion-equivalents; §32.22 makes the debit at the
  instant of combat. The ruling's 1/2 numbers and side mapping are correct.
  Applying them to an otherwise eligible unit participating only in anti-armor
  is a bounded interpretation of the glossary and §32.21, not a quoted
  anti-armor-specific table entry. §32.11 replaces other expenditure rules;
  §14.24 by itself cannot establish an additional Land-only amount.
* **Participation remains an admission boundary.** §14.0/.11/.12 distinguish
  TOE points firing anti-armor from armor units affected by fire; §14.23
  allows a conditional reassignment when the target lacks defending armor.
  RUL-GRA-18 does not settle whether being fired upon, assigning a point that
  never fires, or reassigning it counts as participation. It also does not
  settle the situation where only a portion of a battalion fires, when combat
  begins but no eligible target exists, or which side's instant applies.
  “Otherwise eligible” must not become a hidden implicit answer to these
  questions. Preserve unsupported cases and authoritative event ordering.
* §14.26 prevents assigning the same TOE point to both anti-armor and close
  assault within one segment, but does not settle aggregate unit costs.
  Likewise §12.18 permits qualified forward artillery to participate in a
  later combat role after barrage; this does not establish either a single
  charge or distinct additive charges. The ruling correctly leaves split
  battalions, combined barrage/assault, repeated segments, partial battalions
  and shortages outside its scope. Its no-separate-debit alternative remains
  a comparison case, not an equal-strength printed source rule.

## Conditional checks before admission

1. A complete eligible battalion actually firing only anti-armor: non-phasing
   1 and phasing 2 under this policy, from an accessible friendly source at the
   adopted combat instant. Synthetic fixture does not prove scenario contact.
2. Assigned but no firing; reassigned after no defender armor; non-phasing
   armor merely subject to fire; pinned or aborted actor; partial battalion:
   explicitly gate pending participation and timing decisions. Do not silently
   charge or waive each from the bounded rule.
3. Same battalion also contributes to close assault, barrage, or another
   segment: gate aggregation rather than applying RUL-GRA-18 twice or choosing
   a once-per-stage rule. Maintain component identities and avoid duplicate
   debits on replay.
4. Check insufficient supply, privacy-safe action responses, authorized
   player observations and deterministic replay once those contracts exist.

No runtime changes or tests. Planning decision still needs a reviewed
participation definition and accounting policy before gameplay admission.
