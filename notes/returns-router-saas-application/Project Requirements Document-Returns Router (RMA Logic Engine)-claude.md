Project Requirements Document: Returns Router (RMA Logic Engine)
Version: 1.0
Date: January 9, 2026
Project Owner: [Your Name/Entity]
Status: Planning Phase

1. EXECUTIVE SUMMARY
1.1 Project Overview
Returns Router (RMA Logic Engine) is an API-first, headless decision engine that automates the routing of e-commerce returns to optimal destinations (warehouses, repair centers, liquidation facilities, or direct refund without return). The system eliminates manual decision-making, reduces shipping costs, and accelerates refund cycles for 3PLs and e-commerce aggregators.
1.2 Business Objectives

Launch MVP within 45 days with one paying pilot customer
Achieve $10k MRR within 6 months (10-12 logos)
Reduce customer returns processing time by 75% (from 4 hours to <1 hour per batch)
Cut unnecessary return shipping costs by 30-40% through intelligent routing
Provide sub-second routing decisions to maintain customer experience

1.3 Success Metrics

API response time: p95 < 500ms, p99 < 1000ms
Routing accuracy: >95% (correct destination based on rules)
System uptime: 99.5% (Basic tier), 99.9% (Pro tier)
Customer onboarding: <5 days from contract to first routed return
Pilot customer ROI: >$2,000/month in cost savings documented


2. PROBLEM STATEMENT & MARKET OPPORTUNITY
2.1 Core Problem
When a customer initiates an e-commerce return, the destination decision (which warehouse, repair center, or whether to issue refund without return) is currently:

Hardcoded in legacy systems with no flexibility
Manual via CSVs and email, taking 2-4 hours per batch of returns
Suboptimal resulting in cross-country shipping when a closer facility exists
Inconsistent with different CSRs applying different logic

Cost Impact: A 3PL processing 500 returns/day with average $8 return shipping × 30% suboptimal routing = $36,000/month in unnecessary shipping costs.
2.2 Target Customer Profile (ICP)
Primary: Third-Party Logistics Providers (3PLs)

Company size: 50-500 employees, $10M-$100M revenue
Return volume: 5,000-50,000 returns/month
Tech stack: ShipBob, Flexport, ShipStation, custom WMS
Pain severity: High (direct P&L impact)
Decision maker: Director of Operations, VP Supply Chain

Secondary: E-commerce Aggregators/Operators

Portfolio: 5-20 brands under management
Infrastructure: Centralized ops team, multiple warehouse locations
Tech stack: Shopify Plus, custom fulfillment systems
Pain severity: Medium-High (efficiency across brands)
Decision maker: Head of Operations, COO

2.3 Market Opportunity

US 3PL market: 20,000+ providers, 2,000 in target segment
E-commerce return rate: 20-30% and growing (2024-2025 trends)
Total addressable returns: 5B+ packages/year in US alone
Existing solutions: Enterprise only ($50k+ contracts) or no automation


3. SOLUTION ARCHITECTURE
3.1 System Architecture Diagram
┌─────────────────────────────────────────────────────────────────┐
│                         INGRESS LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  [Marketplace APIs]     [Customer Portal]     [CSV Upload]       │
│   Amazon RMA API         Shopify Webhooks     S3 Trigger         │
│         │                      │                    │            │
│         └──────────────────────┴────────────────────┘            │
│                              │                                   │
│                    [API Gateway + WAF]                           │
│                    JWT/OAuth2 + Rate Limit                       │
│                              │                                   │
└──────────────────────────────┼───────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────┐
│                       PROCESSING LAYER                           │
├──────────────────────────────┼───────────────────────────────────┤
│                              │                                   │
│                    [SQS: Returns Queue]                          │
│                  Deduplication + DLQ                             │
│                              │                                   │
│                ┌─────────────┴─────────────┐                    │
│                │                           │                    │
│         [Lambda: Validator]      [Lambda: Enricher]             │
│         Schema + Rules           Distance/SKU/Condition         │
│                │                           │                    │
│                └─────────────┬─────────────┘                    │
│                              │                                   │
│                  [Lambda: Rules Engine]                          │
│              Python JSON Logic Evaluator                         │
│                 + Distance Matrix API                            │
│                              │                                   │
│                    [DynamoDB: State]                             │
│                 Decisions + Audit Trail                          │
│                              │                                   │
└──────────────────────────────┼───────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────┐
│                         EGRESS LAYER                             │
├──────────────────────────────┼───────────────────────────────────┤
│                              │                                   │
│              ┌───────────────┼───────────────┐                  │
│              │               │               │                  │
│       [WMS Webhook]   [Label Generator]  [Notification]         │
│       ShipBob API      EasyPost/Shippo    SNS → Email/Slack     │
│              │               │               │                  │
│       [Warehouse Task] [Return Label]  [Customer Update]        │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│                    CONFIGURATION & OBSERVABILITY                  │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  [RDS: Tenant Config]         [CloudWatch Logs/Metrics]          │
│  Rules, Mappings, Secrets     Traces, Alarms, Dashboards         │
│                                                                   │
│  [S3: Rule Versions]          [X-Ray: Distributed Tracing]       │
│  Backup + Audit               End-to-end Request Flow            │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
3.2 Data Flow (Numbered Steps)
Step 1: Return Initiation

Customer initiates return via marketplace (Amazon, Shopify) or customer portal
Return request contains: order_id, sku, quantity, reason_code, customer_location, item_condition

Step 2: Ingress & Authentication

Request hits API Gateway with JWT token (scope: returns:route)
WAF validates source IP, rate limits applied (100 req/min per tenant)
Idempotency key checked: SHA256(tenant_id + order_id + sku + timestamp_day)

Step 3: Queueing & Deduplication

Valid requests enqueued to SQS with message deduplication enabled
DLQ configured for failed messages (3 retries with exponential backoff)
Message attributes: tenant_id, priority (standard vs expedited), trace_id

Step 4: Validation

Lambda pulls from SQS, validates payload against JSON schema
Checks: SKU exists in tenant catalog, reason_code is valid, customer_zip is US format
Enrichment: Fetch SKU cost, weight, category from tenant's product catalog cache

Step 5: Geospatial Enrichment

Lambda calls Distance Matrix API (Google Maps or Mapbox)
Input: customer_zip, tenant's warehouse locations array
Output: distances in miles, estimated shipping costs per destination

Step 6: Rules Evaluation

Lambda loads tenant's routing ruleset from DynamoDB (cached 5 min TTL)
Evaluates rules in priority order using JSON Logic library
Example evaluation context:

json{
  "sku_cost": 18.50,
  "sku_category": "electronics",
  "customer_zip": "90210",
  "reason_code": "defective",
  "condition": "damaged",
  "nearest_warehouse": "CA-01",
  "distance_miles": 45,
  "estimated_shipping": 6.80
}
Step 7: Decision Output

Rules engine returns decision object:

