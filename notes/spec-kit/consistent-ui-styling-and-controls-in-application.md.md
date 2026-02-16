
<span style="background:#d3f8b6">Encode a shared design system in the Spec Kit constitution and checklists, reference it in every spec/plan, and run the /speckit.analyze + /speckit.checklist gates with Kilocode before implementation.</span>

**Goal: consistent UI styling and controls across the app**

With GitHub Spec Kit and the Kilocode AI agent, the most reliable way to keep UI uniform is to make the design system a first-class specification artifact, then enforce it through Spec Kit’s built‑in phases and quality gates.

**What to define (once) and reuse everywhere**

- **Design tokens:** color palette, spacing scale, typography scale, elevation, radii, motion durations.
- **Foundational components:** Button, Input, Select, TextArea, Toggle, Checkbox/Radio, Link, Tooltip, Modal/Drawer, Toast, Card, List, Table, Tabs.
- **Composite patterns:** Page header, Sidebar/Navigation, Home page tiles/cards, Form layout, Empty states, Error pages.
- **Usage rules:** states (hover, focus, disabled), accessibility requirements, responsive breakpoints, do/don’t examples, performance constraints.

**Where it lives in Spec Kit**

**Constitution:** .specify/memory/constitution.md — project‑wide UI governance the agent must follow.
**Specs:** specs/<feature>/spec.md — each feature must reference the design system and reuse the same controls.
**Plan:** specs/<feature>/plan.md — technical choices that implement the design system (framework, styling method, component locations).
**Tasks:** specs/<feature>/tasks.md — concrete file paths and steps to build/reuse components, plus validation checkpoints.
**Exact commands and prompts to enforce consistency with Kilocode**

Run these from your initialized Spec Kit project. Kilocode is a supported agent, so use the same slash commands.

**1) Establish governance**
```
/speckit.constitution Create a project-wide design system. Define design tokens (colors, spacing, type, radii, motion), accessibility rules (WCAG 2.1 AA focus/contrast), and a standard control set (Button, Input, Select, TextArea, Toggle, Checkbox, Radio, Link, Tooltip, Modal, Toast, Card, List, Table, Tabs). Specify: 
- Every UI must reuse these tokens and controls. 
- No ad-hoc styles or one-off components. 
- Home page controls are the canonical patterns to be reused across all features. 
Include decision governance on how new components are proposed, reviewed, and added.
```
**2) Specify features that must reuse the system**
```
/speckit.specify For this feature, reuse the standard Button, Input, Select, Modal, and Card patterns from the design system. The UI must use project tokens (color/spacing/type) and match home page interaction patterns. Acceptance criteria: no custom inline styles; all states covered; keyboard navigation; responsive at defined breakpoints.
```
**3) Plan technical implementation (tie the rules to code)**
```
/speckit.plan Implement the design system with a tokens file and a shared component library. Define file locations:
- src/ui/tokens.css (CSS variables for color, spacing, type, radii, motion)
- src/ui/components/<ComponentName>/index.(tsx|vue|js) and styles.(css|scss)
- src/ui/patterns/(page-header|sidebar|card-grid)/...
Add lint rules and CI checks to forbid inline styles and non-standard components. Document imports and usage examples.
```
**4) Generate the task breakdown that builds and reuses shared UI**
```
/speckit.tasks
```
Ensure tasks include:
- Create tokens.css and base theme
- Implement each core control with states and a11y
- Replace home page controls with shared components
- Migrate feature UIs to use shared components
- Add validation checkpoints (visual + a11y + import rules)

**5) Add a quality gate for every request**
Create a dedicated checklist that Kilocode must pass before code is accepted:/speckit.checklist Generate a “UI Consistency Gate” checklist that validates:

```
/speckit.checklist Generate a “UI Consistency Gate” checklist that validates:
- Uses src/ui/tokens.css variables only
- Uses src/ui/components/* (no local one-off components)
- Matches home page interaction patterns
- All states (hover/focus/disabled/error/loading) implemented
- A11y: focus order, roles, labels, contrast
- No inline styles or ad-hoc CSS classes
- Responsive behavior at defined breakpoints
- Performance constraints (no layout thrash; GPU-appropriate transitions)
```
**6) Run cross-artifact analysis before implementation**
```
/speckit.analyze Verify spec, plan, and tasks consistently reference the design system, home page patterns, and tokens. Flag any feature requirements that introduce non-standard controls or styles; propose refactors to shared components instead.
```

**7) Implement only after gates pass**
```
/speckit.implement
```

The agent will execute tasks in order; if the analyze/checklist steps find inconsistencies, fix those first, then proceed.
**How to ensure every Kilocode request “gets taken care of”**
- **Always start feature prompts with a short preamble:** “Follow the constitution. Reuse design system tokens and shared components. No ad-hoc styles.”
- **Reference the home page patterns explicitly:** make them the canonical examples the agent must match.
- **Require the checklist in the request:** “Before producing code, run the ‘UI Consistency Gate’ checklist and remediate findings.”
- **Pin file paths:** ask for imports from src/ui/components/* and tokens from src/ui/tokens.css in every request.
- **Block on divergence:** instruct Kilocode to refuse implementing non-standard controls without a constitution change.

**Minimal example prompt you can reuse**
```
Follow the project constitution and design system. Use src/ui/tokens.css for all styling and only src/ui/components/* for controls. Match home page interaction patterns. Before coding, run /speckit.analyze and the “UI Consistency Gate” checklist; fix findings. Then implement the feature with the shared components and no inline styles or one-off CSS.
```

Kilocode is a supported agent in Spec Kit, so these slash commands and governance artifacts will apply uniformly across features and keep your UI consistent. [AGENTS.md](https://github.com/github/spec-kit/blob/main/AGENTS.md)