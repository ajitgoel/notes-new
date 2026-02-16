# Project Requirements Document: Returns Router (RMA Logic Engine)

**Version:** 1.0  
**Date:** January 2026  
**Project Code:** RR-001  
**Status:** Pre-Development

---

## 1. EXECUTIVE SUMMARY

### 1.1 Project Overview

Returns Router (RMA Logic Engine) is a serverless, API-first SaaS platform that automates return merchandise authorization (RMA) routing decisions for 3PLs, fulfillment centers, and e-commerce aggregators. The system eliminates manual return processing by applying configurable business rules to determine optimal disposition (warehouse routing, refund-no-return, repair center, liquidation) in real-time.

### 1.2 Business Objectives

- **Primary Goal:** Launch MVP in 45 days with 1-2 pilot customers processing 500+ returns/month
- **Revenue Target:** $10k MRR within 6 months (10-12 customers)
- **Technical Goal:** Sub-500ms p95 latency for routing decisions with 99.9% uptime

### 1.3 Success Metrics

- **Customer Value:** Reduce return processing time from 2-4 hours to <15 minutes per return
- **Cost Savings:** 30-50% reduction in return shipping costs via intelligent routing
- **Operational:** Process 50k+ returns/month across all tenants by Month 6
- **Financial:** 85%+ gross margin, <5% monthly churn

---

## 2. PROBLEM STATEMENT & MARKET CONTEXT

### 2.1 Core Problem

When a customer initiates a return, logistics teams face complex routing decisions:

- **Which warehouse?** (East Coast vs West Coast vs regional)
- **What disposition?** (Restock vs repair vs liquidate vs discard)
- **Generate label?** (Customer return vs keep-no-return based on item value)
- **Carrier selection?** (Cheapest vs fastest based on product category)

**Current State:** These decisions are made via:

1. Manual CSV uploads to WMS systems (2-4 hours/day labor)
2. Hardcoded scripts that break when business rules change
3. No automation—operations managers manually triage each return

**Impact:** Delayed refunds (customer dissatisfaction), mis-routed inventory (increased shipping costs), slow restocking (reduced sellable inventory turns).

### 2.2 Market Validation Signals

- E-commerce return rates: 20-30% of sales (rising post-COVID)
- 3PL market growing at 7.2% CAGR through 2028
- Average 3PL processes 5k-50k returns/month manually
- Existing solutions (Loop, Returnly) are UI-heavy and require $2k+/month + 6-month implementation

### 2.3 Target Customer Profile (ICP)

**Primary:**

- **Title:** Operations Manager, Fulfillment Director, VP Operations at 3PL/fulfillment centers
- **Company Size:** 20-200 employees, $5M-$50M revenue
- **Tech Stack:** ShipStation, ShipBob API, NetSuite, or custom WMS with REST APIs
- **Pain Threshold:** Processing >2k returns/month, spending 10-20 hours/week on manual routing
- **Budget Authority:** Can approve $500-$1,500/month tools without executive sign-off

**Secondary:**

- E-commerce aggregators (Thrasio-style) managing 10+ brands
- Large D2C brands with in-house fulfillment (>$10M annual revenue)

---

## 3. PRODUCT REQUIREMENTS

### 3.1 Functional Requirements

#### 3.1.1 Core Capabilities (MVP - Phase 1)

**FR-001: Return Routing API**

- Accept return request via REST API with parameters: `sku`, `order_value`, `customer_zip`, `return_reason`, `condition_notes`
- Evaluate against tenant-configured rule sets in priority order
- Return routing decision within 500ms: `destination_warehouse_id`, `disposition_type`, `generate_return_label`, `carrier_service`

**FR-002: Rule Engine**

- Support JSON-based rule definitions with conditions and actions
- Rule conditions must support:
    - Numeric comparisons (item value < $20)
    - Geographic calculations (distance to warehouse < 100 miles)
    - String matching (SKU prefix = "FRAG-", reason = "defective")
    - Boolean logic (AND/OR/NOT)
- Rule actions: route to warehouse, refund-no-return, send to repair, liquidate, discard
- Rule priority/ordering (execute highest priority matching rule first)

**FR-003: Tenant Configuration Management**

- Multi-tenant architecture with complete data isolation
- Per-tenant configuration:
    - Warehouse locations (lat/lon, capacity constraints)
    - Rule sets (JSON schema with validation)
    - API credentials for downstream systems (WMS, label APIs)
    - Cost thresholds and business logic parameters

**FR-004: Webhook Delivery**

- Send routing decision to tenant webhook endpoints
- Support HMAC signature validation for security
- Retry failed deliveries with exponential backoff (3 attempts)
- Dead letter queue for permanently failed webhooks

**FR-005: Label Generation Integration**

- Integrate with EasyPost or Shippo for return label generation
- Generate label only when `generate_return_label: true`
- Return label URL and tracking number in API response
- Support carrier selection based on rules (USPS Ground vs FedEx)

#### 3.1.2 Enhanced Capabilities (Phase 2 - Post-MVP)

**FR-006: Distance-Based Routing**

- Calculate distance from customer ZIP to all eligible warehouses
- Route to nearest warehouse with available capacity
- Support configurable radius thresholds (prefer warehouse within 50 miles)

**FR-007: Disposition Workflow Tracking**

- Track return through disposition lifecycle (in-transit → received → inspected → restocked)
- Status webhook callbacks at each stage
- Integration with WMS task management APIs

**FR-008: Analytics Dashboard (Minimal UI)**

- Read-only Vue.js dashboard showing:
    - Returns processed (last 7/30 days)
    - Routing decision breakdown (warehouse distribution, disposition types)
    - Cost savings estimate (based on distance optimizations)
    - Rule performance (which rules trigger most frequently)

### 3.2 Non-Functional Requirements

#### 3.2.1 Performance

- **NFR-001:** API response time p50 < 200ms, p95 < 500ms, p99 < 1000ms
- **NFR-002:** Support 1000 requests/minute per tenant (burst capacity 2000 req/min)
- **NFR-003:** Horizontal scalability to 100k returns/day across all tenants

#### 3.2.2 Reliability

- **NFR-004:** 99.9% uptime SLA (Basic tier), 99.95% (Pro tier)
- **NFR-005:** Zero data loss for accepted requests (persist to database before returning 200 OK)
- **NFR-006:** Graceful degradation if distance calculation API fails (fallback to default warehouse)

#### 3.2.3 Security

- **NFR-007:** All API endpoints require JWT authentication (RS256 algorithm)
- **NFR-008:** API keys stored encrypted at rest (AES-256)
- **NFR-009:** Webhook HMAC signatures using SHA-256
- **NFR-010:** Rate limiting per tenant (10k requests/hour standard, 50k pro)
- **NFR-011:** Audit log all routing decisions with tenant_id, timestamp, input, output, rule_matched

#### 3.2.4 Compliance

- **NFR-012:** GDPR-compliant data retention (purge logs after 90 days by default, configurable)
- **NFR-013:** SOC2 Type II controls checklist implementation (access logs, encryption, change management)
- **NFR-014:** Data residency options (US-only or EU-only Supabase regions)

---

## 4. TECHNICAL ARCHITECTURE

### 4.1 Technology Stack

#### 4.1.1 Core Infrastructure (Serverless)

```
Frontend Layer:     Vue.js 3 (Minimal Admin Console)
API Layer:          Cloudflare Workers (Routing API, Webhook dispatcher)
Compute:            Cloudflare Workers (Rule evaluation engine)
Database:           Supabase (Postgres) - Tenant config, rules, audit logs
Object Storage:     Cloudflare R2 - Rule snapshots, audit archives
Queue:              Cloudflare Queues - Webhook delivery, async processing
Monitoring:         Cloudflare Analytics + Supabase Logs
Authentication:     Supabase Auth (JWT issuance and validation)
Secrets:            Cloudflare Workers Secrets / Supabase Vault
```

#### 4.1.2 Third-Party Integrations

- **Label Generation:** EasyPost API (primary), Shippo (fallback)
- **Distance Calculation:** Mapbox Distance Matrix API (Phase 2)
- **Email Notifications:** Resend or SendGrid (transactional emails)

### 4.2 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     CLIENT SYSTEMS                              │
│  (Shopify, WMS, Custom Portal, Marketplace RMA APIs)           │
└────────────┬────────────────────────────────────────────────────┘
             │
             │ HTTPS POST /v1/route
             │ Authorization: Bearer <JWT>
             ▼
