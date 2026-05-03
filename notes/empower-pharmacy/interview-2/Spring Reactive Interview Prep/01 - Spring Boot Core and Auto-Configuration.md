# Spring Boot Core & Auto-Configuration

## What Spring Boot Actually Does

Spring Boot is an opinionated layer on top of the Spring Framework. It solves three problems:
1. **Auto-configuration** — detects classpath dependencies, wires beans automatically
2. **Starter POMs** — curated dependency sets that work together
3. **Embedded server** — no WAR deployment; `java -jar` runs everything

---

## Auto-Configuration Deep Dive

### How it works
1. `@SpringBootApplication` = `@Configuration` + `@EnableAutoConfiguration` + `@ComponentScan`
2. `@EnableAutoConfiguration` triggers `AutoConfigurationImportSelector`
3. Selector reads `META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports`
4. Each auto-config class uses `@Conditional` annotations to decide whether to activate

### Key @Conditional annotations
```java
@ConditionalOnClass(DataSource.class)       // activate if class on classpath
@ConditionalOnMissingBean(DataSource.class) // activate only if user hasn't defined one
@ConditionalOnProperty(name = "app.feature.enabled", havingValue = "true")
@ConditionalOnWebApplication               // only in web contexts
```

### Writing a custom auto-configuration
```java
@AutoConfiguration
@ConditionalOnClass(MetricsClient.class)
@EnableConfigurationProperties(MetricsProperties.class)
public class MetricsAutoConfiguration {

    @Bean
    @ConditionalOnMissingBean
    public MetricsClient metricsClient(MetricsProperties props) {
        return new MetricsClient(props.getEndpoint(), props.getApiKey());
    }
}
```

---

## Configuration & Profiles

### Property resolution order (highest priority first)
1. Command-line args (`--server.port=9090`)
2. `SPRING_APPLICATION_JSON` env variable
3. `application-{profile}.yml`
4. `application.yml`
5. `@PropertySource` annotations
6. Default properties

### Type-safe configuration
```java
@ConfigurationProperties(prefix = "app.mail")
public record MailProperties(
    String host,
    int port,
    boolean ssl,
    Duration timeout
) {}

// application.yml
// app:
//   mail:
//     host: smtp.example.com
//     port: 587
//     ssl: true
//     timeout: 5s
```

### Profile activation
```yaml
# application.yml
spring:
  profiles:
    active: dev
---
spring.config.activate.on-profile: dev
server.port: 8080
---
spring.config.activate.on-profile: prod
server.port: 443
```

---

## Bean Lifecycle

```
Constructor → @Autowired setters → @PostConstruct → afterPropertiesSet() →
  custom init-method → ... Bean in use ... →
  @PreDestroy → destroy() → custom destroy-method
```

### Scopes
| Scope | Meaning |
|-------|---------|
| singleton | One instance per ApplicationContext (default) |
| prototype | New instance every injection |
| request | One per HTTP request (web only) |
| session | One per HTTP session (web only) |

---

## Actuator

```yaml
management:
  endpoints:
    web:
      exposure:
        include: health, info, metrics, env
  endpoint:
    health:
      show-details: always
```

Custom health indicator:
```java
@Component
public class DatabaseHealthIndicator implements HealthIndicator {
    @Override
    public Health health() {
        if (dbIsUp()) return Health.up().withDetail("latency", "12ms").build();
        return Health.down().withDetail("error", "Connection refused").build();
    }
}
```

---

## Interview Questions & Answers

### 1. Explain how auto-configuration works. What happens when you add `spring-boot-starter-data-jpa` to classpath?

When Spring Boot starts, `@EnableAutoConfiguration` triggers `AutoConfigurationImportSelector`, which reads the list of auto-configuration classes from `META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports` in every jar on the classpath.

Adding `spring-boot-starter-data-jpa` puts Hibernate, Spring Data JPA, and a connection pool on the classpath. This triggers:
1. `DataSourceAutoConfiguration` — sees `DataSource.class` on classpath, creates a `HikariDataSource` bean using `spring.datasource.*` properties
2. `HibernateJpaAutoConfiguration` — sees Hibernate on classpath, creates an `EntityManagerFactory` using the DataSource
3. `JpaRepositoriesAutoConfiguration` — scans for interfaces extending `JpaRepository` and creates proxy implementations
4. `TransactionAutoConfiguration` — creates a `PlatformTransactionManager`

