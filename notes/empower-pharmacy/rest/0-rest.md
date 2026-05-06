**Headless/Web API Fundamentals** 
**REST API Design** 
REST (Representational State Transfer) uses HTTP methods to operate on resources:
- ==**GET** `/users/123` — read a resource==
- ==**POST** `/users` — create a resource==
- ==**PUT** `/users/123` — replace a resource==
- ==**PATCH** `/users/123` — partially update==
- ==**DELETE** `/users/123` — remove==
**Key principles:** Use nouns for URLs (not verbs), return proper HTTP status codes (200, 201, 404, 422, 500), and keep endpoints predictable.
**Versioning** 
Prevents breaking existing clients when the API changes.
- **URL path:** `/api/v1/users` (most common)
- **Header:** `Accept: application/vnd.myapi.v2+json`
- **Query param:** `/users?version=2`

**Idempotency** 
An operation is **idempotent** if calling it multiple times produces the same result as calling it once. This matters for retries over unreliable networks.
- **GET, PUT, DELETE** — naturally idempotent
- **POST** — not idempotent by default. Fix this with an **idempotency key**: the client sends a unique ID (e.g., `Idempotency-Key: abc-123`), and the server deduplicates repeated requests.

**Pagination** 
For large collections, return data in pages:
- **Offset-based:** `?offset=20&limit=10` — simple but slow on large datasets (DB has to skip rows).
- **Cursor-based:** `?cursor=eyJpZCI6MTAwfQ&limit=10` — faster, uses an opaque token pointing to the next page. Preferred for feeds or real-time data.

**Filtering, Sorting, Searching** 
Keep it consistent:
```
GET /products?category=shoes&price_min=50&sort=-created_at&q=running
```
- Filters as query params
- Prefix `-` for descending sort
- `q` for full-text search

**Rate Limiting** 
Protects the server from abuse. Typically communicated via response headers:
```
X-RateLimit-Limit: 1000        # max requests per window
X-RateLimit-Remaining: 847     # requests left
X-RateLimit-Reset: 1713000000  # when the window resets (Unix timestamp)
```
Return **429 Too Many Requests** when exceeded. Common strategies: fixed window, sliding window, or token bucket.

**OpenAPI / Swagger** 
**OpenAPI** is a standard spec format (YAML/JSON) that describes your entire API — endpoints, parameters, request/response schemas, auth methods. **Swagger** is the tooling ecosystem around it (Swagger UI for interactive docs, Swagger Codegen for client SDKs).
Benefits: auto-generated documentation, client code generation, contract-first development, and easier testing.
**The short version:** Design around resources with clean URLs, version from day one, make writes idempotent, ==paginate with cursors,== rate-limit to protect yourself, and document everything with OpenAPI.

========
**Cursor-Based Pagination — Deeper Dive** 
**The Problem with Offset Pagination** 
With `?offset=1000&limit=20`, the database must scan and skip 1,000 rows before returning 20. As offset grows, queries get slower. Worse, if a row is inserted or deleted between page requests, you can **skip items or see duplicates**.

**How Cursor Pagination Works** 
A **cursor** is an opaque pointer to a specific item in the dataset — usually an encoded version of the last item’s unique, sortable field (like an ID or timestamp).

**Flow:**
1. Client requests: `GET /posts?limit=10`
2. Server returns 10 posts + a cursor for the next page:
3. ```json
    {
      "data": [ ... 10 posts ... ],
      "next_cursor": "eyJpZCI6NDJ9",
      "has_more": true
    }
    ```
4. Client requests next page: `GET /posts?limit=10&cursor=eyJpZCI6NDJ9`
5. Server decodes the cursor (e.g., `{"id": 42}`), then queries:
6. ```sql
    SELECT * FROM posts WHERE id > 42 ORDER BY id ASC LIMIT 10
    ```
7. Repeat until `has_more` is `false`.
**Why It’s Better** 
||Offset|Cursor|
|---|---|---|
|Performance on large datasets|Degrades (skips rows)|Constant (indexed WHERE clause)|
|Handles inserts/deletes mid-pagination|Duplicates or skips|Stable — always picks up after last seen item|
|Can jump to arbitrary page|Yes|No — forward/backward only|
|Simplicity|Simpler|Slightly more complex|

**When to Use Which** 
- **Cursor:** Feeds, timelines, infinite scroll, real-time data, large tables
- **Offset:** Small datasets, admin panels where users need “page 7 of 12”