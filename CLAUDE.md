For project architecture, authority boundaries, current test commands and documentation ownership,
follow [AGENTS.md](AGENTS.md). Current delivery status lives in the
[roadmap](docs/roadmap/pre-alpha-roadmap.md). Historical research and reviews retain their original cut.

<!-- codebase-memory-block-version: 1 -->
## Codebase search: use codebase-memory-mcp

This project replaced the prior "Context Engine (CCE)" setup (its hooks were
never actually installed on disk — `$HOME/.cce/hooks/cce_hook.sh` did not
exist, so none of its tools or hooks ever ran) with the `codebase-memory-mcp`
MCP server, registered in this repo's `.mcp.json`. It is a code-structure
search/graph tool, not a decision ledger — it has no `session_recall`/
`record_decision`-style cross-session memory. Use Claude Code's own memory
system for durable decisions instead.

**Use the graph tools instead of reading files directly** when exploring the
codebase, answering questions about code, or understanding how something
works:

- `mcp__codebase-memory-mcp__search_graph` — find symbols by name/kind.
- `mcp__codebase-memory-mcp__trace_path` — find callers/callees, what calls
  or imports a function.
- `mcp__codebase-memory-mcp__get_code_snippet` — fetch source for a found
  symbol without opening the whole file.
- `mcp__codebase-memory-mcp__query_graph` — multi-hop structural queries.
- `mcp__codebase-memory-mcp__get_architecture` — broad structural overview.
- `mcp__codebase-memory-mcp__search_code` / `Grep` — literal strings,
  non-code files, or anything the graph doesn't cover.
- `mcp__codebase-memory-mcp__check_index_coverage` — call this for every file
  you rely on; if coverage is incomplete, read the reported missed lines
  directly rather than trusting a partial graph result.

When to use `Read` instead:
- You need to edit a specific file (read before editing).
- You need the exact, complete content of a known file path.

The session's project index auto-refreshes; a different project needs
re-indexing (`mcp__codebase-memory-mcp__index_repository`).

### Output style

This compressed style applies to chat replies only. PR descriptions, commit
messages, dispatch/worker briefs, review findings, and any text another
agent or reviewer will read must be written in plain, full sentences.
(Kept in sync with `AGENTS.md`'s identical section; update both together.)

Respond in compressed style. Drop articles (a, an, the) in prose. Use
sentence fragments over full sentences. Use short synonyms (fix not resolve,
check not investigate). Pattern: [thing] [action] [reason]. [next step].
No filler, hedging, pleasantries, trailing summaries, or restating what
the user said. One sentence if one sentence is enough.

When suggesting code changes, show only the changed lines with 3 lines of
context. Never rewrite entire files. Multiple changes in one file: show each
change separately. Never echo back unchanged code the user already has.

Code blocks, file paths, commands, error messages: always written in full.
Security warnings and destructive action confirmations: use full clarity.
<!-- /codebase-memory-block -->
