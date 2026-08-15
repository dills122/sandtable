# Security Policy

## Supported Versions

Sandtable has not published a release. Security fixes currently target the latest commit on
`main`.

## Reporting a Vulnerability

Use the repository host's private vulnerability-reporting feature when available. Do not open a
public issue containing exploit details, secrets, private campaign data, or hidden game state.

Include:

- the affected commit and component;
- reproduction steps or a minimal proof of concept;
- expected and observed security boundaries;
- likely impact; and
- any suggested remediation or temporary mitigation.

Maintainers should acknowledge a report before discussing public disclosure. There is no guaranteed
response SLA until the project publishes a supported release and named security contact.

## Security-Critical Boundaries

- The intelligence plane must never receive opposing hidden state.
- Intelligence responses are untrusted and cannot mutate authoritative state directly.
- Decision IDs, state versions, plan IDs, and ruleset hashes must be validated before execution.
- Telemetry must not contain secrets, complete prompts, hidden game state, or unbounded-cardinality
  identifiers.
- External model failure must degrade to a deterministic scripted decision rather than fail a game
  turn.
