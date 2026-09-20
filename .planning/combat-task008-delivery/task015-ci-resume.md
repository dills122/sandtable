# Task015 exact-candidate CI readback

Candidate `00a8b68c580a7efc38070571750c9a973e0fa579`; source/test tree identical to refreshed main `ba54efd720b38627b473cfd5d2e2ba4ee638d6b2`.

[Verify run](https://github.com/dills122/sandtable/actions/runs/35512324229/job/106082261240) completed successfully. Retrieved2026-09-20 via `gh run view 35512324229 --repo dills122/sandtable --log`; full local log `/tmp/task015-resume-ci-verify.log`. Selected exact output:

```text
verify	UNKNOWN STEP	2026-09-20T13:02:32.6029215Z ##[group]Run dotnet format Sandtable.slnx --verify-no-changes --no-restore
verify	UNKNOWN STEP	2026-09-20T13:02:32.6029528Z dotnet format Sandtable.slnx --verify-no-changes --no-restore
verify	UNKNOWN STEP	2026-09-20T13:03:35.2485514Z Build succeeded.
verify	UNKNOWN STEP	2026-09-20T13:03:35.2485943Z     0 Warning(s)
verify	UNKNOWN STEP	2026-09-20T13:03:35.2486199Z     0 Error(s)
verify	UNKNOWN STEP	2026-09-20T13:08:33.6890675Z Test run summary: Passed!
verify	UNKNOWN STEP	2026-09-20T13:08:33.6895453Z   total: 2324
verify	UNKNOWN STEP	2026-09-20T13:08:33.6895619Z   failed: 0
verify	UNKNOWN STEP	2026-09-20T13:08:33.6895813Z   succeeded: 2324
verify	UNKNOWN STEP	2026-09-20T13:08:33.6896012Z   skipped: 0
```

Other exact-head checks all success: CodeQL, dependency review, offline links, all four Analyze jobs. These are remote CI observations, distinct from retained prior local2324/full81-boundary/build/format evidence. Fresh independent reviews remain required.
