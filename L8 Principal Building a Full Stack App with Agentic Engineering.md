https://www.youtube.com/watch?v=kPN564Kol14&t=3100s
**High-level flow of Kun’s agentic workflow** 

Kun’s process in this video is:  
**one main agent (Firstmate) + multiple delegated “crewmates” + rich artifacts (Lavish, Claude Design) + real infra + end‑to‑end test.**
I’ll walk through the video in order, showing:
- What Kun does at each step.
- How you can mirror it for your own project.
**1. Environment and tool setup** 
**1.1 Start from a clean terminal + Herdr** 
**What Kun does**
- Starts from an empty terminal, launches **Herdr**, his agent‑aware terminal multiplexer. ​⁠
- Herdr manages multiple agent sessions as tabs/panes and keeps an “agents side panel” so he can see which agents are doing what later. ​⁠
**How to apply**
- Use some way to manage multiple concurrent agent sessions:
- If you don’t have Herdr, use `tmux` or multiple terminals, but think in terms of “one tab = one agent/crewmate”.
- Keep a **main “captain” tab** where you talk to your orchestrator agent, and separate tabs for sub‑agents.
**1.2 Clone and launch Firstmate** 
**What Kun does**
- Clones his **Firstmate** repo and `cd`’s into it. ​⁠
- Launches a **Pi agent** inside this repo, configured to use **GPT‑5.6 Luna** via Codex as the underlying model. ​⁠
- Gives Firstmate a global constraint: **every task and crewmate must use GPT‑5.6 Luna on X‑high reasoning effort**. ​⁠
**How to apply**
- Decide your **agent harness + model** combo (Pi, Claude Code, Cursor, etc.).
- Have one “system” prompt or bootstrap message that sets:
- The model you want all sub‑agents to use.
- Global rules (e.g. no expensive models, specific coding style, languages, etc.).
- Ensure **your orchestrator can spawn sub‑agents** or equivalent (shell scripts, separate tools, etc.).
**1.3 Turn on “calm mode”** 
**What Kun does**
- Firstmate prints a lot of tool‑call noise by default.
- He enables `/calm` **mode**, which hides tool calls and only shows his prompts and Firstmate’s high‑level responses. ​⁠
**How to apply**
- If your tool supports “quiet” or “minimal logging” mode, turn it on for day‑to‑day work.
- Only inspect raw tool calls when debugging.
- Principle: **protect your attention**; let the agent do tool/IO chatter in the background.
**2. Kick off a brand‑new project with a “ramble”** 
**2.1 Make a repo + initial commit** 
**What Kun does**
- Asks Firstmate to:
- Create a new repo called **“Eddie’s Wallet”**.
- Initialize it locally with a `README` and initial commit. ​⁠
Firstmate runs a “session start” script, sets up the repo, and confirms it’s ready. ​⁠
**How to apply**
- For any new project, **delegate repo bootstrapping**:
- “Create a new repo called X, init README, set up basic `.gitignore`, and make an initial commit.”
- This ensures your agent always has a **grounded file system** to work within.
**2.2 Long, natural‑language product brief (“rambling”)** 
**What Kun does**
He then **talks for several minutes** to Firstmate, giving a narrative brief for “Eddie’s Wallet”: ​⁠
- Real‑world problem: tracking his son’s allowance (currently notebooks / memory).
- Product concept: **iOS‑only** allowance app for kids:
- Parent mode (deposit/withdraw, rules).
- Child mode (read‑only view of balance, loans, etc.).
- Educational angle:
- Introduce **balance, loans, credit cards, repayments, interest** gradually.
- Constraints:
- **Virtual money only**, not real bank integration.
- Data must be **cloud‑backed** (multiple devices).
- Tasks he wants **in parallel**:
1. **Market research** (does such an app already exist?).
2. **Technical research** (how to implement full stack, iOS + backend).
He doesn’t compress this into a tiny prompt; he **dumps his thinking** and trusts the agent to structure it. ​⁠
**How to apply**
- For your project, open the orchestrator and “ramble” for 2–5 minutes:
- Problem + who it’s for.
- Real‑world constraints.
- Tech stack preferences (if any).
- What parallel tracks you want (e.g., “market scan + tech architecture”, “backend plan + UX skeleton”).
- Don’t over‑optimize the first prompt; let it be **rich, messy, and honest**. Kun shows the models can rationalize it.
**3. Let the orchestrator spawn parallel “crewmates”** 
**3.1 Market research & technical research as separate agents** 
**What Kun does**
- Firstmate responds that it has started **two “scouts”**:
- A **market research** crewmate.
- A **technical research** crewmate. ​⁠
- In Herdr, each crewmate runs in its own tab. They:
- Search the web.
- Fetch docs.
- Build **markdown reports** in the project folder. ​⁠

