# Integrated campaign save/resume proof

Branch `codex/rc-overnight-plan-20261023`, base `ee1d7645`, PR329.
Ruflo task `task-1792887429855-zwiatr`, namespace `sandtable-rc-20261023`.
Implementation commit is the commit introducing
`CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs`.

## Witness and scope

Use established fixed-seed6 double-contact setup and accepted first paid assault,
ending at pending required Retreat. Clone that journal with SQLite BackupDatabase,
which includes committed WAL pages and preserves keys, credentials and references.
Keep one branch uninterrupted; save the other repeatedly to distinct paths and
reopen only its latest copy. All clocks and choices match between branches.

Four bounded paths share that accepted prefix: a genuine second paid assault,
repeated Selection timeout, repeated RBA timeout, and single-seal Force timeout.
Initial Retreat timeout and terminal cycle-control timeout bracket the flow.
Pending Reserve, Cycle, repeated Selection, RBA and single-seal Force cuts are
saved where reached. No initial snapshot or synthetic later-cycle history is
substituted. No seed scan or eligibility expansion.

Compare checkpoint metadata, verified reconstruction on each branch, event-stream
hashes, exact reconstructed bytes/hashes, contacts and logical journal records.
Physical SQLite file bytes are not compared. Terminal reconstructed bytes can be
an event rather than complete World, so history and explicit World/RNG evidence
are required in addition to checkpoint equality. Paid-result RNG advances and
matches its retained Combat successor; this is not a separately exported final
RNG cursor for cancellation branches. Full accepted history and verified replay
provide final fallback determinism. Final Reserve World hash is compared with
cycle finish, and expected paid-result/Force-completion counts are checked.

This is same-process saved-copy resume, not fresh-process crash/recovery or a
playable full campaign. Original databases are not reopened as resumed authority
once a saved copy has advanced. Historical source/public certificates are not
implied by this native bounded witness.

## Validation

Four integrated branches passed on first run, exit0,71.327s receipt elapsed.
No runtime defect exposed; this packet changes tests and evidence only.

Command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj
-c Release --no-build --no-restore -- --filter-method '*IntegratedCampaign*'`.
Artifacts: `artifacts/rc-integrated-campaign/focused.log` and
`focused-receipt.json`. Each branch asserts at least six saved-copy resumptions,
verified reconstruction, identical event/contact/logical authority records,
expected paid result/Force completion count and terminal cycle timeout.

| Branch | Paid assaults | Repeated fallback | Common fallbacks |
| --- | ---: | --- | --- |
| paid-assault |2|None; second assault genuinely adjudicated|Initial Retreat, terminal Cycle|
| selection-timeout |1|Selection no-selection|Initial Retreat, terminal Cycle|
| rba-timeout |1|RBA cancel-without-decline|Initial Retreat, terminal Cycle|
| force-timeout |1|Single-seal Force cancellation|Initial Retreat, terminal Cycle|

Host Release build passed with zero warnings/errors,exit0,13.509s. Command:
`dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`. Format verification passed,
exit0,29.098s. Adjacent focused checks passed: `--filter-method '*Genuine*'`3/3,exit0,21.239s;
`--filter-method '*RbaRepeatedAssaultOrForceEntry*'`2/2,exit0,16.429s.
Both use the same Host test command/options as the integrated check. No full Core, whole Host,
Runner, boundary, contract or Python-oracle rerun for this test-only packet;
prior `ee1d7645` runtime evidence remains historical and is not relabelled.
No Core tests changed, so Core discovery/weights remain untouched. Exact-head CI
will discover the four added Host rows.

## Review and remaining gates

Coordinator review plus read-only Core comparison audit. No new independent
review instance or release acceptance claimed. Relevant graph coverage checked;
new/partial/changed paths inspected directly. CCE unavailable; Ruflo retains
coordination and decisions.

Still open unless explicitly proven here: third-cycle Reserve expiry, repeated
captured-lot Custody and required-Retreat expiry, exact Custody/Retreat-entry clock
faults, native fresh-process resume, public original-root certificates and
six-turn product acceptance. Prior source-pin failures and dependency work remain
open. No pins/support rows changed. Exact-head CI and independent release review
remain required before merging PR329.

Nine focused tests passed in total. No local test processes remain active;
worker ownership released. No production code, schemas or source pins changed.
