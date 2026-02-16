Returns Router (RMA Logic Engine) – Project Requirements Document

A) Problem, Target Customer, Business Value
- Pain: Returns are routed manually or with brittle hardcoded rules, causing delays, mis-shipments, and excess cost (wrong facility, wrong carrier, unnecessary return-to-warehouse).
- ICP: 3PL operations managers and e-commerce ops leads at brands/aggregators processing 1k–50k RMAs/month; stacks typically include Shopify/Amazon/Walmart marketplaces, WMS APIs (ShipBob, 3PL Central), and custom portals.
- Value: Sub-second routing decisions reduce refund cycle time by 1–3 days, cut shipping costs 10–25% (nearest facility and refund/no-return logic), prevent restock errors. Target ROI: 500–1,500 ops hours saved/yr, 2–5% fewer chargebacks.

B) Architecture Overview
- Pattern: Headless Rules Engine API with configurable per-tenant routing logic, idempotent RMA processing, and webhook/task egress to WMS and carrier label services.

ASCII Diagram
[Storefront/Marketplace] --(RMA webhook/POST)--> [Edge API: /v1/route]
                                                      |
                                            [Auth + Idempotency Check]
                                                      |
                                       [Rules Engine: JSON Logic + Geo]
                                                      |
                               +-----------+----------+--------------+
                               |           |                         |
                     [WMS Task Create]  [Carrier Label]     [Refund/No-Return]
                       (HTTP/SNS)          (EasyPost)             (API)
                               |                         |
                         [Ack + Audit Log]     [Label URL + Audit Log]

- Components:
  - Ingress: HTTPS API for RMA requests and batch ingestion; marketplace webhooks (Amazon Returns API, Shopify Orders/Returns).
  - Core: Rules Engine evaluating tenant-defined conditions (SKU value, condition, reason codes, distance to facilities, inventory constraints).
  - Validation: Schema/version checks, EDI-like reason code normalization, dedupe via idempotency keys.
  - Egress: WMS task creation (HTTP), label generation (EasyPost/Shippo), refund/no-return directives to storefront/ERP; webhook delivery with HMAC signatures.
  - Retries: Exponential backoff, DLQ for failed egress, compensating actions (cancel task, void label).

- Deployment:
  - Edge compute for sub-second decisions; serverless data stores; queues for reliable egress.
  - Multi-tenant isolation: tenant_id in all rows, RLS on config/state, per-tenant keys/secrets segregated.

C) Low-Level Design
- Data Model (Supabase Postgres recommended):
  - tenants (id UUID PK, name text, status, created_at)
    - Index: (status), (created_at)
  - tenant_secrets (tenant_id FK, key_name text, value encrypted, updated_at)
    - Unique: (tenant_id, key_name)
  - facilities (id UUID PK, tenant_id FK, name, address, lat, lon, carrier_codes jsonb, capacity int)
    - Index: (tenant_id), (tenant_id, lat, lon)
  - routing_rules (id UUID PK, tenant_id FK, priority int, condition jsonb, action jsonb, enabled bool, version int)
    - Index: (tenant_id, enabled, priority) with partial index enabled=true
  - rma_events (id UUID PK, tenant_id FK, idempotency_key text, payload jsonb, status text, decision jsonb, created_at)
    - Unique: (tenant_id, idempotency_key)
    - Index: (tenant_id, created_at DESC), (status)
  - webhooks_outbox (id UUID PK, tenant_id FK, target_url text, body jsonb, signature text, status text, attempt_count int, next_attempt_at timestamptz, created_at)
    - Index: (tenant_id, status), (next_attempt_at)
  - labels (id UUID PK, tenant_id FK, rma_id FK, carrier text, label_url text, cost_cents int, status text, created_at)
    - Index: (tenant_id, rma_id)

- API Surface
  - POST /v1/route
    - Auth: JWT (tenant scope) or OAuth2 client credentials; HMAC signature optional.
    - Rate limits: 1000 req/min per tenant; burst 100; global circuit breaker per tenant.
    - Idempotency: Idempotency-Key header; responses reused for duplicate keys.
  - POST /v1/batch/route
    - Bulk RMAs (up to 1,000); streaming decisions; pagination tokens for large sets.
  - GET /v1/rma/{id}
    - Fetch decision, label URL, audit log.
  - POST /v1/webhooks/ingest/{source}
    - Verified marketplace webhooks (Amazon, Shopify); source-specific schema versions.
  - PUT /v1/config/rules
    - Update routing_rules with versioning; validates and stores JSON Logic templates.
  - GET /v1/config/rules
    - Retrieve active rule set; ETag for caching.
  - POST /v1/test/decision
    - Dry-run with payload; returns decision and rule trace.