**How to apply**

- When you give your initial mission, explicitly ask for **named parallel agents**:

- “Start ‘Market Scout’ to study existing tools and gaps.”
- “Start ‘Tech Architect’ to propose stack, infra, and key decisions.”

- Have them **write reports into your repo** (`market_report.md`, `tech_plan.md`) so you can inspect and version them.

**3.2 Use “fast mode” when demoing or impatient** 

**What Kun does**

- Enables a **“fast mode”** extension for Codex, trading more tokens for lower latency. ​⁠

**How to apply**

- For demos or sprints, turn on any **high‑effort / high‑speed mode** your provider offers.
- When cost matters more, run with standard effort.

**3.3 Inspect crewmates only when needed** 

**What Kun does**

- Dips into the **market research tab** to see the agent writing a long report. ​⁠
- Looks at the **technical research tab**, notices it:

- Is building a backlog of open decisions.
- Is fetching iOS docs based on his requirement. ​⁠

- He emphasizes: usually he **doesn’t** stare at crewmates; he trusts them and only peeks for demos or debugging. ​⁠

**How to apply**

- Treat crewmates like **junior engineers**:

- Let them run.
- Occasionally jump in if something seems off.

- Make sure they **report back** to the orchestrator in a structured way (status messages, artifacts, or PRs).

**4. Decide whether to build and refine the tech plan (Lavish)** 

**4.1 Read market findings and decide to build** 

**What Kun does**

- Firstmate summarizes market scout’s conclusion:

- No existing iOS app that matches his full “virtual family wallet + education + parent/child mode” brief.
- Several somewhat similar products; lists them. ​⁠

- Recommendation: **prototype Eddie’s Wallet** instead of just installing another app. Kun agrees. ​⁠

**How to apply**

- For your own market scout:

- Ask: “Do we have a strong reason to build this?”.
- If yes, explicitly accept and let the orchestrator move on to **deeper design and implementation**.

**4.2 Technical plan via Lavish (interactive HTML doc)** 

**What Kun does**

- Technical research outputs a big `report.md` (351 lines). He opens it in Vim and finds it exhausting to read. ​⁠
- Asks Firstmate to **summarize and structure the backend proposal in Lavish**, an HTML artifact editor he built for interactive planning. ​⁠
- Lavish generates an interactive HTML page:

- Short summary (“buy operational simplicity before buying infrastructure”). ​⁠
- Decision sections (Supabase vs VPS vs CloudKit).
- Architecture diagrams.
- Clickable options and annotations.

Kun **disagrees** with Supabase pricing and the assumption that VPS is $30–$60/month. He annotates the Lavish doc with corrections (e.g., “I’ve used Hetzner; it’s more like $5–$10/month”). ​⁠

Lavish sends his feedback back to the agent; the agent re‑researches pricing and updates the proposal, now recommending a **cheap Hetzner VPS** as viable. Kun then **commits to VPS** in the decision section. ​⁠

**How to apply**

- Let your “Tech Architect” agent:

- Produce **one long markdown report** initially.
- Then ask it to **convert the plan into an interactive artifact** (if you have a tool like Lavish) or at least:

- A clear decision table.
- Architecture diagram (Mermaid or PlantUML).
- Trade‑offs.

- Use your **judgment**:

- Correct wrong assumptions (pricing, complexity).
- Simplify — Kun repeatedly cuts over‑engineering (audit logs, multiple envs, etc.).

- Have the agent **revise the plan** in response to your annotations.

**5. UX prototyping with HTML artifacts** 

**5.1 From scope to playable prototypes** 

**What Kun does**

- A UX crewmate first writes a **scope markdown** (what screens to prototype), but that’s not interactive. ​⁠
- Kun asks: don’t just plan; **build real HTML wireframes in Lavish** that he can click through:

- Parent vs child flows.
- Family setup.
- Allowance rules.
- Loan simulator. ​⁠

- Lavish generates an **interactive prototype**:

- Journey map (onboarding, dashboards, lessons). ​⁠
- Clickable screens, including read‑only child wallet and parent dashboard.

- Kun clicks through and leaves **specific UX feedback**:

- No in‑app “request points” feature; he wants kids to ask parents in person. ​⁠
- Remove redundant buttons (like an extra “Recent activity” button when that list is already visible). ​⁠
- Keep wireframes rough; polish later.

He then instructs Firstmate: treat this prototype + feedback as the basis for a **PRD**. Write the PRD into `README.md` in the repo. ​⁠