┌─────────────────────────────────────────────────────────────────┐
│                  CLOUDFLARE WORKERS (Edge)                       │
│  ┌──────────────────────────────────────────────────────┐       │
│  │  1. API Gateway Worker                                │       │
│  │     - JWT validation (Supabase Auth)                 │       │
│  │     - Rate limiting (KV store)                       │       │
│  │     - Request validation                             │       │
│  │     - Idempotency check (KV store)                   │       │
│  └──────────────┬───────────────────────────────────────┘       │
│                 │                                                │
│                 ▼                                                │
│  ┌──────────────────────────────────────────────────────┐       │
│  │  2. Rules Engine Worker                              │       │
│  │     - Fetch tenant rules (Supabase)                  │       │
│  │     - Evaluate conditions sequentially               │       │
│  │     - Execute first matching action                  │       │
│  │     - Enrich with warehouse metadata                 │       │
│  └──────────────┬───────────────────────────────────────┘       │
│                 │                                                │
│                 ▼                                                │
│  ┌──────────────────────────────────────────────────────┐       │
│  │  3. Label Generation Worker (conditional)            │       │
│  │     - Call EasyPost API if generate_label=true       │       │
│  │     - Return label URL + tracking number             │       │
│  └──────────────┬───────────────────────────────────────┘       │
└─────────────────┼────────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              SUPABASE (Postgres + Auth)                          │
│  Tables:                                                         │
│    - tenants                                                     │
│    - routing_rules                                               │
│    - warehouses                                                  │
│    - routing_decisions (audit log)                              │
│    - webhook_deliveries                                          │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  │ (Async)
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│            CLOUDFLARE QUEUES (Webhook Delivery)                  │
│  ┌──────────────────────────────────────────────────────┐       │
│  │  4. Webhook Dispatcher Worker                        │       │
│  │     - Consume from queue                             │       │
│  │     - POST to tenant webhook URL                     │       │
│  │     - Sign with HMAC (tenant secret)                 │       │
│  │     - Retry on failure (3 attempts, exp backoff)     │       │
│  │     - DLQ for permanent failures                     │       │
│  └──────────────────────────────────────────────────────┘       │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                TENANT WEBHOOK ENDPOINTS                          │
│  (WMS Task API, Slack, Email, Custom Integrations)              │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Data Flow (Routing Decision)

**Step-by-Step Processing:**

1. **Request Ingress** (Cloudflare Worker - API Gateway)
    - Client POSTs to `https://api.returnsrouter.com/v1/route`
    - Extract JWT from Authorization header
    - Validate JWT signature against Supabase public key
    - Extract `tenant_id` from JWT claims
    - Check rate limit in Cloudflare KV (key: `ratelimit:{tenant_id}`)
    - Validate request schema (required fields, data types)
    - Generate `idempotency_key` from hash of request body + tenant_id
    - Check KV for duplicate request (key: `idempotency:{idempotency_key}`)
    - If duplicate found, return cached response (200 OK with previous decision)
2. **Rule Retrieval** (Supabase Query)
    - Query `routing_rules` table: `SELECT * FROM routing_rules WHERE tenant_id = $1 AND active = true ORDER BY priority ASC`
    - Fetch associated `warehouses` for tenant: `SELECT * FROM warehouses WHERE tenant_id = $1 AND active = true`
    - Cache rules in Worker KV for 5 minutes (key: `rules:{tenant_id}`)
3. **Rule Evaluation** (Rules Engine Worker)
    - Initialize context object with request data + warehouse metadata
    - Iterate through rules in priority order
    - For each rule, evaluate `conditions` JSON against context:

javascript

```javascript
     // Example rule condition
     {
       "all": [
         {"fact": "order_value", "operator": "lessThan", "value": 20},
         {"fact": "return_reason", "operator": "equal", "value": "unwanted"}
       ]
     }
```

- Use lightweight JSON rules engine (json-rules-engine or custom evaluator)
- On first match, execute `action`:

javascript

```javascript
     {
       "type": "refund_no_return",
       "generate_label": false,
       "disposition": "discard"
     }
```

- If no rules match, apply default rule (tenant-configured fallback)

4. **Label Generation** (Conditional - EasyPost Integration)
    - If action specifies `generate_label: true`:
        - Construct EasyPost shipment request:

json

```json
       {
         "shipment": {
           "to_address": {
             "zip": "{warehouse_zip}"
           },
           "from_address": {
             "zip": "{customer_zip}"
           },
           "parcel": {
             "weight": 16
           },
           "carrier": "USPS",
           "service": "Ground"
         }
       }
```

```
 - Call EasyPost `/v2/shipments` endpoint
 - Extract `postage_label.label_url` and `tracking_code`
 - Handle API errors (timeout, rate limit, invalid address)
 - Fallback: If EasyPost fails, return decision without label (client can retry label generation separately)
```

5. **Response Construction & Persistence**

- Build response JSON:

json

```json
     {
       "decision_id": "dec_9f8e7d6c5b4a",
       "timestamp": "2026-01-09T18:23:45Z",
       "routing": {
         "destination_warehouse_id": "WH-CA-01",
         "disposition": "restock",
         "priority": "standard"
       },
       "label": {
         "generate": true,
         "url": "https://easypost.com/label/abc123.pdf",
         "tracking": "9400123456789",
         "carrier": "USPS"
       },
       "matched_rule_id": "rule_123",
       "cost_estimate": {
         "shipping": 4.50,
         "handling": 2.00
       }
     }
```

- Insert audit record into Supabase `routing_decisions` table (async, non-blocking)
- Store idempotency response in KV (TTL: 24 hours)
- Return 200 OK with JSON body

6. **Webhook Delivery** (Async via Cloudflare Queue)
    - Enqueue message to `webhook_delivery` queue:

json

````json
     {
       "tenant_id": "tenant_abc",
       "webhook_url": "https://customer-wms.com/api/returns/inbound",
       "payload": { /* routing decision */ },
       "hmac_secret": "encrypted_secret_key",
       "attempt": 1,
       "max_attempts": 3
     }
```
   - Webhook Dispatcher Worker consumes message
   - Generate HMAC signature: `HMAC-SHA256(payload, tenant_secret)`
   - POST to webhook URL with headers:
```
     X-Returns-Router-Signature: sha256=abc123...
     X-Returns-Router-Delivery-ID: del_xyz789
````

- On success (2xx response): Mark delivery as successful in `webhook_deliveries` table
- On failure (timeout, 5xx, connection error):
    - Retry with exponential backoff (delays: 5s, 25s, 125s)
    - After 3 failures, move to Dead Letter Queue
    - Alert tenant via email (configurable)

### 4.4 Multi-Tenant Isolation Strategy

**Data Isolation:**

- All tables include `tenant_id` column (UUID, indexed)
- Row-Level Security (RLS) policies in Supabase:

sql

```sql
  CREATE POLICY tenant_isolation ON routing_rules
  FOR ALL USING (tenant_id = auth.jwt() ->> 'tenant_id');
```

- Cloudflare Workers extract `tenant_id` from validated JWT claims, never from request body

**Configuration Isolation:**

- Tenant-specific secrets (API keys, webhook secrets) stored in Supabase Vault with encryption
- Each tenant has unique JWT signing key (asymmetric RS256 key pairs)
- Rate limits enforced per tenant in Cloudflare KV

**Compute Isolation:**

- Cloudflare Workers inherently multi-tenant (isolated V8 contexts)
- No shared in-memory state between tenant requests
- Database connection pooling handled by Supabase (PgBouncer in transaction mode)

---

## 5. DATA MODEL

### 5.1 Database Schema (Supabase Postgres)

#### Table: `tenants`

sql

```sql
CREATE TABLE tenants (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(255) NOT NULL,
  slug VARCHAR(100) UNIQUE NOT NULL, -- URL-safe identifier
  tier VARCHAR(50) NOT NULL DEFAULT 'basic', -- 'basic' | 'pro' | 'enterprise'
  status VARCHAR(50) NOT NULL DEFAULT 'active', -- 'active' | 'suspended' | 'churned'
  api_key_hash VARCHAR(255) NOT NULL, -- bcrypt hash of API key
  webhook_url TEXT, -- Primary webhook endpoint for routing decisions
  webhook_secret TEXT, -- Encrypted HMAC secret
  rate_limit_per_hour INTEGER NOT NULL DEFAULT 10000,
  default_warehouse_id UUID, -- Fallback warehouse
  settings JSONB DEFAULT '{}', -- Feature flags, preferences
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tenants_slug ON tenants(slug);
CREATE INDEX idx_tenants_status ON tenants(status);
```

