https://www.youtube.com/watch?v=fAdVE_W6xBo&t=120s

Here’s the full transcript, plus a concise summary of the MCP tools used and the build workflow.

**Full transcript (cleaned)**
Okay, I’m not one of those people that will tell you “build anything with AI,” but today I created a full iOS app completely with Kiro.dev. And I’m talking full iOS app in Swift. I’m not talking Replit, not talking Lovable. I’m talking about one that could eventually be put on the App Store. And my goal here was to test out Kiro.dev’s spec‑driven development, to utilize its free usage limits, and to see how far I can take this. And I was really surprised and I’ve learned a lot. I’m going to share it with you.

Here’s the app right here. I’m going to click, and I haven’t really worked on the UI yet, but it works pretty well. As you see here, I can just click one of the activities I want—let’s say cycling, that’s what I did before—less than an hour, 30 minutes. I can add the amount of calories, I’ve got the distance, I can choose indoor or outdoor, and it even shows me past workouts. And the great thing about this is it saves to my Apple Health and then updates to my Apple Watch’s Activity.

  

So now just some context. During Corona, I gained some weight. And one habit I implemented to lose that weight was to work out every day. That could just be doing—here, riding the bike—I don’t know if you can see. Let me turn on my glasses. So this is what I do every day. Ignore the mess. I work out while I learn, while I build, while I work. And I’m able to just lean forward—it’s not the perfect setup—but I’m able to lean forward and type and work on other projects.

  

One of the ways I held myself accountable was to always hit my Move goal—to have a consistent Move streak. So I set myself a goal of 700 Move calories per day, and no matter what, I would have to achieve it. If I did a workout but it didn’t register in the app, then I’ll just do another workout. Obviously, I also changed my eating habits, my sleeping habits, but the main actionable thing was “no matter what, hit my Move goal.” That means ride the bike, swim, lift weights, go for a walk—whatever.

  

But what happens when you do a workout and you forget to trigger it on your Apple Watch? Well, in that situation, you’re kind of screwed, because if it didn’t count on the watch, then you’re not going to hit your goal, and then you’re going to lose your Move streak. Basically, that’s what happened to me yesterday. I did a long workout, I didn’t record it on my watch, and it was lost—it’s like it didn’t happen.

  

So basically, I wanted to build an iPhone app where I could retroactively add workouts if I missed it, or my Apple Watch was dead, or I forgot to wear it, or whatever.

  

Now I’m just going to quickly show you how I did it. I tried to do this with the most basic setup, but I did add some MCP servers: I have Brave Search, Context 7, Sequential Thinking, Bright Data, and Basic Memory—and I’ll explain to you how I used each one.

  

For me, the main selling point of Kiro.dev is its spec‑driven development, where you create specs—you create requirements—first. You see that right away when you open up the chat interface for Kiro. You can either do “vibe code” or “spec.” So I just click “spec.” You can scroll all the way back to the first conversation. This is the first chat. I say: I have an Apple Watch and iPhone, I use Activity Rings/Apple Watch, every day I try to hit my rings—try to hit my activity goals—sometimes I forget my watch. And I go into more detail here; I give it a lot of context. Then it created a PRD, a requirements.mmd file, and it goes up here to the specs. It said, “Do the requirements look good? If so, we can move to the design.” I said, “Yeah, go for it.”

  

Then, because I already had the Context 7 MCP connected, it looked up the information needed about iOS development and Apple Health and everything. I also used Brave Search. And then I hit my first bug, where it pretty much says “due to heavy load, the model failed,” which means too many people are using the agent, or it’s just a problem with Amazon’s connection to Claude. As we know, Claude has a bunch of rate‑limit issues. And this was, by the way, the most common bug I’ve had in Kiro, where it just fails and it gives you this button to retry.

  

So I went back, looked at what it already created, and eventually it said, “Let me create the design file.” I checked out the design file—looked good—and then it said, “Let me create the tasks file.” And this whole process, I pretty much just let it run: I checked what it did, I said “Keep going,” checked it, kept going.

  