**How to apply**

- Ask your agent to:

- First, define **UX scope** (key flows, screens, states).
- Then, generate **clickable HTML prototypes** (or Figma‑like structures) without any app code.

- Interact with the prototype:

- Mark redundant or confusing elements.
- Make decisions (“no gamification for now”, “virtual money only”, “no child‑initiated requests”).

- Convert the final agreed prototype into a **PRD / README** in your repo.

**6. GitHub repo + PR workflow** 

**6.1 Public repo, PRD as README, PR automation** 

**What Kun does**

- Tells Firstmate:

- Commit the updated README/PRD.
- Create a **public GitHub repo** under his account.
- Push the code and wire repo+remote correctly. ​⁠

- Firstmate asks which GitHub account to use, then creates the repo, fixes a spelling error in the project name everywhere via a **PR**, and merges it. ​⁠
- Kun tells Firstmate: for now, **auto‑merge PRs** without asking his approval; later, when the project is more serious, they can turn on stricter “no‑mistakes” flows. ​⁠

**How to apply**

- Have your orchestrator:

- Create/import the GitHub repo.
- Configure **branch & PR settings** based on project maturity:

- Early: allow direct merges or auto‑merges from the agent.
- Later: require tests + manual approval (possibly with something like Kun’s “no‑mistakes” pipeline).

- Keep **product docs (PRDs) in README or** `/docs`, written by agents from your planning artifacts.

**7. Visual design via Claude Design (or equivalent)** 

**7.1 Create a design system, not just screens** 

**What Kun does**

- Uses **Claude Design** to create a **design system** (not just individual screens):

- Links the GitHub repo as project context.
- Writes notes: this is an iOS app for kids + parents; read the README to infer flows; aim for fun, educational, kid‑friendly design. ​⁠

- Claude Design (using **Sonnet**, not Opus, to stay frugal) generates:

- Brand icon & logo variants.
- Color palette.
- Typography.
- Screen mocks: sign‑in, child view, parent dashboard, lessons, etc. ​⁠

He iterates:

- Feels the design isn’t kid‑like enough; asks to **lean more into a children’s vibe**. ​⁠
- Requests **app icon options**, chooses a piggy‑bank icon, and asks for properly sized iOS icon assets. ​⁠

**How to apply**

- Use a design‑capable model or tool to:

- Create a **design system** with tokens (colors, typography, components).
- Derive screen mocks from that system.

- Keep your feedback high‑level and product‑driven:

- “More child‑friendly.”
- “Avoid dark, corporate colors.”
- “Emphasize readability for 8–10‑year‑olds.”

**8. Backend infra and secrets management** 

**8.1 Hetzner VPS + OpenTofu infra as code** 

**What Kun does**

- Decides (based on revised Lavish plan) to use a **Hetzner VPS**, codified with **OpenTofu** (Terraform fork). ​⁠
- Tells Firstmate: “Use OpenTofu to declare the VPS infra (server, DB, backups) as code. No hand‑configured servers.” ​⁠
- Firstmate launches a dedicated crewmate to:

- Read Hetzner pricing via API.
- Provision a small VPS.
- Write OpenTofu modules & configs into a **separate backend repo** (private). ​⁠

**How to apply**

- For your backend, ask an “Infra” crewmate to:

- Choose a modest single‑node infra (Hetzner, DigitalOcean, Lightsail, etc.).
- Codify it via Terraform/OpenTofu.
- Create a **private repo** for backend code & infra.

- Avoid per‑environment complexity until you have real users; Kun explicitly rejects having multiple envs for an MVP. ​⁠

**8.2 Autonomic Vault for secrets** 

**What Kun does**

- Uses **Autonomic Vault** to store secrets (Hetzner token, Cloudflare token), created by Max Howell (Homebrew creator). ​⁠
- When agents need a secret:

- Vault prompts Kun for **approval**.
- Secret is injected as an environment variable, not shown in plaintext to the model. ​⁠

**How to apply**

- Don’t put prod secrets in `.env` files around agents.
- Use a **secret manager** that:

- Mediates access via short‑lived tokens or env injection.
- Logs each use.
- Allows you to approve/deny from a UI.

**8.3 Domain + DNS via Cloudflare** 

**What Kun does**

- Uses his **Cloudflare API token** to let a crewmate:

- Create a subdomain `eddieswallet.kunchenguid.com`.
- Point DNS to the Hetzner VPS. ​⁠

- Corrects himself when he mis‑remembers the secret name; the agent re‑tries with the right one. ​⁠

**How to apply**

- If you own a domain, let an infra agent:

- Manage DNS records via API for your services (CNAME, A records).

- Keep DNS config codified or at least **documented in the repo** (e.g., `infra/dns.md`), similar to Kun’s approach with Lavish artifacts + code.

**9. Browser automation for App Store / Apple Sign‑In** 

**9.1 Agent controls Chrome DevTools** 

**What Kun does**

- Has already enabled **Chrome remote debugging** so agents can control an existing browser session with his cookies. ​⁠
- Firstmate outlines Apple Developer steps to set up bundle IDs and Apple Sign‑In. Kun then says: **“You can do these steps for me with DevTools”**; he’s already logged into App Store Connect. ​⁠
- The agent:

- Navigates App Store Connect.
- Creates app IDs and capabilities.
- Handles validation errors (wrong name) on a second try with Kun’s hints. ​⁠

**How to apply**

- For any **web‑based configuration** (cloud console, dashboards):

- Consider letting an agent drive your browser via DevTools / remote debugging.
- Start with non‑destructive operations; inspect logs to make sure nothing risky is done.

**10. Frontend build aligned to design system** 

**10.1 Import design archive, build real app** 

**What Kun does**

- Exports the full Claude Design system as a **zip archive**.
- Tells Firstmate:

- Import the archive into the repo.
- Use it as the **source of truth** to implement a real iOS app (iPhone+iPad) UI.
- Keep backend code in a separate private repo. ​⁠

- A dedicated **frontend crewmate**:

- Generates SwiftUI (or UIKit) screens consistent with the design system.
- Wires up mock data first.
- Later, integrates real API endpoints once backend is ready. ​⁠

**How to apply**

- Treat design artifacts as **input to a “UI implementation” agent**:

- “Here’s the design archive/Figma export. Implement screens in my chosen UI framework, following layout and tokens.”

- Keep front‑ and backend in **separate repos** once things get serious (security + modularity).

**11. End‑to‑end integration and testing** 

**11.1 Define the MVP “done” condition** 

**What Kun does**

- States explicitly: MVP is done when he can:

- On an iOS simulator, launch the app.
- Sign in with his **real Apple ID**.
- Hit his real backend via HTTPS and see persisted data (balance). ​⁠

- He steers the agent away from over‑engineering:

- No multiple env tiers (dev/stage/prod); just one **production‑like** env for now. ​⁠

**How to apply**

- For your project, define a **single concrete E2E test** that marks MVP:

- “From browser/mobile, perform action X, see persisted result Y from real backend.”

- Have the agent **back‑plan** required tasks from that test.

**11.2 Simulator run, bug fixing, neutral language** 

**What Kun does**

- Firstmate coordinates:

- Backend online at a proper HTTPS host.
- iOS app pointing to that host.
- Launch of simulators (iPhone and iPad). ​⁠

- Agents test sign‑in, find an Apple auth bug, and fix it without Kun manually debugging code; he urges them: “Don’t ask me unless only I can do it.” ​⁠
- He also spots a **product‑level issue**: the app uses “Eddie” as if it’s always the child’s name; he asks for neutral wording, and a crewmate updates copy accordingly. ​⁠

**How to apply**

- Have a **“test & QA” agent**:

- Run app in simulator or local environment.
- Log UI and API failures, then propose/fix patches.

- Reserve your own attention for **semantic issues** (copy, branding, flows), not wiring bugs.

**11.3 Final demo: it works end‑to‑end** 

**What Kun does**

- At the end, he manually verifies: ​⁠

- Launches the app in iOS simulator.
- Signs in using Apple.
- Adds $10 in parent mode; sees balance update.
- Switches to child view; sees $10.
- Signs out and back in; verifies data persists via the VPS backend.

- He then plans to:

- Build a TestFlight build.
- Install on his son’s real iPad and start using it “tomorrow.” ​⁠

**How to apply**

- Always finish a big agentic session with a **hands‑on E2E test**:

- Don’t trust logs alone; click through like a user.
- Only then declare that version of the project “done enough.”

**12. Core patterns you can copy directly** 

If you want to apply Kun’s workflow to your own project (like an Azure DevOps learning tool or any app), reuse these patterns:

1. **One orchestrator, many crewmates**  
    Main agent (Firstmate) that:

- Knows your long‑term mission.
- Spawns named crewmates for market, tech plan, infra, UX, backend, frontend, QA.

3. **Rich artifacts, not just chat**

- Markdown reports for initial research.
- Interactive HTML artifacts (Lavish) for plans, diagrams, and wireframes.
- Design systems for UI consistency (Claude Design or similar).

5. **You supply judgment, not execution**