json{
  "decision_id": "dec_7x9k2m",
  "action": "route_to_warehouse",
  "destination": {
    "warehouse_id": "CA-01",
    "name": "Los Angeles Distribution Center",
    "address": "..."
  },
  "generate_label": true,
  "refund_immediately": false,
  "disposition": "inspect_and_restock",
  "reasoning": "Rule: low_value_nearby applied (cost < $20, distance < 50mi)"
}
```

**Step 8: Label Generation (if required)**
- If `generate_label: true`, Lambda calls carrier API (EasyPost/Shippo)
- Inputs: customer address, warehouse address, package dimensions, carrier preference
- Output: `label_url`, `tracking_number`, `estimated_cost`

**Step 9: State Persistence**
- Decision written to DynamoDB `returns_decisions` table with TTL (90 days)
- Audit log written to CloudWatch Logs with structured JSON
- Metrics emitted: `decision_latency_ms`, `rule_applied`, `destination_selected`

**Step 10: Egress Notification**
- EventBridge publishes `ReturnRouted` event
- Subscribers:
  - WMS webhook (creates warehouse task)
  - Customer notification service (email/SMS with tracking)
  - Tenant's Slack channel (for high-value items)

**Step 11: Response to Client**
- API Gateway returns synchronous response (if real-time API call)
- Or webhook callback sent to tenant's endpoint (if async batch)

### 3.3 Deployment Architecture

**AWS Services Mapping:**

- **API Gateway (REST API):** Primary ingress, handles authentication, throttling, CORS
- **Lambda Functions:**
  - `returns-validator` (.NET Core 8, 512MB, timeout 10s)
  - `returns-enricher` (.NET Core 8, 1GB, timeout 15s for API calls)
  - `returns-engine` (Python 3.11, 1GB, timeout 20s for complex rules)
  - `label-generator` (.NET Core 8, 512MB, timeout 30s for carrier APIs)
- **SQS:** Standard queue with content-based deduplication, 14-day retention
- **DynamoDB:**
  - `returns_decisions` (partition: tenant_id, sort: decision_id)
  - `tenant_rules` (partition: tenant_id, sort: version)
  - `routing_cache` (TTL enabled, 5-minute caching)
- **RDS (PostgreSQL 15):** Tenant configuration, mappings, catalog data (Multi-AZ)
- **S3:** Rule version backups, audit logs archive, CSV upload staging
- **EventBridge:** Event bus for async notifications and integrations
- **CloudWatch:** Logs, metrics, dashboards, alarms
- **X-Ray:** Distributed tracing across Lambda functions
- **Secrets Manager:** Tenant API keys, carrier credentials, OAuth tokens
- **KMS:** Encryption keys for data at rest

**Why Lambda over ECS:**
- Variable load (spiky during business hours, low overnight)
- Sub-second cold start acceptable for async queue processing
- Lower operational overhead for MVP
- Easy to migrate hot paths to ECS Fargate if sustained throughput requires it

### 3.4 Multi-Tenant Isolation Strategy

**Data Isolation:**
- All tables include `tenant_id` as partition key or indexed attribute
- DynamoDB: Partition by `tenant_id`, row-level isolation enforced in query patterns
- RDS: Separate schemas per tenant (`tenant_abc.rules`, `tenant_xyz.rules`) or single schema with mandatory `tenant_id` in WHERE clauses
- S3: Bucket structure `s3://returns-router-prod/tenant-{id}/...`

**Compute Isolation:**
- Shared Lambda functions with per-tenant concurrency limits
- SQS: Separate queues per tenant tier (Basic = shared, Pro = dedicated)
- API Gateway: Per-tenant rate limits and quotas tracked via usage plans

**Secrets Isolation:**
- AWS Secrets Manager: Keys named `prod/tenant-{id}/wms-api-key`
- Lambda execution role has policy restricting access to tenant's own secrets
- Secrets rotated every 90 days via automated Lambda rotation function

**Network Isolation:**
- VPC deployment for RDS and sensitive workloads
- Security groups restrict Lambda → RDS traffic to specific ports
- NAT Gateway for external API calls (carrier APIs, distance matrix)

---

## 4. LOW-LEVEL DESIGN

### 4.1 Data Model

#### 4.1.1 DynamoDB Tables

**Table: `returns_decisions`**
```
Partition Key: tenant_id (String)
Sort Key: decision_id (String)
Attributes:
  - order_id: String (GSI partition key for lookup by order)
  - sku: String
  - sku_cost: Number
  - sku_category: String
  - customer_zip: String
  - reason_code: String (defective, unwanted, wrong_item, damaged)
  - condition: String (new, opened, damaged, defective)
  - decision: Map {
      action: String (route_to_warehouse, refund_no_return, route_to_repair, liquidate)
      destination_id: String
      destination_name: String
      reasoning: String
    }
  - label_url: String (nullable)
  - tracking_number: String (nullable)
  - estimated_shipping_cost: Number
  - distance_miles: Number
  - rule_applied: String (rule_id that matched)
  - created_at: String (ISO8601)
  - ttl: Number (Unix timestamp, 90 days)
  - idempotency_key: String (GSI for dedup)
  - trace_id: String (for debugging)

Indexes:
  - GSI: order_id → decision_id (for customer lookups)
  - GSI: idempotency_key → decision_id (for dedup checks)
  - LSI: created_at (for time-based queries)
```

**Table: `tenant_rules`**
```
Partition Key: tenant_id (String)
Sort Key: version (Number, auto-increment)
Attributes:
  - ruleset: List<Map> [
      {
        rule_id: String (uuid)
        priority: Number (1 = highest)
        enabled: Boolean
        condition: Map (JSON Logic expression)
        action: Map {
          type: String
          parameters: Map
        }
        description: String
      }
    ]
  - active: Boolean (only one version can be active)
  - created_by: String (user_id)
  - created_at: String (ISO8601)
  - updated_at: String (ISO8601)
Example Ruleset JSON:
json[
  {
    "rule_id": "rule_001",
    "priority": 1,
    "enabled": true,
    "condition": {
      "and": [
        {"<": [{"var": "sku_cost"}, 20]},
        {"in": [{"var": "reason_code"}, ["unwanted", "wrong_item"]]}
      ]
    },
    "action": {
      "type": "refund_no_return",
      "parameters": {
        "reason": "Low value item, not worth return shipping"
      }
    },
    "description": "Refund items under $20 for non-defect returns"
  },
  {
    "rule_id": "rule_002",
    "priority": 2,
    "enabled": true,
    "condition": {
      "and": [
        {">=": [{"var": "sku_cost"}, 100]},
        {"==": [{"var": "condition"}, "defective"]}
      ]
    },
    "action": {
      "type": "route_to_repair",
      "parameters": {
        "destination_id": "REPAIR-01"
      }
    },
    "description": "High-value defective items go to repair center"
  },
  {
    "rule_id": "rule_003",
    "priority": 3,
    "enabled": true,
    "condition": {
      "<": [{"var": "distance_miles"}, 100]
    },
    "action": {
      "type": "route_to_warehouse",
      "parameters": {
        "destination_id": "nearest",
        "generate_label": true
      }
    },
    "description": "Route to nearest warehouse if within 100 miles"
  },
  {
    "rule_id": "rule_fallback",
    "priority": 999,
    "enabled": true,
    "condition": true,
    "action": {
      "type": "route_to_warehouse",
      "parameters": {
        "destination_id": "HQ-01",
        "generate_label": true
      }
    },
    "description": "Default: route to HQ warehouse"
  }
]
```

