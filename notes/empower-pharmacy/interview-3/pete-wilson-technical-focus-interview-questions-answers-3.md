Here are realistic questions Pete might ask, with detailed, Safeway-focused answer outlines you can adapt and say in your own words.
**1. “Tell me about a time you set technical standards for a team or platform.”** 
You want to show: raising the bar, codifying best practices, and getting buy-in without formal authority.
You could anchor on Content as a Service / Goals Studio and the health personalization work.
Answer structure:
1. Brief context
2. What standards you set (code, architecture, delivery, observability)
3. How you drove adoption
4. Impact
Example answer:
“At Safeway I was a Senior Staff Engineer focused on the health and personalization experiences for 1.8M+ users. When we started the Content as a Service / Goals Studio platform, different teams were building React/Next.js frontends and Java Spring Boot services with slightly different patterns. That slowed delivery and made it hard for product and compliance partners to reason about behavior.
I proposed a set of **concrete engineering standards** for this platform:
- On the frontend: standard Next.js layout structure, React Query for data fetching, a shared Material UI design system, and Vitest/React Testing Library as the default test stack.
- On the backend: a standard Spring Boot microservice template with opinionated structure (controllers, service layer, domain layer), a consistent error model, idempotent request patterns, and a uniform approach to configuration (AppConfig / feature flags).
- For delivery: CDK v2–based infrastructure definitions, trunk-based development with short-lived branches, and mandatory checks for unit tests and basic performance/security scans in CI.
Because I didn’t manage these engineers directly, the key was influence. I built a reference implementation—the Content as a Service API and the Goals Studio frontend—and paired with other teams to clone the patterns. We documented decisions as lightweight RFCs and hosted brown-bag sessions where I walked through ‘why this standard exists’ in business terms: faster onboarding, easier audits, and safer changes during promotions.
Within a couple of months, new services in the health area were bootstrapped from that template, and we saw measurable benefits: fewer production issues during promotions, faster time-to-first-PR for new engineers, and simpler cross-team handoffs because everyone recognized the architecture and tooling.”
**2. “How have you influenced cross‑functional teams when there were conflicting priorities?”** 
You want to speak their language: TPM cares about stakeholder alignment, trade-offs, and risk management; keep tech details accessible.
Use the rules engine / Goals Studio project where product, compliance, and engineering all had strong opinions.
Example answer:
“At Safeway, one of the more complex cross‑functional efforts I led was replacing a vendor goals engine with an internally owned Java SpEL rules platform integrated into a Next.js–based Goals Studio UI. Product wanted very rapid experimentation, Compliance wanted tight control and auditability, and Engineering wanted to reduce operational risk and vendor lock-in.
Initially, there was tension: product wanted a ‘no guardrails’ UI for configuring homepage zones, compliance wanted every rule change reviewed, and engineering needed to keep the rules engine simple enough to operate at scale.
I helped align the group in three steps.
First, I translated constraints into a shared language. For example, instead of saying ‘SpEL expressions must be pure and side‑effect free,’ I explained to non‑technical partners: ‘If rules can call out to anything, we can’t reliably predict performance or pass audits. So we’ll intentionally limit the building blocks in the UI to safe, pre‑approved attributes.’
Second, I facilitated a workshop where we mapped desired business capabilities—like targeting users with specific chronic conditions or pharmacy behaviors—to a small, composable rules vocabulary. That allowed Compliance to pre‑approve the allowed attributes and operations, while Product still had flexibility to create new combinations without code changes.
Third, I proposed a governance flow: rules created in Goals Studio would go through a staging environment with automated tests and snapshot comparisons before promotion, and we added a lightweight approval step for high‑risk rule sets.
The outcome was that we shipped the internal rules engine, eliminated the vendor cost, and still enabled product owners to update homepage experiences without new deployments. More importantly, the relationship between Product, Compliance, and Engineering improved because we made the trade‑offs explicit and kept everyone involved in the design, not just the implementation.”
**3. “How do you guide enterprise architecture so teams build compatible systems instead of local one‑offs?”** 
Here Pete is probing for “guide enterprise architecture” without you sounding like an ivory-tower architect. Use Safeway’s microservices, Kafka, and shared patterns.
Example answer:
“At Safeway, the personalization and content platforms touched many parts of the retail and health ecosystem—customer data, marketing, pharmacy, and mobile/web channels. My approach to guiding architecture was to create opinionated, but lightweight, patterns that teams could adopt.
One example is how we handled event‑driven integration. We standardized on Kafka for key domain events—like ‘customer segment updated’ or ‘personalized goal achieved’—and I worked with other teams to define those event schemas in a domain‑driven way instead of embedding UI‑specific needs.
For instance, we defined a ‘GoalDefinition’ and ‘GoalEvaluation’ domain model that represented what the business cared about (goal id, criteria, evaluation outcome, timestamps) and ensured that model was independent of any particular UI. I then created reference producer/consumer implementations in our Java Spring Boot services, with patterns for idempotency, DLQs, and backpressure handling.
To propagate this at an enterprise level, I didn’t try to write everyone’s services. Instead, I:
- Documented these patterns in an internal playbook with concrete code snippets.
- Joined architecture reviews for adjacent teams to help them map their use cases onto the shared models, rather than inventing new event types.
- Helped product managers and TPMs understand the value: if we keep to this shared event vocabulary, we can add new consuming applications (like analytics or marketing automation) without re‑plumbing everything.

