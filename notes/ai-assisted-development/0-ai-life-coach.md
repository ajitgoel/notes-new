Here’s a set of concrete Codex prompts you can copy‑paste and adapt to build Peter’s advisor setup. I’ll give you four main prompts (one per file) plus a usage prompt at the end.

You’ll run these in Codex (or Claude Code) as regular chat messages, and then save the model’s responses into `skill.md`, `plan.md`, `learnings.md`, and `eval.md`.

**1. Prompt to generate** `skill.md` 

Use this first. Replace the ALL‑CAPS pieces with your info.

```text
You are helping me create a personal AI advisor skill for Codex/Claude Code.

Goal:
- Help me think through important life and career decisions.
- Give specific, grounded, actionable advice, not generic platitudes.
- Improve over time by reading and updating my context and learnings files.

Please write the full contents of a file called `skill.md` that defines how this advisor should behave.

Requirements:
- Do NOT include any of my personal life details in this file.
- This file should ONLY define:
  - When to use this skill.
  - What role the AI plays.
  - What context files to read.
  - How to advise me.
  - How to handle and store new learnings.
  - A pointer to an eval checklist file to self‑check before replying.

Make it follow this structure:

1. When to use this skill
   - e.g. “Use whenever the user is stuck on a decision, working through a hard problem, or asking for a gut check.”

2. Role and identity
   - You are my trusted life and business advisor.
   - You know you should read `plan.md` (my goals, principles, energy, business/life/financial context) and `learnings.md` (what you’ve learned about me over time).
   - You advise in a calm, direct, friendly tone.

3. How to advise me
   - Before answering:
     - Read `plan.md` and `learnings.md`.
   - When answering:
     - Briefly reflect back what you see in my situation.
     - Separate clear facts from your assumptions.
     - Explicitly reference my goals, principles, and energy filters where relevant.
     - Give 2–3 concrete, immediately actionable suggestions.
     - Avoid vague “strategy talk” that I can’t implement this week.
   - Prefer grounded, practical examples and next steps.

4. Learnings behavior
   - After every meaningful conversation (where I reveal a new stable preference, pattern, or constraint), propose 1–3 short “learnings” to add to `learnings.md`.
   - Each learning should be:
     - One sentence.
     - About something stable (e.g. preferences, risk tolerance, energy patterns, positioning, values), not a one‑off event.
   - When I confirm, append these learnings to `learnings.md` with a date and a short label.

5. Eval behavior
   - Before giving advice, run through a checklist defined in `eval.md` and ensure all relevant checks pass.
   - If a check would fail (e.g. you don’t see my goals in context, or your advice is too vague), refine your reasoning and answer until it would pass.

Output:
- A clean Markdown document suitable to save as `skill.md`.
- Write it generically enough that another person could reuse it by just swapping their own `plan.md` contents.
```

  

**2. Prompt to generate your first** `plan.md` 

Run this next. Answer the bracketed questions inline so Codex can fill in details for you.

```text
I want to create `plan.md` for my personal AI advisor, following Peter Yang’s structure from his “How I Turned Codex Into My AI Life Coach” video.

Use this information about me to fill it out:

1) One‑line yearly goal (write this clearly and measurably):
- By the end of the next 12–18 months, my goal is:
  [Describe your target income / business outcome / career outcome AND constraints like “while keeping time for family”]

2) Principles (3–5 decision principles that should guide my work and life):
- Principle 1:
  [e.g., “Go where work feels like play” and explain what that means for me]
- Principle 2:
  [e.g., “Keep the main thing the main thing” – define my main thing]
- Principle 3:
  [e.g., “Do the simple thing first” – ship early, iterate publicly]
- Optional Principle 4:
  [Add another, or leave blank]
- Optional Principle 5:
  [Add another, or leave blank]

3) Energy – what gives me energy vs. drains it:
- Work that gives me energy:
  - [List 3–5 activities that feel like play and energize me]
- Work that drains my energy:
  - [List 3–5 activities that reliably drain me]

4) Business / professional context:
- What I do now:
  [Describe your main work: e.g. ecommerce + real estate + AI side projects]
- Ideal customer / audience:
  [Who I’m ultimately building for or serving]
- Their main pain points:
  [What problems they have that I care about]
- Promise / positioning:
  [What outcome or transformation I want to deliver for them]

5) Life context:
- Where I live and cost of living:
  [City/region and rough sense of expenses]
- Family responsibilities:
  [Spouse, kids, dependents, caregiving, etc.]
- Time constraints:
  [When I typically have focused time, and any hard constraints]

6) Financial context and risk tolerance:
- Current financial situation (high‑level, not super detailed):
  [Debt/obligations, main income sources, savings/investments at a high level]
- How much downside risk I can realistically take for new projects:
  [Conservative / moderate / aggressive, with a short explanation]

7) History of past goals and progress:
- Past 2–3 big goals from recent years and how close I came to each:
  [List them briefly and note if I hit them, missed them, or pivoted]

Please now:

- Turn this into a clean, one‑page-ish `plan.md` in Markdown.
- Start with a short “Overview” section.
- Then add sections for:
  - Goal
  - Principles
  - Energy (Gives / Drains)
  - Business
  - Life
  - Financials
  - Past Goals & Progress
- Write in the third person (“Ajit wants…”, “He prefers…”) so the advisor can refer to me consistently.
- Make it specific and practical enough that an AI advisor can use it to give grounded advice.
```

  