Basically, it created the specs, it created the design, it created the task list, and used Brave Search, Sequential Thinking, Context 7 to get all the context to build this all out. Then all you do is you go to the task list and you just press “Start Task.” And what it does is it opens up a new chat—fresh context—and just does all the things in the task list. And what’s great about it is it also creates tests and tests it out.

  

Long story short: I went through all these tasks one at a time. Only when it finished did it mark as complete. It relates to the requirements file, which you go here and see that—very cool.

  

The main bug I had was when the model pretty much failed—it got overloaded—and it gave me the retry button. Unfortunately, retry starts the whole task from the beginning. It will look at the task list and then it will look at the code that’s already written, and it will just go through everything and figure it out and then pick up where it left off. But that takes more tokens—eats into the context window. So instead of pressing retry, what I ended up doing was just sending this message: “Try again. Don’t start the whole task from the beginning. You got stuck. Look at the chat to figure out where you got stuck.” And it picked up where it left off. This was a very effective workaround. And I’m sure this and other bugs will be fixed very soon.

  

One thing I really like about the agent here is if you just press up—just like in the terminal—you can see your previous messages, so you can resend the same thing. You don’t have to copy and paste. Very useful, very effective. To be honest, I’m not sure if Cursor or Windsurf has that functionality—I’ve never tried it—but I’ve been using it a lot here.

  

I just want to talk about two other MCP servers I was using in this: Bright Data and Basic Memory. So you have the agent “steering,” which is pretty much like Cursor rules, Claude’s agents.md, Gemini, etc. If you press the plus button, it will start a new chat. It will look at your codebase, create separate product structure and text files. What I noticed, though, is that it doesn’t look at your specs. So what I ended up doing was I said “look at the specs as well and incorporate that into each subsequent file,” and it did that and made it much better.

  

But then I noticed something else: the problem was at first I didn’t implement Basic Memory. So I put a new line in my agent steering saying: “At the end of every task, when you’ve completed it, take what you learned—issues we faced—save them to Basic Memory.” And then I also add another line saying: “When starting a new task, look at Basic Memory.” Essentially what I was able to do was get it to learn from its mistakes.

  

Lastly, the Bright Data MCP. This was very useful where Context 7 and Brave Search just didn’t cut it. Context 7 is really good at getting the latest documentation. Brave Search can do very shallow searches on the Internet. Bright Data is way more powerful because it could answer CAPTCHAs, it has ways of looking at Reddit. So basically I said “use Bright Data—look at forums, look at Reddit—to find out all the nuances of Apple HealthKit and Apple Watch.” It was able to dig deeper and find other developers that had similar issues and problems. And once I figured all that out, it updated the task list.

  

Bottom line: For the last few hours I was using Kiro.dev just to build an iPhone app. I think it was pretty successful. Obviously there’s more work to do on it, but the functionality was there. The spec‑driven development really worked—it was amazing. It understood the context, it broke it into tasks, and it achieved every task on the list sequentially and ran tests. Having this all built into one IDE really made the difference. I just got to sit back and relax, press run, start new task, got it to pick itself back up when the model failed, pushed it to use Bright Data or use other MCPs when I needed to. And this is just my first go at building something with Kiro, and I think this is the way forward. I believe everyone else will add this functionality to their IDEs, but they’re the first ones. And right now there’s no limits. So go forward, build things, let me know what you guys think. Thank you for watching and have a great [day].

  

Summary of MCP tools used

- Brave Search: Used for quick, shallow web lookups during planning and implementation. Helpful, but limited depth compared to Bright Data.
- Context 7 (Upstash): Pulled recent documentation and official references (e.g., iOS/HealthKit) to inform design and coding decisions. Strong for docs retrieval.
- Sequential Thinking: Assisted with structured, stepwise reasoning in multi-step tasks, helping the agent follow the task list methodically.
- Bright Data: Heavy-duty web access for deeper research, including handling CAPTCHAs and scraping forums/Reddit. Used to uncover nuanced Apple HealthKit/Apple Watch integration issues that basic search tools missed; findings fed back into the task list.
- Basic Memory (Basic Machines): Lightweight memory layer. The agent was instructed to save “lessons learned” at the end of each task and read them before starting the next, enabling it to avoid repeating mistakes across tasks/sessions.

  

