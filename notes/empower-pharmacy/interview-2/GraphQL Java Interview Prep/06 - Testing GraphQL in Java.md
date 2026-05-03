# Testing GraphQL in Java

## Unit Testing Resolvers

Test resolvers as plain Java methods — mock the services.

```java
@ExtendWith(MockitoExtension.class)
class UserControllerTest {

    @Mock UserService userService;
    @InjectMocks UserController controller;

    @Test
    void user_returnsUser() {
        User expected = new User("1", "Alice", "alice@example.com");
        when(userService.findById("1")).thenReturn(expected);

        User result = controller.user("1");

        assertThat(result).isEqualTo(expected);
    }
}
```

---

## Integration Testing with Spring GraphQL

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class UserIntegrationTest {

    @Autowired
    HttpGraphQlTester tester;

    @Test
    void queryUser() {
        tester.document("""
            query {
              user(id: "1") {
                name
                email
                orders {
                  id
                  total
                }
              }
            }
            """)
            .execute()
            .path("user.name").entity(String.class).isEqualTo("Alice")
            .path("user.orders").entityList(Order.class).hasSize(3);
    }

    @Test
    void queryUser_notFound() {
        tester.document("""
            query { user(id: "999") { name } }
            """)
            .execute()
            .errors()
            .satisfy(errors -> {
                assertThat(errors).hasSize(1);
                assertThat(errors.get(0).getExtensions()
                    .get("classification"))
                    .isEqualTo("NOT_FOUND");
            });
    }

    @Test
    void placeOrder_mutation() {
        tester.document("""
            mutation {
              placeOrder(input: {
                userId: "1"
                items: [{ productId: "p1", quantity: 2 }]
              }) {
                order { id total }
                errors { message }
              }
            }
            """)
            .execute()
            .path("placeOrder.order.id").hasValue()
            .path("placeOrder.errors").entityList(Object.class).hasSize(0);
    }
}
```

---

## DGS Testing with DgsQueryExecutor

```java
@SpringBootTest
class UserDataFetcherTest {

    @Autowired
    DgsQueryExecutor executor;

    @Test
    void queryUser() {
        String name = executor.executeAndExtractJsonPath(
            "{ user(id: \"1\") { name } }",
            "data.user.name"
        );
        assertThat(name).isEqualTo("Alice");
    }

    @Test
    void queryUserWithVariables() {
        Map<String, Object> vars = Map.of("id", "1");
        ExecutionResult result = executor.execute(
            "query($id: ID!) { user(id: $id) { name email } }",
            vars
        );
        assertThat(result.getErrors()).isEmpty();
    }
}
```

---

## Testing DataLoaders

```java
@Test
void ordersDataLoader_batchesCorrectly() {
    // Verify N+1 is prevented
    tester.document("""
        query {
          users(first: 5) {
            name
            orders { id }
          }
        }
        """)
        .execute()
        .path("users").entityList(Object.class).hasSize(5);

    // Verify orderService was called exactly once (batched)
    verify(orderService, times(1))
        .findOrdersByUserIds(anySet());
}
```

---

## Contract Testing with Schema Validation

```java
@Test
void schemaIsValid() {
    SchemaParser parser = new SchemaParser();
    TypeDefinitionRegistry registry = parser.parse(
        new File("src/main/resources/schema/schema.graphqls")
    );
    // Verify no parsing errors
    assertThat(registry.types()).isNotEmpty();

    // Verify required types exist
    assertThat(registry.getType("User")).isPresent();
    assertThat(registry.getType("Order")).isPresent();
}

@Test
void allQueriesHaveResolvers() {
    // Smoke test: execute introspection
    tester.document("{ __schema { queryType { fields { name } } } }")
        .execute()
        .path("__schema.queryType.fields[*].name")
        .entityList(String.class)
        .contains("user", "orders", "search");
}
```

---

## Interview Questions & Answers

### 1. How do you integration-test a GraphQL endpoint in Spring Boot?

Use `HttpGraphQlTester` (Spring for GraphQL) or `DgsQueryExecutor` (DGS). Both let you send real GraphQL queries against a running application context and assert on specific JSON paths in the response.

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class UserIntegrationTest {
    @Autowired HttpGraphQlTester tester;

    @Test
    void queryUser() {
        tester.document("{ user(id: \"1\") { name email } }")
            .execute()
            .path("user.name").entity(String.class).isEqualTo("Alice")
            .path("user.email").entity(String.class).isEqualTo("alice@example.com");
    }
}
```

