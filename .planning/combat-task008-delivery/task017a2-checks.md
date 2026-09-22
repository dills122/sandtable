# Task017A2 checks

Candidate3811202ee23b3ef5c972b85953f19ca77759b36f; basea1cd425.
Start2026-09-21 22:45UTC. No source/schema/fixture changes outside four-path implementation manifest.

## Executed
- Meaningful RED: first-I lifecycle on compiled no-op skeleton failed expected open/actual unopened; /tmp/017a2-red-allowed.log.
- Initial sandboxed test failed on local named-pipe permissions; not behavioral evidence. Approved IPC retry supplied RED.
- Focused initial parity2passed; /tmp/017a2-focused-1.log.
- Intermediate negative-test compile error (collection-expression syntax) corrected; not behavioral RED.
- Focused10:9passed/1failed because numeric effect kind raised InvalidOperationException. Added raw ID validation; /tmp/017a2-focused-3.log.
- Focused lifecycle10 plus existing A1six:16passed/0failed/0skipped3.366s; /tmp/017a2-focused-4.log.
- Scoped dotnet format passed using approved IPC; sandboxed attempt failed connecting compiler pipe.
- Full dotnet build Sandtable.slnx --no-restore:0warnings/0errors3.69s; /tmp/017a2-build.log.
- Full dotnet format Sandtable.slnx --verify-no-changes --no-restore:exit0; /tmp/017a2-format-check.log.
- Boundary trait:81passed/0failed/0skipped10.912s; /tmp/017a2-boundary.log.
- git diff --check:pass.
- Task016 eight source/test pins:allpass. A1 base methods untouched; shared models/codec extended intentionally. Existing A1six passed.

- Full dotnet test --solution Sandtable.slnx --no-build:2,347passed/0failed/0skipped9m46.822s; /tmp/017a2-full.log.

## Acceptance
Exact candidatea4b4f63 CI:all8checks successful. Verify35665875884/job106551403293 confirms2,347passed/0failed/0skipped9m55.443s (/tmp/017a2-ci.log). Final documentation publication follows. Reviews1/2/3 Ready, no findings; root read all preliminary and final reports. Reviewer3 independently rebuilt full Release solution0warnings/errors13.90s, then16focused/81Boundary passed.
No parent017/public gameplay/campaign lineage acceptance from isolated lifecycle proof.

## Publication and documentation checks
PR139 draft created and attached. PR138 had been squash-merged as32a038c. Ancestry-only rebase
3811202→a4b4f630d1250fd00d61ebae486f18813ffedd01; complete tree equality verified, as was
oldbasea1cd425/main32a038c equality. Guarded force-with-lease updated exact published prior SHA.
PR now mergeable and ordinary CI active. Review1 covers identical tree3811202; later reviews usea4b4f63.
Changed Markdown target check:7files/670relative targets/0missing, anchors not checked.
Broader ad-hoc686file scan found34pre-existing absolute historical worktree/line-suffix links;
not changes from this slice, no unrelated edits. CI offline links succeeds.
