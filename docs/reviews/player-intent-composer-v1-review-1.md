# Independent Review 1: Player Intent Composer v1

**Review instance:** 1 of 3

**Date:** 2026-08-24

**Mode:** Fresh-task, read-only review of the documentation target before implementation

**Verdict:** Needs revision before owner acceptance as the governing specification

## Findings

### P1 — Raw request recording conflicts with privacy

The specification and research referred to observing or recording parser “request bytes,” while the
privacy policy prohibited raw strategic-content logging. Because a parser request contains the raw
utterance, ordinary telemetry must retain byte counts only. Full request/response bodies require an
explicitly consented, isolated evaluation corpus.

**Author response:** Accept. The research, specification, and design now distinguish production byte
counts from separately authorized corpus bodies and explicitly prohibit raw bodies in operational
logs/telemetry.

### P1 — Hot-seat isolation lacks a complete contract and acceptance test

The design referenced an “existing privacy curtain/session transition,” but Maproom does not exist
and no test proved the next local hot-seat player could not recover the prior player's strategic
draft data.

**Author response:** Accept. The specification now requires a seat/session epoch, cancellation or
invalidation of in-flight work, complete clearing or reviewed isolation of every composition
surface, and late-response rejection. A new acceptance scenario and testing strategy cover UI,
DOM/live regions, memory, browser storage/cache, parser workers/sessions, back navigation, and
delayed responses. The design defines the transition boundary and Phase 1 gate.

### P2 — Needle package metadata is stale

The research recorded 2.0.8 even though PyPI published 2.0.9 on 2026-08-21.

**Author response:** Accept. The research now cites PyPI and records version 2.0.9 as of 2026-08-24.

## Confirmed Claims

- Branch, HEAD, merge-base, and review scope matched the bootstrap.
- The target was documentation-only.
- README, technical design, and naming overview were synchronized.
- Requirement and task identifiers were unique.
- Authority and fog boundaries were preserved.
- Needle remained optional and untested.
- Missing gameplay, Staff, Maproom, and legal-plan predecessors were disclosed and gated.
- Whitespace and local-link checks passed.

## Verification Performed By Reviewer

- Git branch, status, base/head, tracked/untracked scope, and tracked whitespace
- Untracked-file whitespace diagnostics
- Local Markdown targets
- Requirement/task identifier uniqueness
- Primary-source spot checks of Needle model/package facts

No files were changed by the reviewer. No .NET, frontend, model, or accessibility tests were run for
the documentation-only target.

## Re-review Decision

Both P1 corrections materially strengthen the product privacy contract and acceptance plan. Review
instance 2 of 3 is therefore required against the corrected target.
