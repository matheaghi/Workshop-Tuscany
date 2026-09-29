---
name: help-me-choose
description: Pick the next natural backlog story — which open stories build on the closed ones, one recommendation.
disable-model-invocation: true
---

Run these steps in order. Each ends on its completion criterion; move on only when it holds.

## 1. Candidates

Read `docs/backlog.md`. A story whose heading starts with `### [CLOSED]` is done; the choice is among the rest. If no story is closed yet, tell the user that `/clean-backlog` can mark finished stories first, and continue.

For each open story, judge how natural a next step it is:

- **Builds on** done work: reuses a model, endpoint or page that now exists (e.g. a field added for one story that another story needs).
- **Size**: fits one `/work` session, or can be cut into a first slice that does.
- **Unblocks** other open stories.
- **User value**: the story is useful to the user, not just a refactor or internal improvement.

Done when you have picked the top three candidates, each with a one-line reason tied to the points above.

## 2. Recommend

Present the three candidates, with your recommended one first and marked as such. Ask the user which to take, as one question.

Done when the user has picked a story. Suggest starting it with `/work`.
