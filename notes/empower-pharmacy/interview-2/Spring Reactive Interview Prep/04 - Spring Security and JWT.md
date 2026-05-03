# Spring Security & JWT

## Security Filter Chain

Every HTTP request passes through a filter chain. Spring Security adds ~15 filters. Key ones:

```
SecurityContextPersistenceFilter
  → CorsFilter
  → CsrfFilter
  → UsernamePasswordAuthenticationFilter (or your JWT filter)
  → ExceptionTranslationFilter
  → FilterSecurityInterceptor (authorization)
```

---

## SecurityFilterChain Configuration (Spring Boot 3+)

```java
@Configuration
@EnableWebSecurity
@EnableMethodSecurity
public class SecurityConfig {

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        return http
            .csrf(csrf -> csrf.disable())  // disable for stateless API
            .sessionManagement(sm ->
                sm.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/api/auth/**").permitAll()
                .requestMatchers("/api/admin/**").hasRole("ADMIN")
                .requestMatchers(HttpMethod.GET, "/api/products/**").permitAll()
                .requestMatchers("/api/**").authenticated()
                .anyRequest().denyAll()
            )
            .addFilterBefore(jwtAuthFilter, UsernamePasswordAuthenticationFilter.class)
            .exceptionHandling(ex -> ex
                .authenticationEntryPoint((req, res, e) -> {
                    res.setStatus(401);
                    res.getWriter().write("{\"error\":\"Unauthorized\"}");
                })
                .accessDeniedHandler((req, res, e) -> {
                    res.setStatus(403);
                    res.getWriter().write("{\"error\":\"Forbidden\"}");
                })
            )
            .build();
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
}
```

---

## JWT Authentication Filter

```java
@Component
public class JwtAuthFilter extends OncePerRequestFilter {

    private final JwtService jwtService;
    private final UserDetailsService userDetailsService;

    @Override
    protected void doFilterInternal(HttpServletRequest request,
            HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {

        String header = request.getHeader("Authorization");
        if (header == null || !header.startsWith("Bearer ")) {
            chain.doFilter(request, response);
            return;
        }

        String token = header.substring(7);
        String username = jwtService.extractUsername(token);

        if (username != null && SecurityContextHolder.getContext()
                .getAuthentication() == null) {
            UserDetails user = userDetailsService.loadUserByUsername(username);
            if (jwtService.isValid(token, user)) {
                var auth = new UsernamePasswordAuthenticationToken(
                    user, null, user.getAuthorities());
                auth.setDetails(new WebAuthenticationDetailsSource()
                    .buildDetails(request));
                SecurityContextHolder.getContext().setAuthentication(auth);
            }
        }
        chain.doFilter(request, response);
    }
}
```

---

## Method-Level Security

```java
@Service
public class OrderService {

    @PreAuthorize("hasRole('ADMIN') or #userId == authentication.principal.id")
    public List<Order> getOrders(String userId) { ... }

    @PreAuthorize("hasAuthority('ORDER_CREATE')")
    public Order createOrder(OrderRequest request) { ... }

    @PostAuthorize("returnObject.userId == authentication.principal.id")
    public Order getOrder(String orderId) { ... }

    @PreFilter("filterObject.userId == authentication.principal.id")
    public void deleteOrders(List<Order> orders) { ... }
}
```

---

## UserDetailsService

```java
@Service
public class AppUserDetailsService implements UserDetailsService {

    private final UserRepository userRepo;

    @Override
    public UserDetails loadUserByUsername(String email)
            throws UsernameNotFoundException {
        User user = userRepo.findByEmail(email)
            .orElseThrow(() -> new UsernameNotFoundException(
                "User not found: " + email));

        return org.springframework.security.core.userdetails.User.builder()
            .username(user.getEmail())
            .password(user.getPasswordHash())
            .authorities(user.getRoles().stream()
                .map(role -> new SimpleGrantedAuthority("ROLE_" + role.name()))
                .toList())
            .accountLocked(!user.isActive())
            .build();
    }
}
```

---

## CORS Configuration

```java
@Bean
public CorsConfigurationSource corsConfigurationSource() {
    CorsConfiguration config = new CorsConfiguration();
    config.setAllowedOrigins(List.of("https://app.example.com"));
    config.setAllowedMethods(List.of("GET", "POST", "PUT", "DELETE"));
    config.setAllowedHeaders(List.of("Authorization", "Content-Type"));
    config.setAllowCredentials(true);
    UrlBasedCorsConfigurationSource source = new UrlBasedCorsConfigurationSource();
    source.registerCorsConfiguration("/api/**", config);
    return source;
}
```

---

## Interview Questions & Answers

### 1. Explain the Spring Security filter chain. Where does your JWT filter sit and why?

Spring Security is built on a chain of servlet filters. Each filter handles one concern. The key filters in order:

1. `SecurityContextPersistenceFilter` — loads/saves the SecurityContext (authentication state)
2. `CorsFilter` — handles CORS preflight requests
3. `CsrfFilter` — validates CSRF tokens
4. **Your JWT filter sits here** — before `UsernamePasswordAuthenticationFilter`
5. `UsernamePasswordAuthenticationFilter` — handles form login (POST `/login`)
6. `ExceptionTranslationFilter` — converts security exceptions to HTTP responses
7. `FilterSecurityInterceptor` — final authorization check (does the authenticated user have access?)

