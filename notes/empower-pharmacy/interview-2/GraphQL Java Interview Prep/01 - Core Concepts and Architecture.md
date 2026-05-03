# GraphQL as a Java Orchestration Layer — Core Concepts

## Why GraphQL as an Orchestration Layer?

Traditional REST-based orchestration (BFF or API gateway) forces the server to define fixed response shapes. GraphQL flips this: **the client declares what it needs, and the server assembles it from underlying services.**

In Java, this means your GraphQL server sits between clients and downstream microservices/databases, acting as a **single composable entry point**.

```
Client ──▶ GraphQL (Java) ──▶ User Service (REST)
                             ──▶ Order Service (gRPC)
                             ──▶ Product DB (JDBC)
                             ──▶ Inventory Cache (Redis)
```

### Key advantages over REST orchestration
- **No over-fetching**: clients get exactly the fields they request
- **Single round-trip**: one query can fan out to multiple services
- **Strong typing**: schema acts as a contract between frontend and backend
- **Evolvable**: add fields without versioning; deprecate gracefully

---

## Java GraphQL Frameworks

### 1. Spring for GraphQL (official, Spring Boot 3+)
- Built on `graphql-java`
- Annotation-driven: `@QueryMapping`, `@SchemaMapping`, `@MutationMapping`
- Integrates with Spring WebFlux for reactive pipelines
- Auto-wires `DataLoader` via `@BatchMapping`

### 2. Netflix DGS (Domain Graph Service)
- Also built on `graphql-java`
- Annotation-driven: `@DgsQuery`, `@DgsData`, `@DgsMutation`
- Code generation from schema (`dgs-codegen`)
- Built-in federation support for federated graphs

### 3. graphql-java (low-level)
- The foundational engine both frameworks use
- Manual wiring of `DataFetcher` instances
- Full control but more boilerplate

---

## Execution Model

GraphQL execution in Java follows this pipeline:

1. **Parse** — SDL string → AST (Abstract Syntax Tree)
2. **Validate** — AST checked against schema (type/field existence, argument types)
3. **Execute** — Engine walks the AST, calling DataFetchers per field
4. **Serialize** — Java objects → JSON response

### Key class: `graphql.GraphQL`
```java
GraphQL graphQL = GraphQL.newGraphQL(schema)
    .instrumentation(new TracingInstrumentation())
    .build();

ExecutionResult result = graphQL.execute(
    ExecutionInput.newExecutionInput()
        .query(query)
        .variables(variables)
        .context(securityContext)
        .dataLoaderRegistry(registry)
        .build()
);
```

---

## Schema-First vs Code-First

| Aspect | Schema-First | Code-First |
|--------|-------------|------------|
| **Definition** | Write `.graphqls` SDL files | Build schema in Java code |
| **Frameworks** | Spring GraphQL, DGS | graphql-java-kickstart |
| **Pros** | Readable contract, codegen | Refactoring-safe, DRY |
| **Cons** | Schema/code drift risk | Schema less visible |
| **Recommendation** | Preferred for orchestration | Better for rapid prototyping |

---

## Interview Questions & Answers

### 1. Why would you choose GraphQL over REST for service orchestration?

REST orchestration layers (BFF pattern) require you to build fixed endpoints with predetermined response shapes. When a new client needs a different combination of data, you either over-fetch and waste bandwidth, or build another endpoint. GraphQL solves this by letting the client specify exactly which fields it needs in a single request. The server resolves each field independently, which means one query can fan out to multiple microservices and compose the results. This is particularly valuable in orchestration because:
- **Reduced round trips**: a mobile client can get user + orders + recommendations in one call instead of three
- **No endpoint proliferation**: you don't need `/api/dashboard-mobile` and `/api/dashboard-web`
- **Strong typing**: the schema serves as a living API contract, unlike REST where you need separate OpenAPI specs
- **Incremental adoption**: you can add fields backed by new services without versioning or breaking existing clients

### 2. How does `graphql-java` execute a query? Walk through the pipeline.

The execution follows four phases:

1. **Parse**: The query string is parsed into an Abstract Syntax Tree (AST). The parser checks syntax — mismatched braces, invalid tokens — but not whether the fields actually exist in the schema.
2. **Validate**: The AST is validated against the `GraphQLSchema`. This checks that requested types and fields exist, arguments have correct types, required arguments are provided, and fragment spreads are valid.
3. **Execute**: The engine walks the validated AST depth-first. For each field, it invokes the registered `DataFetcher`. If no custom fetcher exists, it uses a `PropertyDataFetcher` that reads the field name from the parent object via getter. Execution respects the DataLoader batching boundaries — it collects all loads at the same level before dispatching.
4. **Serialize**: The Java objects returned by DataFetchers are serialized to JSON. Custom scalars use registered `Coercing` implementations to convert values.

Additionally, `Instrumentation` hooks (like `TracingInstrumentation`) can intercept each phase for metrics, logging, or security checks.

### 3. Compare Spring for GraphQL vs Netflix DGS — when would you pick one over the other?

**Spring for GraphQL**: Official Spring project, tightly integrated with Spring Boot 3+. Uses standard Spring patterns (`@Controller`, `@Autowired`). Best when you're already in the Spring ecosystem and want first-party support. `@BatchMapping` is a clean abstraction over DataLoader. Integrates naturally with Spring Security, WebFlux, and Spring Data.

**Netflix DGS**: Built by Netflix, battle-tested at scale. Its killer feature is **code generation** — `dgs-codegen` generates Java types from your schema, eliminating drift between schema and code. Has built-in **federation support** for multi-team graph architectures. Also provides a testing framework (`DgsQueryExecutor`) that's arguably more ergonomic for GraphQL-specific assertions.

**Pick Spring for GraphQL** when: you want official Spring support, simpler architecture, and your team already knows Spring patterns. **Pick DGS** when: you need federation, want codegen, or are building at a scale where Netflix's production-hardened patterns matter.

### 4. What does "schema-first" mean and why is it preferred in team settings?

Schema-first means you write the `.graphqls` SDL files first, then implement Java resolvers to match. The schema is the source of truth. This contrasts with code-first, where you build the schema programmatically in Java and the SDL is generated.

Schema-first is preferred in teams because:
- **Frontend and backend can work in parallel** — the schema is the contract. Frontend mocks against it while backend implements resolvers
- **Schema review is accessible** — SDL is readable by anyone, including PMs and designers. Java code is not
- **Tooling support** — IDEs, linters, and breaking-change detectors work with SDL files
- **Prevents over-engineering** — you design the API clients need, not the one that mirrors your internal object model

The risk is schema/code drift, but codegen tools (DGS codegen, graphql-java-codegen) and compile-time validation mitigate this.

### 5. How does GraphQL handle versioning differently from REST?

REST typically uses URL versioning (`/api/v1/users`, `/api/v2/users`) or header-based versioning. This creates parallel implementations that must be maintained.

GraphQL avoids versioning entirely through **additive evolution**:
- **Add new fields** at any time — existing clients don't request them, so nothing breaks
- **Deprecate old fields** with `@deprecated(reason: "Use fullName instead")` — clients see warnings in tooling but continue to work
- **Remove fields** only after monitoring confirms zero usage (tools like Apollo Studio track field-level usage)

This works because clients explicitly request fields. An old client requesting `{ user { name } }` isn't affected when you add `{ user { fullName } }`. In REST, changing the response shape of `/api/users` risks breaking every client.
