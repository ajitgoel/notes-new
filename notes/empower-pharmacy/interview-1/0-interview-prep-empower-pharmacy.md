## Staff Full Stack Engineer | Interviewer: Dave Watson

---

## Your Interviewer

**Dave Watson** — Principal Software Engineer at Empower Pharmacy (since Jun 2025).
27+ years in software, last 15 deep in .NET.

**Recent work:**
- .NET Core 8, AWS Lambda, Kafka, Redshift (Orion)
- Microservices using C#, .NET Core 6, RabbitMQ, MongoDB (rithmXO)
- ASP.NET/MVC 4, jQuery, Entity Framework (Knowlysis)
- Champions **unit testing** with FakeItEasy
- Lists **"Algorithms and Data Structures"** as a skill

> [!info] Signal
> Dave's career is deeply practical — expect production-style coding, not LeetCode puzzles. He'll want to see you think through architecture while writing working code.

---

## Empower's Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | C#, .NET Core, ASP.NET, Python |
| **Frontend** | Angular, TypeScript, React.js, Next.js |
| **Cloud** | Azure (primary), AWS |
| **Data** | Azure SQL, MySQL, Entity Framework |
| **DevOps** | Azure DevOps CI/CD, Terraform, Docker, Kubernetes |
| **Tools** | Jira, Bitbucket, Slack, New Relic |

> [!warning] Healthcare Context
> Empower is a HIPAA-regulated 503B outsourcing pharmacy filling 15,000+ prescriptions daily. Compliance and data security matter in every design decision.

---

## What Dave Will Likely Test

### Most Likely

1. **Build a REST API endpoint from scratch**
   "Create a controller that handles CRUD for a pharmacy order/prescription resource."
   He'll watch for: proper DI, async/await, model validation, separation of concerns.

2. **Refactor or debug existing code**
   "Here's a service class — what's wrong with it?"
   Expect: tightly coupled code, missing async, poor error handling, or DI violations.

### Likely

3. **Data processing / LINQ problem**
   "Given a collection of orders, filter/group/transform them."
   Empower processes 15,000+ prescriptions daily — data manipulation is core.

4. **Unit testing a service**
   Dave champions testing. He may ask you to write tests for a service, or give you untestable code and ask how you'd make it testable.

---
## Quick-Fire Q&A to Prep For

### "What's the difference between Scoped, Transient, and Singleton in DI?"
- ==**Transient:** new instance every time it's requested==
- ==**Scoped:** one instance per HTTP request (most common for services/repos)==
- ==**Singleton:** one instance for the app's lifetime (caches, config)==
- **Trap:** injecting a Scoped service into a Singleton causes a captured dependency bug
### "How does the ASP.NET Core middleware pipeline work?"
==Request flows through middleware in order they're registered. Each can short-circuit or pass to the next via `next()`. Order matters — auth before MVC, exception handling first.== Think of it as nested Russian dolls, not a flat chain.
### =="When would you use IEnumerable vs IQueryable?"==
- ==**IQueryable:** querying a database — expression tree translates to SQL, filtering happens server-side==
- ==**IEnumerable:** in-memory collections where filtering runs in your app==
- ==Returning IEnumerable from a repo when you meant IQueryable pulls the entire table into memory==
### "Explain the Repository pattern — is it still worth using?"
It abstracts data access behind an interface, making services testable without a real database. Strong answer: "It depends on the project's testing strategy and whether you need to swap data sources — at Empower's scale with multiple data stores, it likely adds value."

---

## Live Coding Tactics

- **Talk through your thinking.** Dave is a Principal — he's evaluating reasoning as much as syntax
- **Start with the interface.** Define the contract first — signals architectural thinking
- **Use DI from the start.** Never `new` up a dependency. Inject via constructor
- **Name things well.** `Async` suffix, `I` prefix on interfaces, meaningful variable names
- **When stuck on syntax:** say so. "I know this takes a Func — let me think through the signature." Staff engineers reason through problems clearly
- **Healthcare context:** mention HIPAA awareness — validation, audit logging, data access controls
---
## Refresher Checklist

- [ ] Dependency Injection (constructor, scoped/transient/singleton) 🔴
- [ ] Async/await patterns and Task vs ValueTask 🔴
- [ ] LINQ — Where, Select, GroupBy, Aggregate, Join 🔴
- [ ] REST API design (status codes, routing, DTOs) 🔴
- [ ] Interface vs abstract class — when to use each 🔴
- [ ] Value types vs reference types (struct vs class) 🟡
- [ ] Generics and generic constraints 🟡
- [ ] Entity Framework — DbContext, migrations, Include 🟡
- [ ] Middleware pipeline in ASP.NET Core 🟡
- [ ] Unit testing basics — Arrange/Act/Assert, mocking 🟡
- [ ] Nullable reference types (C# 8+)
- [ ] Record types and pattern matching (C# 9+)
- [ ] IEnumerable vs IQueryable (deferred execution)
- [ ] Exception handling best practices