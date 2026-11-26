## Codebase search

Follow [AGENTS.md](../AGENTS.md) for authority boundaries, contract-first rules,
test commands and codebase-memory graph search. The old Context Engine / CCE
workflow is not installed. Use graph lookup with coverage verification, and source
reads for gaps when those tools are available. Fall back to source search otherwise.

### Output style

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
