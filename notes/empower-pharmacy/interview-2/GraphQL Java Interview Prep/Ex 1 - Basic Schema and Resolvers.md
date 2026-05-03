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

<details>
<summary>Schema (click to reveal)</summary>

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
</details>

<details>
<summary>Controller (click to reveal)</summary>

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
</details>
