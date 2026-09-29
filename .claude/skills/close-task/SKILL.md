---
name: close-task
description: Mark one backlog story as done by adding [CLOSED] to its heading in docs/backlog.md.
disable-model-invocation: true
---

The user names a story in $ARGUMENTS, by number or title. If $ARGUMENTS is empty, ask which story to close.

A closed story's heading in `docs/backlog.md` looks like this, with the marker before the number:

```markdown
### [CLOSED] 13. Min historikk
```

Find the story's `###` heading and add `[CLOSED] ` after `### `. Change nothing else in the file. If the heading already has `[CLOSED]`, tell the user and leave it.

Done when the heading carries `[CLOSED]` and you have told the user which story you closed, quoting the heading.
