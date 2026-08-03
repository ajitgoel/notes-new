Initialize Spec Kit with a Claude integration, then drive development via the /speckit.* commands.
Here’s a concrete, end‑to‑end walkthrough of using **GitHub Spec Kit** with **Claude Code** for spec‑driven development.
**1. Install Spec Kit (Specify CLI)** 
In a terminal (macOS/Linux/WSL on your MacBook Pro is fine), install the CLI via `uv` as the README shows:
```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v0.12.4
```
If you prefer another version, swap `v0.12.4` for the tag listed in the [Releases](https://github.com/github/spec-kit/releases) page.
You can later upgrade/check:
```bash
specify self check
specify self upgrade          # to latest stable
# or
specify self upgrade --tag v0.12.4
```
**2. Bootstrap a project wired to Claude Code** 
Pick or create your project folder:
```bash
mkdir taskify
cd taskify
```
Run `specify init` and choose an integration that works with Claude Code. From the docs, Spec Kit supports many agents via integrations; one of them will correspond to your Claude setup (e.g. a Claude CLI or IDE integration). If you know the integration name, you can pass it directly:
```bash
specify init . --integration claude
# or whatever integration name matches your Claude Code setup
```
If you’re unsure or the CLI complains about agent tools, you can temporarily bypass the check:
```bash
specify init . --integration claude --ignore-agent-tools
```
After this, your repo will contain `.specify/` plus templates, and a `specs/` folder once you create your first feature.
Now open this folder in the environment where you use **Claude Code** (e.g. VS Code + Claude, or a Claude CLI pointed at this directory).
**3. Confirm Spec Kit commands inside Claude Code** 
With Claude Code running in that project directory, you should see slash commands like:
- `/speckit.constitution`
- `/speckit.specify`
- `/speckit.plan`
- `/speckit.tasks`
- `/speckit.implement`
- plus optional ones like `/speckit.clarify`, `/speckit.analyze`, `/speckit.checklist`, `/speckit.converge`

If you don’t see them, make sure:

1. You initialized with `specify init ... --integration <your-claude-integration>`.
2. The integration actually supports commands/skills and they’re loaded (often via `.claude/commands/` or similar which Spec Kit writes during `init`).

**4. Step 1 in the Spec Kit flow: Constitution (project principles)** 

This uses `/speckit.constitution` and writes `.specify/memory/constitution.md`.

In Claude Code, in the project:

```text
/speckit.constitution Create principles focused on code quality, testing standards, user experience consistency, and performance requirements. Include governance for how these principles should guide technical decisions and implementation choices.
```

  

Claude will:

- Ask questions or directly generate a constitution.
- Save it under `.specify/memory/constitution.md`.

You should skim that file and tweak anything that doesn’t match your preferences (e.g. you care about strict test coverage, simpler dependencies, token‑efficiency for agents, etc.).

Going forward, Claude Code will treat this constitution as the **governing document** for specs, plans, and implementation.

**5. Step 2: Create the functional spec with Claude +** `/speckit.specify` 

Now describe **what** you want to build, not the tech stack.

In Claude Code:

```text
/speckit.specify
Develop Taskify, a team productivity platform. It should allow users to create projects, add team members,
assign tasks, comment and move tasks between boards in Kanban style. In this initial phase for this feature,
let's call it "Create Taskify," let's have multiple users but the users will be declared ahead of time, predefined.
I want five users in two different categories, one product manager and four engineers. Let's create three
different sample projects. Let's have the standard Kanban columns for the status of each task, such as "To Do,"
"In Progress," "In Review," and "Done." ...
```

  

Spec Kit will:

- Create a **feature branch** like `001-create-taskify`.
- Create a directory such as `specs/001-create-taskify/`.
- Generate `spec.md` in that folder, based on the spec template.

At this point you have:

```text
.
├── .specify
│   ├── memory/constitution.md
│   ├── scripts/...
│   └── templates/spec-template.md, plan-template.md, tasks-template.md
└── specs
    └── 001-create-taskify
        └── spec.md
```

  

Open `specs/001-create-taskify/spec.md` and actually read it. This document is what Claude should implement against, not your ad‑hoc prompts.

**6. Step 3: Clarify the spec before planning** 

Use Spec Kit’s clarification flow to tighten the spec before you talk about tech stack.

In Claude Code:

```text
/speckit.clarify
```

  

Claude will ask structured questions and record answers in the spec (often in a Clarifications section and a Review & Acceptance checklist). If you still see vagueness, add a free‑form refinement prompt:

```text
For each sample project that you create there should be a variable number of tasks
between 5 and 15, randomly distributed across the Kanban columns, with at least
one task in each column.
```

  

Then ask Claude to run the checklist inside `spec.md`:

```text
Read the review and acceptance checklist in the spec, and check off each item if the feature spec meets the criteria. Leave it empty if it does not.
```

  

Result: you have a **high‑fidelity spec** that Claude Code will be held to.

**7. Step 4: Create a technical implementation plan with** `/speckit.plan` 

Now you tell Claude Code the **how** (stack, architecture) and let Spec Kit generate a detailed plan and supporting docs.

In Claude Code, run something like:

```text
/speckit.plan
We are going to generate this using .NET Aspire, using Postgres as the database.
The frontend should use Blazor Server with drag-and-drop task boards and real-time updates.
There should be REST APIs for projects, tasks, and notifications.
```

  

Spec Kit will expand this into a full set of planning artifacts under your feature directory, for example:

```text
specs/
  001-create-taskify/
    spec.md
    plan.md
    data-model.md
    quickstart.md
    research.md
    contracts/
      api-spec.json
      signalr-spec.md
```

  

Now:

- Review `plan.md` for structure of services, components, and phases.
- Check `data-model.md` to ensure entities look right.
- Use `research.md` for stack‑specific notes, especially for rapidly changing tech.

If Claude over‑engineers something, push back in‑spec:

```text
In plan.md you introduced an additional microservice I did not request.
Explain the rationale; then simplify to match the original spec and the constitution’s focus on minimal complexity.
```

  

This keeps the plan aligned with `.specify/memory/constitution.md` and your spec.

**8. Step 5: Have Claude audit and refine the plan** 

Before tasks and implementation, use Claude Code as a planner‑auditor:

```text
Now I want you to audit the implementation plan and the implementation detail files.
Identify any missing sequences of tasks that are implied by the spec but not covered,
and annotate plan.md with references to the relevant detail files for each step.
Also flag any over-engineered components that violate the constitution.
```

  

You can iterate here until the plan feels like a **clear, finite roadmap**.

If you’re using Git and GitHub CLI, this is a good moment to commit and optionally open a PR from your `001-create-taskify` branch so everything is tracked.

**9. Step 6: Generate a task breakdown with** `/speckit.tasks` 

Now use Spec Kit’s task generator so implementation is structured.

In Claude Code:

```text
/speckit.tasks
```

  

Spec Kit will create `tasks.md` under the feature directory, containing:

- Tasks grouped by user story.
- Dependency ordering (models → services → endpoints → UI).
- Parallelizable tasks marked `[P]`.
- File paths per task.
- TDD structure if tests were requested.
- Checkpoints at the end of each story.

Skim `tasks.md` and adjust if needed, then tell Claude explicitly:

```text
Use tasks.md as the canonical source of implementation steps. Do not invent new major tasks without updating tasks.md first.
```

  

**10. Step 7: Implement with** `/speckit.implement` **using Claude Code** 

Now you let Claude Code do the building, but still in a controlled loop.

In Claude Code:

```text
/speckit.implement
```

  

The integration will:

- Ensure constitution, spec, plan, and tasks exist.
- Walk tasks.md in order.
- Run local commands (`dotnet`, `npm`, etc.) as specified in tasks.
- Update files and report progress.

Your job as human‑in‑the‑loop:

1. Watch commits/diffs or Claude’s file summaries to ensure each task matches:

- The **spec** (`spec.md`)
- The **plan** (`plan.md`)
- The **constitution** (`constitution.md`)

3. When something drifts, reference the artifacts:
4. ```text
    The spec says there is no login. However, you added authentication plumbing in the API.
    Remove that and keep to spec.md and constitution.md’s simplicity constraints.
    ```
    
5.   
    
6. Run tests and the app locally (e.g. `dotnet test`, `dotnet run`, front‑end dev server).
7. Paste any runtime errors back into Claude Code for targeted fixes.

If Spec Kit/Claude add extras (e.g. too many services), you can re‑run a small planning or task update cycle, then re‑invoke `/speckit.implement`.

**11. Iterating, converging, and handling legacy** 

For subsequent features in the same project:

- Use scripts under `.specify/scripts/` (like `create-new-feature.sh`) or just `/speckit.specify` again to spin up `002-...`, `003-...`, etc.
- Each feature gets its own `spec.md`, `plan.md`, `tasks.md`, artifacts.
- You can use `/speckit.converge` to compare the existing codebase against spec/plan/tasks and generate a “remaining work” task list.

For an existing “brownfield” repo:

1. Run `specify init --here --integration <claude-integration>`.
2. Use `/speckit.constitution` plus Claude’s code understanding to draft a constitution that reflects what’s already there.
3. For each new feature or modernization step, go through:

- `/speckit.specify`
- `/speckit.clarify`
- `/speckit.plan`
- `/speckit.tasks`
- `/speckit.implement`

Your **Claude Code role** becomes: keep everything grounded in constitution + specs, not one‑off vibes.

**12. Optional: Customize Spec Kit around your style** 

Once you’re comfortable:

- Look at **presets** for custom templates and language:
- ```bash
    specify preset search
    specify preset add <preset-name>
    ```
    
-   
    
- Look at **extensions** to add extra capabilities (e.g. Jira integration, extra QA steps):
- ```bash
    specify extension search
    specify extension add <extension-name>
    ```
    
-   
    
- Combine these via **bundles** for a “developer” or “PM” stack you can install into any repo:
- ```bash
    specify bundle search
    specify bundle install <bundle-id>
    ```
    
-   
    

This lets you shape Spec Kit + Claude Code exactly to your agentic workflow preferences.

If you tell me your actual stack for the next project (e.g. “FastAPI + React” or “ASP.NET + Blazor”), I can sketch the exact first three or four prompts you’d send through `/speckit.constitution`, `/speckit.specify`, and `/speckit.plan` tailored to that environment, ready to paste into Claude Code.