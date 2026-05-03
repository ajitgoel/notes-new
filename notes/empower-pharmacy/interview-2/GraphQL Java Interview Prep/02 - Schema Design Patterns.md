# Schema Design Patterns for Orchestration

## Foundational Types

```graphql
type Query {
  user(id: ID!): User
  orders(userId: ID!, first: Int, after: String): OrderConnection!
  search(term: String!): [SearchResult!]!
}

type Mutation {
  placeOrder(input: PlaceOrderInput!): PlaceOrderPayload!
  updateProfile(input: UpdateProfileInput!): User!
}

type Subscription {
  orderStatusChanged(orderId: ID!): Order!
}
```

### Input Types — Always use input objects for mutations
```graphql
input PlaceOrderInput {
  userId: ID!
  items: [OrderItemInput!]!
  shippingAddress: AddressInput!
}

# Payload pattern — return the mutated object + errors
type PlaceOrderPayload {
  order: Order
  errors: [UserError!]!
}

type UserError {
  field: String
  message: String!
  code: ErrorCode!
}
```

---

## Relay Connection Pattern (Cursor Pagination)

The industry standard for paginated lists in GraphQL:

```graphql
type OrderConnection {
  edges: [OrderEdge!]!
  pageInfo: PageInfo!
  totalCount: Int!
}

type OrderEdge {
  node: Order!
  cursor: String!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

### Why Relay-style over offset pagination?
- **Stable under inserts/deletes** — cursor-based doesn't skip or duplicate
- **Efficient with large datasets** — `WHERE id > :cursor LIMIT :first`
- **Standardized** — tooling (Relay, Apollo) understands it natively

---

## Interfaces and Unions

Use **interfaces** when types share fields. Use **unions** when types are unrelated.

```graphql
interface Node {
  id: ID!
}

interface Timestamped {
  createdAt: DateTime!
  updatedAt: DateTime!
}

type User implements Node & Timestamped {
  id: ID!
  name: String!
  email: String!
  createdAt: DateTime!
  updatedAt: DateTime!
}

# Union for polymorphic search
union SearchResult = User | Product | Order

type Query {
  search(term: String!): [SearchResult!]!
}
```

In Java, resolve unions with a `TypeResolver`:
```java
@Bean
public RuntimeWiringConfigurer runtimeWiringConfigurer() {
    return builder -> builder
        .type("SearchResult", wiring -> wiring
            .typeResolver(env -> {
                Object obj = env.getObject();
                if (obj instanceof User) return env.getSchema().getObjectType("User");
                if (obj instanceof Product) return env.getSchema().getObjectType("Product");
                return env.getSchema().getObjectType("Order");
            })
        );
}
```

---

## Federation (for multi-team orchestration)

When multiple teams own different parts of the graph:

```graphql
# User service schema
type User @key(fields: "id") {
  id: ID!
  name: String!
  email: String!
}

