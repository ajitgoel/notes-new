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

## Interview Questions to Prepare

1. Why would you choose GraphQL over REST for service orchestration?
2. How does `graphql-java` execute a query? Walk through the pipeline.
3. Compare Spring for GraphQL vs Netflix DGS — when would you pick one over the other?
4. What does "schema-first" mean and why is it preferred in team settings?
5. How does GraphQL handle versioning differently from REST?