#### Table: `warehouses`

sql

```sql
CREATE TABLE warehouses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  code VARCHAR(50) NOT NULL, -- e.g., "WH-CA-01"
  name VARCHAR(255) NOT NULL,
  address_line1 VARCHAR(255),
  city VARCHAR(100),
  state VARCHAR(50),
  zip VARCHAR(20) NOT NULL,
  country VARCHAR(2) NOT NULL DEFAULT 'US',
  latitude DECIMAL(10, 7), -- For distance calculations
  longitude DECIMAL(10, 7),
  capacity_limit INTEGER, -- Max units, optional constraint
  active BOOLEAN NOT NULL DEFAULT true,
  metadata JSONB DEFAULT '{}', -- Custom fields (specialization, hours, contact)
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE(tenant_id, code)
);

CREATE INDEX idx_warehouses_tenant ON warehouses(tenant_id);
CREATE INDEX idx_warehouses_active ON warehouses(tenant_id, active);
CREATE INDEX idx_warehouses_location ON warehouses(latitude, longitude); -- For geo queries
```

#### Table: `routing_rules`

sql

```sql
CREATE TABLE routing_rules (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  priority INTEGER NOT NULL, -- Lower number = higher priority
  active BOOLEAN NOT NULL DEFAULT true,
  conditions JSONB NOT NULL, -- JSON rules engine format
  action JSONB NOT NULL, -- { "type": "route_warehouse", "warehouse_id": "...", ... }
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_rules_tenant_priority ON routing_rules(tenant_id, priority) WHERE active = true;
CREATE INDEX idx_rules_tenant_active ON routing_rules(tenant_id, active);
```

**Example Rule JSON:**

json

```json
{
  "conditions": {
    "all": [
      {
        "fact": "order_value",
        "operator": "lessThan",
        "value": 25
      },
      {
        "fact": "return_reason",
        "operator": "in",
        "value": ["unwanted", "wrong_size"]
      }
    ]
  },
  "action": {
    "type": "refund_no_return",
    "disposition": "customer_keep",
    "generate_label": false,
    "notification": "Customer keeps item, issue refund"
  }
}
```

#### Table: `routing_decisions` (Audit Log)

sql

```sql
CREATE TABLE routing_decisions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  idempotency_key VARCHAR(255) NOT NULL,
  request_payload JSONB NOT NULL, -- Original request
  matched_rule_id UUID REFERENCES routing_rules(id), -- NULL if default rule
  decision JSONB NOT NULL, -- Full response object
  processing_time_ms INTEGER, -- Latency tracking
  label_generated BOOLEAN DEFAULT false,
  label_cost_usd DECIMAL(10, 2),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX idx_decisions_tenant_created ON routing_decisions(tenant_id, created_at DESC);
CREATE INDEX idx_decisions_idempotency ON routing_decisions(tenant_id, idempotency_key);
```

#### Table: `webhook_deliveries`

sql

````sql
CREATE TABLE webhook_deliveries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
  decision_id UUID NOT NULL REFERENCES routing_decisions(id) ON DELETE CASCADE,
  webhook_url TEXT NOT NULL,
  payload JSONB NOT NULL,
  attempt INTEGER NOT NULL DEFAULT 1,
  status VARCHAR(50) NOT NULL, -- 'pending' | 'success' | 'failed' | 'dead_letter'
  http_status_code INTEGER,
  response_body TEXT,
  error_message TEXT,
  next_retry_at TIMESTAMPTZ,
  delivered_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_webhooks_status ON webhook_deliveries(status, next_retry_at) WHERE status = 'pending';
CREATE INDEX idx_webhooks_tenant ON webhook_deliveries(tenant_id, created_at DESC);
```

### 5.2 Cloudflare KV Namespaces

**Namespace: `RATE_LIMITS`**
- Key format: `ratelimit:{tenant_id}:{window}` (window = hourly timestamp)
- Value: Integer counter
- TTL: 3600 seconds (1 hour)

**Namespace: `IDEMPOTENCY_CACHE`**
- Key format: `idempotency:{idempotency_key}`
- Value: Cached response JSON
- TTL: 86400 seconds (24 hours)

**Namespace: `RULES_CACHE`**
- Key format: `rules:{tenant_id}`
- Value: Array of rules JSON
- TTL: 300 seconds (5 minutes)

---

## 6. API SPECIFICATION

### 6.1 Authentication
All endpoints require JWT bearer token in `Authorization` header.

**Header Format:**
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
````

**JWT Claims:**

json

````json
{
  "sub": "tenant_abc123",
  "tenant_id": "550e8400-e29b-41d4-a716-446655440000",
  "tier": "pro",
  "iat": 1704844800,
  "exp": 1704931200
}
```

### 6.2 Core Endpoints

#### `POST /v1/route`
**Purpose:** Submit return for routing decision

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
X-Idempotency-Key: {optional_client_generated_key}
````

**Request Body:**

json

```json
{
  "return_id": "RMA-2026-001234", // Client's return identifier
  "order_id": "ORD-98765",
  "sku": "SHOE-RUN-42-BLK",
  "product_name": "Running Shoe Black 42",
  "order_value": 89.99,
  "item_cost": 45.00, // Wholesale cost for profitability calc
  "customer": {
    "zip": "90210",
    "country": "US"
  },
  "return_reason": "wrong_size", // Enum: unwanted, defective, damaged, wrong_item, wrong_size
  "condition_notes": "Unworn, original packaging",
  "requested_at": "2026-01-09T18:00:00Z",
  "metadata": { // Optional custom fields
    "marketplace": "shopify",
    "brand": "AcmeSports"
  }
}
```

**Response (200 OK):**

json

```json
{
  "decision_id": "dec_9f8e7d6c5b4a3210",
  "timestamp": "2026-01-09T18:00:01.234Z",
  "processing_time_ms": 187,
  "routing": {
    "destination_warehouse_id": "550e8400-e29b-41d4-a716-446655440001",
    "warehouse_code": "WH-CA-01",
    "warehouse_name": "California Distribution Center",
    "disposition": "restock", // Enum: restock, repair, liquidate, discard, customer_keep
    "priority": "standard", // Enum: standard, expedited
    "notes": "Route to nearest warehouse for inspection"
  },
  "label": {
    "generate": true,
    "url": "https://easypost-files.s3.amazonaws.com/labels/USPS_abc123.pdf",
    "tracking_number": "9400123456789012345678",
    "carrier": "USPS",
    "service": "Ground",
    "cost_usd": 4.50
  },
  "matched_rule": {
    "rule_id": "550e8400-e29b-41d4-a716-446655440002",
    "rule_name": "High-Value Items - Route to Nearest Warehouse",
    "priority": 10
  },
  "cost_estimate": {
    "shipping_usd": 4.50,
    "handling_usd": 2.00,
    "total_usd": 6.50
  }
}
```

**Error Responses:**

json

```json
// 400 Bad Request - Invalid input
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "Missing required field: sku",
    "details": {
      "field": "sku",
      "constraint": "required"
    }
  }
}

// 401 Unauthorized
{
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Invalid or expired JWT token"
  }
}

// 429 Too Many Requests
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded: 10000 requests per hour",
    "retry_after": 3600
  }
}

// 500 Internal Server Error
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "Routing engine failure",
    "trace_id": "tr_abc123def456"
  }
}
```

**Rate Limits:**

- Basic tier: 10,000 requests/hour
- Pro tier: 50,000 requests/hour
- Burst allowance: 2x sustained rate for 60 seconds

**Idempotency:**

- If `X-Idempotency-Key` header provided, response cached for 24 hours
- Duplicate requests return cached response (same decision_id, status 200)
- If no header provided, system generates key from hash of request body + tenant_id

---

#### `GET /v1/decisions/:decision_id`

**Purpose:** Retrieve routing decision details (audit/debugging)

**Response (200 OK):**

json

