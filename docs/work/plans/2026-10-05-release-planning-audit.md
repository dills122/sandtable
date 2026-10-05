# Release planning audit and documentation synchronization

## Objective and boundary

User requested a full project planning audit toward first release and comprehensive documentation
sync/cleanup. Baseline main `af37155e4880050ae7789d2d58056fbeb03ac872` (PR163).
First-release interpretation follows the existing first playable MVP: two local players finish the
six-turn Land-only Graziani scenario, with deterministic replay, privacy, save/resume and no model.
This audit does not change gameplay, contracts, source rulings, release scope or shipping status.

## Delivery ownership

- High-effort audit worker: evidence-backed release audit and canonical pre-alpha roadmap.
- Medium documentation worker: README, documentation index, technical design, vocabulary,
  contribution guide and project website; extract historical detail and unique runbook when needed.
- Coordinator: portable historical link repair, integration, release/issue metadata checks,
  validation, preservation and publication. Primary user configuration changes stay untouched.
- Fresh medium independent reviewer: full combined diff and plan, preliminary assessment before
  separate author testimony. At most three passes per set, research recovery as previously agreed.

## Acceptance

1. Clear distinction between public execution, private native code, executable contracts, research
   and unstarted work across all release capabilities; evidence and uncertainty attached.
2. Ordered release gates and bounded next tasks with dependencies, acceptance and verification;
   estimates use observed history and explicitly avoid unsupported overall percentages or dates.
3. Current docs agree with canonical roadmap and source; unique runbook/architecture information
   remains reachable, historical decisions preserved, repository links portable.
4. Full offline Markdown link check, site static/JavaScript checks and proportionate rendering;
   no runtime/full-suite claims from documentation changes. Independent Ready before merge.

## Evidence limitations

Codebase Memory project sandtable was indexed at2026-10-05T02:49UTC against older primary main;
coverage returned not_tracked/metadata_changed for queried scopes and summaries. Focused current
worktree source reads supply audit evidence. No graph completeness claim is made.
GitHub release list was empty; sole open issue63 is a historical Breakdown decision request,
whose adoption state is checked against repository evidence. No external comments sent.