**Table: `routing_cache`**
```
Partition Key: cache_key (String, format: "dist:{from_zip}:{to_zip}")
Sort Key: N/A
Attributes:
  - distance_miles: Number
  - estimated_cost: Number
  - carrier: String
  - service_level: String (ground, 2day, etc)
  - ttl: Number (5 minutes)
4.1.2 RDS (PostgreSQL) Tables
Table: tenants
sqlCREATE TABLE tenants (
  tenant_id VARCHAR(50) PRIMARY KEY,
  company_name VARCHAR(255) NOT NULL,
  tier VARCHAR(20) NOT NULL CHECK (tier IN ('basic', 'pro', 'enterprise')),
  status VARCHAR(20) NOT NULL CHECK (status IN ('active', 'suspended', 'trial')),
  api_key_hash VARCHAR(255) NOT NULL,
  rate_limit_per_min INTEGER DEFAULT 100,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_tenants_status ON tenants(status);
Table: warehouses
sqlCREATE TABLE warehouses (
  warehouse_id VARCHAR(50) PRIMARY KEY,
  tenant_id VARCHAR(50) REFERENCES tenants(tenant_id),
  name VARCHAR(255) NOT NULL,
  address_line1 VARCHAR(255),
  city VARCHAR(100),
  state VARCHAR(2),
  zip VARCHAR(10),
  country VARCHAR(2) DEFAULT 'US',
  lat DECIMAL(10, 8),
  lng DECIMAL(11, 8),
  active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_warehouses_tenant ON warehouses(tenant_id);
Table: product_catalog
sqlCREATE TABLE product_catalog (
  sku VARCHAR(100),
  tenant_id VARCHAR(50) REFERENCES tenants(tenant_id),
  name VARCHAR(255),
  cost DECIMAL(10, 2),
  category VARCHAR(100),
  weight_oz DECIMAL(8, 2),
  dimensions_inches VARCHAR(50), -- "10x8x6"
  active BOOLEAN DEFAULT TRUE,
  PRIMARY KEY (tenant_id, sku)
);

CREATE INDEX idx_catalog_sku ON product_catalog(sku);
Table: wms_endpoints
sqlCREATE TABLE wms_endpoints (
  endpoint_id VARCHAR(50) PRIMARY KEY,
  tenant_id VARCHAR(50) REFERENCES tenants(tenant_id),
  wms_type VARCHAR(50) NOT NULL, -- 'shipbob', 'flexport', 'custom'
  base_url VARCHAR(255) NOT NULL,
  auth_type VARCHAR(20) CHECK (auth_type IN ('api_key', 'oauth2', 'basic')),
  credentials_secret_arn VARCHAR(255) NOT NULL, -- AWS Secrets Manager ARN
  webhook_url VARCHAR(255),
  active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP DEFAULT NOW()
);
Table: audit_logs
sqlCREATE TABLE audit_logs (
  log_id BIGSERIAL PRIMARY KEY,
  tenant_id VARCHAR(50) REFERENCES tenants(tenant_id),
  event_type VARCHAR(50) NOT NULL, -- 'rule_updated', 'decision_made', 'api_call'
  actor VARCHAR(100), -- user_id or 'system'
  resource_id VARCHAR(100),
  details JSONB,
  created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_audit_tenant_time ON audit_logs(tenant_id, created_at DESC);
4.2 API Surface
4.2.1 Core Routing API
Endpoint: POST /v1/route
Authentication: Bearer JWT token in Authorization header
Rate Limit: 100 req/min (Basic), 500 req/min (Pro)
Idempotency: Idempotency-Key header (SHA256 hash, 24-hour window)
Request:
json{
  "order_id": "ORD-2024-12345",
  "sku": "WIDGET-PRO-001",
  "quantity": 1,
  "reason_code": "defective",
  "condition": "damaged",
  "customer": {
    "zip": "90210",
    "country": "US"
  },
  "metadata": {
    "marketplace": "amazon",
    "rma_id": "RMA-2024-67890",
    "order_date": "2024-12-15T10:30:00Z"
  }
}
Response (200 OK):
json{
  "decision_id": "dec_7x9k2m",
  "order_id": "ORD-2024-12345",
  "sku": "WIDGET-PRO-001",
  "action": "route_to_warehouse",
  "destination": {
    "warehouse_id": "CA-01",
    "name": "Los Angeles Distribution Center",
    "address": {
      "line1": "123 Logistics Way",
      "city": "Los Angeles",
      "state": "CA",
      "zip": "90001"
    }
  },
  "label": {
    "url": "https://ship.easypost.com/label/abc123.pdf",
    "tracking_number": "1Z999AA10123456784",
    "carrier": "UPS",
    "service": "Ground",
    "cost": 6.85
  },
  "disposition": "inspect_and_restock",
  "refund_immediately": false,
  "reasoning": "Applied rule_003: Route to nearest warehouse (45 miles)",
  "estimated_processing_days": 3,
  "created_at": "2026-01-09T15:42:10Z"
}
Response (400 Bad Request):
json{
  "error": {
    "code": "INVALID_SKU",
    "message": "SKU 'WIDGET-PRO-001' not found in tenant catalog",
    "details": {
      "tenant_id": "tenant_abc",
      "sku": "WIDGET-PRO-001"
    }
  }
}
Response (409 Conflict - Duplicate):
json{
  "error": {
    "code": "DUPLICATE_REQUEST",
    "message": "Request already processed",
    "existing_decision_id": "dec_7x9k2m"
  }
}
4.2.2 Batch Routing API
Endpoint: POST /v1/route/batch
Request:
json{
  "returns": [
    {
      "order_id": "ORD-001",
      "sku": "WIDGET-001",
      "reason_code": "unwanted",
      "condition": "new",
      "customer": {"zip": "90210"}
    },
    {
      "order_id": "ORD-002",
      "sku": "WIDGET-002",
      "reason_code": "defective",
      "condition": "damaged",
      "customer": {"zip": "10001"}
    }
  ]
}
Response (202 Accepted):
json{
  "batch_id": "batch_xyz789",
  "status": "processing",
  "total_items": 2,
  "estimated_completion_seconds": 15,
  "status_url": "/v1/route/batch/batch_xyz789"
}
4.2.3 Rules Management API
Endpoint: GET /v1/rules
Authentication: Bearer JWT with rules:read scope
Response:
json{
  "tenant_id": "tenant_abc",
  "version": 5,
  "active": true,
  "rules": [
    {
      "rule_id": "rule_001",
      "priority": 1,
      "enabled": true,
      "description": "Refund items under $20",
      "condition": {"<": [{"var": "sku_cost"}, 20]},
      "action": {
        "type": "refund_no_return"
      }
    }
  ],
  "created_at": "2026-01-05T10:00:00Z",
  "updated_at": "2026-01-08T14:20:00Z"
}
Endpoint: PUT /v1/rules
Authentication: Bearer JWT with rules:write scope
Request:
json{
  "rules": [
    {
      "rule_id": "rule_001",
      "priority": 1,
      "enabled": true,
      "description": "Refund low-value items",
      "condition": {
        "and": [
          {"<": [{"var": "sku_cost"}, 25]},
          {"!=": [{"var": "reason_code"}, "defective"]}
        ]
      },
      "action": {
        "type": "refund_no_return"
      }
    }
  ]
}
Response (200 OK):
json{
  "version": 6,
  "status": "active",
  "message": "Ruleset updated successfully",
  "updated_at": "2026-01-09T16:00:00Z"
}
4.2.4 Webhook Configuration API
Endpoint: POST /v1/webhooks
Purpose: Register endpoints to receive ReturnRouted events
Request:
json{
  "url": "https://wms.customer.com/api/returns/webhook",
  "events": ["return.routed", "return.label_generated"],
  "auth": {
    "type": "hmac_sha256",
    "secret": "whsec_abc123..."
  }
}
4.3 Validation Rules
SKU Validation:

Must exist in product_catalog for tenant
Must have valid cost (non-null, > 0)
Must have category (for rule evaluation)

Customer Location Validation:

zip: US 5-digit or 9-digit format (^\d{5}(-\d{4})?$)
country: ISO 3166-1 alpha-2 code (default: US)

Reason Code Validation:

Allowed values: defective, damaged, wrong_item, unwanted, not_as_described, sizing, other
Case-insensitive normalization to lowercase

Condition Validation:

Allowed values: new, opened, damaged, defective, missing_parts
Required if reason_code is defective or damaged

JSON Logic Schema Validation:

Rules must be valid JSON Logic expressions
All var references must match context schema (sku_cost, distance_miles, etc.)
Circular references detected and rejected

4.4 Error Handling & Retry Strategy
Error Categories:

Client Errors (4xx):

No retry, return immediately
Examples: invalid SKU, malformed JSON, auth failure


Transient Errors (5xx):

Retry with exponential backoff: 1s, 2s, 4s
Max 3 retries before moving to DLQ
Examples: Distance Matrix API timeout, WMS endpoint unavailable


Poison Messages:

After 3 retries, message moved to DLQ
DLQ monitored with CloudWatch alarm
Manual intervention required (view in admin UI)



Compensating Actions:

If label generation fails after decision made:

Decision still persisted with label_url: null
Webhook sent with label_pending: true
Background job retries label generation every 5 minutes (max 12 attempts)


If WMS webhook delivery fails:

Retry 5 times over 1 hour (1m, 5m, 15m, 30m, 60m)
If all fail, email alert sent to tenant's operations contact
Tenant can manually trigger webhook replay via API



Circuit Breaker:

Per-destination circuit breaker for external APIs (Distance Matrix, carrier APIs)
Trip threshold: 50% error rate over 10 requests
Open duration: 30 seconds
Half-open: Allow 1 test request
If test succeeds, close circuit; if fails, reopen

4.5 Observability
Structured Logging (JSON format):
json{
  "timestamp": "2026-01-09T15:42:10.234Z",
  "level": "INFO",
  "service": "returns-engine",
  "function": "evaluate_rules",
  "trace_id": "1-5e9c8a2f-abc123def456",
  "tenant_id": "tenant_abc",
  "decision_id": "dec_7x9k2m",
  "order_id": "ORD-2024-12345",
  "message": "Rule evaluation completed",
  "rule_applied": "rule_003",
  "latency_ms": 287,
  "destination_id": "CA-01"
}
CloudWatch Metrics:

DecisionLatencyMs (p50, p95, p99) - dimension: tenant_id, action_type
RuleEvaluationCount (count) - dimension: rule_id, matched (true/false)
APIRequestCount (count) - dimension: endpoint, status_code
DistanceAPILatency (ms) - dimension: provider (google, mapbox)
LabelGenerationSuccessRate (%) - dimension: carrier
WebhookDeliveryAttempts (count) - dimension: tenant_id, success (true/false)

X-Ray Tracing:

Full trace from API Gateway → Lambda → DynamoDB → External APIs
Trace ID propagated via X-Amzn-Trace-Id header
Segments: validation, enrichment, rule_evaluation, label_generation, webhook_delivery
Annotations: tenant_id, decision_id, rule_applied

Dashboards:

Operations Dashboard: Request rate, error rate, p95 latency by endpoint
Business Dashboard: Decisions by action type, cost savings (estimated), top rules applied
Tenant Dashboard: Per-tenant usage, API quota consumption, webhook success rate

4.6 Security
Authentication:

JWT tokens issued via OAuth2 client credentials flow
Token expiry: 1
Continue12:05 AMhour

Refresh tokens not supported (stateless, regenerate as needed)
Scopes: returns:route, rules:read, rules:write, webhooks:manage

Authorization:

Tenant ID extracted from JWT sub claim
All queries automatically filtered by tenant_id (row-level security)
Admin operations require admin scope (tenant creation, usage reports)

Webhook Security:

HMAC-SHA256 signature in X-Signature header
Signature computed over: timestamp + request_body
Recipient verifies signature using shared secret from Secrets Manager
Timestamp validation: reject if >5 minutes old (replay protection)

Secrets Management:

All tenant credentials stored in AWS Secrets Manager
Naming convention: prod/tenant-{id}/{service}-api-key
Lambda execution role restricted to tenant's own secrets via IAM policy condition:

json  {
    "Condition": {
      "StringEquals": {
        "secretsmanager:ResourceTag/tenant_id": "${aws:PrincipalTag/tenant_id}"
      }
    }
  }

Automatic rotation every 90 days

Encryption:

Data at rest: KMS encryption for DynamoDB, RDS, S3
Data in transit: TLS 1.3 for all API calls
Secrets encrypted with tenant-specific KMS key

Audit Logging:

All rule changes logged to audit_logs table with actor, timestamp, diff
All API calls logged with IP address, user agent, request/response samples
Retention: 1 year in RDS, 7 years in S3 Glacier (compliance requirement)

4.7 Performance
Throughput Targets:

Real-time API: 1,000 req/sec (aggregate), sub-500ms p95 latency
Batch API: 10,000 returns/batch, 2-minute processing time
Rule Evaluation: 100 rules evaluated in <100ms

Optimization Strategies:

Caching:

Distance Matrix results cached 5 minutes (reduce API costs)
Product catalog cached in-memory per Lambda execution (30-second TTL)
Tenant rules cached per Lambda execution (5-minute TTL)


Batching:

Distance Matrix API: Batch up to 25 origin-destination pairs per call
DynamoDB: BatchWriteItem for bulk decisions (25 items/request)


Pagination:

List endpoints (GET /v1/decisions) paginated at 100 items/page
Cursor-based pagination using next_token (encoded DynamoDB exclusive start key)


Concurrency:

Lambda: Reserved concurrency per tenant tier (Basic: 10, Pro: 50, Enterprise: 100)
SQS: Batch size 10 messages, concurrent Lambda invocations limited to prevent overwhelming downstream APIs



4.8 Configuration Management
Tenant-Level Configuration (stored in RDS):
json{
  "tenant_id": "tenant_abc",
  "settings": {
    "default_carrier": "ups",
    "label_format": "pdf",
    "auto_generate_labels": true,
    "refund_threshold_usd": 20.00,
    "distance_unit": "miles",
    "webhook_retry_max": 5
  },
  "field_mappings": {
    "wms_destination_field": "facility_code",
    "wms_notes_field": "return_comments"
  },
  "feature_flags": {
    "enable_smart_routing": true,
    "enable_repair_center_routing": false,
    "enable_liquidation_routing": false
  }
}
```

