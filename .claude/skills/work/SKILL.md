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

Done when every decision the task needs is settled and the user approves the plan in `ExitPlanMode`. The plan names: the scope, the behaviour to change, the tests that prove it (each marked as a red test or a contract test, see step 4), and what is explicitly left out.

## 4. Red → green, slice by slice

First run `aspire ps`. If an AppHost is running, note that, then run `aspire stop`, so the tests build and run against a free machine.

Then work in vertical slices with the `tdd` skill, one layer per slice, from the lowest layer that can express the behaviour upward: Domain, then Application, then Api. For each slice:

1. Write the slice's tests from the plan and run them. Show the failure output: each test is **red**, for the reason the plan predicts.
2. Change the code until they pass. Stay inside the plan's scope. If the change alters a domain rule, update `docs/TRONDER_LEIKAN.md` to match.

A **contract test** is the one exception to red first. It sits on a layer the plan leaves unchanged, such as an Api test that pins JSON field names and enum strings produced by a lower layer. It has no code of its own to drive, so write it after the slice below goes green, and show it passing on its first run. If it fails, the contract is broken: treat that as a red test and fix the code.

Done when every slice's tests pass, and each one was shown red first or is a contract test named as such in the plan.

## 5. Full run

Run `dotnet test` (Api and Infrastructure tests need Docker). If the frontend changed, run `npm run lint` and `npm run build` in `src/frontend`.

Then, if step 4 stopped an AppHost, restart it with `aspire start` from the main clone.

Done when everything is green and any AppHost stopped in step 4 is running again. Report to the user: what changed (file references), the before/after test result, and anything left open. End the report with a reminder to commit, listing the uncommitted files from `git status`. The user makes the commit.
