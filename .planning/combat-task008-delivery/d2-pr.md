# PR metadata

Branch: codex/combat-task008-reserve-completion
Base: codex/combat-task008-reserve-designation
Title: Add creation-rooted Reserve completion codec

Task008 D2 now derives Reserve completion2 bytes and ordinal-1 cycle identity from complete creation-rooted history. Readers independently reconstruct the predecessor and reject changed actors, occurrences, histories, canonical encodings and coherently re-signed cycle forgeries.

The existing D1 serializer only gains shared member/World writers; all designation bytes remain unchanged. Task019A still owns applying this same completion event atomically, terminal replay/readback and retries. Task008 restoration and HOST-PUB-001 publication proof remain open.

Validation: 44 focused tests (21 new completion +23 designation), all16 chains/48 frozen fingerprints; full solution1,989 passed,0 failed/skipped; build0 warnings/errors, format and diff checks passed. Dev review and three sequential independent reviews passed with no findings.

Stacked on #129. No public gameplay activation or durable publication changes.
