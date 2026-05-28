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
2.   
    
3. ```bash
    mkdir -p .claude/skills
    ```
    

  

4. Install just the core skills Matt uses most for this workflow:
5.   
    
6. ```bash
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
2.   
    
3. ```markdown
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
    

  

4. Paste key API docs, examples, and decisions here (not the whole internet, just what you’ll actually use).
5. Tell Claude Code explicitly:“All Stripe/billing API facts should be treated as canonical when found in `docs/RESEARCH.md`. Use that file before going to external docs.”

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

4. Decide where PRD lives:

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

4. Now you have a **Kanban board** (GitHub projects or Linear) with a graph of work to be done.

This is what Ralph loops will chew through.

**7. Phase 6 – AI Execution with Ralph loops (+ TDD & guardrails)** 

This is where Matt wires AI into actual implementation.

**7.1 Configure git and safety** 

Use `git-guardrails-claude-code` so the agent can’t trash your repo:

1. Ensure that skill is installed (see step 1).
2. In Claude Code, explicitly instruct:“When performing any git operations, follow the `git-guardrails-claude-code` skill. Dangerous operations like force‑push, deleting branches, or rewriting history must be blocked or require my approval.”

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

**7.3 Run Ralph loop over Kanban** 

Ralph is basically an **agent loop runner** for your issues.

Typical setup:

1. Create a config (YAML/JSON) describing:

- Where issues live (GitHub repo / project).
- How to map ticket fields into prompts.
- What tools/skills Claude can use (file edits, tests, git, etc.).

3. From your terminal:
4.   
    
5. ```bash
    ralph run --config ralph.config.yaml
    ```
    

  

6. Ralph then:

- Pulls a “ready” ticket from the board (non‑blocked).
- Starts a loop:

- Read ticket + PRD + relevant code.
- Plan steps.
- Edit files using Claude (respecting `tdd` and git guardrails).
- Run tests / linters.
- Update the ticket status (e.g., “In progress → Done”).

- Moves on to next eligible ticket.

You can choose:

- **Sequential**: one loop at a time.
- **Parallel**: multiple Ralph workers, each on a different non‑blocking ticket.

Matt’s point is that if PRD + research + prototype + tickets are good, Ralph can run almost AFK and still produce quality work.

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