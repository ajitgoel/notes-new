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

## Interview Questions

1. When would you use an interface vs a union type?
2. Explain the Relay Connection spec and why it exists.
3. How do you handle schema evolution without breaking clients?
4. What is Apollo Federation / DGS Federation, and when do you need it?
5. Design a schema for an e-commerce app with users, products, orders, and reviews.