Over time, that consistency made it much easier to reason about flows end‑to‑end and to add new features—like new health goals—without every team reinventing the integration approach.”

**4. “Describe a system you architected end‑to‑end and how you’d explain it to both engineers and non‑technical stakeholders.”** 
This maps directly to “guide enterprise architecture” and “communicate architecture to technical and non‑technical audiences.” Use Content as a Service / Goals Studio or the Safeway health personalization experience.
Example answer (pick one system and stick to it):
“At Safeway I architected and delivered the Content as a Service / Goals Studio platform, which powers personalized health content on the homepage for about 1.8M users.
To engineers, I described it as a modular, event‑aware content platform:
- A Next.js/React Goals Studio frontend that lets product owners define content blocks and targeting rules.
- A Java Spring Boot rules engine using SpEL to evaluate which content applies to a given user, based on inputs like health profile, behavior, and time.
- A content‑delivery API running on Kubernetes that serves personalized payloads at scale to the mobile‑web frontend, with caching and feature‑flagged rollout.
- Supporting infrastructure defined in code (CDK/Helm), wired into CI/CD, with observability via logging, metrics, and synthetic checks.

To non‑technical stakeholders, I framed it differently: ‘We’re building a self‑service ‘playbook’ tool where you can define who should see which health message, without asking engineers for each change. Behind the scenes, a rules engine takes in data we’re already allowed to use, decides what’s relevant for each user, and sends that to the website. We’ll give you guardrails so you can’t create rules that hurt performance or violate policy, and we’ll have dashboards so you can see how many customers each rule is affecting.’

In reviews, I often used simple diagrams: boxes for ‘Content Studio,’ ‘Rules Engine,’ and ‘Website,’ with arrows showing ‘rules in,’ ‘content out,’ and where approvals or audits happen. Then I would dive into more detail only when the audience was technical.

This dual‑level explanation built trust: product and compliance understood the capabilities and safety mechanisms; engineers understood the detailed patterns—microservice boundaries, event flows, and resilience tactics.”