**Feature Flags:**
- Managed via RDS, cached in Lambda environment
- Evaluated per-request (can toggle instantly)
- Use cases: gradual rollout of new features, A/B testing routing strategies

---

## 5. OPERATIONS & RELIABILITY

### 5.1 Idempotency

**Idempotency Key Generation:**
- Client-provided: `Idempotency-Key` header (recommended)
- Server-generated fallback: `SHA256(tenant_id + order_id + sku + date_YYYYMMDD)`
- Stored in DynamoDB GSI for fast lookup (24-hour window)

**Idempotency Behavior:**
- If duplicate request detected within 24 hours:
  - Return `409 Conflict` with reference to existing `decision_id`
  - Do not re-evaluate rules or generate new label
  - Do not charge for duplicate request

**Exactly-Once Semantics:**
- SQS FIFO queues with content-based deduplication (5-minute window)
- DynamoDB conditional writes prevent duplicate decision records
- Webhook deliveries tracked with `delivery_id` to prevent double-processing

### 5.2 Rate Limiting & Backpressure

**API Gateway Rate Limits:**
- **Basic Tier:** 100 req/min, burst 200
- **Pro Tier:** 500 req/min, burst 1000
- **Enterprise Tier:** Custom limits (negotiated per contract)

**SQS Backpressure Handling:**
- Queue depth alarm triggers at 10,000 messages (CloudWatch)
- If depth > 50,000: API Gateway returns `503 Service Unavailable` with `Retry-After` header
- Auto-scaling Lambda concurrency based on queue depth (target: 100 messages/invocation)

**Circuit Breaker (per external API):**
- Open after 10 consecutive failures or 50% error rate in 1-minute window
- While open: Return cached/fallback response, emit metric
- Half-open test after 30 seconds

**Tenant-Level Quotas:**
- Monthly usage limits enforced (Basic: 10k decisions, Pro: 100k, Enterprise: unlimited)
- Soft limit warning at 80% (email notification)
- Hard limit at 100% (API returns `429 Too Many Requests`)

### 5.3 Disaster Recovery & High Availability

**Multi-AZ Deployment:**
- RDS: Multi-AZ with synchronous replication (RPO = 0)
- Lambda: Automatically deployed across 3 AZs
- DynamoDB: Global tables with cross-region replication (for Enterprise tier)

**Backup Strategy:**
- **RDS:** Automated daily snapshots, 30-day retention, point-in-time recovery enabled
- **DynamoDB:** Point-in-time recovery enabled (35-day window), on-demand backups before major rule changes
- **S3:** Versioning enabled for rule backups, lifecycle policy to Glacier after 90 days

**Recovery Objectives:**
- **RTO (Recovery Time Objective):** 1 hour (Basic/Pro), 15 minutes (Enterprise)
- **RPO (Recovery Point Objective):** 1 hour (Basic/Pro), 5 minutes (Enterprise with cross-region replication)

**Failover Procedures:**
1. RDS failure: Automatic failover to standby (1-2 minutes downtime)
2. Regional outage: Manual DNS update to DR region (requires runbook execution)
3. Lambda failure: Automatic retry via SQS DLQ, manual replay if needed

**Runbooks (stored in Confluence/Wiki):**
- "RDS Failover Procedure" (automated, verify only)
- "DynamoDB Restore from Backup" (manual, 30-minute procedure)
- "Re-process Failed Decisions from DLQ" (manual, 15-minute procedure)
- "Tenant Offboarding Data Purge" (manual, GDPR compliance)

### 5.4 Service Level Agreements (SLAs)

**Basic Tier ($299/mo):**
- Uptime: 99.5% (43 minutes downtime/month allowed)
- Latency: p95 < 1000ms
- Support: Email, 24-hour response time

