https://www.youtube.com/watch?v=-QFHIoCo-Ko
Matt’s workflow is basically a **repeatable pipeline** you wire into your tools so AI behaves like a disciplined senior dev, not a vibes-only autocomplete. I’ll walk you through how to actually set it up and run it end‑to‑end.

**0. Prerequisites and mental model** 

The core pieces Matt uses:

- Claude Code with a `.claude` directory at the project root.
- Matt’s `mattpocock/skills` repo installed as “skills” (each skill is a Markdown runbook the agent follows).
- A ticket system (usually GitHub Issues or Linear).
- A “Ralph loop” style executor (ralph.sh) to run agents against your Kanban board.
- Human QA at the end.

High-level flow (from his X post and writeups):

Idea → `/write-a-prd` → PRD  
PRD → `/prd-to-issues` → Kanban board  
Kanban → `ralph.sh` → AI execution loop  
Ralph loop → manual QA

Plus, from his “7 Phases of AI Development”: optional research and prototyping phases before the PRD, and iterative QA at the end.

**1. Wire up Matt’s skills in your repo** 

At your project root:

1. Create the Claude skills directory:
2. ```bash
    mkdir -p .claude/skills
    ```
3. Install just the core skills Matt uses most for this workflow:
```bash
    npx skills@latest add mattpocock/skills/write-a-prd
    npx skills@latest add mattpocock/skills/prd-to-issues
    npx skills@latest add mattpocock/skills/tdd
    npx skills@latest add mattpocock/skills/git-guardrails-claude-code
    npx skills@latest add mattpocock/skills/grill-me
```

  This drops `SKILL.md` files under `.claude/skills/`. Claude Code automatically picks these up when it runs in that repo and will follow the workflows encoded in those files instead of ad‑hoc prompting.

You don’t normally “call” the skills by filename; you just say things like “Let’s write a PRD for X” and Claude Code maps that to `write-a-prd`, or you literally type `/write-a-prd` in chat depending on your setup.

**2. Phase 1 – The Idea (plus** `/grill-me`**)** 
You start with a vague problem: “I want a billing dashboard for motel bookings” or “Let’s add Azure Service Bus retry logic.”
Implementation steps:
1. Open Claude Code on the repo.
2. Describe the idea briefly in natural language.
3. Invoke the **grill-me** skill to sharpen it. For example in Claude chat:“Use the `/grill-me` skill on this idea and interrogate me until the use‑case is completely clear.”
4. `grill-me` will:
- Ask you structured questions about users, constraints, risks, edge cases.
- Force you to articulate domain concepts and expectations.
By the end, you should have a sharper, text description of the problem and constraints that lives in the chat history (and you can paste/save it to `docs/` if you want).
**3. Phase 2 – Optional Research (**`RESEARCH.md`**)** 
If you’re integrating with a tricky API (Stripe, a custom PMS, bank API, etc.), Matt suggests caching the research so agents don’t have to re‑discover it every time.
Implementation steps:
1. Create `docs/RESEARCH.md` (or `research/my-feature-research.md`):

```markdown
    # Billing Dashboard – Research
    
    ## APIs
    - Stripe endpoints used
    - Auth model
    - Webhook events
    
    ## Constraints
    - Rate limits
    - Required fields
    - Error modes
```

3. Paste key API docs, examples, and decisions here (not the whole internet, just what you’ll actually use).
4. Tell Claude Code explicitly:“All Stripe/billing API facts should be treated as canonical when found in `docs/RESEARCH.md`. Use that file before going to external docs.”
Agents now have a local source of truth they can reference across sessions.

**4. Phase 3 – Prototype (throwaway exploration)** 
Here you impose your _taste_ before formalizing anything.
Implementation steps:
1. Create an explicit “playground” route or module, e.g.:
- Frontend: `src/app/_playground/billing-prototype/page.tsx`
- Backend: `src/playground/billing-prototype.ts`

