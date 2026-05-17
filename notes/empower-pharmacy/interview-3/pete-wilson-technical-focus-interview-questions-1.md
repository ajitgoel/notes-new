You should expect a **purely leadership- and architecture-focused conversation**, not a coding or deep API-design round.
**What this interview is likely to cover** 
Pete is a Principal TPM, so his lens will be: “Can this person operate at principal scope in my org?” That usually means four broad themes, all grounded in stories from your past:
1. Setting standards 
He’ll probe whether you just “do good work” or actually raise the bar around you.
Expect questions like:
- “Tell me about a time you established or raised engineering standards on a team.”
- “How have you driven consistency across multiple squads (code quality, testing, observability, CI/CD, security, etc.)?”
- “What’s an example of you pushing back on a quick-and-dirty solution to uphold long-term quality?”
Given your background, you’ll want examples like:
- Moving JP Morgan from semi-annual releases to continuous Jenkins CD as a new standard.
- Implementing observability and deployment standards at Amazon Pharmacy (CloudWatch dashboards, alarms, AppConfig playbooks).
- Defining patterns for AWS CDK v2, microservices reliability (idempotency, DLQs, backpressure, etc.) that others adopted.
He’ll care less about exact tools and more about: did you diagnose a systemic gap, define a better standard, get buy-in, and make it stick?
1. **Influencing cross-functional teams (without authority)** 
This is the “Principal vs Senior/Staff” line: can you get multiple teams and stakeholders aligned when you’re not their manager?
Expect:
- “Describe a time you had to align multiple teams or domains on a technical direction.”
- “Give an example where product, compliance, or a partner team disagreed with your approach—what did you do?”
- “How have you influenced roadmaps outside your direct team?”
You can lean on:
- Safeway: coordinating product owners, backend teams, maybe legal/compliance for personalisation, PII handling, recommendation logic.
- Amazon Pharmacy: aligning with security, compliance, ops on content-delivery platform and throttling behavior during incidents.
- JP Morgan: getting multiple billing and data teams to adopt new microservice and streaming standards in a high-risk financial domain.
He’ll listen for:
- How you map stakeholders, understand their incentives, and adjust your message.
- Whether you use data, customer impact, and risk framing rather than just “this is better engineering.”
- Evidence that people followed your lead across org boundaries.
1. **Guiding enterprise architecture (and communicating it)** 
They explicitly called out: “develop and communicate architectural designs to partner teams, both technical and non-technical.” That’s Pete’s home turf as a TPM.
Expect:
- “Walk me through an architecture you led that had impact across multiple systems or domains.”
- “How do you approach decomposing a monolith or evolving a system toward microservices?”
- “How do you explain complex architecture to non-technical stakeholders? To execs? To individual engineers?”
This is where you bring 2–3 clear systems:
- The high-RPS content-delivery API on AWS ECS at Amazon: ingress, throttling, AppConfig, S3, caching, observability.
- The fee-billing microservices suite at JP Morgan: streaming ingestion, idempotent batch processing, DLQs, caching, SQL scale.
- Safeway’s personalization / rules engine platform and Content as a Service / Goals Studio architecture.
He’ll be watching:
- Can you talk at “enterprise” level (domains, boundaries, contracts, risks, regulatory constraints) rather than just services and tables.
- Can you articulate tradeoffs (e.g., microservices vs modular monolith, Kafka vs service bus, eventual vs strong consistency).
- Can you switch altitude: deep enough to be credible, but able to simplify for non-technical leaders.
1. Leadership behaviors and mindset 
They namechecked “larger scope and leadership competencies,” so this will be quite behavioral.
Expect prompts around:
- Owning ambiguous, messy problems (“Tell me about a time no one owned an issue and you stepped in.”)
- Handling conflict between engineering quality vs delivery commitments.
- Dealing with failure: production incidents, design bets that didn’t work, how you responded and changed the system.
- Mentoring and multiplying others: “How have you raised the capabilities of engineers around you?”
Your resume gives you good material:
- Transformations (release process at JP Morgan; IaC migration at Amazon).
- Performance/resiliency turns (Safeway and earlier healthcare billing systems).
- Coaching teams in new tooling or patterns (CDK v2, streaming, caching patterns, AI-assisted workflows).
He’ll look for:
- Ownership and accountability language (“I owned… I drove… I brought X/Y together…”).
- Clarity on business outcomes (dollars, risk reduction, latency/throughput wins, regulatory compliance).
- Reflection: what you learned, how you’d do it differently now.
**How “non-technical” it really will be** 
Even though they say “they will not have a technical focus,” Pete is an ex-Amazon TPM with cloud background at a pharma company. The discussion will still be technical at the architecture / system level; they just won’t quiz you on syntax, exact AWS calls, or coding.
So you should:
- Speak in diagrams verbally: “At a high level, we had three main components…”
- Emphasize tradeoffs and decision-making, not low-level implementation.
- Constantly tie back to **patients, compliance, safety, cost, reliability, and team efficiency**—that’s very aligned with Empower’s mission and regulatory environment.
**Quick prep plan (based on your resume and this JD)** 
1. Pick 3–4 “principal-scale” stories:
- 1 architecture story (enterprise-wide or multi-team).
- 1 standards / transformation story (release practices, observability, reliability).
- 1–2 cross-team influence stories (conflict, alignment, regulatory/PII/financial risk).
1. For each, rehearse them in a structured way:
- Context (org, scale, constraints).
- Problem (why it mattered for business/patients/customers).
- Options & tradeoffs you considered.
- The decision and how you got buy-in.
- Execution and measurable outcomes.
- What you learned.
1. Prepare a concise narrative of how you:
- Design distributed, event-driven systems (Kafka, microservices, DDD).
- Think about security, compliance, and data integrity (very important in a pharmacy).
- Work with product, TPMs, legal/compliance, HR/operations to land changes safely.
1. Have 1–2 thoughtful questions for Pete that show you’re thinking at principal scope, e.g. about their current architecture evolution, standards, or how principals and TPMs partner at Empower.
That’s the shape of what you should expect: a leadership, influence, and architecture conversation where your job is to prove you already operate at the Principal level they’re hiring for.