- Request/Response Examples
  - POST /v1/route (JSON)
    Request:
    {
      "rma_id": "RMA-88321",
      "order_id": "ORD-5521",
      "sku": "SKU-ABC-123",
      "declared_value_cents": 1399,
      "reason_code": "DAMAGED",
      "condition": "UNUSABLE",
      "customer_zip": "90210",
      "customer_country": "US",
      "weight_oz": 16,
      "dimensions": {"l_in": 8, "w_in": 4, "h_in": 2},
      "photos": [],
      "metadata": {"channel": "AMAZON", "marketplace_rma": "AMZ-4411"}
    }
    Response:
    {
      "destination_id": "FAC-CA-01",
      "actions": [
        {"type": "CREATE_WMS_TASK", "task_type": "RETURN_INTAKE"},
        {"type": "GENERATE_LABEL", "carrier": "UPS_GROUND"},
        {"type": "TAG", "value": "FAST_REFUND"}
      ],
      "label_url": "https://labels.example/UPS/AMZ-4411.pdf",
      "decision_trace": [
        {"rule_id": "RULE-12", "matched": true, "reason": "value<200 && reason=DAMAGED"},
        {"rule_id": "RULE-20", "matched": true, "reason": "nearest facility CA"}
      ]
    }

  - Edge case: refund-no-return
    Response:
    {
      "destination_id": null,
      "actions": [{"type": "REFUND_NO_RETURN"}],
      "label_url": null,
      "decision_trace": [{"rule_id": "RULE-2", "matched": true, "reason": "value<200 && condition=UNUSABLE"}]
    }

- Validation Rules
  - reason_code must be in normalized set: DAMAGED, WRONG_ITEM, SIZE_FIT, DEFECTIVE, OTHER.
  - condition: NEW, OPEN_BOX, USED, UNUSABLE.
  - declared_value_cents >= 0; weight_oz >= 0.
  - Address/ZIP format per country; US ZIP 5 or 9 digits; phone/email optional and format-checked if present.
  - Schema versioning: X-Schema-Version header; reject unknown versions with 400 and suggested upgrade path.

- Error Handling
  - 4xx: Validation errors with field-specific messages; no retry.
  - 5xx transient: Retry with exponential backoff; egress failures moved to DLQ after N attempts.
  - Poison messages: Sent to DLQ with diagnostic; compensating action to void labels or cancel WMS tasks if partial success.
  - Idempotent duplicates: Return previous decision with 200.

- Observability
  - Structured logs (tenant_id, rma_id, trace_id, decision_time_ms, rule_ids).
  - Metrics: p50/p95 latency, decision success/error counts, egress success rate, labels generated, refund-no-return rate.
  - Distributed trace IDs propagated to egress calls.

- Security
  - JWT/OAuth2 scopes: route:write, route:read, config:write, webhook:ingest.
  - Webhook HMAC signatures (per-tenant secret), timestamp checks, replay window 5 minutes.
  - Encryption at rest (KMS-managed) for secrets; labels and audit logs stored with SSE.
  - Audit logs: who/when changed rules, before/after snapshots.

- Performance
  - Assumptions: 20 rps peak per tenant; 100 rps aggregate; target p95 decision < 250 ms at edge.
  - Batching: /batch/route supports up to 1k items with streaming results to keep memory bounded.
  - Streaming vs bulk: single-item endpoint for synchronous UI flows; batch for back-office.
  - Pagination for rma_events queries (cursor-based).

- Configuration
  - Per-tenant JSON Logic mappings:
    {
      "version": 3,
      "rules": [
        {"priority": 1, "if": {"<": [{"var":"declared_value_cents"}, 2000]}, "and": [{"==":[{"var":"reason_code"},"DAMAGED"]},{"==":[{"var":"condition"},"UNUSABLE"]}], "then": {"action":"REFUND_NO_RETURN"}},
        {"priority": 10, "if": {"==":[{"var":"customer_country"},"US"]}, "then": {"action":"ROUTE_NEAREST", "constraints":{"carrier":"UPS_GROUND"}}}
      ]
    }
  - Feature flags: enable_label_generation, enable_refund_no_return, enable_distance_matrix, enable_wms_task_create.
  - Endpoint URLs and secrets per tenant: WMS base URL, webhook targets, EasyPost/Shippo keys.

D) Operations and Reliability
- Idempotency
  - Idempotency-Key required on POST /v1/route; dedupe store keyed by (tenant_id, idempotency_key) in rma_events; exactly-once semantics for decision creation; egress guarded by outbox pattern with item-level status checks.

- Rate limiting & Backpressure
  - Token bucket per tenant; bursts allowed but throttled with 429 and Retry-After.
  - Queue buffering for egress; circuit breaker trips if downstream error rate > 20% for 1 minute; fall back to default rule (route to HQ) and log alerts.

- DR/HA
  - Multi-AZ/region for data store; daily backups; PITR (point-in-time recovery) enabled.
  - RTO 1 hour, RPO 15 minutes for transactional data; runbooks: webhook replay, DLQ drain, rule rollback.

- SLAs
  - Basic: 99.5% monthly uptime, p95 decision < 500 ms, standard support.
  - Pro: 99.9% uptime, p95 < 300 ms, priority support, dedicated queues, custom DR tests.

