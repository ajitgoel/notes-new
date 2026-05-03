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

## Interview Questions

1. How does GraphQL error handling differ from REST (HTTP status codes)?
2. How do you implement field-level authorization in a GraphQL schema?
3. What is a schema directive and how would you use one for auth?
4. How do you prevent introspection attacks in production?
5. Walk through how you'd handle partial failures when orchestrating 3 services.