Detailed workflow to create the app (as shown in the video)

1. Define the problem and goals in Kiro “spec” mode

- Describe the use case (retroactive workout logging that writes to Apple Health and updates Apple Watch Activity).
- Provide extensive context and requirements directly in the chat.

3. Auto-generate specs and design

- Kiro creates a PRD/requirements file (requirements.mmd).
- Confirm specs, then generate a design file.
- Generate a task list that maps specs to implementable steps and tests.

5. Connect MCP servers and gather context

- With Context 7 and Brave Search connected, pull iOS/HealthKit docs and implementation patterns.
- Use Sequential Thinking to plan and execute multi-step work.
- When surface-level search isn’t enough, switch to Bright Data for deep dives (forums, Reddit) to resolve edge cases and API nuances; update the task list with new insights.

7. Execute tasks in Kiro

- Click “Start Task” to open a fresh chat context for that task.
- Let Kiro implement code, generate tests, and run them.
- Tasks are marked complete only when finished; they stay linked back to the requirements.

9. Handle model failures efficiently

- Common issue: model load/rate-limit failures.
- Avoid pressing the generic Retry (which restarts the task and consumes tokens).
- Instead, message the agent to “try again without restarting the task; review the chat to resume where stuck.”
- Use the “up arrow” recall to quickly resend prior steering messages.

11. Add persistent memory

- Configure steering so the agent saves what it learned after each task to Basic Memory.
- At the start of each new task, instruct the agent to consult Basic Memory, reducing repeated mistakes.

13. Ensure specs are incorporated across new chats

- New chats (via the plus button) analyze the codebase, but may ignore specs by default.
- Explicitly tell the agent to load and honor the spec files in each new chat to keep work aligned.

15. Iterate to functional app

- Build out UI minimally but focus on core flows: select activity type (e.g., cycling), set duration, calories, distance, indoor/outdoor; view past workouts.
- Confirm writes to Apple Health and that Apple Watch Activity rings update accordingly.
- Continue refining tasks/tests as issues surface from research.

17. Wrap-up and results

- The spec-driven approach plus targeted MCP usage enabled a production-viable Swift app in hours.
- The integrated IDE flow (specs → design → tasks → tests) and selective use of research MCPs were key to speed and reliability.

--------------

Below are concrete, implementation-level details tied to what’s covered across JeredBlu’s videos on this page (e.g., Sequential Thinking, Context 7, Bright Data, Cursor/Claude MCP setup). Where relevant, I’ll point you to the specific videos here that dive into each tool so you can see it done end-to-end.

**Connect MCP servers and gather context**

  

Goal: Wire up the same research/planning stack shown across the videos—Sequential Thinking, Brave Search, Context 7, Bright Data—so your agent can (1) plan multi-step work, (2) grab official docs fast, (3) do quick web lookups, and (4) escalate to deep forum/reddit research when needed.

1. Install and run MCP servers

- Sequential Thinking (planning): lets the agent structure work into sub-steps and check progress.
- Brave Search (light web search): quick external lookups.
- Context 7 (docs retrieval): fetches latest framework docs (e.g., Apple, iOS, Swift).
- Bright Data (deep research): handles CAPTCHAs, reddit/forums, nuanced issues.
- Basic Memory (used later for persistence).
- You’ll typically run these as local processes (or Docker) with per-server env vars (keys/tokens). The “Cursor + MCP Servers: Complete Setup Guide” and “Claude Code + MCP Setup: Late Night Tutorial” on this page demonstrate practical setup end to end.

3. Register servers in your MCP client (Cursor/Claude Code/Kiro-like IDE)

