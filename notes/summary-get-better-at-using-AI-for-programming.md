Debate on using AI agents for coding: plan-first, small verifiable tasks, fresh context, and realistic expectations.

The Hacker News thread centers on practical ways to improve AI-assisted programming and where current tools fall short. A Claude Code team member suggests four tactics: <span style="background:rgba(240, 107, 5, 0.2)">keep project rules in CLAUDE.md, use Plan mode to agree on steps first, add a verification loop (e.g., Puppeteer/Playwright), and use Opus 4.5 for tougher work</span>. Several commenters confirm Plan mode and stronger models help, but note adherence issues, context rot, and rising costs.

A recurring theme is breaking work into small, verifiable chunks. Many advocate <span style="background:rgba(240, 107, 5, 0.2)">writing concise plan.md specs, adding good/bad examples, and iterating in fresh sessions</span>. Tests—especially BDD—are emphasized as guardrails. People recommend treating agents like junior devs: specify conventions, restrict tools, and review changes closely. Voice-to-text prompting comes up as a way to capture fuller intent with lower friction.

Skepticism is common. Users report CLAUDE.md being ignored, agents drifting, saving outdated plans, and producing low-quality or risky code. Some argue agents are best for boilerplate, refactors, and “software carpentry,” not novel architecture. Others claim production success with careful planning and granular execution, while critics question the lack of public, high-quality shipped results.

Svelte-specific notes: newer models struggle with Svelte 5 runes; constraining to Svelte <5 may help. One workflow: translate legacy templates to Next.js/React first, then to SvelteKit. Another suggests a distillation step (e.g., YAML) before final translation.

Pricing and models: Opus 4.5 often feels better and may net-token cheaper via faster correct solutions, but many still weigh latency and cost.

Bottom line: effective AI coding today hinges on <span style="background:rgba(240, 107, 5, 0.2)"><span style="background:rgba(240, 107, 5, 0.2)">tight specs, small steps, robust tests, context hygiene, and realist</span>ic scope</span>. Use agents to accelerate repetitive, verifiable work; keep human judgment on architecture and final quality.