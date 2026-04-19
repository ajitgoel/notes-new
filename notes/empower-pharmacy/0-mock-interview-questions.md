Q: How would you implement idempotent API endpoints in .NET?
Client sends an Idempotency-Key header (UUID) with every POST. Server checks Redis keyed by that UUID. If found, return cached response immediately. If not, process the request, cache the result with a TTL (e.g. 24h), return result. Prevents duplicate prescription submissions on network retries. Store key + status code + body — return original status, not 200.

Q: Explain multi-tenancy options and which you'd choose for a healthcare SaaS.
Three models: shared DB with tenant_id column (cheap, leaky risk), ==schema-per-tenant (moderate isolation), database-per-tenant (strong isolation, higher cost)==. For healthcare, database-per-tenant is the gold standard — no cross-tenant query leakage possible. Row-level security in SQL is a viable middle ground: enforces tenant_id filtering at the DB engine level even if the app layer has a bug.

<mark style="background:#d3f8b6">Q: Walk me through how React renders and where you'd optimize performance.</mark>
<mark style="background:#d3f8b6">React re-renders a component when state or props change; the virtual DOM diffs against previous and patches only what changed. Optimize with: React.memo to skip re-render of pure components, useMemo/useCallback to stabilize references passed as props, virtualization (react-window) for long lists, code splitting via lazy()/Suspense, and avoid inline object/function literals in JSX that create new references on every render.</mark>

Q: How do you version a REST API when clients are on different versions?
URL versioning (/v1/, /v2/) is most explicit and works with caching and APIM policies. Use the Asp.Versioning NuGet package — decorate controllers with [ApiVersion("2.0")], add a deprecated sunset header on v1. Support at minimum N-1. Breaking changes go in a new version; additive changes (new fields, new optional params) are backward compatible within a version.

Q: How do you ensure HIPAA compliance in an audit log?
==Append-only,== immutable storage (Azure WORM Blob). Log: user identity, timestamp (UTC), action type, resource ID, IP address, outcome. ==Store in a separate system from operational DB so a compromised app can't alter logs. Alert on anomalous access patterns (unusual hours, bulk exports).== ==Retain for 6 years per HIPAA.== Test log completeness regularly — missing entries are a compliance gap.

Q: Authentication vs authorization in ASP.NET Core.
Authentication = who are you? — JWT bearer tokens, OAuth2/OpenID Connect. Authorization = what can you do? — RBAC, policy-based, claims-based. In ASP.NET Core middleware: UseAuthentication() sets HttpContext.User from the token, UseAuthorization() enforces [Authorize] attributes and policies. Order matters — authentication must run before authorization.

Behavioral (STAR format)

Q: Tell me about a time you improved system scalability.
Frame: Situation (what was hitting limits), Task (your ownership), Action (profile first — find the actual bottleneck, then: async refactor, caching, DB indexing, connection pooling, or infra scaling), Result (quantified — latency reduction, throughput increase). Tip: pull from GPU pipeline or FolderMind search latency work — frame the CLIP/LanceDB indexing as a scalability engineering decision.

Q: Describe making an architectural decision with incomplete information.
They're testing: how you frame tradeoffs, whether you write ADRs, whether you design for reversibility. Strong answer: what you knew vs. didn't, how you de-risked (spike, prototype, reversible boundary), what criteria you used, what you'd do differently. Show you can commit and execute — not just analyze indefinitely.

Q: How do you handle technical disagreements with teammates?
Show intellectual humility + structured debate. Good answer: propose a time-boxed spike for each approach, define evaluation criteria upfront, present tradeoffs to the team, then fully commit to and execute the decision — even if it's not what you'd have chosen. Avoid the trap of continuing to relitigate after the decision is made.