- Correct pricing, simplify architecture, push back on over‑engineering.
- Decide product and UX trade‑offs.
- Let agents own **execution, code, and wiring** as much as possible.

7. **Secrets + infra as code from day one**

- Vault‑style secret manager.
- Terraform/OpenTofu for VPS and DB.
- DNS managed via API.

9. **MVP defined by an E2E test**

- One crisp end‑to‑end flow that proves everything works in the real stack.
- All planning and implementation oriented around passing that test.

If you tell me what project you’re currently building, I

------------------
Yes. You can absolutely have Firstmate implement independent tickets in parallel feature branches and merge them via PRs.
Given how Kun uses Firstmate in the video, what you’re describing is very much in‑pattern. He already has Firstmate:
- Spin up **multiple crewmates in parallel** for different workstreams (market research, tech research, UX, backend, frontend). ​⁠
- Have each crewmate do a chunk of work, write artifacts or code, and then **report back**. ​⁠
- Use a **PR‑based workflow** where a worker agent prepares changes and Firstmate reviews/merges them. ​⁠
So, adapting that to “multiple tickets in parallel” is natural.
**How to structure it with Firstmate** 
1. **Start from your main repo state**Make sure `main` (or `master`) is clean and pushed. Firstmate should understand this as the “source of truth” branch.
2. **Give Firstmate the ticket list and dependency info**Talk to it the same way Kun “rambles” requirements:“==We have these three tickets that are independent of each other.==  
    ==– TICKET‑1: …==  
    ==– TICKET‑2: …==  
    ==– TICKET‑3: …==  
    ==None of them depend on one another.==  
    ==I want you to implement them **in parallel**, each in its own feature branch, then raise PRs back to `main`==.”That’s analogous to how Kun tells Firstmate to kick off both market and technical research simultaneously, and then also UX prototypes, backend infra, etc. ​⁠
3. **Ask Firstmate to create one crewmate per ticket**Explicitly:“Create one crewmate per ticket. For each ticket:  
    – Branch from `main` into `feature/<ticket-id>`  
    – Implement the ticket  
    – Open a PR from that branch back to `main`  
    – Run tests/linters you consider relevant before marking the PR ready.”This matches the way Firstmate dispatches “market research”, “technical research”, “UX prototypes”, etc. as separate agents rather than doing everything itself. ​⁠
4. **Let crewmates work and Firstmate juggle**Firstmate’s role is to:
- Keep you in one “captain” session while it **juggles multiple agents**.
- Receive status updates when each crewmate is done. ​⁠
- Tear down finished sessions automatically.For you, that means: you just talk to Firstmate; it worries about which ticket‑agent needs steering.
1. **PR review and merge policy**You can configure policies similar to Kun’s:
- Early stage: allow Firstmate to **auto‑merge PRs** once the agent has checked them (“you don’t need my approval for PRs right now”). ​⁠
- Later: enable **“no‑mistakes”‑style validation** (tests, linters, maybe a manual glance) before merging.Tell Firstmate something like:“For each ticket PR:  
    – Ensure tests pass and diff looks sane.  
    – If everything is green, you may merge without asking me.  
    – If there are conflicts or failing tests, pause and summarize the issue to me.”
1. **Handling conflicts and ordering**Even “independent” tickets can conflict (same file, same function). To keep things sane:
- Ask Firstmate to **keep branches short‑lived**:
- Implement ticket → open PR → merge once green → rebase the others on updated `main`.
- Or state an order:
- “Implement TICKET‑1 and TICKET‑2 in parallel, but don’t merge both at once. Merge whichever PR goes green first, then rebase the other and resolve conflicts before merging.”That keeps autonomy but avoids subtle merge hell.
1. **What you actually say to Firstmate (example prompt)**In your captain session, something like:“We’re in repo X on the `main` branch.  
    I have three tickets that are independent:  
    – TICKET‑123: …  
    – TICKET‑124: …  
    – TICKET‑125: …I want you to:  
    – Create one crewmate per ticket.  
    – For each ticket:  
    – Branch from `main` into `feature/<ticket-id>`  
    – Implement the ticket  
    – Run tests and linting  
    – Open a PR from `feature/<ticket-id>` → `main`  
    – Merge each PR once tests pass and the diff is reasonable.  
    – If there are conflicts between PRs, rebase and resolve them in the feature branch before merging.  
    – Keep me updated when each ticket is done and its PR merged, but don’t block on my manual review unless something looks risky.”
That is fully aligned with the patterns Kun shows: one orchestrator, multiple parallel crewmates, PR‑based integration, you providing judgment on policies and trade‑offs, not on every line of code.