# Order service extends User
extend type User @key(fields: "id") {
  id: ID! @external
  orders: [Order!]!
}
```

DGS supports federation natively. Spring GraphQL requires additional setup.

---

## Design Rules for Orchestration Schemas

1. **Think in graphs, not endpoints** — model relationships, not REST resources
2. **Non-nullable by default** — use `!` unless the field genuinely can be null
3. **ID fields are opaque** — don't expose database internals
4. **Prefer specific queries over generic ones** — `userById(id: ID!)` over `entity(type: String, id: ID!)`
5. **Deprecate, don't delete** — `@deprecated(reason: "Use fullName instead")`

---

## Interview Questions & Answers

### 1. When would you use an interface vs a union type?

**Interface**: Use when types share a common set of fields. For example, `Node` with `id: ID!` or `Timestamped` with `createdAt`/`updatedAt`. All implementing types must include the interface's fields, so you can query those shared fields without inline fragments. Interfaces support polymorphic queries like `{ allNodes { id ... on User { name } } }`.

**Union**: Use when types have no fields in common but can appear in the same context. A `SearchResult = User | Product | Order` is the classic example — these types are structurally unrelated, but a search might return any of them. With unions, you *must* use inline fragments (`... on User { name }`) because there are no guaranteed shared fields.

**Decision rule**: If you'd naturally say "X *is a* Y" (a User *is a* Node), use an interface. If you'd say "X *or* Y" (a result is a User *or* a Product), use a union.

### 2. Explain the Relay Connection spec and why it exists.

The Relay Connection spec standardizes cursor-based pagination in GraphQL using three types: `Connection` (has `edges` and `pageInfo`), `Edge` (has `node` and `cursor`), and `PageInfo` (has `hasNextPage`, `hasPreviousPage`, `startCursor`, `endCursor`).

It exists to solve problems with offset-based pagination:
- **Stability**: If items are inserted or deleted between page requests, offset pagination skips or duplicates items. Cursor-based pagination uses an opaque pointer to a specific position, so it's stable regardless of mutations.
- **Performance**: `WHERE id > :cursor LIMIT :n` is O(1) with an index, while `OFFSET 10000 LIMIT 20` must skip 10,000 rows.
- **Tooling**: Relay, Apollo, and urql understand this format natively, enabling automatic pagination merge, cache updates, and infinite scroll.
- **Metadata**: `totalCount` and `pageInfo` give clients everything they need to build pagination UIs without extra queries.

### 3. How do you handle schema evolution without breaking clients?

- **Only add, never remove or rename**: New fields, new types, new arguments with defaults — all are safe.
- **Deprecate first**: Mark fields with `@deprecated(reason: "...")`. Clients see warnings in IDE tooling.
- **Monitor field usage**: Tools like Apollo Studio or DGS metrics track which clients use which fields. Only remove a field when usage hits zero.
- **Default new arguments**: `query users(status: Status = ACTIVE)` — existing clients that don't pass `status` still work.
- **Nullable return types for new fields**: If the resolver might fail, make the field nullable so old clients that don't request it aren't affected, and new clients handle null gracefully.
- **Avoid changing field semantics**: If `price` was in dollars and now needs to be in cents, add `priceInCents` and deprecate `price`.

### 4. What is Apollo Federation / DGS Federation, and when do you need it?

Federation is a pattern for composing a single GraphQL API from multiple independently deployed services ("subgraphs"). Each team owns their subgraph and defines the types they're responsible for. A gateway (Apollo Router, DGS Gateway) combines them into one unified schema.

Key concepts:
- **`@key`**: Marks a type as an entity that can be referenced across subgraphs. `type User @key(fields: "id")` means other subgraphs can extend `User` if they know the `id`.
- **`extend type`**: A subgraph can add fields to an entity it doesn't own. The Orders subgraph adds `orders: [Order!]!` to `User`.
- **Reference resolvers**: When the gateway needs to resolve a `User` in the Orders subgraph, it calls a `__resolveReference` function with the `id`.

**When you need it**: Multiple teams contributing to the same API, large schemas that can't be owned by one team, or independent deployment lifecycles for different domain areas.

### 5. Design a schema for an e-commerce app with users, products, orders, and reviews.

```graphql
type Query {
  me: User!
  product(id: ID!): Product
  products(first: Int, after: String, filter: ProductFilter): ProductConnection!
  order(id: ID!): Order
}

type Mutation {
  placeOrder(input: PlaceOrderInput!): PlaceOrderPayload!
  addReview(input: AddReviewInput!): Review!
}

type User {
  id: ID!
  name: String!
  email: String!
  orders(first: Int, after: String): OrderConnection!
  reviews: [Review!]!
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

type Order {
  id: ID!
  user: User!
  items: [OrderItem!]!
  total: Float!
  status: OrderStatus!
  createdAt: DateTime!
}

type OrderItem {
  product: Product!
  quantity: Int!
  unitPrice: Float!
}

type Review {
  id: ID!
  product: Product!
  author: User!
  rating: Int!
  comment: String
  createdAt: DateTime!
}

enum OrderStatus { PENDING, CONFIRMED, SHIPPED, DELIVERED, CANCELLED }
enum Category { ELECTRONICS, BOOKS, CLOTHING, HOME }
input ProductFilter { category: Category, minPrice: Float, maxPrice: Float, inStock: Boolean }
input PlaceOrderInput { items: [OrderItemInput!]! }
input OrderItemInput { productId: ID!, quantity: Int! }
input AddReviewInput { productId: ID!, rating: Int!, comment: String }
type PlaceOrderPayload { order: Order, errors: [UserError!]! }
type UserError { field: String, message: String!, code: String! }
```

Key design decisions: Relay-style connections for paginated lists, input/payload pattern for mutations, non-nullable where data is guaranteed, `averageRating` as a computed field, and `UserError` for domain-level validation errors.
