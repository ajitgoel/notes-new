# Exercise 6: Authentication & Field-Level Security

## Scenario

Your orchestration layer serves both public and authenticated clients. Certain fields (email, phone, salary) should only be visible to authorized users.

---

## Task 1: Implement Auth Interceptor

```java
// TODO: Extract JWT from Authorization header,
// validate it, and place UserContext into GraphQL context

@Component
public class AuthInterceptor implements WebGraphQlInterceptor {

    private final JwtValidator jwtValidator;

    @Override
    public Mono<WebGraphQlResponse> intercept(
            WebGraphQlRequest request, Chain chain) {
        // 1. Extract "Bearer <token>" from Authorization header
        // 2. Validate and decode token
        // 3. Create UserContext with userId, roles, permissions
        // 4. Put into GraphQL context
        // 5. If no token, create anonymous UserContext
    }
}

public record UserContext(
    String userId,
    Set<String> roles,
    boolean authenticated
) {
    public boolean hasRole(String role) {
        return roles.contains(role);
    }

    public static UserContext anonymous() {
        return new UserContext(null, Set.of(), false);
    }
}
```

---

## Task 2: Implement @auth Schema Directive

```graphql
directive @auth(
  requires: Role = AUTHENTICATED
) on FIELD_DEFINITION

enum Role {
  AUTHENTICATED
  ADMIN
  SUPER_ADMIN
}

type User {
  id: ID!
  name: String!
  email: String! @auth
  phone: String @auth(requires: ADMIN)
  salary: Float @auth(requires: SUPER_ADMIN)
}

type Query {
  me: User! @auth
  users: [User!]! @auth(requires: ADMIN)
}
```

```java
// TODO: Implement SchemaDirectiveWiring that:
// 1. Reads the required role from the directive
// 2. Wraps the original DataFetcher
// 3. Checks UserContext from GraphQL context
// 4. Throws AccessDeniedException if unauthorized
// 5. Calls original fetcher if authorized
```

---

## Task 3: Owner-Based Access Control

Some fields should be visible to the resource owner OR admins:

```java
// TODO: Implement a resolver where:
// - User can see their OWN email/phone
// - Admins can see anyone's email/phone
// - No one else can

@SchemaMapping(typeName = "User")
public String email(User user, DataFetchingEnvironment env) {
    // Check: is caller the owner OR admin?
    // If not, return null or throw
}
```

---

## Task 4: Test Security

```java
@Test
void unauthenticated_cannotAccessProtectedFields() {
    // No auth header
    // query { me { name email } }
    // Assert: error with PERMISSION_DENIED
}

@Test
void authenticatedUser_canSeeOwnEmail() {
    // Auth header with user1's token
    // query { me { name email phone } }
    // Assert: name and email returned, phone only if admin
}

@Test
void admin_canSeeAllUserEmails() {
    // Auth header with admin token
    // query { users { name email phone } }
    // Assert: all fields returned
}

@Test
void regularUser_cannotAccessAdminFields() {
    // Auth header with regular user token
    // query { users { name } }
    // Assert: error because @auth(requires: ADMIN)
}
```

---

## Complete Solution

### Auth Interceptor

```java
@Component
public class AuthInterceptor implements WebGraphQlInterceptor {

    private final JwtValidator jwtValidator;

    @Override
    public Mono<WebGraphQlResponse> intercept(
            WebGraphQlRequest request, Chain chain) {
        String header = request.getHeaders().getFirst("Authorization");

        UserContext userContext;
        if (header != null && header.startsWith("Bearer ")) {
            String token = header.substring(7);
            try {
                Claims claims = jwtValidator.validate(token);
                Set<String> roles = Set.copyOf(
                    claims.get("roles", List.class));
                userContext = new UserContext(
                    claims.getSubject(), roles, true);
            } catch (Exception e) {
                userContext = UserContext.anonymous();
            }
        } else {
            userContext = UserContext.anonymous();
        }

        UserContext ctx = userContext;
        request.configureExecutionInput((input, builder) ->
            builder.graphQLContext(Map.of("user", ctx)).build()
        );

        return chain.next(request);
    }
}

public record UserContext(
    String userId,
    Set<String> roles,
    boolean authenticated
) {
    public boolean hasRole(String role) {
        return roles.contains(role);
    }

    public static UserContext anonymous() {
        return new UserContext(null, Set.of(), false);
    }
}
```

### Auth Directive Wiring

