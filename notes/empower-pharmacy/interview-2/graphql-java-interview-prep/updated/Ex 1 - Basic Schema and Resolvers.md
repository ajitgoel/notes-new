# Exercise 1: Basic Schema & Resolvers

## Objective
Build a GraphQL API for a product catalog with Spring for GraphQL (or DGS).

## Setup

Add to `pom.xml`:
```xml
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-graphql</artifactId>
</dependency>
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-web</artifactId>
</dependency>
```

---

## Task 1: Define the Schema

Create `src/main/resources/graphql/schema.graphqls`:

```graphql
# TODO: Define these types and queries
# - Product: id, name, description, price, category, inStock
# - Category enum: ELECTRONICS, BOOKS, CLOTHING, HOME
# - Query: product(id: ID!), products(category: Category): [Product!]!
# - Mutation: createProduct(input: CreateProductInput!): Product!
# - CreateProductInput: name, description, price, category
```

---

## Task 2: Create the Domain Model

```java
// TODO: Create Product record/class
// - id: String
// - name: String
// - description: String
// - price: BigDecimal
// - category: Category (enum)
// - inStock: boolean
```

---

## Task 3: Implement Resolvers

```java
@Controller
public class ProductController {

    // TODO: Implement these methods

    // @QueryMapping — resolve product(id)
    // @QueryMapping — resolve products(category)
    // @MutationMapping — resolve createProduct(input)
}
```

---

## Task 4: Add a Nested Field

Add a `reviews` field to Product:
```graphql
type Review {
  id: ID!
  rating: Int!
  comment: String
  author: String!
}

type Product {
  # ... existing fields
  reviews: [Review!]!
  averageRating: Float
}
```

Implement a `@SchemaMapping` for `reviews` and a computed `averageRating`.

---

## Acceptance Criteria

- [ ] `query { product(id: "1") { name price category } }` returns a product
- [ ] `query { products(category: ELECTRONICS) { name } }` filters correctly
- [ ] `mutation { createProduct(input: { name: "Test", price: 9.99, category: BOOKS }) { id name } }` creates and returns a product
- [ ] `query { product(id: "1") { name reviews { rating comment } averageRating } }` returns nested reviews

---

## Solution Hints

### Schema

```graphql
enum Category {
  ELECTRONICS
  BOOKS
  CLOTHING
  HOME
}

type Product {
  id: ID!
  name: String!
  description: String
  price: Float!
  category: Category!
  inStock: Boolean!
  reviews: [Review!]!
  averageRating: Float
}

type Review {
  id: ID!
  rating: Int!
  comment: String
  author: String!
}

input CreateProductInput {
  name: String!
  description: String
  price: Float!
  category: Category!
}

type Query {
  product(id: ID!): Product
  products(category: Category): [Product!]!
}

type Mutation {
  createProduct(input: CreateProductInput!): Product!
}
```

### Domain Model

```java
public record Product(
    String id,
    String name,
    String description,
    BigDecimal price,
    Category category,
    boolean inStock
) {}

public record Review(
    String id,
    String productId,
    int rating,
    String comment,
    String author
) {}

public enum Category { ELECTRONICS, BOOKS, CLOTHING, HOME }

public record CreateProductInput(
    String name,
    String description,
    BigDecimal price,
    Category category
) {}
```

### Service

```java
@Service
public class ProductService {

    private final Map<String, Product> products = new ConcurrentHashMap<>();
    private final AtomicLong idCounter = new AtomicLong();

    public Optional<Product> findById(String id) {
        return Optional.ofNullable(products.get(id));
    }

    public List<Product> findAll() {
        return new ArrayList<>(products.values());
    }

    public List<Product> findByCategory(Category category) {
        return products.values().stream()
            .filter(p -> p.category() == category)
            .toList();
    }

    public Product create(CreateProductInput input) {
        String id = String.valueOf(idCounter.incrementAndGet());
        Product product = new Product(id, input.name(), input.description(),
            input.price(), input.category(), true);
        products.put(id, product);
        return product;
    }
}

@Service
public class ReviewService {

    private final Map<String, List<Review>> reviewsByProduct = new ConcurrentHashMap<>();

    public List<Review> findByProductId(String productId) {
        return reviewsByProduct.getOrDefault(productId, List.of());
    }
}
```

### Controller

```java
@Controller
public class ProductController {

    private final ProductService productService;
    private final ReviewService reviewService;

    @QueryMapping
    public Product product(@Argument String id) {
        return productService.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("Product", id));
    }

    @QueryMapping
    public List<Product> products(@Argument Category category) {
        if (category != null) {
            return productService.findByCategory(category);
        }
        return productService.findAll();
    }

    @MutationMapping
    public Product createProduct(@Argument CreateProductInput input) {
        return productService.create(input);
    }

    @SchemaMapping(typeName = "Product")
    public List<Review> reviews(Product product) {
        return reviewService.findByProductId(product.getId());
    }

    @SchemaMapping(typeName = "Product")
    public Double averageRating(Product product) {
        List<Review> reviews = reviewService.findByProductId(product.getId());
        return reviews.stream()
            .mapToInt(Review::rating)
            .average()
            .orElse(0.0);
    }
}
```

### Integration Test

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class ProductControllerTest {

    @Autowired HttpGraphQlTester tester;

    @Test
    void queryProduct() {
        tester.document("""
            query {
              product(id: "1") { name price category }
            }
            """)
            .execute()
            .path("product.name").hasValue()
            .path("product.price").hasValue()
            .path("product.category").hasValue();
    }

    @Test
    void queryProductsByCategory() {
        tester.document("""
            query {
              products(category: ELECTRONICS) { name category }
            }
            """)
            .execute()
            .path("products[*].category")
            .entityList(String.class)
            .satisfies(cats -> cats.forEach(c ->
                assertThat(c).isEqualTo("ELECTRONICS")));
    }

    @Test
    void createProduct() {
        tester.document("""
            mutation {
              createProduct(input: {
                name: "New Book"
                price: 19.99
                category: BOOKS
              }) {
                id name price category
              }
            }
            """)
            .execute()
            .path("createProduct.id").hasValue()
            .path("createProduct.name").entity(String.class).isEqualTo("New Book")
            .path("createProduct.category").entity(String.class).isEqualTo("BOOKS");
    }

    @Test
    void queryProductWithReviews() {
        tester.document("""
            query {
              product(id: "1") {
                name
                reviews { rating comment }
                averageRating
              }
            }
            """)
            .execute()
            .path("product.name").hasValue()
            .path("product.reviews").entityList(Object.class).hasSizeGreaterThanOrEqualTo(0)
            .path("product.averageRating").hasValue();
    }
}
```
