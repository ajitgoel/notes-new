Let me review the attachments to understand Kyle’s background and the job details.
Now I have the full picture. Here’s what you can expect:
What to Expect in Your Intro Call with Kyle
Who Kyle is: Kyle Ybarra is a Talent Acquisition recruiter at Empower Pharmacy. He previously recruited for Amazon (Luna, Games). He leads technical recruiting for early career through Director-level roles under Empower’s CTO. This will be a recruiter screen, not a technical interview.
Likely Call Structure (20-30 minutes)
1. Company pitch. Kyle will walk you through Empower Pharmacy — the largest 503A compounding pharmacy and FDA-registered 503B outsourcing facility in the U.S. He’ll emphasize their four values (People, Quality, Service, Innovation) and the mission around affordable medication access.

2. Role overview. He’ll describe the Senior Software Engineer – Full Stack position: C#/.NET Core, Python, TypeScript, Angular frontend, REST APIs, Terraform IaC, Azure DevOps CI/CD, and HIPAA compliance. He may mention the “nice to haves” like React Native and HL7/HIPAA familiarity.

3. Your background walkthrough. Expect questions like:
- Walk me through your recent experience
- Why are you interested in Empower / this role?
- What’s your experience with C#/.NET Core? (strong match for you — JPMorgan, Thomson Reuters, RealPage)
- Have you worked in healthcare or with HIPAA? (your Amazon Pharmacy and SourceRad healthcare experience are directly relevant here)
1. Logistics questions:
- Compensation expectations
- Location/remote preferences (many of their roles are remote)
- Availability to start / notice period
- Are you interviewing elsewhere?

Where Your Resume Aligns Well
- C#/.NET Core — deep experience across JPMorgan, Thomson Reuters, RealPage
- Healthcare — Amazon Pharmacy + early career medical imaging platform (SourceRad)
- Terraform & IaC — listed on your resume, matches their requirement
- CI/CD pipelines — Azure DevOps and AWS CodePipeline experience
- REST APIs & microservices — core to your entire career

Potential Gaps to Prepare For
- Angular — the JD calls for Angular, but your frontend experience is React/Next.js. Be ready to frame this as transferable and express willingness to pick it up.
- Python — listed in the JD but not prominent on your resume. If you have any Python experience, mention it.
- Title/level — the JD says “Senior Software Engineer,” while you’re targeting Staff/Principal. Ask Kyle directly about growth trajectory and whether the level is flexible.

Questions You Should Ask Kyle
- ==What does the engineering team structure look like under the CTO (Goldi Goel)?==
- ==Is this role Staff/Principal level or strictly Senior? Is there flexibility?==
- ==What’s the tech modernization roadmap — are they migrating stacks, building greenfield?==
- ==Remote vs. hybrid expectations?==
- ==Interview process and timeline?==
Good luck — your healthcare background at Amazon Pharmacy and deep .NET experience make you a strong fit for this role.

------------
Empower Pharmacy is the **largest 503A compounding pharmacy** and **FDA-registered 503B outsourcing facility** in the United States, focused on making quality, affordable medications accessible to millions of patients nationwide.

**What they do:**
- ==**Compounding medications** — they create customized medications tailored to individual patient needs, serving the functional medicine market.== This includes formulations that aren’t available as standard commercial drugs (different dosages, forms, or ingredient combinations).
- **503A vs 503B** — their 503A side ==fills individual patient prescriptions;== their 503B ==outsourcing facility produces larger batches of compounded medications without individual prescriptions, supplying hospitals and clinics.==
- ==**Vertical supply chain integration**== — they control manufacturing, distribution, and quality control in-house, which is unusual for a compounding pharmacy and lets them keep costs down.

**Scale and growth:**
- Ranked **#116 in Healthcare & Medical on the Inc. 5000 list** (2025)
- One of Houston’s fastest-growing private companies
- Headquartered in Houston, TX, with a growing remote engineering team

**Their tech angle** (relevant to the role you’re interviewing for):
- ==They’re investing heavily in technology to modernize pharmacy operations — building out cloud infrastructure, APIs, CI/CD pipelines, and patient-facing applications==
- ==The CTO is **Goldi Goel**, and Kyle mentioned they’re actively expanding the engineering org with many remote positions==
- Healthcare compliance (HIPAA, HL7) is a core part of the engineering work
In short, they sit at the intersection of healthcare and tech — a traditional compounding pharmacy that’s scaling rapidly and building a modern engineering organization to support that growth.

-----------