3. Ask Claude:“In `src/app/_playground/billing-prototype/page.tsx`, build 2–3 variants of a billing dashboard layout. We’re exploring, not shipping. Use fake data.”
4. Iterate a couple of times until you find something you like.
5. Once you’re happy:
- Move the chosen implementation into “real” files, e.g. `src/app/billing/page.tsx`.
- Delete or archive the unused prototypes.
Crucially, now your repo contains **concrete examples** that later skills (PRD, TDD, execution) can see and imitate.

**5. Phase 4 – PRD via** `/write-a-prd` 
Now you capture the “destination” in a Product Requirements Document.
Implementation steps:
1. In Claude Code:“Use the `/write-a-prd` skill to create a PRD for the billing dashboard feature. Use the prototype in `src/app/billing/page.tsx` and the constraints in `docs/RESEARCH.md` as context.”
2. The skill’s workflow (baked into `SKILL.md`) typically:
- Scans the repo for relevant files and past prototypes.
- Interviews you for missing requirements and edge cases.
- Assembles a PRD with sections like:
- Problem / Background
- Goals & non‑goals
- User stories
- UX outline / flows
- Acceptance criteria
- Open questions
1. Decide where PRD lives:
- As a GitHub issue (often how Matt runs it).
- Or as `docs/PRD-billing-dashboard.md` checked into the repo.
You end this phase with a **stable PRD artifact** that describes _what_ you’re building, not _how_.
**6. Phase 5 – Turn PRD into Kanban (**`/prd-to-issues`**)** 
Now you transform that PRD into a ticket set that agents can execute.
Implementation steps:
1. In Claude Code:“Use the `/prd-to-issues` skill on the billing dashboard PRD. Create GitHub issues for each logical unit of work with clear acceptance criteria and blocking relationships.”
2. The skill:
- Parses the PRD.
- Creates issues like:
- “Implement billing dashboard read model”
- “Create billing dashboard UI”
- “Wire up Stripe webhooks”
- “Add tests for billing dashboard filters”
- Each issue has:
- Description
- Acceptance criteria
- Links back to the PRD
- Optional “Blocked by #X” references
1. Now you have a **Kanban board** (GitHub projects or Linear) with a graph of work to be done.
This is what Ralph loops will chew through.

**7. Phase 6 – AI Execution with Ralph loops (+ TDD & guardrails)** 
This is where Matt wires AI into actual implementation.
**7.1 Configure git and safety** 
Use `git-guardrails-claude-code` so the agent can’t trash your repo:
2. Ensure that skill is installed (see step 1).
3. In Claude Code, explicitly instruct:“When performing any git operations, follow the `git-guardrails-claude-code` skill. Dangerous operations like force‑push, deleting branches, or rewriting history must be blocked or require my approval.”
The skill enforces:
- No force pushes to protected/main branches.
- No history rewrites without confirmation.
- Encouraging feature branches + PRs.
**7.2 Enforce TDD for implementation** 
For each ticket, Matt prefers the `tdd` skill to force Red–Green–Refactor.
1. From a ticket description, say:“Use the `tdd` skill to implement this ticket. Write a failing test first, confirm it fails for the expected reason, then implement the minimal code to pass, then refactor.”
2. The `tdd` skill steps:
- Locate the relevant test suite or create a new test file.
- Add a failing test expressing the acceptance criteria.
- Run tests, confirm the failure is due to missing behavior.
- Implement minimal change to make test pass.
- Optionally refactor and keep tests green.

You can encode a project‑wide rule like “All feature tickets must be implemented through the `tdd` skill” in your own README or team norms.

**7.3 Run the Ralph loop (GitHub Issues)**

**Important clarification:** Ralph is NOT a tool you install. There is no `npm install ralph`, no Docker image, and no YAML config file. "Ralph" is a **bash script pattern** — a loop that:
1. Fetches the next GitHub Issue via the `gh` CLI
2. Spawns a **fresh Claude Code process** with that issue
3. Claude implements using TDD
4. Marks the issue done
5. Repeats

**Does it need Docker?** No. Docker is optional and only used if you want to sandbox the AFK mode so the agent can't accidentally trash your system. The basic loop runs directly on your machine.

