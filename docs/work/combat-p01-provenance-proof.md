# P01 retained provenance and participant audit

**Status:** read-only evidence check for ordinal-two Combat Position; no support or native admission.

Run `python3 docs/work/verify-combat-p01-provenance.py PATH_TO_P01_EVIDENCE` against the retained
P01 evidence directory. The directory contains the 300 authentic R03 terminal vectors generated
from the frozen reference and original creation/eleven-ledger inputs. Its path is an argument so
neither large row vectors nor primary PDFs enter Git.

The checker pins the summary, manifest, physical predicate audit, Land PDF and errata bytes before
parsing rows. It verifies all 11 compressed chunks against the summary hashes; 300 contiguous unique
keys; seven canonical vector hashes per row; 11 parent ledgers per row; ordinal-one original and
ordinal-two active cycle; Combat Position Determination; unchanged R03 World and RNG; exactly two
independent, co-located representation bindings to the expected opposing battalions; acting-side
participant selection; and agreement with the independent physical partition. It also checks the
retained Land rules and September errata PDF bytes against the adopted pins. On the retained
2026-10-17 evidence it reports 44 provisional positive and 256 provisional empty rows.

This checks **retained-vector consistency**. It does not rerun the 300 expensive source replays,
prove that the original ledgers are authentic from scratch, certify Combat eligibility, choose
public aliases, or admit the ordinal-two native reader. The existing R03 reference and native
consumer own source replay; P01 must invoke equivalent authentication before it trusts these
facts. Current `CampaignCombatCertification.Admit` and `CertifyInitialProfileFacts` accept only
ordinal one. The audit neither changes that guard nor treats the provisional partition as
supported actions.

P01 contract acceptance still needs source-backed result and settlement coverage, retreat and
custody geometry, explicit decline-only RBA, exact private candidate identity and audience
privacy, adversarial negatives, and independent review. P02 selection transitions and P03 native
consumer remain separate gates. The accepted bounded policy in
[`combat-cycle-policy-reconciliation.md`](../design/combat-cycle-policy-reconciliation.md)
continues to apply, including the adopted source amendment at +2 defender loss rolls 34–36.