```json
{
  "decision_id": "dec_9f8e7d6c5b4a3210",
  "return_id": "RMA-2026-001234",
  "timestamp": "2026-01-09T18:00:01.234Z",
  "request": { /* original request payload */ },
  "response": { /* original response payload */ },
  "webhook_status": "delivered", // Enum: pending, delivered, failed
  "webhook_delivered_at": "2026-01-09T18:00:02.100Z"
}
```

---

#### `GET /v1/health`

**Purpose:** Health check endpoint (no auth required)

**Response (200 OK):**

json

```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2026-01-09T18:00:00Z",
  "services": {
    "database": "healthy",
    "label_api": "healthy"
  }
}
```

---

### 6.3 Admin/Configuration Endpoints (Phase 2)

#### `GET /v1/rules`

**Purpose:** List all routing rules for tenant

**Response (200 OK):**

json

```json
{
  "rules": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "name": "High-Value Items",
      "priority": 10,
      "active": true,
      "conditions": { /* JSON */ },
      "action": { /* JSON */ },
      "created_at": "2026-01-01T00:00:00Z"
    }
  ],
  "total": 12
}
```

#### `POST /v1/rules`

**Purpose

Continue

12:20 AM

:** Create new routing rule

#### `PUT /v1/rules/:rule_id`

**Purpose:** Update existing rule

#### `DELETE /v1/rules/:rule_id`

**Purpose:** Delete rule (soft delete, sets active=false)

---

## 7. BUSINESS LOGIC & RULES ENGINE

### 7.1 Rule Evaluation Algorithm

**Pseudocode:**

```
function evaluateRouting(request, tenant):
  // 1. Fetch rules for tenant from cache/DB
  rules = getRulesForTenant(tenant.id, cache_ttl=300s)
  
  // 2. Sort by priority (already sorted in query)
  rules = sortByPriority(rules) // ASC order
  
  // 3. Build evaluation context
  context = {
    "order_value": request.order_value,
    "item_cost": request.item_cost,
    "sku": request.sku,
    "customer_zip": request.customer.zip,
    "return_reason": request.return_reason,
    "condition_notes": request.condition_notes,
    "warehouses": getWarehousesForTenant(tenant.id)
  }
  
  // 4. Evaluate each rule
  for rule in rules:
    if rule.active == false:
      continue
    
    if evaluateConditions(rule.conditions, context):
      // First match wins
      decision = executeAction(rule.action, context)
      decision.matched_rule_id = rule.id
      decision.matched_rule_name = rule.name
      return decision
  
  // 5. No rules matched, use default
  return executeDefaultAction(tenant.default_warehouse_id, context)

function evaluateConditions(conditions, context):
  // Use json-rules-engine or custom evaluator
  // Supports: all, any, not operators
  // Comparisons: equal, notEqual, lessThan, greaterThan, in, contains
  engine = new RulesEngine()
  engine.addFacts(context)
  result = engine.run(conditions)
  return result.matched

function executeAction(action, context):
  decision = {
    "destination_warehouse_id": action.warehouse_id || context.default_warehouse,
    "disposition": action.disposition,
    "generate_label": action.generate_label,
    "priority": action.priority || "standard"
  }
  
  if action.generate_label:
    label = generateLabel(context.customer_zip, warehouse.zip)
    decision.label = label
  
  return decision
```

### 7.2 Rule Examples

**Example 1: Low-Value Refund Without Return**

json

```json
{
  "name": "Low-Value Items - Keep & Refund",
  "priority": 5,
  "conditions": {
    "all": [
      {
        "fact": "item_cost",
        "operator": "lessThan",
        "value": 15
      },
      {
        "fact": "return_reason",
        "operator": "in",
        "value": ["unwanted", "wrong_size"]
      }
    ]
  },
  "action": {
    "type": "refund_no_return",
    "disposition": "customer_keep",
    "generate_label": false,
    "notification": "Issue refund, customer keeps item. Return shipping > item value."
  }
}
```

**Example 2: Defective Items to Repair Center**

json

```json
{
  "name": "Defective - Route to Repair",
  "priority": 1,
  "conditions": {
    "any": [
      {
        "fact": "return_reason",
        "operator": "equal",
        "value": "defective"
      },
      {
        "fact": "condition_notes",
        "operator": "contains",
        "value": "broken"
      }
    ]
  },
  "action": {
    "type": "route_warehouse",
    "warehouse_id": "550e8400-e29b-41d4-a716-446655440099", // Repair center
    "disposition": "repair",
    "generate_label": true,
    "priority": "expedited",
    "notification": "Defective unit requires inspection"
  }
}
```

**Example 3: Distance-Based Routing (Phase 2)**

json

```json
{
  "name": "Route to Nearest Warehouse",
  "priority": 20,
  "conditions": {
    "all": [
      {
        "fact": "order_value",
        "operator": "greaterThan",
        "value": 50
      }
    ]
  },
  "action": {
    "type": "route_nearest_warehouse",
    "disposition": "restock",
    "generate_label": true,
    "max_distance_miles": 500,
    "fallback_warehouse_id": "550e8400-e29b-41d4-a716-446655440001"
  }
}
```

### 7.3 Default Fallback Logic

If no rules match or evaluation errors occur:

json

```json
{
  "destination_warehouse_id": "{tenant.default_warehouse_id}",
  "disposition": "restock",
  "generate_label": true,
  "priority": "standard",
  "matched_rule": null,
  "notes": "Default routing applied - no matching rules"
}
```

---

## 8. OPERATIONS & RELIABILITY

### 8.1 Idempotency Strategy

**Goal:** Ensure duplicate requests return identical responses without re-processing

**Implementation:**

1. **Key Generation:**
    - If client provides `X-Idempotency-Key` header, use that value
    - Otherwise, generate: `SHA256(tenant_id + request_body_json + date)`
    - Date ensures natural expiry (keys unique per day)
2. **Check Before Processing:**
    - Query Cloudflare KV: `idempotency:{key}`
    - If exists: Return cached response immediately (status 200, cached decision)
    - If not exists: Proceed with routing logic
3. **Store After Processing:**
    - Insert into KV with TTL=24 hours
    - Also insert into `routing_decisions` table for audit (permanent)
4. **Edge Cases:**
    - Label generation API called only once (idempotency prevents duplicate labels)
    - Webhook delivery queued only once per decision_id

### 8.2 Rate Limiting

**Per-Tenant Quotas:**

- Basic: 10k requests/hour (sustained), 20k burst (60 seconds)
- Pro: 50k requests/hour, 100k burst
- Enterprise: Custom limits

**Implementation (Cloudflare Worker):**

javascript

````javascript
async function checkRateLimit(tenantId, tier) {
  const window = Math.floor(Date.now() / 3600000); // Hourly window
  const key = `ratelimit:${tenantId}:${window}`;
  
  const current = await KV_RATE_LIMITS.get(key);
  const count = current ? parseInt(current) : 0;
  
  const limit = tier === 'pro' ? 50000 : 10000;
  
  if (count >= limit) {
    throw new RateLimitError(`Exceeded ${limit} requests/hour`);
  }
  
  await KV_RATE_LIMITS.put(key, (count + 1).toString(), { expirationTtl: 3600 });
  return count + 1;
}
```

**Response Headers:**
```
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9847
X-RateLimit-Reset: 1704848400
```

### 8.3 Error Handling & Retries

**Retry Strategy (Webhook Delivery):**
```
Attempt 1: Immediate delivery
Attempt 2: 5 seconds delay (exponential backoff: 5s)
Attempt 3: 25 seconds delay (exponential backoff: 5s * 5)
After 3 failures: Move to Dead Letter Queue
````

**Circuit Breaker (Label API):**

- If EasyPost returns 3 consecutive 5xx errors, open circuit for 60 seconds
- During open state: Skip label generation, return decision without label
- Log incident for manual review

**Compensating Actions:**

- If database write fails after successful label generation:
    - Store label details in Cloudflare Durable Objects as temporary buffer
    - Background job retries database insert every 30s for 10 minutes
    - Alert on-call engineer if still failing

### 8.4 Monitoring & Observability

**Key Metrics (Exported to Cloudflare Analytics):**

- `routing_requests_total` (counter, labels: tenant_id, status_code)
- `routing_latency_ms` (histogram, p50/p95/p99)
- `rule_matches` (counter, labels: tenant_id, rule_id)
- `label_generation_errors` (counter, labels: provider)
- `webhook_delivery_status` (counter, labels: tenant_id, status)

**Structured Logging (JSON format):**

json