**Prerequisites:**
- [Claude Code installed](https://claude.ai/install) (`claude` command available in terminal)
- [GitHub CLI](https://cli.github.com/) installed and authenticated (`gh auth login`)
- Issues already created in your repo (from Phase 5: `/prd-to-issues`)
- Skills installed: `tdd`, `git-guardrails-claude-code`

**Step 1: Create the prompt file**

Create `ralph-prompt.md` at your project root:

```markdown
You are implementing a feature in this codebase. Follow these rules strictly:

1. Read the issue below carefully. Understand the acceptance criteria.
2. Use TDD: write a failing test first, confirm it fails, then implement, then refactor.
3. Touch only what the issue requires. No speculative refactoring.
4. Run tests and type checks before every commit.
5. Make focused, minimal commits (one commit per logical change).
6. When all acceptance criteria are met and tests pass, include the marker:
   <promise>COMPLETE</promise>
7. Do NOT close the issue. The loop script handles that.
```

**Step 2: Create the loop script**

Create `ralph.sh` at your project root:

```bash
#\!/bin/bash
# Ralph loop for GitHub Issues
# Usage: ./ralph.sh [max-iterations]
# Requires: gh CLI, claude CLI
# No Docker required.

set -e
MAX=${1:-20}
REPO=$(gh repo view --json nameWithOwner --jq .nameWithOwner)
BASE_BRANCH="main"
BRANCH_NAME="ralph/run-$(date +%s)"

git checkout -b "$BRANCH_NAME" "$BASE_BRANCH"

for ((i=1; i<=MAX; i++)); do
  echo ""
  echo "=== Ralph iteration $i/$MAX ==="

  # Fetch open issues, filter out blocked ones, pick the first
  ISSUES_JSON=$(gh issue list     --repo "$REPO"     --state open     --json number,title,body,labels     --limit 30)

  # Find an issue whose labels don't include "blocked" or "in-progress"
  ISSUE=$(echo "$ISSUES_JSON" | python3 -c "
import json, sys
issues = json.load(sys.stdin)
for i in issues:
    labels = [l['name'].lower() for l in i.get('labels', [])]
    if 'blocked' not in labels and 'in-progress' not in labels:
        print(json.dumps(i))
        break
")

  if [ -z "$ISSUE" ]; then
    echo "No issuable issues found. Done."
    break
  fi

  NUMBER=$(echo "$ISSUE" | python3 -c "import json,sys; print(json.load(sys.stdin)['number'])")
  TITLE=$(echo "$ISSUE" | python3 -c "import json,sys; print(json.load(sys.stdin)['title'])")

  echo "Picking up: #$NUMBER — $TITLE"

  # Mark as in-progress via a label
  gh issue edit "$NUMBER" --repo "$REPO" --add-label "in-progress" 2>/dev/null || true

  # Fetch full issue body
  ISSUE_BODY=$(gh issue view "$NUMBER" --repo "$REPO" --json body --jq .body)

  # Spawn a FRESH Claude session (smart zone) with the issue
  claude --model sonnet     -p "Implement this GitHub issue.

Issue #$NUMBER — $TITLE
Description:
$ISSUE_BODY

$(cat ralph-prompt.md)"

  # Mark done and close
  gh issue close "$NUMBER" --repo "$REPO" --comment "Implemented by Ralph loop iteration $i"
  gh issue edit "$NUMBER" --repo "$REPO" --remove-label "in-progress" 2>/dev/null || true
  gh issue edit "$NUMBER" --repo "$REPO" --add-label "done" 2>/dev/null || true

  echo "=== Completed: #$NUMBER ==="
done

echo "Branch: $BRANCH_NAME"
echo "Push: git push -u origin $BRANCH_NAME"
```

Make it executable:
```bash
chmod +x ralph.sh
```

**Step 3: Run it**

```bash
./ralph.sh 20
```

The script will:
1. Create a feature branch from `main`
2. Fetch open GitHub Issues, skip blocked/in-progress ones
3. Pick the first eligible issue
4. Mark it with the `in-progress` label
5. Spawn a **fresh Claude Code session** with the issue body + tdd instructions
6. Claude reads the codebase, writes tests, implements, commits
7. Script closes the GitHub Issue and labels it `done`
8. Repeats until no issues remain or max iterations reached
9. Prints the branch name for PR creation

**Step 4: Run in parallel (optional)**

Start workers in separate terminals. Each creates its own branch:

```bash
# Terminal 1 — runs odd-numbered issues on branch-1
./ralph.sh 10

# Terminal 2 — runs even-numbered issues on branch-2
./ralph.sh 10
```

**Step 5: Review (back to Phase 6a–6d)**

After the loop finishes:
1. Push the branch: `git push -u origin $BRANCH_NAME`
2. Create a PR
3. Run automated review (see sections 6a–6d above)

**HITL (Human-in-the-Loop) variant**

For more control, run one iteration at a time:

```bash
#\!/bin/bash
# ralph-once.sh — Single iteration, for HITL mode
# Usage: ./ralph-once.sh <issue-number>
set -e
NUMBER=$1
TITLE=$(gh issue view "$NUMBER" --json title --jq .title)
BODY=$(gh issue view "$NUMBER" --json body --jq .body)
gh issue edit "$NUMBER" --add-label "in-progress" 2>/dev/null || true
claude --model sonnet -p "Implement issue #$NUMBER — $TITLE

$BODY

$(cat ralph-prompt.md)"
echo "Done. Review the changes, then close the issue manually."
```

**Matt's key insight (from his blog post "Why the Anthropic Ralph plugin sucks"):**
> *"The official Anthropic Ralph Plugin keeps everything in one session, causing context bloat. Past ~40% context usage, the AI gets dumber. A simple bash loop that restarts a fresh Claude session each iteration keeps the AI in the smart zone."*

This bash script pattern IS Matt's recommended approach. No plugin, no Docker, no Ralph binary — just bash + `gh` + `claude`.**7.4 Running the loop when issues are markdown files**

Ralph only works with GitHub Issues / Linear. If your issues are markdown files in the repo, you need a different approach. Here is a complete, copy-pasteable setup.

**Step 1: Define your issue file format**

Store each issue as an individual markdown file in `issues/` with YAML frontmatter for status tracking:

```markdown
---
title: Implement billing dashboard read model
status: pending     # pending | in-progress | done | blocked
blocked_by: []       # list of issue filenames this depends on
---

## Description
Create the read model that aggregates booking data for the billing dashboard.

## Acceptance Criteria
- [ ] Read model returns total revenue for a date range
- [ ] Read model returns revenue grouped by motel/unit
- [ ] Handles empty date ranges without error
- [ ] Tests cover edge cases
```

Name each file clearly, e.g.:
```
issues/
  01-billing-read-model.md       # status: in-progress
  02-billing-dashboard-ui.md     # status: pending, blocked_by: [01-billing-read-model.md]
  03-stripe-webhook-integration.md
  04-billing-dashboard-filters.md
  README.md                      # optional: overview
```

**Step 2: Write a simple loop script**

Create `scripts/issue-loop.sh` at your project root:

```bash
#!/bin/bash
# issue-loop.sh — Sequential issue executor for markdown-based issues
# Usage: ./scripts/issue-loop.sh
# Picks the first "pending" issue whose blocking issues are all "done",
# feeds it to Claude Code with TDD, commits, updates status, repeats.

set -e

ISSUES_DIR="issues"
BASE_BRANCH="main"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

pick_next_issue() {
  for file in "$ISSUES_DIR"/*.md; do
    [[ "$(basename "$file")" = "README.md" ]] && continue
    status=$(grep -m1 '^status: ' "$file" | sed 's/status: //')
    [[ "$status" != "pending" ]] && continue
    blocked_by=$(grep -m1 '^blocked_by: \[' "$file" | sed 's/blocked_by: \[//;s/\]//')
    if [[ -n "$blocked_by" ]]; then
      all_unblocked=true
      IFS=',' read -ra BLOCKERS <<< "$blocked_by"
      for blocker in "${BLOCKERS[@]}"; do
        blocker=$(echo "$blocker" | xargs)
        blocker_status=$(grep -m1 '^status: ' "$ISSUES_DIR/$blocker" | sed 's/status: //')
        if [[ "$blocker_status" != "done" ]]; then
          all_unblocked=false; break
        fi
      done
      $all_unblocked || continue
    fi
    echo "$file"
    return 0
  done
  return 1
}

issue_title() { grep -m1 '^title: ' "$1" | sed 's/title: //'; }
mark_in_progress() { sed -i '' 's/^status: pending$/status: in-progress/' "$1"; }
mark_done() { sed -i '' 's/^status: in-progress$/status: done/' "$1"; }

BRANCH_NAME="feature/issue-loop-$(date +%s)"
git checkout -b "$BRANCH_NAME" "$BASE_BRANCH"

echo -e "${BLUE}Starting issue loop on branch: $BRANCH_NAME${NC}"

while issue_file=$(pick_next_issue); do
  title=$(issue_title "$issue_file")
  echo -e "\n${GREEN}==== Picking up: $title${NC}"
  mark_in_progress "$issue_file"
  git add "$issue_file"
  git commit -m "chore: mark issue in-progress — $title"
  echo -e "${YELLOW}Running Claude Code on this issue...${NC}"
  claude --model sonnet \
    -p "Implement this issue using the tdd skill.

Issue:
$(cat "$issue_file")

- Read docs/PRD-*.md and docs/RESEARCH.md for context
- Use TDD: write failing test first, then implement, then refactor
- Run tests and type checks before committing
- Make minimal commits with clear messages"
  mark_done "$issue_file"
  git add "$issue_file"
  git commit -m "chore: mark issue done — $title"
  echo -e "${GREEN}Completed: $title${NC}"
done

echo -e "\n${BLUE}No more issuable issues. Loop complete.${NC}"
echo "Branch: $BRANCH_NAME"
echo "Push and create a PR: git push -u origin $BRANCH_NAME"
```

Make it executable:
```bash
chmod +x scripts/issue-loop.sh
```

**Step 3: Run the loop**

```bash
./scripts/issue-loop.sh
```

The script:
1. Creates a feature branch from `main`.
2. Scans `issues/*.md` for the first issue with `status: pending` whose `blocked_by` are all `done`.
3. Marks it `in-progress` and commits that status change.
4. Spawns a **fresh Claude Code session** (new process = smart zone, per Matt’s principle) with the issue content, TDD skill.
5. Claude implements using TDD (red → green → refactor), runs tests, commits.
6. The script marks the issue `done` and commits the status change.
7. Repeats until no issuable issues remain.
8. Prints the branch name so you can push and create a PR.

**Step 4: Run in parallel (optional)**

For independent (non-blocking) issues, run multiple workers on separate branches:

```bash
# Terminal 1
./scripts/issue-loop.sh
# Terminal 2 (will pick a different non-blocked issue)
./scripts/issue-loop.sh
```

Each script creates its own feature branch, so parallel workers don’t conflict on git state.

**Limitations vs. Ralph:**
- No real-time Kanban visualization — you read file statuses directly.
- Parallel workers don’t coordinate — two scripts could briefly race on the same issue.
- No webhook integration — you trigger the script manually.
- No built-in CI gate — you rely on Claude running tests inside each session.

**Tradeoff:** This trades Ralph’s automation and dashboard visibility for zero external dependencies. The core principle — fresh context per issue, TDD, git guardrails — is preserved.

**8. Phase 7 – Manual QA and iteration** 

When Ralph finishes the “Done” column for a slice of work:

1. Ask Claude to draft a **QA plan**:“Given the tickets marked complete and the original PRD, generate a detailed QA plan for manual testing of the billing dashboard.”
2. You (or your team) then:

- Manually run through the QA plan.
- Open new tickets for bugs or UX tweaks.
- Optionally run another Ralph loop on the new tickets.

4. For high‑risk areas, you also:

- Perform a manual code review (read for security, performance, readability, architecture).
- Enforce your own standards beyond what skills encode.

This continues until the PRD is truly satisfied and QA passes.

**9. How to adopt this in your own stack** 

Given your background and that you’re not limited to TypeScript:

- Reuse **the phases + artifacts** (Idea → Research → Prototype → PRD → Issues → Execution → QA).
- Use Matt’s skills as a **template** and fork where needed:

- Adapt `write-a-prd` examples to your Java/Spring or .NET patterns.
- Adjust `tdd` to your test runner (JUnit, Testcontainers, etc.).
- Customize git guardrails to your branching / CI rules.

- Keep everything in‑repo:

- `docs/PRD-*.md`
- `docs/RESEARCH-*.md`
- `.claude/skills/*`

Once that’s in place, your “workflow for AI coding” stops being “prompt engineering” and becomes a **set of codified practices** that tools enforce.

If you tell me which repo or stack you want to start with (e.g., your motel admin app, Java backend, Next.js front), I can sketch the exact files/commands for that project’s first AI‑driven feature.

==============
Based on the Matt Pocock workshop you’re watching, here’s the process for adding a new feature to an application that was already built from a PRD and issues:

The Process (7 Phases) ￼

1. Research & Prototyping

Start by understanding the feature request. If it involves UI, have the AI generate throwaway prototypes you can visually evaluate before committing to a direction.

2. The Grill Session

Run a “grill me” session — have the AI interview you relentlessly about every aspect of the new feature. This builds a shared understanding (what Matt calls the “design concept”) between you and the AI. This is human-in-the-loop and can’t be skipped. You may need to loop in teammates or domain experts for questions you can’t answer.

3. Write a New PRD

Summarize the grill session into a new PRD (destination document). This captures user stories, implementation decisions, testing strategy, and out-of-scope items. Matt says he doesn’t even review the PRD closely — the alignment was built during the grill session; the PRD is just a summarization of it.

4. Slice into Issues (Kanban Board)

Break the PRD into vertical slices (tracer bullets), not horizontal layers. Each issue should cut through all layers of the stack (database → API → frontend) so you get feedback on the full flow immediately. Issues should have blocking relationships so they can be parallelized. Matt specifically warns against letting the AI create horizontal slices (e.g., “do all schema first, then all API, then all frontend”).

5. Implementation (AFK)

This is where the human steps back. Pass the issues to an AI agent (or multiple agents in parallel) using a loop script. Each agent picks a task from the backlog, explores the repo, implements using TDD (red-green-refactor), runs feedback loops (tests, type checks), and commits. The key insight: clear the context between tasks — don’t compact. Fresh context = smart zone.

6. Automated Review

After implementation, run a separate AI reviewer in a fresh context (smart zone). Push your coding standards to the reviewer so it checks the implementation against them. Matt uses Sonnet for implementation and Opus for review.

### 6a. Reviewing a PR with Claude Code's built-in `/code-review` skill

The `/code-review` skill is a **built-in Claude Code skill** (not from Matt Pocock). It reviews the current diff for correctness bugs and cleanup opportunities. Use it directly in the Claude Code chat:

```
/code-review --effort high
```

What it does:
- Reads the unstaged/staged diff in your working tree
- Checks for correctness bugs, security issues, edge cases
- Suggests reuse/simplification/efficiency cleanups
- At `--effort high` it's more thorough (covers broader surface, may include uncertain findings)

Flags:
- `--effort low|medium|high` — controls depth. Low/medium = fewer, high-confidence findings. High/max = broader coverage.
- `--comment` — post findings as inline PR comments (if working against a GitHub PR)
- `--fix` — apply the suggested fixes directly to the working tree

### 6b. Reviewing with a separate Opus session (Matt's approach)

Matt's key insight: **review in a different model in a separate process**, never in the same session that wrote the code. The implementation session has "context contamination" — it knows what it *meant* to write, so it misses what it actually wrote. A fresh Opus session catches the real diff.

**For a PR on a branch (already pushed):**
```bash
gh pr checkout <number>
claude --model opus-4-8 -p "Review the diff on this branch against main. Check for correctness bugs, security issues, edge cases, and whether the implementation satisfies the acceptance criteria. Be thorough and adversarial — try to find problems, not confirm success."
```

**For uncommitted changes (before push):**
```bash
claude --model opus-4-8 -p "Review the working tree diff against my coding standards in CLAUDE.md."
```

### 6c. Pushing coding standards to the reviewer

Matt emphasizes **push vs. pull**: the reviewer gets the standards baked into its prompt; the implementer doesn't need to know them. The reviewer's prompt should include:

- Link to `CLAUDE.md` / `PRINCIPLES.md` / project coding standards
- Specific concerns: security, performance, error handling, edge cases
- The PRD or acceptance criteria to validate against

This works because:
- The reviewer is a **fresh session** — it has no context about implementation details, so it judges purely by the code and the standards
- The implementer focuses on **speed and correctness**; the reviewer focuses on **quality and style**
- Standards evolve in one place (the reviewer prompt), not spread across every implementation prompt

### 6d. Andrej Karpathy's code review style (from karpathy-guidelines skill)

The `karpathy-guidelines` skill (from `forrestchang/andrej-karpathy-skills`) encodes four behavioral guidelines that directly apply to review. The skill is invokeable as a slash command: `/karpathy-guidelines`.

**Note on installation:** If `/plugin install` fails with marketplace schema errors (corrupted `claude-plugins-official` registry), the skill may still be usable if already present. Check by running `/karpathy-guidelines` or looking in `.claude/plugins/marketplaces/karpathy-skills/`.

The four guidelines and how they apply to code review:

**1. Think Before Coding — Applied as reviewer:**
- Does the implementation surface its assumptions, or hide them?
- If multiple interpretations of the requirement exist, did the implementer pick silently?
- Did simpler approaches exist that were ignored?

**2. Simplicity First — Applied as reviewer:**
- Every line of code should trace directly to the user's request. If it doesn't, flag it.
- No abstractions for single-use code. No speculative flexibility or configurability.
- Overengineered code is a bug — flag it even if functionally correct.

**3. Surgical Changes — Applied as reviewer:**
- Did the diff touch code unrelated to the task? Adjacent refactoring, formatting changes, style rewrites — all red flags.
- Every changed line should trace directly to the request. If you can't explain why a line changed, it shouldn't have.
- Pre-existing dead code should be mentioned, not deleted.

**4. Goal-Driven Execution — Applied as reviewer:**
- Does the implementation have verifiable success criteria? Tests before code?
- For a fix: is there a test that reproduces the bug before the fix?
- Weak success criteria mean the implementation is incomplete.

**To use as a review checklist with Opus:**
```bash
claude --model opus-4-8 -p "Review the diff on this branch against these four criteria:
1. Are assumptions surfaced or hidden?
2. Is every line necessary, or is there overengineering?
3. Does every changed line trace to the request (no collateral changes)?
4. Does the fix have a test that reproduces the original problem?"
```

The skill's CLAUDE.md tradeoff note: *"These guidelines bias toward caution over speed. For trivial tasks, use judgment."*

7. Human QA & Code Review

Manually test the feature. This is where you impose your taste back onto the codebase. Any bugs or improvements you find become new issues on the Kanban board, feeding back into the implementation loop.

Key Principles ￼

- Keep tasks in the “smart zone” (~100K tokens). Beyond that, AI gets dumber.

- Prefer clearing context over compacting — fresh starts are predictable.

- Vertical slices over horizontal — get end-to-end feedback fast.

- Don’t keep old PRDs in the repo — they rot and mislead future AI sessions. Close the issues once done.

- Design deep modules (small interface, lots of functionality inside) rather than many shallow ones. This makes the codebase easier for AI to navigate and test.

- Push vs. Pull: Push coding standards to reviewers; let implementers pull them as needed via skills.

The overall mental model: the planning phases (1–4) are your day shift where humans are in the loop. Implementation (5–6) is the night shift where agents work autonomously. QA (7) brings the human back to close the loop and queue more work.
