# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. What it is and how to run it: [README.md](README.md). Not built yet: [TODO.md](TODO.md).

This repository is a set of NuGet packages, public on GitHub and nuget.org: every public type is a promise to the apps
that use it, and every published version stays forever.

## Commands

- For people: a recipe in `justfile` with a one-line description; `just` lists them.
- For agents: the gate script `tests/Suwbackkit.Gate/scripts/check.sh`, which answers `--help`. A recipe only calls it.
- `check.sh --quick` while working, `check.sh` before calling a change done.

## Git

- **The main branch is `master`**, never `main`.
- Work on a feature branch, never on `master`. Agents do not rewrite or discard history: no
  amend, reset, rebase, stash, force-push or deleting an unmerged branch. Merging and releasing are the owner's.
- **A branch merged into `master` is deleted** right after the merge: local, remote and its worktree, with
  `git branch -d`, which refuses unmerged work. Needed again? Start a new one from `master`.
- A change with a red gate is not committed: it stays in the working tree and the summary says why.
- **One task, one branch.** Unrelated refactors, renames, version bumps and fixes go to their own
  branch or into `TODO.md`; say so once when you reach for one, and put it in if the owner confirms.
- A release is a tag `v<major>.<minor>.<patch>` on `master`, pushed by the owner; agents never tag and never publish.
- Personal rules (how you commit, your tools) go in the gitignored `CLAUDE.local.md`, not here.

## Rules

- KISS and YAGNI first: the repo grows only when a real need shows up; one developer maintains it after hours. When in doubt, leave it out, and say in the change what was left out and why.
- Code comes here only when it is generalised from at least two apps that need it today ([docs/architecture.md](docs/architecture.md)). One app's need stays in that app.
- A package never depends on an app, and nothing here knows which apps use it.
- A change to a public type is a change to every app that uses it: semantic versioning decides the next tag (a breaking change raises the major, or the minor while it is 0.x), and the change says which.
- Plan a change by listing what it makes obsolete, and delete that first, in the same change.
- Correction or a new rule? Make the mistake impossible or loud first: a type → a lint → a test → a hook. Add a sentence here only for what none of these can check.
- A guard's message states the fix. Never weaken a guard to make a change pass: fix the change.

## Specs

- A spec written before its feature is built is a draft in `docs/draft/spec/`; it moves to `docs/visual/spec/` when its feature is built, together with the tests that guard its criteria.
- Each acceptance criterion has an id `AC-<feature>-<n>`, set with `vdoc.py ids`, never by hand: a dropped id is never reused, an id is never renumbered.
- A criterion is guarded by a test that names its id (`[Trait("criterion", "<id>")]`). A criterion no automated check can prove is a wrong criterion: ask the owner, never keep it unguarded.
- A criterion gets the lightest check that can fail for it. A criterion that promises something for every member of a set that grows is guarded by structure (a test that walks every member from the source), not by examples.
- Before merging a spec'd feature, run `/suwagent:visual-guardrails`: the gate does not yet check that each criterion is guarded (TODO.md).

## Where code goes

[docs/architecture.md](docs/architecture.md). Read it before adding a folder, a project or a package.
Docs say what and why, not where: a line that must change when the code moves does not belong in the docs.

## What no test can check

- Names are chosen word by word, in technical English. One word per thing, everywhere. A package is `Suwbackkit.<Area>`.
- Secrets never enter the repo: not in code, settings, tests or history. Publishing uses nuget.org trusted publishing, never an API key;
  a real hit is removed and rotated, never allow-listed.

## No comments

No comments in project files: agents mostly use comments to excuse bad code and comments rot, so names, small functions, types and tests say what a comment would; if code is unclear without a comment, fix the code.

- Covered: every code and config file (C#, shell, YAML, ignore files, `.editorconfig`, MSBuild and XML files, JSON, justfiles), `///` docs, commented-out code and task markers included. Not covered: Markdown, `docs/` and data strings (messages).
- Not scanned: lockfiles, `bin`, `obj`, `artifacts`, `docs/`.
- Allowed, because a tool reads them (the guards hold the same list, nothing is judged by eye):
  - in a justfile, the one line directly above a recipe or `mod` that `just --list` shows;
  - a shebang `#!` on the first line; `# shellcheck <directive>=…` in shell scripts;
  - in C#, `/*lang=…*/` and `// <auto-generated>`;
  - a comment that is exactly a criterion id: `# AC-x-n` in `.editorconfig`, `<!-- AC-x-n -->` in the root `Directory.Build.props`.
- `#pragma warning disable`, `dynamic` and the null-forgiving `!` are banned; no comment excuses them.
- A reason that cannot live in code goes into a test, the spec, this file or `docs/` as a sentence. A new format that needs a comment to work goes on this list and into its guard in the same change.
