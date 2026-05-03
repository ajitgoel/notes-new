# Error Handling & Security

## GraphQL Error Model

GraphQL always returns HTTP 200 (even on errors). Errors live in the `errors` array:

```json
{
  "data": { "user": null },
  "errors": [{
    "message": "User not found",
    "locations": [{ "line": 2, "column": 3 }],
    "path": ["user"],
    "extensions": {
      "classification": "NOT_FOUND",
      "code": "USER_NOT_FOUND"
    }
  }]
}
```

**Partial responses**: GraphQL can return data AND errors simultaneously — some fields succeed, others fail.

---

## Custom Exceptions in Spring for GraphQL

```java
// Custom exception
public class ResourceNotFoundException extends RuntimeException {
    private final String resourceType;
    private final String resourceId;

    public ResourceNotFoundException(String type, String id) {
        super(type + " not found: " + id);
        this.resourceType = type;
        this.resourceId = id;
    }
}

// Exception resolver
@Component
public class CustomExceptionResolver
        extends DataFetcherExceptionResolverAdapter {

    @Override
    protected GraphQLError resolveToSingleError(
            Throwable ex, DataFetchingEnvironment env) {
        if (ex instanceof ResourceNotFoundException e) {
            return GraphqlErrorBuilder.newError(env)
                .message(e.getMessage())
                .errorType(ErrorType.NOT_FOUND)
                .extensions(Map.of(
                    "code", "RESOURCE_NOT_FOUND",
                    "resourceType", e.getResourceType()
                ))
                .build();
        }
        // Don't leak internal errors
        return GraphqlErrorBuilder.newError(env)
            .message("Internal error")
            .errorType(ErrorType.INTERNAL_ERROR)
            .build();
    }
}
```

---

## DGS Custom Exceptions

```java
@DgsComponent
public class UserDataFetcher {

    @DgsQuery
    public User user(@InputArgument String id) {
        return userService.findById(id)
            .orElseThrow(() -> new DgsEntityNotFoundException(
                "User " + id + " not found"
            ));
    }
}

// Custom exception with extensions
public class BusinessException extends DgsException {
    public BusinessException(String msg, String code) {
        super(msg, Map.of("code", code));
    }
}
```

---

## Authentication & Authorization

### Request-level auth (who are you?)
```java
@Component
public class AuthInterceptor implements WebGraphQlInterceptor {

    @Override
    public Mono<WebGraphQlResponse> intercept(
            WebGraphQlRequest request, Chain chain) {
        String token = request.getHeaders()
            .getFirst("Authorization");
        UserContext ctx = authService.validate(token);
        request.configureExecutionInput((input, builder) ->
            builder.graphQLContext(Map.of("user", ctx)).build()
        );
        return chain.next(request);
    }
}
```

### Field-level auth (are you allowed to see this?)
```java
@SchemaMapping(typeName = "User")
public String email(User user, DataFetchingEnvironment env) {
    UserContext caller = env.getGraphQlContext().get("user");
    if (!caller.getId().equals(user.getId())
            && !caller.hasRole("ADMIN")) {
        throw new AccessDeniedException(
            "Cannot view other users' emails");
    }
    return user.getEmail();
}
```

### Schema directive approach
```graphql
directive @auth(role: String!) on FIELD_DEFINITION

type User {
  id: ID!
  name: String!
  email: String! @auth(role: "ADMIN")
  ssn: String! @auth(role: "SUPER_ADMIN")
}
```

```java
// Implement via SchemaDirectiveWiring
public class AuthDirective implements SchemaDirectiveWiring {
    @Override
    public GraphQLFieldDefinition onField(
            SchemaDirectiveWiringEnvironment<GraphQLFieldDefinition> env) {
        String requiredRole = env.getAppliedDirective("auth")
            .getArgument("role").getValue();
        DataFetcher<?> original = env.getFieldDataFetcher();

        DataFetcher<?> authFetcher = dfe -> {
            UserContext user = dfe.getGraphQlContext().get("user");
            if (!user.hasRole(requiredRole)) {
                throw new AccessDeniedException("Forbidden");
            }
            return original.get(dfe);
        };

        return env.setFieldDataFetcher(authFetcher);
    }
}
```

---

## Rate Limiting & Query Cost

```java
@Component
public class RateLimitInterceptor implements WebGraphQlInterceptor {

    private final RateLimiter limiter; // e.g., Bucket4j

    @Override
    public Mono<WebGraphQlResponse> intercept(
            WebGraphQlRequest request, Chain chain) {
        String clientId = extractClientId(request);
        if (!limiter.tryConsume(clientId)) {
            throw new RateLimitExceededException();
        }
        return chain.next(request);
    }
}
```

---

## Security Checklist

