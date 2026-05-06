# Exercise 4: Error Handling Pipeline

## Scenario

Your orchestration layer calls multiple downstream services. You need structured, client-friendly errors without leaking internals.

---

## Task 1: Define Custom Exceptions

```java
// TODO: Create a hierarchy of business exceptions
// - GraphqlBusinessException (base class)
//   - fields: message, errorCode (enum), extensions (Map)
// - ResourceNotFoundException extends GraphqlBusinessException
// - ValidationException extends GraphqlBusinessException
//   - has a list of field-level errors
// - ServiceUnavailableException extends GraphqlBusinessException
//   - includes which downstream service failed

// ErrorCode enum should include:
// RESOURCE_NOT_FOUND, VALIDATION_ERROR, SERVICE_UNAVAILABLE,
// PERMISSION_DENIED, RATE_LIMITED
```

---

## Task 2: Build the Exception Resolver

```java
// TODO: Implement DataFetcherExceptionResolverAdapter
// Requirements:
// - Map each business exception to appropriate GraphQL error
// - Include errorCode in extensions
// - For ValidationException, include field-level errors in extensions
// - For unknown exceptions, return generic "Internal Error" (no stack trace)
// - Log the full exception server-side for debugging

@Component
public class GraphqlExceptionResolver
        extends DataFetcherExceptionResolverAdapter {

    @Override
    protected List<GraphQLError> resolveToMultipleErrors(
            Throwable ex, DataFetchingEnvironment env) {
        // Your implementation here
    }
}
```

---

## Task 3: Implement the Mutation Payload Pattern

Instead of throwing errors, return them as data:

```graphql
# TODO: Implement this schema pattern

type Mutation {
  updateProduct(input: UpdateProductInput!): UpdateProductPayload!
}

type UpdateProductPayload {
  product: Product
  errors: [UserError!]!
}

type UserError {
  field: String
  message: String!
  code: String!
}
```

```java
// TODO: Implement the resolver
@MutationMapping
public UpdateProductPayload updateProduct(
        @Argument UpdateProductInput input) {
    // Validate input
    // If validation fails, return payload with errors (don't throw)
    // If succeeds, return payload with product and empty errors
}
```

---

## Task 4: Test Error Scenarios

```java
// TODO: Write tests for each error case

@Test
void queryMissingProduct_returnsNotFoundError() {
    // Assert: error classification = NOT_FOUND
    // Assert: extensions contain errorCode
    // Assert: data.product is null
}

@Test
void updateProduct_validationError_returnsFieldErrors() {
    // Send invalid input (negative price, empty name)
    // Assert: payload.errors contains field-level messages
    // Assert: payload.product is null
    // Assert: no top-level errors (this is a data error, not a GraphQL error)
}

@Test
void queryWhenServiceDown_returnsPartialData() {
    // Mock review service to throw ServiceUnavailableException
    // Assert: product data is returned
    // Assert: reviews field has an error
    // Assert: error extensions contain SERVICE_UNAVAILABLE
}
```

---

## Solution

<details>
<summary>Exception resolver (click to reveal)</summary>

```java
@Component
@Slf4j
public class GraphqlExceptionResolver
        extends DataFetcherExceptionResolverAdapter {

    @Override
    protected List<GraphQLError> resolveToMultipleErrors(
            Throwable ex, DataFetchingEnvironment env) {

        if (ex instanceof ValidationException ve) {
            return ve.getFieldErrors().stream()
                .map(fe -> GraphqlErrorBuilder.newError(env)
                    .message(fe.getMessage())
                    .errorType(ErrorType.BAD_REQUEST)
                    .extensions(Map.of(
                        "code", "VALIDATION_ERROR",
                        "field", fe.getField()
                    ))
                    .build()
                ).toList();
        }

        if (ex instanceof ResourceNotFoundException rnf) {
            return List.of(GraphqlErrorBuilder.newError(env)
                .message(rnf.getMessage())
                .errorType(ErrorType.NOT_FOUND)
                .extensions(Map.of("code", "RESOURCE_NOT_FOUND"))
                .build());
        }

        if (ex instanceof ServiceUnavailableException sue) {
            log.error("Downstream failure: {}", sue.getServiceName(), sue);
            return List.of(GraphqlErrorBuilder.newError(env)
                .message("Service temporarily unavailable")
                .errorType(ErrorType.INTERNAL_ERROR)
                .extensions(Map.of(
                    "code", "SERVICE_UNAVAILABLE",
                    "service", sue.getServiceName()
                ))
                .build());
        }

        log.error("Unexpected error in resolver", ex);
        return List.of(GraphqlErrorBuilder.newError(env)
            .message("Internal error")
            .errorType(ErrorType.INTERNAL_ERROR)
            .extensions(Map.of("code", "INTERNAL_ERROR"))
            .build());
    }
}
```
</details>