This tests the full pipeline: query parsing, validation, resolver execution, DataLoader dispatch, and serialization. You can mock downstream services with `@MockBean` to isolate the GraphQL layer, or use testcontainers for full end-to-end testing.

### 2. How would you verify that DataLoader is actually batching?

Mock the batch service method and verify it was called exactly once with all the expected IDs:

```java
@Test
void ordersAreBatched() {
    tester.document("{ users(first: 10) { name orders { id } } }")
        .execute()
        .path("users").entityList(Object.class).hasSize(10);

    // If batching works: 1 call with all 10 user IDs
    // If N+1: 10 separate calls with 1 ID each
    verify(orderService, times(1)).findOrdersByUserIds(anySet());
}
```

You can also enable SQL logging (`spring.jpa.show-sql=true` or `logging.level.io.r2dbc=DEBUG`) and count the actual queries. In production, instrument with Micrometer to track DataLoader batch sizes.

### 3. What's the difference between `HttpGraphQlTester` and `DgsQueryExecutor`?

**`HttpGraphQlTester`** (Spring for GraphQL): Sends HTTP requests to the GraphQL endpoint, so it tests the full HTTP stack including security filters, interceptors, and serialization. Provides a fluent path-based assertion API. Works with `@AutoConfigureHttpGraphQlTester`.

**`DgsQueryExecutor`** (Netflix DGS): Executes queries directly against the schema (no HTTP layer). Faster because it skips HTTP overhead. Returns `ExecutionResult` or extracts values via JSON path strings (`executeAndExtractJsonPath`). Better for unit-style testing of resolvers.

Choose `HttpGraphQlTester` when you need to test auth headers, HTTP status codes, or the full request pipeline. Choose `DgsQueryExecutor` for fast resolver-focused tests.

### 4. How do you test error responses and partial failures?

```java
// Test that a missing resource returns a GraphQL error (not an HTTP error)
@Test
void missingUser_returnsNotFoundError() {
    tester.document("{ user(id: \"999\") { name } }")
        .execute()
        .path("user").valueIsNull()
        .errors().satisfy(errors -> {
            assertThat(errors).hasSize(1);
            assertThat(errors.get(0).getMessage()).contains("not found");
            assertThat(errors.get(0).getExtensions().get("classification"))
                .isEqualTo("NOT_FOUND");
        });
}

// Test partial failure: user data succeeds, orders fail
@Test
void partialFailure_returnsDataAndErrors() {
    when(orderService.findByUserId(any())).thenThrow(new ServiceException("timeout"));

    tester.document("{ user(id: \"1\") { name orders { id } } }")
        .execute()
        .path("user.name").entity(String.class).isEqualTo("Alice")
        .path("user.orders").valueIsNull()
        .errors().satisfy(errors ->
            assertThat(errors.get(0).getPath()).containsExactly("user", "orders")
        );
}
```

### 5. How do you test a subscription endpoint?

Use `WebSocketGraphQlTester` (Spring) or DGS's subscription testing support:

```java
@SpringBootTest(webEnvironment = WebEnvironment.RANDOM_PORT)
class SubscriptionTest {
    @Autowired WebSocketGraphQlTester wsTester;

    @Test
    void orderStatusSubscription() {
        Flux<OrderStatus> statuses = wsTester.document("""
            subscription { orderStatusChanged(orderId: "123") { status } }
            """)
            .executeSubscription()
            .toFlux("orderStatusChanged.status", OrderStatus.class);

        StepVerifier.create(statuses.take(3))
            .expectNext(OrderStatus.CONFIRMED)
            .expectNext(OrderStatus.SHIPPED)
            .expectNext(OrderStatus.DELIVERED)
            .verifyComplete();
    }
}
```

Key differences from query testing: subscriptions return a `Flux` (stream) instead of a single response, you need a WebSocket connection, and you use `StepVerifier` to assert on the sequence of emitted events. Use `take(n)` to limit the subscription for testing.