**Pro Tier ($999/mo):**
- Uptime: 99.9% (4.3 minutes downtime/month)
- Latency: p95 < 500ms
- Support: Email + Slack shared channel, 4-hour response time, 24/7 on-call for P1 incidents

**Enterprise Tier (Custom):**
- Uptime: 99.99% (26 seconds downtime/month)
- Latency: p95 < 300ms
- Support: Dedicated Slack channel, 1-hour response time, TAM (Technical Account Manager)
- Cross-region replication included
- Dedicated queues and Lambda concurrency

**SLA Enforcement:**
- Automated monitoring via CloudWatch Synthetics (canary tests every 5 minutes)
- Monthly SLA reports generated automatically, emailed to tenants
- SLA credits: 10% of monthly fee per 1% shortfall below target uptime

---

## 6. MONETIZATION & PRICING

### 6.1 Pricing Structure

**Basic Tier:**
- Setup Fee: $500 (one-time)
- Monthly Platform Fee: $299
- Per-Decision Fee: $0.05 (first 10,000 decisions included, then $0.05/decision)
- Overages: $0.10/decision beyond monthly quota

**Pro Tier:**
- Setup Fee: $1,500 (includes custom rules workshop)
- Monthly Platform Fee: $999
- Per-Decision Fee: $0.03 (first 100,000 decisions included)
- Smart Routing (distance-based): Included
- Dedicated queue: Included
- SLA: 99.9%

**Enterprise Tier:**
- Custom pricing (typically $5k-$15k/month)
- Unlimited decisions
- Cross-region deployment
- Custom integrations (1-2 included per year)
- Dedicated TAM

**Add-Ons:**
- Additional Warehouse: $50/month per location
- Custom Rules Workshop: $2,000 (2-hour session with solutions architect)
- Priority Support: $500/month (upgrade Basic to Pro-level support)
- Compliance Audit Pack: $3,000 (SOC2 readiness assessment)

### 6.2 Path to $10k MRR

**Scenario 1: 10 Pro Tier Customers**
- 10 customers × $999/month = $9,990/month
- Assumptions: Each customer processes 50k returns/month (well within quota)

**Scenario 2: 5 Pro + 20 Basic**
- 5 Pro × $999 = $4,995
- 20 Basic × $299 = $5,980
- Total = $10,975/month

**Scenario 3: 2 Enterprise + 5 Pro**
- 2 Enterprise × $7,500 = $15,000/month (exceeds target)

**Customer Acquisition Timeline:**
- Month 1-2: 3 pilot customers (free trials converting to Basic)
- Month 3-4: 8 customers (5 Basic, 3 Pro)
- Month 5-6: 15 customers (12 Basic, 3 Pro)
- Month 6+: Upsell 3 Basic → Pro, add 2 new Pro = $10k+ MRR

### 6.3 Upsells & Expansion Revenue

**Expansion Triggers:**
1. **Volume Growth:** Customer processes >10k decisions/month → Upsell to Pro
2. **Multi-Location:** Customer adds 2nd warehouse → $50/month add-on
3. **Custom Logic:** Customer needs repair center routing → Custom rules workshop + feature flag enable
4. **Compliance:** Customer needs SOC2 attestation → Compliance audit pack

**Pricing Psychology:**
- Setup fees create sunk cost (reduce churn)
- Usage tiers encourage growth (more volume = lower per-unit cost)
- Pro tier "sweet spot" at $999 (3.3x Basic, perceived value jump)

### 6.4 Churn Defense

**Contract Terms:**
- Annual contracts: 10% discount (lock in revenue)
- Month-to-month: 30-day notice required
- Setup fee non-refundable (sunk cost barrier)

**Onboarding Playbook:**
- Day 1: Kickoff call, credentials exchange
- Day 3: Test API integration in sandbox
- Day 7: First 100 decisions routed (celebrate milestone)
- Day 14: Weekly sync, review metrics dashboard
- Day 30: QBR (Quarterly Business Review) scheduled

**ROI Dashboard (sent monthly):**
- Decisions processed: 12,500
- Estimated shipping cost savings: $3,200 (vs manual routing)
- Processing time saved: 48 hours (vs manual CSV workflows)
- ROI: $3,200 saved / $299 cost = 10.7x

**Health Score Monitoring:**
- API usage trending down 50%+ month-over-month → Proactive outreach
- Webhook delivery failures >10% → Technical support escalation
- No logins to admin portal in 30 days → Check-in call

---

## 7. GO-TO-MARKET: COLD EMAIL + UPWORK (STEALTH)

### 7.1 Cold Email Strategy

**3-Step Sequence:**

**Email 1: Problem (Day 1)**
```
Subject: Still routing returns manually at [Company]?

Hi [FirstName],

I noticed [Company] handles fulfillment for [Brand1, Brand2] – congrats on the growth.

Quick question: when a return comes in from Amazon or Shopify, how do you decide which warehouse to send it to?

Most 3PLs I talk to are either:
- Hardcoding rules that break when they add locations
- Burning 2-4 hours/day on CSV exports and manual routing

If that sounds familiar, I built something that might help.

Want to see a 2-min demo?

[Your Name]
Founder, ReturnsRouter
[Email] | [Calendar Link]

P.S. We just helped [3PL Name] cut their returns processing time from 4 hours to 15 minutes. Happy to share how.
```

**Email 2: Proof (Day 4, if no reply)**
```
Subject: Re: Still routing returns manually at [Company]?

Hi [FirstName],

Following up on my note below. Here's what we're doing for 3PLs like yours:

→ API that decides where returns go in <500ms
→ Connects to Amazon, Shopify, your WMS (ShipBob, Flexport, etc)
→ Auto-generates return labels (FedEx, UPS)
→ Saves $2k-$5k/month in shipping + labor

[3PL Name] was manually routing 500 returns/day through Excel. Now it's automatic.

[Download 1-page case study]

Open to a quick call this week?

[Calendar Link]

Thanks,
[Your Name]
```

**Email 3: CTA (Day 8, if no reply)**
```
Subject: Last note on automating returns at [Company]

[FirstName],

I'll keep this short – if you're not interested in automating your returns routing, no worries. I'll stop bugging you.

But if you want to:
✓ Stop manual CSV exports
✓ Route returns to the nearest warehouse automatically
✓ Cut processing time by 75%

...let's talk for 15 minutes.

[Calendar Link]

If this isn't a priority, just reply "not now" and I'll check back in Q2.

Thanks,
[Your Name]

P.S. We're running a pilot program this month – free setup ($500 value) if you're willing to be a case study. Details here: [Link]
```

**Personalization Tokens:**
- `[Company]`: Scraped from LinkedIn, website
- `[Brand1, Brand2]`: Scraped from 3PL's "Clients" page or press releases
- `[FirstName]`: From Hunter.io, Apollo.io
- `[3PL Name]`: Real customer (with permission) or anonymous "a West Coast 3PL"

### 7.2 Target List Building

**Job Titles:**
- Director of Operations
- VP Supply Chain
- Head of Fulfillment
- Operations Manager
- VP Logistics
- Warehouse Operations Lead

**Company Criteria:**
- Industry: Third-Party Logistics, E-commerce Fulfillment
- Employee count: 50-500
- Revenue: $10M-$100M (estimated via LinkedIn Sales Navigator)
- Location: US, Canada (start domestic)
- Tech signals: ShipBob, Flexport, ShipStation mentioned in job posts or tech stack scrapers

**Data Sources:**
- **Apollo.io:** Filter by title + industry, export 1,000 contacts
- **LinkedIn Sales Navigator:** Boolean search: `(3PL OR "third party logistics" OR "fulfillment services") AND ("operations" OR "logistics")`
- **BuiltWith:** Scrape sites using ShipStation, Shopify Plus (signal they handle returns at scale)
- **Crunchbase:** Filter by category "Supply Chain" + funding stage (Seed to Series B = growing, need automation)

**List Hygiene:**
- Verify emails via NeverBounce, ZeroBounce (remove bounces)
- Remove generic emails (info@, hello@)
- Segment by company size (50-150 employees = SMB campaign, 150+ = mid-market campaign)