E) Monetization and Pricing
- Tiers
  - Standard: $299/month + $0.10 per routed RMA; shared queues; basic SLA.
  - Pro “Smart Logic”: $799/month + $0.15 per RMA; distance matrix + label optimization; dedicated egress queues; Pro SLA.
  - Enterprise: $1,499/month + $0.20 per RMA; custom mappings, compliance add-ons, SSO.

- Example Path to $10k MRR
  - 8 logos on Pro, each ~5k RMAs/month: 8 × ($799 + 5,000×$0.15 = $799 + $750) = 8 × $1,549 ≈ $12,392/month.
  - Or 12 logos mixed Standard/Pro averaging $850/month ≈ $10,200/month.

- Upsells
  - Premium SLAs; custom carrier rate shopping; dedicated compute/queues; audit/compliance exports; rule authoring assistance.

- Churn Defense
  - 3-month minimum term; onboarding playbooks; ROI dashboard (refund time, cost savings); monthly business reviews.

F) GTM: Cold Email + Upwork (Stealth)
- Cold Email (3-step)
  1. Problem: “Are Amazon/Walmart RMAs still routed by CSV/manual rules at [Company]?”
  2. Proof: “We cut returns routing time from 4h to 15m; p95 decision 250ms; nearest facility logic reduces cost ~18%.”
  3. CTA: “Pilot in 10 days—reply with your WMS and markets.”

  Subjects: “Automate returns routing in 48h”, “Stop misrouting RMAs”, “Sub-second RMA decisions.”

  Personalization tokens: {3PL name}, {WMS}, {marketplaces}, {daily returns volume}.

- Target Lists
  - Titles: 3PL Ops Manager, E-commerce Ops Lead.
  - Data sources: Job posts (WMS mentions), tech stack signals (BuiltWith), LinkedIn company tech notes; filter for >1k RMAs/month, ShipBob/3PL Central users.

- Upwork Listing
  - Headline: “Build Sub-Second Returns Router (RMA Logic + WMS/Labels)”
  - Scope: Marketplace webhook, rules engine, WMS task creation, label generation, audit.
  - Deliverables: API + docs; pilot for one marketplace/one WMS; ROI metrics; proof pack.

- Case Study Outline
  - Context: 3PL processing 8k RMAs/month.
  - Problem: Manual routing + shipping cost overrun.
  - Approach: Edge API, nearest facility logic, label optimization.
  - Metrics: -18% shipping cost, -2 days refund cycle, <300ms p95.
  - Testimonial: “Refund times down, ops team relieved.”

- Landing Page Wireframe
  - Headline: “Sub-Second Returns Routing—Decisions That Save Cost”
  - Outcomes: Faster refunds, cheaper labels, fewer misroutes.
  - Single CTA: “Start a 10-day pilot”
  - API docs link.

G) Competitive Landscape and Moat
- Alternatives: In-house scripts; iPaaS (Zapier/Make won’t handle complex routing); full-stack returns platforms (Loop/Returnly).
- Differentiators: Headless/edge speed; deep domain mappings; audit/compliance; fixed SLAs; nearest facility logic + label optimization out-of-the-box.
- Defensibility: Library of marketplace/WMS integrations; proprietary rules templates; data quality scores; outbox/egress reliability; switching costs via config and integrations.

H) Compliance, Legal, and Risk
- DPA basics, data residency (US/EU selectable); minimal PII—ZIP/country only; redact/remove personal fields at ingest.
- Moonlighting controls: agency entity, non-solicit of employer clients, defined work windows.
- SOC2-style controls checklist: access logs, change management, incident runbooks, backup/restore drills.

I) Day-by-Day Build Plan (30–45 days)
- Week 1: Schema and core endpoints (/route, /config/rules); JSON Logic evaluator; idempotency model; validation & normalization of reason codes.
  - Deliverables: ERD, API specs, unit tests for rules evaluator, initial docs.
  - Success: Sub-second decisions in dev; rule versioning working.

- Week 2: Marketplace ingest (Amazon Returns webhook) and WMS egress (ShipBob or 3PL Central); outbox pattern; HMAC webhooks; retries/DLQ.
  - Deliverables: End-to-end decision → WMS task create; DLQ viewer; audit logs.

- Week 3: Distance matrix (Mapbox/Google) and nearest facility routing; EasyPost/Shippo label generation; refund/no-return action plumbing.
  - Deliverables: Facility management, geo logic; label URL in decisions; compensating actions.

- Week 4: Observability (metrics, structured logs), ROI dashboard seeds, Pro tier SLA instrumentation; docs and Postman collection; pilot kickoff.
  - Deliverables: p95 latency <300ms at edge; pilot with one tenant; testimonial draft.

- Week 5–6 (optional): Add Walmart/Shopify webhooks; second WMS; rule editor UI (thin admin); enterprise features (SSO, dedicated queues).