````json
{
  "timestamp": "2026-01-09T18:00:01.234Z",
  "level": "INFO",
  "trace_id": "tr_abc123",
  "tenant_id": "tenant_abc",
  "event": "routing_decision",
  "decision_id": "dec_9f8e7d6c5b4a",
  "matched_rule_id": "rule_123",
  "processing_time_ms": 187,
  "label_generated": true
}
```

**Alerting Thresholds:**
- p95 latency > 1000ms for 5 minutes → PagerDuty alert
- Error rate > 5% for 10 minutes → PagerDuty alert
- Webhook delivery failure rate > 10% → Slack alert
- Database connection pool exhaustion → PagerDuty critical

### 8.5 Disaster Recovery

**Backup Strategy:**
- Supabase automated daily backups (7-day retention)
- Weekly full backup to Cloudflare R2 (90-day retention)
- Point-in-time recovery (PITR) enabled on Supabase

**RTO/RPO Targets:**
- RTO (Recovery Time Objective): 1 hour
- RPO (Recovery Point Objective): 5 minutes (Supabase PITR granularity)

**Incident Response Runbook:**
1. **API Degradation:**
   - Check Cloudflare status dashboard
   - Review error logs for patterns (tenant_id, endpoint)
   - Scale Supabase compute if database CPU > 80%
   - Enable maintenance mode if critical (return 503 with Retry-After header)

2. **Database Failure:**
   - Supabase auto-failover to replica (30-60 seconds)
   - If prolonged: Restore from most recent backup to new instance
   - Update DNS/connection strings (5-10 minutes)

3. **Third-Party API Outage (EasyPost):**
   - Circuit breaker auto-activates
   - Routing decisions continue without labels
   - Queue label generation requests for retry when service recovers

---

## 9. SECURITY & COMPLIANCE

### 9.1 Authentication & Authorization

**JWT Token Issuance (Supabase Auth):**
- RS256 asymmetric signing (2048-bit keys)
- Token expiry: 1 hour
- Refresh token: 7 days (rotate on each use)
- Include claims: `tenant_id`, `tier`, `scopes`

**API Key Management:**
- API keys hashed with bcrypt (cost factor 12) before storage
- Key rotation policy: Force rotation every 90 days
- Revocation: Immediate blacklist in Redis/KV, propagates within 60 seconds

**Scopes (Future Enhancement):**
- `routing:write` - Submit routing requests
- `routing:read` - Retrieve decisions
- `admin:rules` - Manage rules
- `admin:webhooks` - Configure webhooks

### 9.2 Data Security

**Encryption at Rest:**
- Supabase Postgres: AES-256 encryption (platform default)
- Cloudflare R2 backups: Server-side encryption (SSE)
- Secrets (API keys, webhook secrets): Supabase Vault with per-tenant encryption keys

**Encryption in Transit:**
- All API endpoints: TLS 1.3 only
- Certificate: Cloudflare Universal SSL (auto-renewed)
- Webhook delivery: Require HTTPS, reject HTTP

**HMAC Webhook Signatures:**
```
Signature-Header: sha256=abc123def456...
Signature calculated as: HMAC-SHA256(request_body, tenant_webhook_secret)
````

**Client Validation Example:**

javascript

````javascript
const crypto = require('crypto');
const receivedSignature = req.headers['x-returns-router-signature'];
const calculatedSignature = 'sha256=' + crypto
  .createHmac('sha256', process.env.WEBHOOK_SECRET)
  .update(req.body)
  .digest('hex');

if (receivedSignature !== calculatedSignature) {
  throw new Error('Invalid signature');
}
```

### 9.3 Compliance Requirements

**SOC 2 Type II Controls (Checklist):**
- [ ] Access controls: MFA for all admin accounts
- [ ] Audit logging: All API requests logged with trace IDs
- [ ] Encryption: At rest and in transit
- [ ] Data retention: Configurable purge policies (default 90 days for logs)
- [ ] Incident response: Documented runbooks, <1 hour RTO
- [ ] Vendor management: Annual security review of Supabase, Cloudflare, EasyPost
- [ ] Change management: All production deploys via CI/CD with approval gates

**GDPR Compliance:**
- Data Processing Agreement (DPA) template for customers
- Data residency: Supabase US-only or EU-only regions (tenant-configurable)
- Right to deletion: API endpoint `DELETE /v1/data-export` (exports all tenant data as JSON, then purges)
- Minimal PII: Only customer ZIP codes stored, no names/emails in routing_decisions table

**PCI DSS (Not Applicable):**
- No credit card data processed or stored
- Payment processing via Stripe (handles PCI compliance)

### 9.4 Moonlighting Risk Mitigation

**Legal Structure:**
- Operate under separate LLC/entity (not personal name)
- Non-solicitation clause: Do not target employer's clients (maintain blacklist)
- Time boundaries: Development outside work hours (evenings/weekends)
- IP assignment: Ensure employment agreement allows side projects (consult attorney)

**Ethical Safeguards:**
- No use of employer code, infrastructure, or proprietary knowledge
- No recruitment of employer colleagues
- Transparent disclosure if employer has conflict-of-interest policy

---

## 10. MONETIZATION & PRICING

### 10.1 Pricing Tiers

**Basic Tier - $299/month**
- 10,000 routing requests/hour
- 5 active routing rules
- Standard webhook delivery (best-effort retries)
- Email support (24-hour response)
- 99.9% uptime SLA
- Community Slack access

**Pro Tier - $999/month**
- 50,000 routing requests/hour
- Unlimited routing rules
- Priority webhook delivery (guaranteed 3 retries)
- Distance-based routing (Mapbox integration)
- Dedicated Slack channel support (4-hour response)
- 99.95% uptime SLA
- Monthly ROI report (cost savings dashboard)

**Enterprise Tier - Custom Pricing (starts $2,500/month)**
- Custom rate limits (100k+ requests/hour)
- Multi-region deployment (US + EU)
- SLA credits for downtime
- Dedicated CSM + technical account manager
- Custom integrations (WMS, ERP connectors)
- Annual contract with volume discounts

### 10.2 Usage-Based Add-Ons

**Label Generation Fee:**
- $0.10 per label generated via EasyPost
- Billed monthly in arrears
- Volume discounts: >10k labels/month = $0.08 per label

**Overage Charges:**
- Requests beyond tier limit: $0.01 per request
- Example: Basic tier customer processes 12k requests in an hour = $20 overage ($0.01 × 2000)

**Custom Rules Development:**
- One-time fee: $500 per complex rule (with consultation)
- Includes testing and documentation

### 10.3 Revenue Model Math

**Path to $10k MRR in 6 Months:**

| Month | New Logos | Tier Mix | MRR | Cumulative MRR |
|-------|-----------|----------|-----|----------------|
| 1 | 2 | 2 × Basic | $598 | $598 |
| 2 | 2 | 1 Basic + 1 Pro | $1,298 | $1,896 |
| 3 | 3 | 2 Basic + 1 Pro | $1,597 | $3,493 |
| 4 | 2 | 2 × Pro | $1,998 | $5,491 |
| 5 | 3 | 1 Basic + 2 Pro | $2,297 | $7,788 |
| 6 | 2 | 1 Pro + 1 Enterprise | $3,499 | $11,287 |

**Assumptions:**
- 0% churn (focus on delivering ROI)
- 50% label generation revenue ($500-$1,000/month average per customer)
- 2-3 new logos per month via cold email + Upwork

**Customer Unit Economics:**
- CAC (Customer Acquisition Cost): $200 (cold email tools + time)
- LTV (Lifetime Value): $14,388 (Pro tier, 12-month average tenure)
- LTV:CAC Ratio: 72:1 (exceptional for B2B SaaS)

### 10.4 Churn Prevention

**Onboarding Playbook (First 30 Days):**
- Day 1: Kickoff call, technical integration guide
- Day 3: First test routing request
- Day 7: Production integration complete
- Day 14: Review first 100 routing decisions, optimize rules
- Day 30: ROI report (time saved, shipping cost reduction)

**Quarterly Business Reviews (Pro+ Tiers):**
- Present cost savings metrics (vs manual processing)
- Discuss new rule opportunities (seasonal patterns)
- Roadmap preview (upcoming features)

**Contract Terms:**
- Monthly contracts (no lock-in) for Basic
- Annual contracts for Pro/Enterprise (10% discount)
- 30-day cancellation notice required

---

## 11. GO-TO-MARKET STRATEGY (STEALTH)

### 11.1 Cold Email Campaigns

**Target List Building:**

**Job Titles:**
- Operations Manager (3PL/Fulfillment)
- VP Operations
- Director of Logistics
- Fulfillment Director
- Warehouse Operations Manager

**Company Identification (Data Sources):**
- LinkedIn Sales Navigator: Filter by industry "Logistics & Supply Chain", employee count 20-200
- BuiltWith: Scrape companies using ShipStation, ShipBob, NetSuite
- Crunchbase: Filter e-commerce aggregators (Thrasio, Perch competitors)
- Google search operators: `"3PL" + "ShipBob" + "careers"` (scrape job postings for tech stack signals)

**List Criteria:**
- Company processes >2k returns/month (estimate from Glassdoor reviews, job posts mentioning "high volume")
- Uses modern tech stack (API integrations visible in job descriptions)
- Funded or profitable (avoid early-stage startups with no budget)

**3-Step Email Sequence:**

**Email 1 - Problem Identification (Day 0)**
```
Subject: Quick Q about [Company] return routing