**Expected Conversion:**
- 1,000 contacts → 250 opens (25%) → 50 replies (5%) → 10 demos (2%) → 2-3 pilots (0.2-0.3%)

### 7.3 Upwork Strategy

**Profile Headline:**
"API Integration Specialist | 3PL & E-commerce Automation | .NET + AWS"

**Listing 1: RMA Automation Project**
```
Title: Automate Your Returns Routing in 2 Weeks

Scope:
- Build API that decides where returns go (nearest warehouse, repair center, or refund without return)
- Integrate with your WMS (ShipBob, Flexport, custom) and marketplaces (Amazon, Shopify)
- Generate return labels automatically via carrier APIs (FedEx, UPS, USPS)
- Provide simple rules editor (if item cost < $20, refund without return)

Deliverables:
✓ API endpoints (REST) with authentication
✓ Webhook integration to your WMS
✓ 2 weeks of bug fixes + support
✓ Documentation + Postman collection

Proof:
I built this exact system for a 3PL in California. They process 500 returns/day and cut processing time from 4 hours to 15 minutes.

[Attach 1-page case study PDF]

Price: $5,000 fixed (2-week delivery)

Ideal Client:
- 3PL or e-commerce brand doing 5,000+ returns/month
- Currently routing returns manually (CSV exports, emails)
- Using ShipBob, Flexport, or similar WMS

Questions?
Schedule a free 15-min scoping call: [Calendar Link]
```

**Listing 2: WMS Integration Project**
```
Title: Connect Your WMS to Amazon/Shopify Returns API

Scope:
Build middleware that syncs returns data from Amazon Seller Central / Shopify to your warehouse management system.

What you get:
- Webhook receiver for Amazon RMA events
- Data normalization (XML → JSON)
- POST to your WMS API (create warehouse task)
- Retry logic + error handling

Timeline: 1 week

Price: $2,500 fixed

This is a productized service. I've built this 8 times for different 3PLs. You're getting a proven pattern, not a custom experiment.

[Attach code sample + architecture diagram]
```

**Upwork Search Keywords (to find relevant gigs):**
- "returns automation"
- "RMA integration"
- "WMS API integration"
- "ShipStation webhook"
- "3PL automation"
- "reverse logistics"