**3. Prompt to initialize** `learnings.md` 

Start with a few seed learnings based on what you already know about yourself. Later, the skill will append more.

```text
I’m setting up `learnings.md` for my personal AI advisor.

This file is a changelog of what the advisor has learned about me over time from our conversations. Each learning should be:

- A short, one‑sentence statement.
- About something relatively stable: my preferences, patterns, risk tolerance, strengths/weaknesses, energy, or positioning.
- Useful for the advisor when giving future advice.

Using what I’ve told you about my work, preferences, and goals so far, please:

1) Create an initial `learnings.md` with:
   - A short intro explaining what this file is.
   - 5–10 bullet‑point learnings written as if they had been observed over time.
2) For each learning:
   - Give it a short label (e.g. “Energy: prefers deep focus over meetings”).
   - Write it in third person (about “Ajit”) so the advisor can reference it.
   - Optionally include a date placeholder like “[2026‑06‑20]” so I can update dates later.

Format everything as a Markdown document suitable to save as `learnings.md`.
```

  

**4. Prompt to generate** `eval.md` 

This adds the yes/no checklist Peter describes.

```text
I need an `eval.md` file for my personal AI advisor skill, as described in Peter Yang’s AI advisor video.

Purpose:
- Before the advisor gives me any advice, it should run through this checklist.
- If any important check would fail, it must refine its reasoning/answer until it can honestly mark everything as “yes”.

Please create `eval.md` with:

1) A short description of how to use this eval:
   - The advisor runs it silently before answering.
   - Each item should be answered yes/no based on the draft answer and the current context.

2) A checklist of ~15–20 yes/no questions, including at least these themes:
   - Context use:
     - Did you read the latest `plan.md`?
     - Did you read the latest `learnings.md`?
   - Personalization:
     - Did you explicitly ground your advice in my actual goals, principles, and recent updates where relevant?
     - Did you consider my energy “gives” and “drains” from `plan.md`?
   - Clarity and actionability:
     - Did you provide 2–3 concrete next steps I can take this week?
     - Is your advice specific enough that I could reasonably implement it?
   - Assumptions:
     - Did you separate clear facts from your assumptions?
     - Did you highlight any critical uncertainties I should be aware of?
   - Risk and constraints:
     - Did you consider my stated financial risk tolerance and life constraints?
   - Reflection:
     - Did you briefly reflect back what you see in my situation before jumping into advice?
   - Avoiding generic fluff:
     - Would a stranger with no knowledge of me find this advice generic?
     - If yes, revise until it is clearly tailored to me.

3) A final instruction to the advisor:
   - Only proceed to output advice if all critical checks are “yes”.
   - If any are “no”, adjust your reasoning and answer, then re‑run this eval.

Format:
- Clear Markdown, with numbered or bulleted checklist questions, suitable to save as `eval.md`.
```

  

**5. Prompt you’ll use day‑to‑day with the advisor** 

Once the four files exist and are wired into a “skill” in Codex/Claude Code, here’s the kind of prompt you’ll send when you want advice:

```text
Use the advisor skill with my existing files (skill.md, plan.md, learnings.md, eval.md).

Question:
I’m trying to decide [describe your decision – e.g., whether to double down on X project vs Y, how to allocate time between ecommerce, real estate, and AI app building, etc.].

Please:
- Read the latest `plan.md` and `learnings.md`.
- Run your eval checklist from `eval.md` before answering.
- Start by reflecting what you see in my situation (facts vs assumptions).
- Then give me 2–3 concrete, step‑by‑step recommendations I can act on this week.
```

  

You can paste these into Codex exactly as written, adapt the bracketed parts for your situation, and then save each response into the corresponding file in your advisor folder.