==**HIPAA (Health Insurance Portability and Accountability Act)**== 
**What it is:** ==A 1996 U.S. federal law that sets national standards for protecting sensitive patient health information==.
**The key rules engineers need to know:**
- **Privacy Rule** — Defines what counts as Protected Health Information (PHI): names, dates of birth, Social Security numbers, medical records, billing info — any data that can identify a patient linked to their health condition or treatment.
- **Security Rule** — Requires technical safeguards for electronic PHI (ePHI):
- **Access controls** — role-based access, unique user IDs, automatic session timeouts
- **Encryption** — data encrypted at rest and in transit (AES-256, TLS 1.2+)
- **Audit logging** — every access to PHI must be logged and traceable
- **Integrity controls** — mechanisms to prevent unauthorized alteration of ePHI
- **Breach Notification Rule** — Organizations must notify affected individuals, HHS, and sometimes media within 60 days of discovering a breach.

**What this means for you as an engineer at Empower:**
- You’ll need to ensure APIs don’t leak PHI in logs, error messages, or URLs
- Database access must be audited and role-restricted
- Cloud infrastructure (Azure/AWS) must use HIPAA-eligible services with a signed Business Associate Agreement (BAA)
- CI/CD pipelines need to avoid exposing PHI in build artifacts or test data
- No real patient data in non-production environments — use synthetic/anonymized data

==**HL7 (Health Level Seven)**== 
**What it is:** ==A set of international standards for exchanging healthcare data between systems.== The name “Level Seven” refers to the application layer (Layer 7) of the OSI networking model.
**The two major versions:**
**HL7 v2 (most widely used)** 
- **Pipe-delimited message format** — looks like this:
- ```
    MSH|^~\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|202604231400||ADT^A01|12345|P|2.5
    PID|||12345^^^MRN||Doe^John||19800101|M
    ```

- Used for **ADT** (Admit/Discharge/Transfer), **ORM** (Orders), **ORU** (Results), **RDE** (Pharmacy dispensing)
- ==Transmitted over TCP/IP (MLLP protocol) or increasingly over REST==
- You’ll likely encounter this at Empower for prescription orders and dispensing records

==**HL7 FHIR (Fast Healthcare Interoperability Resources)**== 
- ==The **modern, REST-based** standard — uses JSON/XML over HTTP==
- ==Resources are modular (Patient, Medication, MedicationRequest, Observation, etc.)==
- Example — a patient resource:
-```json
    {
      "resourceType": "Patient",
      "id": "example",
      "name": [{"family": "Doe", "given": ["John"]}],
      "birthDate": "1980-01-01"
    }
    ```
- Increasingly mandated by CMS and ONC for interoperability
- Much easier to work with as a web developer — standard REST APIs, OAuth 2.0 auth

**Common HL7 message types relevant to pharmacy:**

|Message Type|Purpose|
|---|---|
|**RDE** (Pharmacy/Treatment Dispense)|Prescription dispensing events|
|**RDS** (Pharmacy/Treatment Dispense)|Dispense status notifications|
|**ORM** (Order Message)|Medication orders|
|**ORU** (Observation Result)|Lab/test results|
|**ADT** (Admit/Discharge/Transfer)|Patient demographics updates|

**How These Apply to the Empower Role** 
As a compounding pharmacy, Empower likely integrates with hospitals, clinics, and EHR systems. Your work would involve:
- ==Building **HIPAA-compliant APIs** with proper encryption, audit trails, and access controls==
- ==Potentially parsing or generating **HL7 v2 messages** for pharmacy dispensing workflows==
- ==Possibly working with **FHIR APIs** for modern integrations==
- ==Ensuring the **Azure DevOps CI/CD pipelines** and cloud infrastructure maintain HIPAA compliance==

Your Amazon Pharmacy experience is a strong talking point here — you’ve already worked in a HIPAA-regulated environment building patient-facing health applications. During the call with Kyle, you can reference that experience directly without needing deep HL7 protocol knowledge at the recruiter screen stage.

------------

**1. “Walk me through your recent experience”** 

- I’m currently a Senior Staff Engineer at Safeway, part of Albertsons Companies, where I’ve been since May 2024. I own the full-stack health personalization platform — I built the Content as a Service application from scratch using Next.js, React, and Azure AD, which gives product owners a self-service interface to manage personalized homepage content without needing engineering deployments.
- One of the bigger wins was replacing a heavyweight commercial rules engine with a Java SpEL-based solution I built with a React query-builder frontend. That eliminated licensing costs and gave us full engineering control over personalization logic serving 1.8 million Safeway users.
- Before that, I spent one year at Amazon Pharmacy as a Senior Software Engineer. I owned a content-delivery API on AWS ECS that sustained 5,700 requests per second at peak. I built dynamic throttling through AWS AppConfig so we could adjust rate controls during incidents without redeploying. I also automated the entire delivery pipeline with CDK v2 and CodePipeline, and set up production observability with CloudWatch dashboards and Synthetic Canaries.
- Prior to Amazon, I spent four years at JPMorgan Chase leading an 8-application Fee Billing suite that processed 44 million records per month and generated $1.6 billion in annual revenue. I built greenfield ASP.NET Core microservices on Docker and Linux, and transformed the team from semiannual manual releases to continuous delivery through Jenkins.