**Proposal Template (for inbound gigs):**
```
Hi [Client Name],

I saw your post about [automating returns / WMS integration]. This is exactly what I specialize in.

I've built a returns routing system for 3 3PLs in the past 6 months. The pattern:
1. Webhook from marketplace (Amazon, Shopify)
2. Decision engine (where should return go?)
3. POST to WMS API (create task)
4. Generate return label (FedEx, UPS)

For your use case, here's what I'd do:
- [Specific detail from their post]
- [Specific detail from their post]
- [Specific detail from their post]

Timeline: 2 weeks
Price: $4,500 (includes 1 week of bug fixes)

I can start Monday. Here's a case study from a similar project: [Link]

Questions?
[Your Name]
[Calendar Link]
7.4 Case Study Outline
Title: "How [3PL Name] Cut Returns Processing Time by 80%"
Context:
[3PL Name] is a California-based 3PL handling fulfillment for 12 e-commerce brands. They process 500 returns per day from Amazon, Shopify, and Walmart.
Problem:

Returns arrived via email, CSV exports, and manual entry
Operations team spent 4 hours/day routing returns to the right warehouse
No logic for "refund without return" on low-value items
Lost inventory due to routing errors (sent East when item was West Coast only)

Approach:

Built API that connects Amazon RMA API → [3PL]'s ShipBob account
Implemented rules engine: "If item cost < $20 and reason = 'unwanted', refund without return"
Distance-based routing: "Send return to nearest warehouse within 100 miles"
Auto-generate FedEx Ground labels via EasyPost API

Metrics:

Processing time: 4 hours → 45 minutes (81% reduction)
Shipping cost savings: $3,200/month (routing to nearest warehouse)
Refund speed: 5 days → 2 days (happier customers)
Error rate: 8% → 0.5% (fewer misdirected returns)

Testimonial Template:

"Before ReturnsRouter, we were drowning in CSV files. Now returns just… route themselves. We're saving 15 hours/week and our clients love the faster refunds."
– [Name], Director of Operations, [3PL Name]

Call to Action:
Want similar results? Schedule a free 15-minute consultation: [Calendar Link]
7.5 Landing Page Wireframe
Headline:
"Stop Routing Returns Manually. Start Saving $2k+/Month."
Subheadline:
API-first returns routing for 3PLs and e-commerce brands. Connects to your WMS in 48 hours.
3 Outcomes (Icons + Bullets):

⚡ Faster Refunds – Route returns in <1 second, not 4 hours
💰 Lower Shipping Costs – Smart routing to nearest warehouse (30-40% savings)
🤖 Zero Manual Work – Rules engine handles decisions, you handle growth

Social Proof:
"We process 10,000 returns/month and cut processing time by 75%."
– [Name], [3PL Company]
Single CTA:
[Schedule Free Demo] (Big button, links to Calendly)
Secondary CTA:
[View API Docs] (Text link)
Trust Badges:

AWS Partner
SOC2 Compliant (in progress)
99.9% Uptime SLA

Footer:

Link to API documentation
Privacy Policy
Contact email


8. COMPETITIVE LANDSCAPE & MOAT
8.1 Alternatives
Alternative 1: In-House Scripts

What: Custom Python/Node scripts, cron jobs, CSV processors
Pros: Full control, no recurring cost
Cons: Breaks when APIs change, no support, hard to scale, requires engineering time
Our edge: We maintain integrations, provide support, handle API versioning

Alternative 2: iPaaS (Zapier, Make, MuleSoft)

What: No-code/low-code integration platforms
Pros: Easy to start, visual workflow builder
Cons: Can't handle complex logic (distance calculations, conditional routing), expensive at scale ($500+/mo for high volume), slow (multi-step workflows = latency)
Our edge: Purpose-built for returns routing, <500ms latency, flat pricing, advanced rules engine

Alternative 3: Enterprise Platforms (Loop Returns, Returnly)

What: Full-stack returns management (portal + routing + analytics)
Pros: White-label portal, customer-facing, brand experience
Cons: $2k-$5k/month, 3-6 month implementation, requires replacing existing portal, overkill for 3PLs who just need routing
Our edge: API-only (headless), 2-week implementation, 1/5 the cost, integrates with existing systems

Alternative 4: Manual Operations

What: CSR reviews each return, decides manually, creates label
Pros: Flexible, handles edge cases
Cons: Slow (4+ hours/day), error-prone, doesn't scale, expensive labor
Our edge: Instant decisions, 99.5% accuracy, scales infinitely

8.2 Differentiators
1. Speed to Deploy:

Industry: 3-6 months (enterprise platforms)
Us: 2 weeks (API integration + rules setup)

2. Deep Domain Mappings:

Pre-built connectors for top WMS systems (ShipBob, Flexport, ShipStation)
EDI format support (for enterprise suppliers)
Carrier API integrations (EasyPost, Shippo, direct carrier APIs)

3. Audit & Compliance:

Every decision logged with reasoning, timestamp, user
Immutable audit trail (DynamoDB + S3 archive)
SOC2 Type II readiness (checklist, controls documentation)

4. Fixed SLAs:

99.9% uptime guarantee (Pro tier)
<500ms p95 latency
Proactive monitoring + incident response

5. Transparent Pricing:

No hidden fees, no per-connector charges
Simple per-decision pricing
Volume discounts kick in automatically

8.3 Defensibility (Moat)
Moat 1: Proprietary Rules Library

50+ pre-built rules templates (refund thresholds, distance-based routing, condition-based disposition)
Continuously improved based on customer data
New customers get instant value (don't start from scratch)

Moat 2: Integration Library

Deep integrations with 10+ WMS platforms
Maintained as APIs change (Amazon updates RMA API → we adapt within 48 hours)
Switching cost: Competitor must rebuild all integrations

Moat 3: Data Quality Scores

Proprietary scoring: "This return has 85% confidence of being restockable"
Trained on thousands of returns (condition, reason_code, SKU category)
Improves routing accuracy over time (ML-enhanced rules engine - Phase 2)

Moat 4: Network Effects (Weak, but Growing):

More customers → more edge cases discovered → better rules templates
Shared anonymized benchmarks: "3PLs in your segment route 35% of returns without requiring return shipping"

Moat 5: Vertical Lock-In:

Once a 3PL standardizes on our API for routing decisions, expanding to label generation, disposition management, and carrier optimization is natural
Land with routing, expand to full returns lifecycle management


9. COMPLIANCE, LEGAL, & RISK
9.1 Data Processing & Privacy
Data Minimization:

Only collect: order_id, SKU, customer_zip, reason_code, condition
Do NOT collect: customer name, email, phone, full address (except zip)
Retention: 90 days in hot storage (DynamoDB), then archive to S3 Glacier (if customer requests extended retention)

GDPR Compliance (if serving EU customers):

Data Processing Addendum (DPA) template available
Right to deletion: API endpoint DELETE /v1/decisions/{decision_id} (soft delete, mark as anonymized)
Right to export: API endpoint GET /v1/decisions/export (returns JSON/CSV)
Data residency: Option to deploy in EU region (DynamoDB + Lambda in eu-west-1)

CCPA Compliance (California customers):

Privacy policy discloses data collection, retention, sharing
"Do Not Sell My Data" honored (we don't sell data anyway)
Consumer requests handled within 45 days

Data Sharing:

Tenant data NEVER shared with other tenants
No third-party data sales
Only shared with: Carrier APIs (for label generation), Distance Matrix API (for routing)

9.2 Data Processing Addendum (DPA) Template
Key Terms:

Processor: ReturnsRouter (processes data on behalf of tenant)
Controller: Tenant (owns customer data, determines purposes)
Subprocessors: AWS, EasyPost, Google Maps API (disclosed in appendix)
Security measures: Encryption at rest (KMS), in transit (TLS 1.3), access controls (IAM)
Breach notification: 72 hours
Data deletion: Within 30 days of contract termination

9.3 Moonlighting Risk Controls
Entity Structure:

Operate via LLC or S-Corp (not personal name)
Business name: "ReturnsRouter, LLC" (generic, not tied to employer industry)
Domain: returnsrouter.dev or .io (separate from personal brand)

Non-Compete / IP Considerations:

Review employer agreement: Ensure no clause prohibits side projects in same industry
Time boundaries: Work on ReturnsRouter only during personal time (evenings, weekends)
No employer resources: Do not use employer laptop, code repositories, or proprietary knowledge
Client non-solicitation: Do not target employer's clients or partners for ReturnsRouter

Legal Review:

Consult employment lawyer before launch (1-hour consultation, ~$300)
If employer agreement is restrictive: Consider launching post-employment or negotiating carve-out

Disclosure:

If employer policy requires disclosure of outside business activities: Disclose as "technology consulting" (not specific SaaS product)
Avoid mentioning on employer-linked LinkedIn profile (stealth requirement)

9.4 Terms of Service (ToS) Key Clauses
Liability Limitation:

ReturnsRouter liable only for direct damages, capped at 12 months of fees paid
No liability for: lost profits, data loss (customer responsible for backups), third-party API failures

Acceptable Use:

Prohibit: Reverse engineering, reselling API access, using for illegal purposes, overloading system (DDoS)

Service Modifications:

ReturnsRouter reserves right to update API with 30 days notice
Breaking changes communicated via email + changelog

Termination:

Either party may terminate with 30 days notice (month-to-month) or at contract end (annual)
Upon termination: Customer has 30 days to export data, then deleted

Indemnification:

Customer indemnifies ReturnsRouter for claims arising from customer's data, use of API, or violation of ToS


10. DAY-BY-DAY BUILD PLAN (45 DAYS)
Week 1: Foundation & Schema (Days 1-7)
Deliverables:

AWS account setup (IAM roles, VPC, security groups)
DynamoDB tables created (returns_decisions, tenant_rules, routing_cache)
RDS PostgreSQL instance provisioned (tenants, warehouses, product_catalog)
API Gateway + Lambda scaffolding (.NET Core 8 "Hello World" deployed)
JWT authentication middleware implemented
Git repository initialized (GitHub private repo)

Success Criteria:

Can deploy Lambda via dotnet lambda deploy-function
API Gateway endpoint returns 200 OK with JWT token
DynamoDB tables queryable via AWS CLI
RDS accessible from Lambda (connection pooling configured)

Day-by-Day:

Day 1: AWS account, IAM roles, VPC setup
Day 2: DynamoDB table creation, test writes
Day 3: RDS instance, schema DDL execution
Day 4: API Gateway + Lambda "Hello World"
Day 5: JWT authentication (generate/validate tokens)
Day 6: Lambda → DynamoDB read/write test
Day 7: Lambda → RDS read/write test


Week 2: Core Routing API (Days 8-14)
Deliverables:

POST /v1/route endpoint fully functional
Validation: SKU lookup, customer_zip format, reason_code enum
Rules engine: JSON Logic evaluator (Python Lambda or .NET with port)
Idempotency: Dedupe via Idempotency-Key header
Distance Matrix API integration (Google Maps API)
Basic routing logic: Nearest warehouse within 100 miles

Success Criteria:

Can POST return request, get
Continue12:10 AMback decision with destination_id

Rules evaluated correctly (test with 3 sample rules)
Idempotent requests return 409 Conflict with existing decision_id
Distance calculated accurately (spot-check 5 zip pairs)

Day-by-Day:

Day 8: POST /v1/route endpoint skeleton
Day 9: SKU validation (lookup in RDS product_catalog)
Day 10: Rules engine (JSON Logic library integration)
Day 11: Distance Matrix API wrapper, caching logic
Day 12: Routing decision logic (evaluate rules, select destination)
Day 13: Idempotency implementation (DynamoDB GSI)
Day 14: End-to-end test (Postman collection with 10 scenarios)


Week 3: Label Generation & Enrichment (Days 15-21)
Deliverables:

EasyPost API integration (label generation)
Label URL returned in API response
Webhook delivery to tenant's WMS endpoint
Error handling: Retry logic for carrier API failures
CloudWatch logging: Structured JSON logs with trace_id
Metrics: Decision latency, rule evaluation count

Success Criteria:

Can generate FedEx/UPS label via EasyPost
Webhook delivered to test endpoint (RequestBin)
Retries triggered on 5xx errors (verify in logs)
CloudWatch dashboard shows p95 latency <1000ms

Day-by-Day:

Day 15: EasyPost API wrapper (create shipment, buy label)
Day 16: Integrate label generation into routing flow
Day 17: Webhook delivery (EventBridge → Lambda → HTTP POST)
Day 18: Retry logic (exponential backoff, DLQ for failures)
Day 19: CloudWatch structured logging
Day 20: Metrics (custom CloudWatch metrics)
Day 21: Dashboard creation (operational view)


Week 4: Rules Management & Configuration (Days 22-28)
Deliverables:

GET /v1/rules endpoint (fetch current ruleset)
PUT /v1/rules endpoint (update ruleset, create new version)
Rule validation: JSON Logic schema check
Tenant configuration API (GET /v1/config, PUT /v1/config)
Feature flags implementation (enable/disable smart routing per tenant)
Warehouse management API (POST /v1/warehouses, GET /v1/warehouses)

Success Criteria:

Can fetch, update, and activate rules via API
Invalid rules rejected with clear error messages
Tenant can configure default carrier, label format
New warehouse added via API, available for routing decisions

Day-by-Day:

Day 22: GET /v1/rules endpoint
Day 23: PUT /v1/rules endpoint + versioning logic
Day 24: Rule validation (JSON Logic schema)
Day 25: Tenant config API
Day 26: Feature flags (DynamoDB-backed, Lambda-cached)
Day 27: Warehouse CRUD API
Day 28: End-to-end test (create tenant, add warehouse, route return)


Week 5: Batch Processing & Admin UI (Days 29-35)
Deliverables:

POST /v1/route/batch endpoint (accept array of returns)
SQS queue processing (batch of 10 messages per Lambda invocation)
Batch status tracking (GET /v1/route/batch/{batch_id})
Simple admin UI (Vue.js): View decisions, update rules, manage warehouses
API documentation (Swagger/OpenAPI spec)

Success Criteria:

Can submit 100 returns in one batch, processed within 2 minutes
Admin UI: Can view last 50 decisions, filter by tenant
API docs hosted at /docs (Swagger UI)

Day-by-Day:

Day 29: Batch API endpoint skeleton
Day 30: SQS queue setup, Lambda batch processor
Day 31: Batch status tracking (DynamoDB table)
Day 32: Vue.js admin UI scaffolding
Day 33: Admin UI: Decisions list view
Day 34: Admin UI: Rules editor (JSON editor component)
Day 35: API docs (OpenAPI spec + Swagger UI)


Week 6: Security Hardening & Pilot Prep (Days 36-42)
Deliverables:

OAuth2 scopes enforcement (returns:route, rules:write, etc.)
Secrets Manager integration (tenant API keys, carrier credentials)
Rate limiting (API Gateway usage plans)
Audit logging (all API calls logged to RDS)
Pilot customer onboarding checklist
Postman collection (for pilot customer testing)

Success Criteria:

OAuth2 scopes correctly restrict access
Secrets rotated successfully via Lambda
Rate limits enforced (test with 150 req/min, expect 429)
Audit logs queryable in admin UI

Day-by-Day:

Day 36: OAuth2 scope enforcement
Day 37: Secrets Manager integration
Day 38: Rate limiting configuration
Day 39: Audit logging to RDS
Day 40: Pilot onboarding checklist (doc)
Day 41: Postman collection (20 example requests)
Day 42: Security review (checklist: OWASP Top 10, SANS 25)


Week 7: Pilot Onboarding & Iteration (Days 43-45)
Deliverables:

Pilot customer #1 onboarded (credentials issued, rules configured)
First 100 real decisions processed
Bug fixes from pilot feedback
Monitoring dashboard shared with pilot customer
Testimonial request prepared

Success Criteria:

Pilot customer successfully routes 100 returns via API
No P1 bugs encountered
Customer verbally confirms value (will convert to paid)

Day-by-Day:

Day 43: Pilot onboarding (kickoff call, credentials exchange)
Day 44: Pilot testing (customer makes first API calls)
Day 45: Review results, gather feedback, iterate


Pilot Checklist
Pre-Launch:

 Tenant account created in RDS
 API key generated, stored in Secrets Manager
 Product catalog imported (CSV → RDS)
 Warehouses configured (addresses, lat/lng)
 Default rules applied (3 sample rules)
 Webhook endpoint URL provided by customer, tested

Launch Day:

 Kickoff call (30 minutes): Explain API, share Postman collection
 Customer makes first test request (sandbox environment)
 First successful routing decision within 1 hour

Week 1 Post-Launch:

 Daily check-ins (Slack or email)
 Review CloudWatch dashboard together
 Adjust rules based on customer feedback
 Document any edge cases discovered

Week 2 Post-Launch:

 Weekly sync meeting
 Review metrics: Decisions processed, latency, error rate
 Request testimonial (if customer is happy)
 Discuss conversion to paid plan


11. SUCCESS METRICS & KPIs
Product Metrics

Decisions Processed: Total count, daily/monthly trend
API Latency: p50, p95, p99 (target: <500ms p95)
Error Rate: % of failed requests (target: <1%)
Rule Evaluation Time: Avg ms per ruleset (target: <100ms for 50 rules)
Webhook Delivery Success: % delivered on first attempt (target: >95%)

Business Metrics

MRR (Monthly Recurring Revenue): Track by tier (Basic, Pro, Enterprise)
Customer Count: Active tenants, segmented by tier
Churn Rate: % of customers canceling per month (target: <5%)
ARPU (Average Revenue Per User): MRR / customer count
CAC (Customer Acquisition Cost): Sales/marketing spend / new customers
LTV (Lifetime Value): ARPU × average customer lifetime (months)

Operational Metrics

Support Tickets: Count, avg resolution time (target: <24 hours)
Uptime: % (target: 99.5% Basic, 99.9% Pro)
Incident Response Time: Minutes to acknowledge P1 incidents (target: <15 min)


12. NEXT STEPS & LAUNCH PLAN
Immediate Actions (Next 7 Days):

Register LLC or S-Corp entity
Set up AWS account, enable billing alerts
Purchase domain (returnsrouter.dev)
Set up warm email domain (Google Workspace, SPF/DKIM/DMARC)
Build Apollo.io target list (1,000 contacts)
Start Week 1 build plan (foundation & schema)

Month 1 Goals:

Complete MVP build (all core features functional)
Onboard 1 pilot customer (free trial)
Send 500 cold emails (expect 10-15 replies)
Apply to 5 relevant Upwork gigs

Month 2 Goals:

Onboard 2 more pilot customers (convert 1 to paid)
Iterate based on pilot feedback
Build case study from pilot #1
Send 1,000 cold emails
Launch landing page (SEO optimized)

Month 3 Goals:

5 paying customers ($2k-$3k MRR)
Refine pricing based on actual usage patterns
Build out feature requests from customers (disposition routing, repair center integration)

Month 6 Goals:

10-12 paying customers ($10k+ MRR)
Hire part-time support engineer (Upwork contractor)
Apply for SOC2 certification (3-month process)


13. APPENDICES
Appendix A: Technology Stack Summary
Backend:

.NET Core 8 (C#) - Lambda functions, API logic
Python 3.11 - Rules engine (JSON Logic library)
AWS Lambda - Serverless compute
API Gateway - REST API, authentication, rate limiting
DynamoDB - NoSQL for decisions, rules, cache
RDS PostgreSQL 15 - Relational data (tenants, catalog, audit)
SQS - Message queuing, decoupling
EventBridge - Event-driven architecture
S3 - File storage, backups
CloudWatch - Logging, metrics, alarms
X-Ray - Distributed tracing
Secrets Manager - Credentials storage
KMS - Encryption keys

Frontend (Admin UI):

Vue.js 3 - Lightweight SPA
Tailwind CSS - Styling
Axios - HTTP client
Hosted on S3 + CloudFront (static site)

External APIs:

Google Maps Distance Matrix API (or Mapbox)
EasyPost (label generation)
Shippo (alternative to EasyPost)
ShipBob API (WMS integration)
Flexport API (WMS integration)

Appendix B: Sample Rules in JSON Logic
Rule: Refund Without Return for Low-Value Items
json{
  "if": [
    {
      "and": [
        {"<": [{"var": "sku_cost"}, 25]},
        {"in": [{"var": "reason_code"}, ["unwanted", "wrong_item"]]}
      ]
    },
    {
      "action": "refund_no_return",
      "reasoning": "Item under $25, not worth return shipping"
    },
    null
  ]
}
Rule: Route to Repair Center for High-Value Defective Items
json{
  "if": [
    {
      "and": [
        {">=": [{"var": "sku_cost"}, 100]},
        {"==": [{"var": "condition"}, "defective"]}
      ]
    },
    {
      "action": "route_to_repair",
      "destination_id": "REPAIR-01"
    },
    null
  ]
}
Rule: Route to Nearest Warehouse if Within 100 Miles
json{
  "if": [
    {"<": [{"var": "distance_miles"}, 100]},
    {
      "action": "route_to_warehouse",
      "destination_id": "nearest",
      "generate_label": true
    },
    null
  ]
}
Appendix C: Glossary

3PL: Third-Party Logistics provider (handles warehousing, fulfillment for e-commerce brands)
RMA: Return Merchandise Authorization (the process/number for initiating a return)
WMS: Warehouse Management System (software for managing inventory, tasks, shipments)
EDI: Electronic Data Interchange (legacy format for B2B transactions)
DLQ: Dead Letter Queue (storage for failed messages after retries exhausted)
Idempotency: Property ensuring duplicate requests produce same result without side effects
Circuit Breaker: Pattern to prevent cascading failures by stopping requests to failing services
Exactly-Once Semantics: Guarantee that a message is processed exactly once (no duplicates or omissions)


END OF PROJECT REQUIREMENTS DOCUMENT

Document Metadata:

Version: 1.0
Last Updated: January 9, 2026
Author: [Your Name]
Review Cycle: Quarterly or after major milestones
Distribution: Internal team, advisors (under NDA)