- In Cursor/Claude Code, you’ll use a config file (project- or user-level).
- Example config (redact your own tokens):
```
{
  "mcpServers": {
    "sequentialthinking": {
      "command": "node",
      "args": ["./servers/sequentialthinking/index.js"]
    },
    "brave-search": {
      "command": "node",
      "args": ["./servers/brave-search/index.js"],
      "env": { "BRAVE_API_KEY": "YOUR_KEY" }
    },
    "context7": {
      "command": "node",
      "args": ["./servers/context7/index.js"],
      "env": {
        "CONTEXT7_URL": "https://api.context7.example",
        "CONTEXT7_TOKEN": "YOUR_TOKEN"
      }
    },
    "brightdata": {
      "command": "node",
      "args": ["./servers/brightdata/index.js"],
      "env": {
        "BRIGHT_DATA_USER": "YOUR_USER",
        "BRIGHT_DATA_PASS": "YOUR_PASS",
        "BRIGHT_DATA_PROXY": "zproxy.lum-superproxy.io:22225"
      }
    },
    "basic-memory": {
      "command": "node",
      "args": ["./servers/basic-memory/index.js"],
      "env": {
        "BASIC_MEMORY_STORE": "./.mcp-memory"
      }
    }
  }
}
```

  

- See “Cursor + MCP Servers: Complete Setup Guide,” “Claude Code + MCP Setup,” “Context 7 MCP: Get Documentation Instantly,” and “Bright Data MCP Beats ChatGPT Agent—Here’s Why” on this page for tool-specific nuances.

1. Add steering so the agent uses the right tool for the job

- In your project’s “rules”/“steering” file (e.g., cursor-rules.md, agents.md, or Kiro steering), add explicit routing preferences:

- Use Context 7 first for official docs and API references (iOS/HealthKit).
- Use Brave Search for quick, shallow lookups.
- Escalate to Bright Data for CAPTCHAs/reddit/forums and “edge-case” troubleshooting.
- Use Sequential Thinking to decompose tasks and track progress/checkpoints.

- Example steering snippet:

- “When researching APIs, first query Context 7 for docs. If the answer is incomplete or implementation-specific, do a quick Brave Search. If questions remain or content is gated/behind CAPTCHAs, use Bright Data to extract detailed forum/reddit discussions. Use Sequential Thinking to outline research steps and checkpoint conclusions before coding.”

3. Use a research pattern inside tasks

- For each task, have the agent:

- Plan with Sequential Thinking: outline steps and success criteria.
- Docs pass with Context 7: pull official API types, required entitlements, permissions, examples.
- Shallow pass with Brave: confirm patterns or find common snippets.
- Deep pass with Bright Data: find edge cases, forum wisdom, pitfalls (e.g., HealthKit write permissions, retroactive workout caveats).

5. Cache and reference findings

- Save brief notes from each research pass (even just a summary file in docs/research/). This will tie into persistent memory later.

  

Tip: If you’re following along with videos here, the ones titled “Sequential Thinking MCP…,” “Context 7 MCP…,” and “Bright Data MCP…” each show usage patterns and troubleshooting in the exact tools used.

  

**Add persistent memory**

  

Goal: Make the agent “remember” lessons learned—errors, fixes, API quirks—across tasks/sessions using the Basic Memory MCP shown on this page.

1. Stand up Basic Memory

- Start the Basic Memory server from the repo and register it in your MCP config (see previous JSON example).
- Choose a store path (e.g., ./.mcp-memory) or a remote backend if supported.

3. Define a structured memory schema

- Use a simple schema to store actionable items:

```
{
  "type": "ios-healthkit-workflow",
  "taskId": "task-setup-health-permissions",
  "timestamp": "2025-08-29T14:00:00Z",
  "tags": ["healthkit", "permissions", "xcode", "entitlements"],
  "summary": "HKHealthStore requestAuthorization must include HKObjectType.workoutType() writes; missing caused write failures.",
  "details": {
    "symptom": "Writes silently failed",
    "fix": "Include workout write types in requestAuthorization and add HealthKit capability in Signing & Capabilities.",
    "links": ["doc://apple/healthkit/requestAuthorization", "forum://reddit/post/abc123"]
  }
}
```

  

1. Write after each task; read before each task

- Add steering lines:

- “At the end of every task: store a compact memory with problem, root cause, fix, and references via Basic Memory.”
- “At the start of every task: query Basic Memory for items tagged with the technologies and file paths you’ll touch; summarize applicable lessons and incorporate them before planning.”

- Pseudo-calls the agent can make:// On task start

```
// On task start
{ "tool": "basic-memory.query", "input": { "tags": ["healthkit","workout","permissions"] } }

// On task end
{ "tool": "basic-memory.write", "input": { "record": { "...": "see schema above" } } }
```

  