Each class uses `@ConditionalOnClass` (only if the class is on classpath), `@ConditionalOnMissingBean` (only if you haven't defined your own), and `@ConditionalOnProperty` to decide whether to activate. If you define your own `DataSource` bean, the auto-configured one backs off.

### 2. What's the difference between `@Component`, `@Service`, `@Repository`, and `@Configuration`?

All four are stereotypes that mark a class for component scanning, but they carry different semantics:

- **`@Component`**: Generic stereotype. Use when no other stereotype fits.
- **`@Service`**: Marks a business logic class. No technical difference from `@Component`, but communicates intent. Spring doesn't add special behavior.
- **`@Repository`**: Marks a data access class. Spring adds **exception translation** — it wraps JDBC/JPA exceptions into Spring's `DataAccessException` hierarchy, giving consistent error handling across database vendors.
- **`@Configuration`**: Marks a class that defines `@Bean` methods. Unlike the others, Spring creates a **CGLIB proxy** for `@Configuration` classes, ensuring that calling a `@Bean` method from within the same class returns the singleton bean instead of creating a new instance.

### 3. How does Spring resolve property values when the same key exists in multiple sources?

Spring uses a `PropertySource` hierarchy with strict precedence (highest wins):
1. Command-line args (`--server.port=9090`)
2. `SPRING_APPLICATION_JSON` environment variable
3. Servlet init params
4. OS environment variables (`SERVER_PORT=9090`)
5. Profile-specific files (`application-prod.yml`)
6. `application.yml` / `application.properties`
7. `@PropertySource` annotations
8. Default properties (`SpringApplication.setDefaultProperties()`)

A key in a higher source completely overrides the same key in a lower source. This is why you can set defaults in `application.yml` and override them per environment using environment variables or command-line args. Profile-specific files always override the base `application.yml` for the active profile.

### 4. Walk through bean lifecycle from creation to destruction.

1. **Instantiation**: Spring calls the constructor (using `@Autowired` constructor injection or the default constructor)
2. **Dependency injection**: `@Autowired` fields and setters are populated
3. **Aware interfaces**: `BeanNameAware.setBeanName()`, `ApplicationContextAware.setApplicationContext()`, etc.
4. **`@PostConstruct`**: Called after all dependencies are injected. Use for initialization logic.
5. **`InitializingBean.afterPropertiesSet()`**: Spring's interface-based init hook (prefer `@PostConstruct`)
6. **Custom init method**: If `@Bean(initMethod = "init")` is specified
7. **Bean is in use**: Available for injection and method calls
8. **`@PreDestroy`**: Called during shutdown. Use for cleanup (closing connections, flushing caches)
9. **`DisposableBean.destroy()`**: Spring's interface-based destroy hook
10. **Custom destroy method**: If `@Bean(destroyMethod = "cleanup")` is specified

`@PostConstruct` and `@PreDestroy` are the standard approach. `InitializingBean`/`DisposableBean` are legacy Spring interfaces.

### 5. How would you create a custom starter for an internal library?

A starter is a combination of two modules:

**`my-library-spring-boot-autoconfigure`**:
```java
@AutoConfiguration
@ConditionalOnClass(MyLibraryClient.class)
@EnableConfigurationProperties(MyLibraryProperties.class)
public class MyLibraryAutoConfiguration {

    @Bean
    @ConditionalOnMissingBean
    public MyLibraryClient myLibraryClient(MyLibraryProperties props) {
        return new MyLibraryClient(props.getUrl(), props.getApiKey());
    }
}
```
Register it in `META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports`.

**`my-library-spring-boot-starter`** (pom-only): Depends on the autoconfigure module and the library itself. Users add only this dependency.

Key principles: use `@ConditionalOnMissingBean` so users can override, use `@ConfigurationProperties` for type-safe config, and document properties in `META-INF/additional-spring-configuration-metadata.json` for IDE autocomplete.

### 6. What's the difference between `@Bean` and `@Component`? When would you prefer one over the other?

**`@Component`** (+ `@Service`, `@Repository`): Annotated on the class itself. Discovered via classpath scanning. You control the class source code. One bean per class (unless using `@Scope`).

**`@Bean`**: Annotated on a method inside `@Configuration`. Creates a bean from the method's return value. Used when: (a) you don't control the class (third-party library), (b) you need conditional or parameterized creation, (c) you need multiple beans of the same type with different configs.

```java
// @Component — you own the class
@Component
public class MyService { ... }

// @Bean — third-party class or conditional creation
@Bean
@Profile("prod")
public MetricsClient prodMetrics() {
    return new MetricsClient("https://prod.metrics.com");
}

@Bean
@Profile("dev")
public MetricsClient devMetrics() {
    return new MetricsClient("http://localhost:9090");
}
```

Prefer `@Component` for your own classes (less boilerplate). Use `@Bean` for third-party classes or when you need method-level control.