**5. “How do you ensure quality, reliability, and observability in the systems you build?”** 
Tie this to Empower’s emphasis on quality, compliance, and observability, but use Safeway examples: performance tuning, reliability patterns, monitoring.
Example answer:
“In the Safeway health personalization work, quality and reliability were critical because any issue on the homepage directly affected millions of users and brand perception.
I think about it in three layers.
First, ==at the code and design level, we built for correctness and resilience==: idempotent operations in the rules evaluation pipeline, graceful fallbacks when external dependencies were slow, and configuration‑driven behavior so we could adjust rules or thresholds without redeploying. For example, the content‑delivery API had clear separation between input validation, rules evaluation, and response shaping, making it easier to test each piece in isolation.
Second, we invested in ==automated verification==. For the Next.js/React frontends, we used unit and integration tests with React Testing Library and Vitest to verify both UI behavior and HTTP interactions. On the Java Spring Boot side, we had tests around the SpEL rules evaluation and performance‑sensitive code paths, plus contract tests ensuring that schema changes didn’t break consumers.
Third, I put a lot of emphasis on ==observability==. We defined SLIs/SLOs for response time, error rate, and rule evaluation latency. We instrumented critical paths with structured logging and metrics, and we built dashboards that product and operations could understand at a glance—e.g., ‘percentage of homepage requests with personalized content vs. fallback content.’ When we tuned database queries or caching strategies, we used these dashboards to confirm improvement rather than guessing.
This combination—sound design, automated tests, and actionable observability—let us ship new rules, new content types, and performance optimizations with confidence and catch regressions early.”
**6. “Can you give an example of mentoring or enabling other engineers to be more effective?”** 
He’s looking for “set standards” plus leadership/enablement, not people management.
Example answer:
“At Safeway I played a technical leadership role across multiple teams contributing to the health and personalization roadmap, though I didn’t manage people directly.
One recurring issue was that engineers were reinventing patterns for things like React data fetching, Kafka consumers, or Spring Boot configuration. That led to inconsistent quality and slower onboarding. Instead of simply reviewing PRs and pointing out issues, I took a more proactive approach.
On the frontend, I built a small ‘starter kit’ for Next.js/React that included our preferred folder structure, a base layout, shared components, and examples of API integration and tests. Then I ran a workshop walking through the starter kit, explaining why we made each choice—performance considerations, maintainability, and accessibility—and invited feedback. The result was that new features across the health experience looked and behaved consistently, and newer engineers had a concrete reference to follow.
On the backend, I paired with engineers to factor messy controllers into cleaner service/domain layers, introduced patterns for handling retries and idempotency, and shared those examples in code reviews and documentation. In some cases I helped folks break a monolithic service into smaller, more cohesive services by first identifying bounded contexts around content, rules, and evaluation.
Over time, I saw the quality of PRs improve and the need for heavy‑handed review decline. Engineers started using the patterns themselves when mentoring interns and new hires, which is my favorite sign that standards have really taken root.”
**7. “How do you balance innovation (e.g., AI, new frameworks) with stability and regulatory requirements?”** 
This ties directly to the job description’s AI/ML mention and Empower’s regulated environment, but you can ground your answer in Safeway’s cautious use of new tooling plus your personal AI-assisted workflows.
Example answer:
“At Safeway we operated in a space that, while not strictly regulated like a pharmacy, still had strong constraints around privacy, PII, and customer trust. At the same time, we wanted to innovate in personalization and move quickly.
My approach has been to differentiate between where experimentation is safe and where it needs more control.
For core user‑facing systems—the health homepage, personalization rules, content delivery—we were conservative: we chose proven stacks (Next.js, React, Java Spring Boot, Kafka) and made incremental changes backed by tests and feature flags. For anything touching customer data, we followed corporate guidelines for PII handling and access control and worked closely with security and compliance teams during design.
Where I pushed more innovation was on _how_ we build, not just _what_ we build. For example, I used AI‑assisted tools like Claude Code and Copilot heavily in my own workflow for tasks like generating test scaffolding, drafting RFCs, or exploring refactoring options. But the outputs still went through the same code review, testing, and security checks as any human‑written code. That gave us development speed without compromising on quality or compliance.
When introducing any new technique or tool, I frame it to stakeholders as a risk‑managed experiment: what guardrails we’ll put in place, how we’ll measure impact, and how we’ll roll back if needed. That mindset is especially important in a healthcare or pharmacy context, where innovation has to be balanced with regulatory and safety responsibilities.”
**8. “If you joined Empower, how would you start setting or raising engineering standards in your first 90 days?”** 
This lets you tie everything back to their environment and values (People, Quality, Service, Innovation), using your Safeway experience as proof.
Example answer:
“In my first 90 days I’d focus on listening and mapping before prescribing, but I’d leverage a lot of what worked at Safeway.
Step one would be to ==understand the current systems that are most critical to Empower’s mission==—likely areas like prescription workflows, manufacturing/outsourcing, and HR or internal tooling—and learn how teams build and ship today: CI/CD pipelines, coding practices, observability, and how they work with Compliance and QA.
Based on that, I’d look for one or two high‑leverage areas where consistent standards would immediately improve outcomes. At Safeway that turned out to be a shared pattern for Next.js frontends and Spring Boot services plus clear guidelines for event‑driven integration. At Empower it might be a standard microservice template, a common approach to React/Next.js applications, or a playbook for building compliant APIs that handle PHI.
I’d then build a concrete reference implementation—just as I did with the Content as a Service / Goals Studio platform at Safeway—and document the ‘why’ behind each decision in language that resonates with both engineers and TPMs: faster onboarding, fewer production incidents, easier audits, better developer experience.
From there, I’d partner with product and compliance stakeholders to make sure these standards align with regulatory needs and business priorities, and I’d support teams in adopting them through pairing, code reviews, and brown‑bag sessions rather than top‑down mandates.
The goal in 90 days wouldn’t be to ‘fix everything,’ but to establish a strong, collaborative foundation for how we set and evolve standards, grounded in Empower’s values of People, Quality, Service, and Innovation.”
You can treat these as “answer shapes” and adjust words to feel natural, but if you keep coming back to your Safeway work—Content as a Service / Goals Studio, the rules engine, Next.js + Spring Boot, Kafka, standards/templates, and cross‑functional collaboration—you’ll be tightly aligned with what Pete is likely to care about.