**Tip:** Keep it to 2-3 minutes. Focus on Safeway and Amazon Pharmacy since they’re most recent and relevant. Mention JPMorgan to establish your .NET depth. Don’t go further back unless asked.

**2. “Why are you interested in Empower / this role?”** 

- Three things stand out to me. First, I’ve spent meaningful time in healthcare — at Amazon Pharmacy building patient-facing applications and earlier in my career at SourceRad working on a medical imaging and billing platform. I’ve seen firsthand how technology directly impacts patient outcomes and medication access, and that mission resonates with me. Empower’s focus on making affordable compounded medications accessible to millions is the kind of work I want to be doing.
- Second, the tech stack is a strong fit. I have deep experience with C# and .NET Core from JPMorgan, Thomson Reuters, and RealPage — about 14 years total. Combined with my recent cloud infrastructure work with Terraform, Azure DevOps, and CI/CD pipelines, I can contribute immediately to the kind of systems you’re building.
- Third, I’m drawn to the growth stage Empower is in. You’re scaling the engineering org, investing in technology, and modernizing pharmacy operations. I’ve built greenfield applications multiple times — at Safeway, at Amazon — and I thrive in environments where I can shape architecture decisions rather than just maintain existing systems.

**Tip:** Connect your motivation to something specific about Empower, not just “it seems like a great company.” The healthcare mission + tech stack fit + growth stage is a concrete, believable answer.

**3. “What’s your experience with C#/.NET Core?”** 

- C# and .NET have been a core part of my career for about 14 years. Most recently at JPMorgan Chase, I led the architecture of an 8-application Fee Billing suite built entirely on ASP.NET Core microservices running on Docker and Linux. These services processed 44 million records per month with streaming ingestion and Entity Framework Core — I implemented patterns like read-through caching with IMemoryCache and idempotent batch processing to meet financial-grade data integrity requirements.
- At Thomson Reuters, I worked on the Aumentum Tax Assessment platform in ASP.NET and .NET 4.5 — I was promoted to the most architecturally complex module based on my performance. I did deep performance tuning using dotTrace profiling and T-SQL query redesign.
- At RealPage, I spent nine years building a large-scale multi-tenant SaaS product in ASP.NET and C#, where I introduced Protobuf.NET binary serialization to improve API throughput.
So I’ve worked with the full evolution of the .NET ecosystem — from ASP.NET Web Forms through .NET Core microservices on containers. I’m very comfortable in that world.
**Tip:** The JD asks for C# and .NET Core specifically. Lead with JPMorgan since that’s your most modern .NET Core work. The breadth across 14 years is a differentiator — use it.

**4. “Have you worked in healthcare or with HIPAA?”** 

- Yes, in two different contexts. At Amazon Pharmacy, I spent two years building patient-facing health applications. Everything we built operated under strict HIPAA compliance — our APIs, data storage, logging, and infrastructure all had to meet PHI protection requirements. I worked within AWS HIPAA-eligible services, ensured audit logging was in place through CloudTrail, and was careful about things like making sure patient data never leaked into logs or error responses. We used synthetic data in non-production environments and maintained strict access controls.
- Earlier in my career at SourceRad, I worked on a $10 million eRIS/PACS/Billing platform serving 144 multi-site medical imaging centers. That was medical records and billing data — inherently HIPAA-sensitive. I optimized the system’s performance significantly, reducing medical transcription response time by 50% and eliminating database deadlocks in the claims processing pipeline.
- At Safeway, my current work on health personalization also touches health-related data for 1.8 million users, so I’m mindful of data handling practices even outside of strictly regulated environments.
- I don’t have direct experience with HL7 message protocols, but I understand the standard and I’m a quick study on integration patterns — I’ve built plenty of API integrations across complex systems.
**Tip:** Be honest about HL7 — Kyle likely won’t probe deeply on protocol-level details in a recruiter screen, but acknowledging the gap while showing you understand the landscape is better than overstating.

**General Tips for the Call with Kyle** 

- **Keep answers to 1-2 minutes each.** Kyle is a recruiter, not a hiring manager — he’s checking for alignment, not doing a deep technical dive.
- **Ask about level.** The JD says “Senior” but you’re targeting Staff/Principal. Raise this early: _“I noticed the title says Senior — is there flexibility on leveling for someone with 20+ years of experience?”_
- **Prepare for the Angular question.** If it comes up, say something like: _“My frontend experience is in React and Next.js. Angular and React share the same component-driven architecture, and I’ve picked up new frameworks quickly throughout my career — I moved from .NET Web Forms to React to Next.js without missing a beat.”_
- **Have a salary range ready.** Kyle will almost certainly ask. Research Empower’s compensation range beforehand so you’re not caught off guard.