Hi [FirstName],

Saw [Company] is managing fulfillment for [Brand1, Brand2]—congrats on the growth!

Quick question: How are you currently routing returns between your East and West Coast warehouses? 

Most ops teams I talk to are still using CSVs or manual Slack pings, which works... until it doesn't.

Worth a 15-min chat?

[Your Name]
[Company] | API-First Returns Automation
```

**Email 2 - Proof (Day 3 if no response)**
```
Subject: Re: Quick Q about [Company] return routing

[FirstName],

Following up—figured you might be curious about how [3PL Customer Name] cut their returns processing time from 4 hours/day to 15 minutes.

The short version: We built an API that sits between their marketplace RMAs (Amazon, Shopify) and their WMS. Now routing decisions happen in 200ms instead of 2 hours.

Happy to show you a 5-min demo (no slides, just the API in action).

Calendly: [link]

[Your Name]
```

**Email 3 - Final CTA (Day 7 if no response)**
```
Subject: Last note on returns automation

[FirstName],

Last email, I promise!

If return routing isn't a pain point for [Company] right now, totally understood. But if you're spending even 30 min/day manually deciding "does this go to CA or NJ?"—we can automate that this week.

No long implementation. No UI to learn. Just an API endpoint that returns routing decisions.

Hit reply if you want to see how it works.

Cheers,
[Your Name]
```

**Personalization Tokens:**
- [Company]: Target 3PL name
- [Brand1, Brand2]: Brands they fulfill for (from website/LinkedIn)
- [3PL Customer Name]: Use generic "a West Coast 3PL" if no public case study yet

**Sending Infra:**
- Warm domain: register `returnsrouter.com`, set up SPF/DKIM/DMARC
- Email tool: Instantly.ai or Smartlead.ai (both handle warming + deliverability)
- Volume: 50 emails/day per domain (stay under spam radar)
- Open rate target: >40% (adjust subject lines if lower)
- Reply rate target: >5% (adjust copy if lower)

### 11.2 Upwork Acquisition

**Profile Optimization:**
- **Headline:** "API Integrations for Logistics & E-Commerce | ShipStation, ShipBob, NetSuite"
- **Hourly Rate:** $75-$100 (lower to win first projects, increase after 5-star reviews)
- **Portfolio:** Include screenshot of API documentation, sample routing decision JSON
- **Skills:** API Development, .NET Core, AWS Lambda, Webhook Integrations, E-Commerce Logistics

**Target Job Searches:**
- "RMA automation"
- "ShipStation integration"
- "Returns management API"
- "Warehouse routing logic"
- "Shopify fulfillment automation"

**Proposal Template:**
```
Hi [Client Name],

I see you're looking to automate return routing between your warehouses. I've built exactly this for 3PL clients.

Here's what I'd deliver in 2 weeks:
✅ REST API endpoint that accepts return requests (SKU, customer location, reason)
✅ Rules engine that decides: which warehouse? generate label? restock or liquidate?
✅ Webhook integration to your WMS (ShipStation, custom system, etc.)
✅ API documentation + Postman collection

Quick question before I scope this fully: Are you currently using any specific WMS or fulfillment software? (ShipBob, ShipStation, NetSuite, custom?)

I can show you a working prototype in 48 hours if you want to see it in action before committing.

Looking forward to discussing!
[Your Name]
```

**Conversion to SaaS:**
- After delivering Upwork project, propose: "Instead of maintaining this custom code, I can host it as a managed service for $299/month—you get updates, monitoring, and I handle all infrastructure."
- Conversion rate target: 30% of Upwork projects → recurring customers

### 11.3 Case Study Template (Post-Pilot)

**Structure:**

**Context:**
"[3PL Name] manages fulfillment for 12 D2C brands, processing 8,000 returns/month across 3 US warehouses."

**Problem:**
"Their ops team spent 3-4 hours/day manually routing returns via Slack messages and CSV uploads. High-value items sometimes went to the wrong warehouse, adding 2-3 days of transit time and $15-$30 in unnecessary shipping costs per return."

**Approach:**
"We deployed Returns Router API in 2 weeks. Integration: Shopify webhooks → our routing API → ShipStation task creation. Rules configured: items >$100 route to nearest warehouse, items <$20 refund without return."

**Metrics:**
- ⏱️ Time savings: 3.5 hours/day → 15 min/day (93% reduction)
- 💰 Shipping cost savings: $4,200/month (850 returns × $5 avg savings)
- 📈 Refund speed: 4 days → 1 day (better customer NPS)

**Testimonial:**
"[Returns Router] paid for itself in the first week. We're routing 8k returns/month and haven't touched a CSV in 60 days." – [Operations Manager Name, Company]

**Use Cases:**
- Include in Upwork proposals as "proof pack" attachment
- Embed on landing page (see below)
- Send in cold email follow-ups

### 11.4 Landing Page Wireframe

**URL:** `https://returnsrouter.com`

**Above-Fold (Hero Section):**
```
────────────────────────────────────────────────
Automate Return Routing in 48 Hours

Stop manually deciding where returns go. 
Our API routes returns to the right warehouse in 200ms.

[Start Free Trial] [View API Docs]

✅ Integrates with ShipStation, ShipBob, NetSuite
✅ Rules engine (no code required)
✅ Live in 2 weeks, not 2 months
────────────────────────────────────────────────
```

**Social Proof:**
```
────────────────────────────────────────────────
Trusted by 3PLs Processing 50k+ Returns/Month

"Cut our returns processing time by 93%"
– Operations Manager, [3PL Name]

[Logo 1] [Logo 2] [Logo 3]
────────────────────────────────────────────────
```

**Outcomes (3 Bullets):**
```
────────────────────────────────────────────────
⚡ Route Returns Instantly
No more CSV uploads or Slack pings. API decides: 
which warehouse, generate label, restock or liquidate.

💰 Save $3-$8 Per Return
Intelligent routing to nearest warehouse cuts 
shipping costs by 30-50%.

⏱️ Deploy in Days, Not Months
REST API + webhook integration. 
Live in 2 weeks with our onboarding team.
────────────────────────────────────────────────
```

