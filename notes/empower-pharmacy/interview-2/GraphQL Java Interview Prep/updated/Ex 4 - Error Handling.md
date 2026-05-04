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

## Complete Solution

### Custom Exception Hierarchy

```java
public abstract class GraphqlBusinessException extends RuntimeException {
    private final ErrorCode errorCode;
    private final Map<String, Object> extensions;

    protected GraphqlBusinessException(String message, ErrorCode code,
                                        Map<String, Object> extensions) {
        super(message);
        this.errorCode = code;
        this.extensions = extensions;
    }

    public ErrorCode getErrorCode() { return errorCode; }
    public Map<String, Object> getExtensions() { return extensions; }
}

public enum ErrorCode {
    RESOURCE_NOT_FOUND, VALIDATION_ERROR, SERVICE_UNAVAILABLE,
    PERMISSION_DENIED, RATE_LIMITED
}

public class ResourceNotFoundException extends GraphqlBusinessException {
    public ResourceNotFoundException(String type, String id) {
        super(type + " not found: " + id, ErrorCode.RESOURCE_NOT_FOUND,
            Map.of("resourceType", type, "resourceId", id));
    }
}

public class ValidationException extends GraphqlBusinessException {
    private final List<FieldError> fieldErrors;

    public ValidationException(List<FieldError> fieldErrors) {
        super("Validation failed", ErrorCode.VALIDATION_ERROR, Map.of());
        this.fieldErrors = fieldErrors;
    }

    public List<FieldError> getFieldErrors() { return fieldErrors; }

    public record FieldError(String field, String message) {}
}

public class ServiceUnavailableException extends GraphqlBusinessException {
    private final String serviceName;

    public ServiceUnavailableException(String serviceName) {
        super("Service unavailable: " + serviceName,
            ErrorCode.SERVICE_UNAVAILABLE,
            Map.of("service", serviceName));
        this.serviceName = serviceName;
    }

    public String getServiceName() { return serviceName; }
}
```

### Exception Resolver

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
                    .message(fe.message())
                    .errorType(ErrorType.BAD_REQUEST)
                    .extensions(Map.of(
                        "code", "VALIDATION_ERROR",
                        "field", fe.field()
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

### Mutation Payload Pattern

```java
@MutationMapping
public UpdateProductPayload updateProduct(@Argument UpdateProductInput input) {
    List<UserError> errors = new ArrayList<>();

    // Validate
    if (input.name() != null && input.name().isBlank()) {
        errors.add(new UserError("name", "Name cannot be blank", "BLANK_FIELD"));
    }
    if (input.price() != null && input.price().compareTo(BigDecimal.ZERO) < 0) {
        errors.add(new UserError("price", "Price cannot be negative", "INVALID_VALUE"));
    }

    if (!errors.isEmpty()) {
        return new UpdateProductPayload(null, errors);
    }

    // Execute
    Product product = productService.findById(input.id())
        .orElse(null);
    if (product == null) {
        errors.add(new UserError("id", "Product not found", "NOT_FOUND"));
        return new UpdateProductPayload(null, errors);
    }

    Product updated = productService.update(product, input);
    return new UpdateProductPayload(updated, List.of());
}

public record UpdateProductPayload(Product product, List<UserError> errors) {}
public record UserError(String field, String message, String code) {}
```

### Tests

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class ErrorHandlingTest {

    @Autowired HttpGraphQlTester tester;
    @MockBean ReviewService reviewService;

    @Test
    void queryMissingProduct_returnsNotFoundError() {
        tester.document("""
            query { product(id: "nonexistent") { name } }
            """)
            .execute()
            .path("product").valueIsNull()
            .errors().satisfy(errors -> {
                assertThat(errors).hasSize(1);
                assertThat(errors.get(0).getExtensions().get("code"))
                    .isEqualTo("RESOURCE_NOT_FOUND");
            });
    }

    @Test
    void updateProduct_validationError_returnsFieldErrors() {
        tester.document("""
            mutation {
              updateProduct(input: { id: "1", name: "", price: -5 }) {
                product { id name }
                errors { field message code }
              }
            }
            """)
            .execute()
            .path("updateProduct.product").valueIsNull()
            .path("updateProduct.errors").entityList(Object.class).hasSizeGreaterThan(0)
            .path("updateProduct.errors[0].field").hasValue();
        // No top-level errors — validation errors are in the payload
    }

    @Test
    void queryWhenServiceDown_returnsPartialData() {
        when(reviewService.findByProductId(anyString()))
            .thenThrow(new ServiceUnavailableException("review-service"));

        tester.document("""
            query {
              product(id: "1") {
                name
                reviews { rating }
              }
            }
            """)
            .execute()
            .path("product.name").hasValue()  // product data works
            .path("product.reviews").valueIsNull()  // reviews failed
            .errors().satisfy(errors -> {
                assertThat(errors).hasSize(1);
                assertThat(errors.get(0).getPath())
                    .containsExactly("product", "reviews");
                assertThat(errors.get(0).getExtensions().get("code"))
                    .isEqualTo("SERVICE_UNAVAILABLE");
            });
    }
}
```
