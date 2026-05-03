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

## Interview Questions

1. How do you integration-test a GraphQL endpoint in Spring Boot?
2. How would you verify that DataLoader is actually batching?
3. What's the difference between `HttpGraphQlTester` and `DgsQueryExecutor`?
4. How do you test error responses and partial failures?
5. How do you test a subscription endpoint?