The JWT filter sits before `UsernamePasswordAuthenticationFilter` because it needs to populate the `SecurityContext` with the authenticated user BEFORE any authorization checks run. If it ran later, the authorization filter would see an anonymous user and reject the request. We use `addFilterBefore()` to place it precisely.

### 2. What's the difference between `hasRole('ADMIN')` and `hasAuthority('ROLE_ADMIN')`?

They do the same thing. `hasRole('ADMIN')` automatically prepends `ROLE_` — so it checks for the authority `ROLE_ADMIN`. `hasAuthority('ROLE_ADMIN')` checks the exact string.

This is a Spring convention: roles are authorities with the `ROLE_` prefix. When creating `GrantedAuthority` objects, use `new SimpleGrantedAuthority("ROLE_ADMIN")`. Then both `hasRole('ADMIN')` and `hasAuthority('ROLE_ADMIN')` will match.

Use `hasRole()` for role-based checks (ADMIN, USER, MANAGER). Use `hasAuthority()` for fine-grained permissions (ORDER_CREATE, REPORT_VIEW) that don't use the `ROLE_` prefix.

### 3. How does `@PreAuthorize` work under the hood? (AOP proxy + SpEL evaluation)

`@EnableMethodSecurity` creates AOP proxies around beans with security annotations. When a method annotated with `@PreAuthorize` is called:

1. The proxy intercepts the call
2. It evaluates the SpEL (Spring Expression Language) expression against a security-aware `EvaluationContext`
3. The context provides: `authentication` (current user), `principal`, `#paramName` (method parameters), and functions like `hasRole()`, `hasAuthority()`, `isAuthenticated()`
4. If the expression returns `true`, the method executes
5. If `false`, an `AccessDeniedException` is thrown

```java
@PreAuthorize("hasRole('ADMIN') or #userId == authentication.principal.id")
public List<Order> getOrders(String userId) { ... }
```

This evaluates: "Is the caller an admin, OR does the `userId` parameter match the caller's ID?" The `#userId` refers to the method parameter via reflection.

`@PostAuthorize` works similarly but evaluates AFTER the method returns, with access to `returnObject`. Useful for checking ownership of the returned resource.

### 4. Why disable CSRF for stateless APIs?

CSRF (Cross-Site Request Forgery) attacks exploit browser-stored session cookies. The attacker tricks a user's browser into sending a request to your server — the browser automatically includes the session cookie, so the server thinks it's legitimate.

CSRF protection works by requiring a token (not in a cookie) that only the real client page has. This prevents cross-site requests because the attacker's page can't read the CSRF token.

**Stateless APIs (JWT)** don't use cookies for authentication. The JWT is sent in the `Authorization` header, which browsers don't automatically attach. A CSRF attack can't work because the attacker's page can't set the Authorization header on cross-origin requests. Therefore, CSRF protection is unnecessary and adds overhead.

**Important**: If your API uses cookie-based authentication (session cookies, OAuth cookies), you MUST keep CSRF enabled.

### 5. How would you implement refresh tokens alongside access tokens?

**Access token**: Short-lived (15 minutes). Sent in `Authorization: Bearer` header. Contains user ID, roles, expiry. Stateless — server validates by checking signature and expiry.

**Refresh token**: Long-lived (7-30 days). Stored in the database. Used only to get a new access token. Never sent to resource endpoints.

Flow:
1. User logs in → server returns `{ accessToken, refreshToken }`
2. Client uses access token for API calls
3. When access token expires (401 response), client sends refresh token to `POST /api/auth/refresh`
4. Server validates refresh token against database, checks it's not revoked/expired
5. Server issues new access token (and optionally rotates the refresh token)
6. If refresh token is expired/revoked → user must log in again

```java
@PostMapping("/auth/refresh")
public TokenResponse refresh(@RequestBody RefreshRequest request) {
    RefreshToken stored = refreshTokenRepo.findByToken(request.refreshToken())
        .orElseThrow(() -> new InvalidTokenException("Invalid refresh token"));
    if (stored.isExpired()) {
        refreshTokenRepo.delete(stored);
        throw new InvalidTokenException("Refresh token expired");
    }
    String newAccessToken = jwtService.generateAccessToken(stored.getUser());
    return new TokenResponse(newAccessToken, stored.getToken());
}
```

**Security**: Store refresh tokens hashed in the database. Implement token rotation (issue new refresh token on each refresh, invalidate the old one) to detect token theft.

### 6. Explain the `SecurityContextHolder` and how authentication state is propagated.

`SecurityContextHolder` is a thread-local storage for the current user's authentication. After the JWT filter validates the token, it creates a `UsernamePasswordAuthenticationToken` and stores it:

```java
SecurityContextHolder.getContext().setAuthentication(auth);
```

Any code downstream can retrieve it: `SecurityContextHolder.getContext().getAuthentication()`. This is how `@PreAuthorize`, `@AuthenticationPrincipal`, and `SecurityContextHolder` calls all find the current user.

**Thread propagation**: By default, `SecurityContextHolder` uses `MODE_THREADLOCAL` — the authentication is bound to the current thread. This works for Spring MVC (thread-per-request). For async (`@Async`) or reactive (WebFlux), you need different strategies:
- **`@Async`**: Use `DelegatingSecurityContextExecutor` or set `SecurityContextHolder.setStrategyName(MODE_INHERITABLETHREADLOCAL)`
- **WebFlux**: Uses `ReactiveSecurityContextHolder`, which stores authentication in the Reactor `Context` instead of ThreadLocal

After the request completes, `SecurityContextPersistenceFilter` clears the context to prevent thread-local leaks.
