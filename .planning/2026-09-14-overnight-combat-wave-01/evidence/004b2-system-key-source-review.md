# B2 System semantic key source review

Read-only ordinary reviewer cycle_finish_reviewer inspected initial B2 helper and native shapes.
Initial extraction effect.payload.disposition incorrectly yielded null for actual Result2
disposition-recorded and custody-settled, whose semantic value is effect.payload.kind.
Native reserve/bridge Release unit-disposition stores effect.choice. Use explicit authenticated
effect-tag mapping; other admitted tags null. Do not substitute command choice, state status,
receipt, or synthesized fallback strings. Preserve explicit null decisionId.

Sources: World schema19/23, Result2 schema12/15 and oracle193–197, Reserve schema12/oracle200.
Round cancellation uses effect.cause but cancelled-Round execution is not in B1 registry; no
new scope follows. Root sent finding to sole writer before B2 freeze. Acceptance still pending
actual correction and focused regression evidence.