**CTA:**
```
────────────────────────────────────────────────
[Book 15-Min Demo] or [Read API Docs]
────────────────────────────────────────────────
````

**Footer:**

- Link to API documentation (public, no auth required to view)
- Case studies page
- Pricing page
- Contact email

---

## 12. COMPETITIVE ANALYSIS

### 12.1 Alternatives & Differentiators

|Solution|Description|Weakness|Our Advantage|
|---|---|---|---|
|**Manual Ops**|Slack pings, CSV uploads|Slow (2-4 hours/day), error-prone|Automated, 200ms decisions|
|**Loop Returns**|Full-stack returns portal (UI-heavy)|$2k+/month, 6-month implementation, requires customer-facing UI changes|API-first, $299/month, 2-week deployment, headless|
|**Returnly**|Enterprise returns management|Complex, expensive ($5k+/month), overkill for mid-market 3PLs|Focused on routing logic only, affordable|
|**Zapier/Make**|No-code automation|Can't handle complex rules (multi-step logic, distance calculations), breaks often|Native rules engine, reliable, auditable|
|**In-House Scripts**|Custom Python/Node.js scripts|Brittle (breaks on API changes), no monitoring, high maintenance|Managed service, monitored, auto-updates|
|**ShipStation Built-In**|Basic automation rules|Limited to ShipStation ecosystem, no cross-WMS routing|Works with any WMS/marketplace via API|

### 12.2 Moat & Defensibility

**Proprietary Assets:**

1. **Rule Templates Library:** Pre-built rule sets for common scenarios (apparel vs electronics vs fragile goods)
2. **Warehouse Distance Matrix:** Cached calculations for US ZIP → warehouse routing (updated quarterly)
3. **Cost Models:** Historical data on shipping cost savings per routing decision type
4. **Integration Connectors:** Battle-tested webhooks for 15+ WMS systems (ShipBob, Flexport, ShipStation, NetSuite)

**Network Effects:**

- More customers = more rule variations tested = better default templates
- Aggregate anonymized data on return reasons by product category (insights product opportunity)

**Switching Costs:**

- After 3 months, customers have hundreds of custom rules configured
- Historical audit trail valuable for compliance/reporting
- Muscle memory: Operations teams trust the system's decisions

**Technical Moat:**

- Distance-based routing requires Mapbox API + caching layer (complex to replicate)
- Idempotency guarantees + webhook reliability (subtle but critical for production systems)

---

## 13. BUILD PLAN (45-Day Sprint)

### 13.1 Week-by-Week Breakdown

**Week 1: Foundation (Days 1-7)**

**Deliverables:**

- [ ]  Cloudflare Workers project setup + GitHub repo
- [ ]  Supabase project provisioned (US region)
- [ ]  Database schema implemented (all tables, indexes, RLS policies)
- [ ]  JWT authentication flow (Supabase Auth integration)
- [ ]  Health check endpoint (`GET /v1/health`)

**Success Criteria:**

- Database migrations run successfully
- JWT token issuance works (test with Postman)
- Health endpoint returns 200 OK

**Time Estimate:** 30 hours

---

**Week 2: Core Routing API (Days 8-14)**

**Deliverables:**

- [ ]  `POST /v1/route` endpoint (request validation, JWT auth)
- [ ]  Rules engine implementation (JSON evaluation logic)
- [ ]  Rule matching algorithm (priority ordering, first-match wins)
- [ ]  Idempotency check (Cloudflare KV integration)
- [ ]  Basic error handling (400/401/500 responses)
- [ ]  Audit logging (insert into `routing_decisions` table)

**Success Criteria:**

- Accept valid routing request, return decision JSON
- Test with 3 hardcoded rules (low-value refund, defective to repair, default restock)
- Duplicate requests return cached response
- All requests logged to database

**Time Estimate:** 35 hours

---

**Week 3: Label Generation & Webhooks (Days 15-21)**

**Deliverables:**

- [ ]  EasyPost API integration (create shipment, buy label)
- [ ]  Label generation logic (conditional based on rule action)
- [ ]  Webhook delivery system (Cloudflare Queue + dispatcher worker)
- [ ]  HMAC signature generation for webhooks
- [ ]  Retry logic with exponential backoff (3 attempts)
- [ ]  Dead letter queue for failed webhooks

**Success Criteria:**

- Generate USPS label for test return
- Webhook successfully delivered to RequestBin (test URL)
- Failed webhook retries 3 times, then moves to DLQ
- Latency p95 < 800ms (including label generation)

**Time Estimate:** 30 hours

---

**Week 4: Admin UI & Configuration (Days 22-28)**

**Deliverables:**

- [ ]  Vue.js admin console (minimal UI)
    - Rules management page (CRUD operations)
    - Warehouses management page
    - Tenant settings (webhook URL, API key display)
- [ ]  API endpoints for admin:
    - `GET /v1/rules`, `POST /v1/rules`, `PUT /v1/rules/:id`, `DELETE /v1/rules/:id`
    - `GET /v1/warehouses`, `POST /v1/warehouses`, etc.
- [ ]  Rate limiting implementation (Cloudflare KV)
- [ ]  Monitoring setup (Cloudflare Analytics, basic dashboards)

**Success Criteria:**

- Create new rule via UI, see it apply to routing requests
- Add new warehouse, see it available for routing
- Rate limit triggers after 10k requests (Basic tier test)
- View request count and latency metrics in Cloudflare dashboard

**Time Estimate:** 35 hours

Continue

12:24 AM

---

**Week 5: Testing & Documentation (Days 29-35)**

**Deliverables:**

- [ ]  API documentation (OpenAPI/Swagger spec)
- [ ]  Postman collection with example requests
- [ ]  Integration testing (end-to-end flow: request → decision → webhook delivery)
- [ ]  Load testing (simulate 1000 req/min with k6 or Artillery)
- [ ]  Security review (JWT validation, HMAC, secrets storage)
- [ ]  Onboarding guide (PDF for pilot customers)

**Success Criteria:**

- API docs published at `https://docs.returnsrouter.com`
- Load test achieves p95 < 500ms at 1000 req/min
- Zero security vulnerabilities in manual review
- Onboarding guide reviewed by non-technical user (understandable)

**Time Estimate:** 25 hours

---

**Week 6: Pilot Customer Onboarding (Days 36-42)**

**Deliverables:**

- [ ]  Identify pilot customer (Upwork project or cold email lead)
- [ ]  Kickoff call (understand their workflow, configure rules)
- [ ]  Production integration (connect their Shopify/WMS → our API)
- [ ]  Monitor first 100 routing decisions (review logs for errors)
- [ ]  Optimization: Adjust rules based on real-world patterns
- [ ]  Collect feedback and testimonial

**Success Criteria:**

- Pilot customer processes >100 returns via API in first week
- Zero production incidents (no 500 errors, no missed webhooks)
- Customer reports time savings (document quantified metrics)
- Testimonial collected for case study

**Time Estimate:** 20 hours (support + iteration)

---

**Week 7: Polish & Launch Prep (Days 43-45)**

**Deliverables:**

- [ ]  Landing page live (returnsrouter.com)
- [ ]  Pricing page published
- [ ]  Stripe integration for self-service signup (optional, can start with manual invoicing)
- [ ]  Cold email campaigns configured (Instantly.ai, first 100 prospects loaded)
- [ ]  Upwork profile optimized + first proposals sent
- [ ]  Case study published (pilot customer)

**Success Criteria:**

- Landing page loads in <2 seconds
- Cold email tool configured, first batch scheduled
- 5 Upwork proposals submitted
- All systems monitored and stable

**Time Estimate:** 15 hours

---

### 13.2 Total Time Investment

**Total Hours:** ~190 hours (~5 weeks full-time, or 7 weeks part-time at 25 hours/week)

**Contingency:** Add 10 hours for unexpected debugging/rewrites

**Grand Total:** 200 hours (achievable in 6-8 weeks part-time)

---

## 14. PILOT CHECKLIST

### 14.1 Pre-Launch Validation

Before approaching first customer:

- [ ]  All API endpoints return 200 OK for valid requests
- [ ]  Error responses include helpful messages (not generic "Internal Error")
- [ ]  Idempotency tested (duplicate requests return identical responses)
- [ ]  Rate limiting enforced (test account hits limit, returns 429)
- [ ]  Webhook delivery tested with RequestBin (success + retry scenarios)
- [ ]  Label generation tested (real EasyPost account, generate 5 test labels)
- [ ]  Load tested (achieve 1000 req/min with p95 < 500ms)
- [ ]  Security checklist completed (JWT validation, HMAC, encrypted secrets)
- [ ]  Documentation published and reviewed by external reader
- [ ]  Monitoring alerts configured (latency, error rate, webhook failures)

### 14.2 Pilot Customer Criteria

**Ideal Pilot Profile:**

- Processing 1k-5k returns/month (enough volume to prove value, not overwhelming)
- Currently using manual process (high pain, clear "before" state)
- Has technical resource available (can integrate webhook in 1-2 days)
- Willing to give feedback and testimonial
- Budget authority to convert to paid customer ($299-$999/month)

**Red Flags (Avoid for Pilot):**