```java
public class AuthDirectiveWiring implements SchemaDirectiveWiring {

    @Override
    public GraphQLFieldDefinition onField(
            SchemaDirectiveWiringEnvironment<GraphQLFieldDefinition> env) {

        String requiredRole = Optional.ofNullable(
            env.getAppliedDirective("auth")
                .getArgument("requires").getValue()
        ).map(Object::toString).orElse("AUTHENTICATED");

        DataFetcher<?> originalFetcher = env.getFieldDataFetcher();

        DataFetcher<?> authFetcher = dfe -> {
            UserContext user = dfe.getGraphQlContext().get("user");

            if (user == null || !user.authenticated()) {
                throw new AccessDeniedException("Authentication required");
            }

            if (!"AUTHENTICATED".equals(requiredRole)
                    && !user.hasRole(requiredRole)) {
                throw new AccessDeniedException(
                    "Role " + requiredRole + " required");
            }

            return originalFetcher.get(dfe);
        };

        return env.setFieldDataFetcher(authFetcher);
    }
}

// Register the directive in configuration
@Configuration
public class GraphQLConfig {
    @Bean
    public RuntimeWiringConfigurer runtimeWiringConfigurer() {
        return builder -> builder
            .directive("auth", new AuthDirectiveWiring());
    }
}
```

### Owner-Based Access Control

```java
@Controller
public class UserController {

    @SchemaMapping(typeName = "User")
    public String email(User user, DataFetchingEnvironment env) {
        UserContext caller = env.getGraphQlContext().get("user");

        // Owner can see own email
        if (caller.userId().equals(user.getId())) {
            return user.getEmail();
        }
        // Admins can see anyone's email
        if (caller.hasRole("ADMIN")) {
            return user.getEmail();
        }
        // Everyone else gets null
        return null;
    }

    @SchemaMapping(typeName = "User")
    public String phone(User user, DataFetchingEnvironment env) {
        UserContext caller = env.getGraphQlContext().get("user");

        if (caller.userId().equals(user.getId()) || caller.hasRole("ADMIN")) {
            return user.getPhone();
        }
        return null;
    }
}
```

### Tests

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class SecurityTest {

    @Autowired HttpGraphQlTester tester;

    private HttpGraphQlTester withAuth(String token) {
        return tester.mutate()
            .header("Authorization", "Bearer " + token)
            .build();
    }

    @Test
    void unauthenticated_cannotAccessProtectedFields() {
        // No auth header
        tester.document("{ me { name email } }")
            .execute()
            .errors().satisfy(errors -> {
                assertThat(errors).isNotEmpty();
                assertThat(errors.get(0).getMessage())
                    .contains("Authentication required");
            });
    }

    @Test
    void authenticatedUser_canSeeOwnEmail() {
        String userToken = createToken("user1", Set.of("USER"));

        withAuth(userToken)
            .document("{ me { name email phone } }")
            .execute()
            .path("me.name").hasValue()
            .path("me.email").hasValue()       // own email visible
            .path("me.phone").hasValue();       // own phone visible
    }

    @Test
    void admin_canSeeAllUserEmails() {
        String adminToken = createToken("admin1", Set.of("ADMIN"));

        withAuth(adminToken)
            .document("{ users { name email phone } }")
            .execute()
            .path("users[0].email").hasValue()
            .path("users[0].phone").hasValue();
    }

    @Test
    void regularUser_cannotAccessAdminFields() {
        String userToken = createToken("user1", Set.of("USER"));

        withAuth(userToken)
            .document("{ users { name } }")
            .execute()
            .errors().satisfy(errors -> {
                assertThat(errors).isNotEmpty();
                assertThat(errors.get(0).getMessage())
                    .contains("Role ADMIN required");
            });
    }

    @Test
    void regularUser_cannotSeeOtherUsersEmail() {
        String userToken = createToken("user1", Set.of("USER"));

        withAuth(userToken)
            .document("""
                query { user(id: "user2") { name email } }
                """)
            .execute()
            .path("user.name").hasValue()
            .path("user.email").valueIsNull();  // hidden — not owner or admin
    }

    private String createToken(String userId, Set<String> roles) {
        return Jwts.builder()
            .setSubject(userId)
            .claim("roles", List.copyOf(roles))
            .setExpiration(Date.from(Instant.now().plusSeconds(3600)))
            .signWith(secretKey)
            .compact();
    }
}
```
