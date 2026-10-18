# P01 AC-007 private owner projection scaffold evidence

Branch: `codex/combat-p01-owner-projection`. Base: merged `main` after PR #242.

`python3 docs/specs/test-combat-p01-owner-projection-v0.py` passes five deterministic tests:
separate private candidates yield identical proposed visible alias and constant denial bytes;
visible revision changes the alias; private/unapproved fields and duplicate visible bindings
reject; malformed visible values reject; equal-visible/different-legality synthetic trials fail
closed. The draft alias golden vector is
`p01-owner-draft.ffe6cfb584ad7720ff9bff4d3bc2f1c76163aed2e2b5705b962cb930a92c6447`.

These tests are synthetic contract checks. They do not authenticate a hidden-variant R03 twin,
approve visible fields, test public command acceptance, or close AC-007. Existing P01 assessment
continues to return `unsupported`, `admitted=false`, and no public actions. The pinned 300-row
44/256 provisional audit must remain unchanged and is rerun for this PR.