- Enterprise with long procurement cycles (will delay feedback loop)
- Startup with <100 returns/month (insufficient data to prove ROI)
- Customer unwilling to share metrics (can't build case study)

### 14.3 Success Metrics (30-Day Pilot)

**Quantitative:**

- [ ]  >500 routing requests processed
- [ ]  0 production incidents (5xx errors, data loss)
- [ ]  p95 latency < 500ms maintained
- [ ]  >95% webhook delivery success rate
- [ ]  Customer reports >2 hours/week time savings

**Qualitative:**

- [ ]  Customer describes product as "essential" or "can't live without"
- [ ]  Customer refers another 3PL/fulfillment center
- [ ]  Customer requests additional features (proof of engagement)

**Conversion:**

- [ ]  Customer signs annual contract OR commits to 3-month paid trial
- [ ]  Testimonial collected with specific metrics
- [ ]  Permission granted to use logo and case study publicly

---

## 15. RISKS & MITIGATION

### 15.1 Technical Risks

|Risk|Probability|Impact|Mitigation|
|---|---|---|---|
|**EasyPost API downtime**|Medium|High|Implement circuit breaker, fallback to manual label generation|
|**Database connection exhaustion**|Low|High|Use Supabase connection pooler (PgBouncer), monitor pool usage|
|**Cloudflare Workers cold starts**|Low|Medium|Acceptable (<50ms cold start), monitor p99 latency|
|**Rules engine bugs (incorrect routing)**|Medium|Critical|Extensive unit tests, dry-run mode for new rules|
|**Webhook delivery failures (customer endpoint down)**|High|Medium|Retry logic + DLQ, alert customer via email|

### 15.2 Business Risks

|Risk|Probability|Impact|Mitigation|
|---|---|---|---|
|**Low conversion from Upwork to recurring**|Medium|High|Provide 30-day free trial post-project, demonstrate ongoing value|
|**Cold email spam complaints**|Low|Medium|Keep volume <50/day per domain, use warm domains, honor unsubscribes|
|**Customer churn (doesn't see ROI)**|Medium|High|Proactive onboarding, monthly ROI reports, quarterly business reviews|
|**Competitor launches similar product**|Low|Medium|Focus on customer success and switching costs (rule libraries, integrations)|
|**Marketplace API changes break integrations**|Medium|Medium|Monitor API changelog, version endpoints, notify customers of required updates|

### 15.3 Legal/Compliance Risks

|Risk|Probability|Impact|Mitigation|
|---|---|---|---|
|**Moonlighting conflict with employer**|Low|Critical|Separate LLC, no employer client solicitation, legal review of employment contract|
|**GDPR violation (customer data)**|Low|High|Minimal PII collection, DPA template, data residency options|
|**Service outage causing customer financial loss**|Low|High|SLA with liability caps ($500-$5,000 max), errors & omissions insurance|

---

## 16. SUCCESS CRITERIA & KPIs

### 16.1 Phase 1 (Months 1-3): MVP Launch

**Technical:**

- [ ]  99.9% uptime achieved
- [ ]  p95 latency < 500ms maintained
- [ ]  Zero data loss incidents

**Customer:**

- [ ]  3-5 paying customers onboarded
- [ ]  $1,500-$3,000 MRR
- [ ]  1 detailed case study published

**GTM:**

- [ ]  300+ cold emails sent (>5% reply rate)
- [ ]  20+ Upwork proposals submitted (>15% win rate)
- [ ]  Landing page live with >40% organic conversion (demo requests/trial signups)

### 16.2 Phase 2 (Months 4-6): Scale

**Technical:**

- [ ]  Distance-based routing live (Mapbox integration)
- [ ]  Analytics dashboard launched (Vue.js)
- [ ]  3 new WMS integrations added (customer-driven)

**Customer:**

- [ ]  8-12 paying customers
- [ ]  $8,000-$12,000 MRR
- [ ]  3 customer referrals generated
- [ ]  1 Pro tier customer ($999/month)

**GTM:**

- [ ]  2nd cold email domain warmed and active
- [ ]  Upwork Top Rated status achieved (>90% JSS, 10+ projects)
- [ ]  First inbound demo request from content/SEO

### 16.3 Long-Term (Month 12+)

**Technical:**

- [ ]  Multi-region deployment (US + EU)
- [ ]  SOC 2 Type II certification achieved
- [ ]  API uptime >99.95%

**Customer:**

- [ ]  25-30 paying customers
- [ ]  $25,000-$35,000 MRR
- [ ]  <5% monthly churn
- [ ]  2-3 Enterprise customers ($2,500+/month)

**Business:**

- [ ]  Consider full-time transition OR sell to strategic acquirer (3PL, fulfillment tech company)
- [ ]  Gross margin >85%
- [ ]  Operating profit >50% (after infrastructure costs)

---

## 17. APPENDICES

### Appendix A: Example Webhook Payload

**Sent to Customer Endpoint:**

json

```json
POST https://customer-wms.com/api/returns/inbound
Headers:
  Content-Type: application/json
  X-Returns-Router-Signature: sha256=abc123def456...
  X-Returns-Router-Delivery-ID: del_xyz789

Body:
{
  "event": "routing.decision",
  "timestamp": "2026-01-09T18:00:01.234Z",
  "decision_id": "dec_9f8e7d6c5b4a",
  "return": {
    "return_id": "RMA-2026-001234",
    "order_id": "ORD-98765",
    "sku": "SHOE-RUN-42-BLK"
  },
  "routing": {
    "destination_warehouse_id": "550e8400-e29b-41d4-a716-446655440001",
    "warehouse_code": "WH-CA-01",
    "disposition": "restock",
    "priority": "standard"
  },
  "label": {
    "url": "https://easypost-files.s3.amazonaws.com/labels/abc123.pdf",
    "tracking": "9400123456789",
    "carrier": "USPS"
  }
}
```

### Appendix B: Rule JSON Schema

json

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["name", "priority", "conditions", "action"],
  "properties": {
    "name": {
      "type": "string",
      "maxLength": 255
    },
    "description": {
      "type": "string"
    },
    "priority": {
      "type": "integer",
      "minimum": 1
    },
    "active": {
      "type": "boolean",
      "default": true
    },
    "conditions": {
      "type": "object",
      "properties": {
        "all": { "type": "array" },
        "any": { "type": "array" }
      }
    },
    "action": {
      "type": "object",
      "required": ["type"],
      "properties": {
        "type": {
          "enum": ["route_warehouse", "refund_no_return", "route_repair", "liquidate"]
        },
        "warehouse_id": { "type": "string", "format": "uuid" },
        "disposition": {
          "enum": ["restock", "repair", "liquidate", "discard", "customer_keep"]
        },
        "generate_label": { "type": "boolean" },
        "priority": {
          "enum": ["standard", "expedited"]
        }
      }
    }
  }
}
```

### Appendix C: OpenAPI Spec Excerpt

yaml

```yaml
openapi: 3.0.0
info:
  title: Returns Router API
  version: 1.0.0
  description: API-first returns routing and disposition engine

servers:
  - url: https://api.returnsrouter.com/v1

paths:
  /route:
    post:
      summary: Submit return for routing decision
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/RoutingRequest'
      responses:
        '200':
          description: Routing decision returned
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/RoutingDecision'
        '400':
          $ref: '#/components/responses/BadRequest'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '429':
          $ref: '#/components/responses/RateLimitExceeded'

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
  
  schemas:
    RoutingRequest:
      type: object
      required: [return_id, sku, order_value, customer]
      properties:
        return_id:
          type: string
        sku:
          type: string
        order_value:
          type: number
          format: float
        customer:
          type: object
          properties:
            zip:
              type: string
              pattern: '^\d{5}$'
    
    RoutingDecision:
      type: object
      properties:
        decision_id:
          type: string
        routing:
          type: object
        label:
          type: object
```

---

## CONCLUSION

This PRD provides a complete specification for building and launching Returns Router (RMA Logic Engine) as a serverless, API-first B2B SaaS product. The architecture prioritizes simplicity (Cloudflare Workers + Supabase), fast time-to-market (45-day build plan), and stealth GTM (cold email + Upwork).

**Next Steps:**

1. Review and approve this PRD
2. Set up development environment (Cloudflare + Supabase accounts)
3. Begin Week 1 implementation (database schema)
4. Identify pilot customer target list (Upwork + LinkedIn scraping)

**Key Success Factors:**

- Ship fast, iterate based on real customer feedback
- Focus on API reliability and latency (core value prop)
- Build case study momentum (every customer generates referrals)
- Maintain operational discipline (monitoring, runbooks, security)

**Contact for Questions:**

- Technical: [Your Email]
- Business: [Your Email]

---

**Document Version Control:**

- v1.0 (2026-01-09): Initial PRD based on Returns Router concept and technical requirements