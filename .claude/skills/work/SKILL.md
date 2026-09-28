---
name: work
description: Start a new task — goal and limits, domain docs, plan-mode interview, then test-first implementation.
disable-model-invocation: true
---

Run these steps in order. Each ends on its completion criterion; move on only when it holds.

## 1. Goal and limits

Ask the user what the task is: the **goal** (what should be true when we're done) and the **limits** (what is out of scope, what must not change, any deadline or constraint).

Done when you can restate the goal and the limits in one or two sentences each, and the user confirms the restatement.

## 2. Domain rules

Read `docs/TRONDER_LEIKAN.md`, plus the stories in `docs/backlog.md` the task touches. Then read the code the task touches, so you know how it behaves today.

Done when you have listed, for yourself, every domain rule that bears on the task, and every place the current code disagrees with those rules.

## 3. Plan-mode interview

Enter plan mode. Interview the user relentlessly, **one question at a time**, each with your recommended answer. Look facts up in the code yourself; put only decisions to the user. Raise every rule-vs-code disagreement from step 2 as a question.

Done when every decision the task needs is settled and the user approves the plan in `ExitPlanMode`. The plan names: the scope, the behaviour to change, the tests that prove it, and what is explicitly left out.

## 4. Red

Write the tests from the plan (use the `tdd` skill), at the lowest layer that can express the behaviour: Domain before Application before Api. Run them.

Done when each new test fails, and fails for the reason the plan predicts: show the failure output.

## 5. Green

Change the code until the new tests pass. Stay inside the plan's scope. If the change alters a domain rule, update `docs/TRONDER_LEIKAN.md` to match.

Done when the new tests pass.

## 6. Full run

Run `dotnet test` (Api and Infrastructure tests need Docker). If the frontend changed, run `npm run lint` and `npm run build` in `src/frontend`.

Done when everything is green. Report to the user: what changed (file references), the before/after test result, and anything left open. Leave committing to the user.
