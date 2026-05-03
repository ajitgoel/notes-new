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

## Interview Questions

1. Explain how auto-configuration works. What happens when you add `spring-boot-starter-data-jpa` to classpath?
2. What's the difference between `@Component`, `@Service`, `@Repository`, and `@Configuration`?
3. How does Spring resolve property values when the same key exists in multiple sources?
4. Walk through bean lifecycle from creation to destruction.
5. How would you create a custom starter for an internal library?
6. What's the difference between `@Bean` and `@Component`? When would you prefer one over the other?
