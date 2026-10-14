# N1 review1 disposition

Review instance:1 of9; round1 of3; recovery0 of2. Reviewed target:5a0dbc82e7046cd9dc4a8f42a2fc48913099fc75. Report SHA256:e5462bad83f7e3b335e999c7db906733883e375c46ea06fd6b5fe359ed4078a4. Verdict:Not ready, sole P1 discovery-identity collision.

## P1 — Accept

The retained independent discovery log has98 records/89unique IDs. Existing test_partition.discovery_rows rejects it with Empty/duplicate discovery. Hosted CI38609798518 attempt1 on testedmergef635981bef9b33e94005f5b01cf10a6d907c5833 failed prepare; test jobs skipped and no completed inventory exists. Focused passing bodies did not satisfy that gate.

Correction product:3bcaedc74a184a2adc28ad6e722787304c04e214. Only ContractTests changed: Cases now emits ordinal,name and the theory accepts int _,string name. The ordinal remains in xUnit's display name; the documented discard is intentionally identity-only. No custom display override, analyzer suppression, test partition/helper/config change, fixture/hash/literal edit or runtime-module change. All56 fixture rows remain and the theory body is byte-for-byte unchanged. xUnit1026 rejects a named unused ordinal; that first compilation failure was a harness correction attempt, not behavioral RED. The official rule documents discard parameters: https://xunit.net/xunit.analyzers/rules/xUnit1026.

Actual Release discovery now has98 rows/98unique IDs with all56 ordinals0..55. The unchanged discovery_rows gate accepts it. Release focused validation passed98/98,0failed/0skipped,1m56.776s. Scoped formatting and diff whitespace checks pass. Original reference/schema/fixture/modules are unchanged, so prior author and independent reference56/798 evidence remains applicable. Fresh full hosted CI remains required on the new published head; the failed old run is not retried or waived.

The factual carrier, cumulative costs, byte ownership, source/claim authentication, support catalogue and excluded public/continuation scopes are unchanged. The report's prose correction97shapes was accepted and author explanation corrected. Recovery count remains0; review1 consumes1/9. Brain owns fresh neutral review2 and CI on the new immutable publication.