- If your client doesn’t expose explicit tool calls in the UI, keep the instructions in steering so the agent invokes them implicitly.

1. Make memories easy to retrieve

- Tag aggressively (framework, feature, file path, error code).
- Use a consistent “type” (e.g., ios-healthkit-workflow) so a single query pulls everything relevant.
- Keep each memory small and focused so LLMs can digest them quickly.

3. Periodically consolidate

- Add a maintenance task that reads all memories for a feature and writes a concise “playbook.md” in docs/playbooks/. This gives both the agent and you a human-readable reference.

  

The video “Serena MCP + ccusage,” “Claude MCP Tips…,” and “Combining Project‑Level MCP Servers & Nested Cursor Rules…” on this page show similar “make it remember, reduce repeats” patterns that pair well with Basic Memory.

  

**Ensure specs are incorporated across new chats**

  

Goal: New chats (or per-task fresh contexts) sometimes don’t load your PRD/specs by default. You’ll force the agent to re-load and honor them every time, matching the workflow described in the iOS app video.

1. Centralize specs

- Keep all specs/design in a predictable place:

- specs/requirements.mmd
- specs/design.md
- specs/tasks.md (machine- and human-friendly; the IDE may also manage a separate tasks file)

- Add stable filenames so you can reference them in instructions without guesswork.

3. Add an initialization routine to steering

- In your project steering rules, add a “Before doing anything in a new chat” preamble:

- “Load and summarize specs/requirements.mmd and specs/design.md.”
- “Extract constraints (APIs, platforms, performance targets), acceptance criteria, and out-of-scope items.”
- “Restate the current task’s definition in the context of these specs; if conflicts appear, halt and ask for clarification rather than proceeding.”

- Example snippet to pin in rules/agents.md:

- “Session bootstrap: read specs/requirements.mmd and specs/design.md; create a 10–15 bullet alignment summary; reference section IDs from the specs when making decisions.”

5. Program a guardrail check per task

- At task start:

- Summarize the specific requirement IDs this task implements (e.g., RQ-012, RQ-019).
- If requirements are missing or ambiguous, open a “clarify” step before coding.

- At task end:

- Verify the implementation against the acceptance criteria and tests specified in specs/tasks.md.
- If gaps exist, file follow-up subtasks rather than marking complete.

7. Auto-refresh on spec changes

- Use a simple watcher (git hook or a dev task) that:

- Detects changes in specs/*.md(m)
- Triggers a “re-summarize specs” step (agent reads updated specs and refreshes its alignment summary).

- Store the latest alignment summary at .agent/specs-alignment.json for quick re-ingestion.

9. Pin the spec context in long sessions

- If your client supports “pinned context” or “project context,” pin the alignment summary plus links to the canonical spec files so every new chat has instant access.
- If not supported, add a “Load Specs” quick message in your client’s snippets/macros and hit it at the top of each new thread.

11. Verify during code generation

- Require the agent to prepend a “Spec Alignment” section in PRs/patches:

- Which requirements are addressed
- Which tests verify them
- Which non-functional constraints were considered (permissions, HealthKit entitlements, error paths)

  

You’ll see the “MCP + Custom Instructions + Claude 3.7 = The Ultimate PRD Creator,” “Claude Code + MCP Setup,” and “Combining Project‑Level MCP Servers & Nested Cursor Rules…” videos on this page mirror this habit: load specs early, keep them in view, and tie each task to requirement IDs.

  

If you want to follow along with exactly the same tool set shown here, the relevant videos on this page to open next are:

- Cursor + MCP Servers: Complete Setup Guide (for wiring multiple MCP servers cleanly)
- Context 7 MCP: Get Documentation Instantly + VS Code Setup (for docs retrieval)
- Bright Data MCP Beats ChatGPT Agent—Here’s Why (for deep research patterns)
- Claude Code + MCP Setup: Late Night Tutorial (for Claude Code-specific wiring)
- Combining Project-Level MCP Servers & Nested Cursor Rules to 10x AI Dev Workflow (for robust steering/spec discipline)