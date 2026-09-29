---
name: clean-backlog
description: Find backlog stories that are finished in the code but not yet marked, and add [CLOSED] to them after the user confirms.
disable-model-invocation: true
---

Run these steps in order. Each ends on its completion criterion; move on only when it holds.

## 1. Find finished stories

Read `docs/backlog.md`. Stories whose heading starts with `### [CLOSED]` are already closed; check only the open ones.

For each open story, look for evidence it is finished: commits in `git log --oneline` that name it or its behaviour, and code, tests or pages that deliver what the story asks for. A story counts as finished only when the code covers the whole story, not just part of it.

Done when every open story is sorted into finished or not finished, and each finished one has its evidence (commit or file references).

## 2. Confirm

Show the user the finished stories with their evidence, and ask them to confirm or correct the list, as one question. If you found none, say so and stop.

Done when the user has confirmed which stories to close.

## 3. Close

For each confirmed story, add `[CLOSED] ` after `### ` in its heading, e.g. `### [CLOSED] 13. Min historikk`. Change nothing else in the file.

Done when every confirmed story's heading carries `[CLOSED]` and you have listed the headings you changed.