- [ ] Disable introspection in production
- [ ] Set max query depth (10-15)
- [ ] Set max query complexity
- [ ] Validate and sanitize all input arguments
- [ ] Never expose stack traces in error messages
- [ ] Use persisted queries to prevent arbitrary query injection
- [ ] Rate limit by client/API key
- [ ] Audit log mutations

---

## Interview Questions & Answers

### 1. How does GraphQL error handling differ from REST (HTTP status codes)?

REST uses HTTP status codes (200, 404, 500) to signal success or failure. The status code is the primary error indicator, and the response body may contain error details.

GraphQL always returns **HTTP 200**, even when errors occur. Errors are reported in the `errors` array of the response body. This enables **partial responses** — the `data` field can contain successfully resolved fields while `errors` contains failures for specific fields. For example, if `user` resolves but `user.orders` fails, you get `{ "data": { "user": { "name": "Alice", "orders": null } }, "errors": [{ "path": ["user", "orders"], "message": "Order service unavailable" }] }`.

This is a fundamental architectural difference: REST treats the entire response as succeeded or failed, while GraphQL treats each field independently. This makes GraphQL more resilient in orchestration scenarios where some downstream services may fail while others succeed.

### 2. How do you implement field-level authorization in a GraphQL schema?

Three approaches, from simplest to most powerful:

**In the resolver** — check permissions directly:
```java
@SchemaMapping(typeName = "User")
public String email(User user, DataFetchingEnvironment env) {
    UserContext caller = env.getGraphQlContext().get("user");
    if (!caller.getId().equals(user.getId()) && !caller.hasRole("ADMIN")) {
        throw new AccessDeniedException("Cannot view other users' emails");
    }
    return user.getEmail();
}
```

**Schema directive** — declarative, reusable:
```graphql
type User {
  email: String! @auth(role: "ADMIN")
}
```
A `SchemaDirectiveWiring` wraps the original DataFetcher with an authorization check. Clean separation of concerns.

**Spring Security `@PreAuthorize`** — integrates with Spring's security model but less GraphQL-idiomatic. Works well if your team already uses Spring Security extensively.

The directive approach is preferred for orchestration because it's visible in the schema (the contract shows who can access what) and reusable across all fields.

### 3. What is a schema directive and how would you use one for auth?

A directive is a schema annotation that modifies field behavior. For auth, you define `directive @auth(role: String!) on FIELD_DEFINITION` and implement `SchemaDirectiveWiring`. The wiring intercepts the field's DataFetcher: before calling the original fetcher, it checks the caller's roles from the GraphQL context. If unauthorized, it throws an exception. If authorized, it delegates to the original fetcher.

This is powerful because: (a) auth rules are visible in the schema SDL, (b) the implementation is DRY — one wiring class handles all `@auth` fields, (c) it composes with other directives like `@deprecated` or `@cacheControl`.

### 4. How do you prevent introspection attacks in production?

Introspection queries (`{ __schema { types { name fields { name } } } }`) expose your entire schema, including internal types, deprecated fields, and field descriptions. In production:

```java
GraphQL graphQL = GraphQL.newGraphQL(schema)
    .instrumentation(new IntrospectionDisabledInstrumentation())
    .build();
```

Or in Spring Boot: `spring.graphql.schema.introspection.enabled=false`.

Additional measures: use persisted queries (only pre-registered query hashes are accepted), set `MaxQueryDepthInstrumentation` and `MaxQueryComplexityInstrumentation` to prevent exploratory abuse, and rate-limit by API key.

Note: keep introspection enabled in development and staging for tooling like GraphiQL and Apollo Studio.

### 5. Walk through how you'd handle partial failures when orchestrating 3 services.

Scenario: a `dashboard` query calls User Service, Order Service, and Recommendation Service.

1. **Classify services as required or optional**: User data is required (without it the response is meaningless). Orders are required. Recommendations are optional (nice to have).
2. **Required services**: Let errors propagate. If User Service fails, the entire `dashboard` field returns null with an error in the `errors` array.
3. **Optional services**: Catch failures and return defaults:
```java
Mono<List<Product>> recs = recClient.getForUser(userId)
    .timeout(Duration.ofSeconds(2))
    .onErrorReturn(List.of());  // empty list instead of error
```
4. **Add partial errors to response**: Even when degrading gracefully, add an error entry so the client knows recommendations were unavailable:
```java
.doOnError(ex -> env.getGraphQlContext()
    .put("warnings", "Recommendations unavailable"))
```
5. **Result**: The client gets user + orders data with `errors: [{ "message": "Recommendations service timeout", "path": ["dashboard", "recommendations"] }]`. The UI can show the dashboard with a "Recommendations unavailable" placeholder instead of a full error page.
