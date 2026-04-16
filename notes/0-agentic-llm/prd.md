# PRD: Enterprise-Ready IT Helpdesk Agent

## 1. Introduction
The Enterprise-Ready IT Helpdesk Agent is a professional-grade assistant designed to resolve employee IT issues autonomously while maintaining strict security, observability, and reliability standards. It moves beyond simple "chatbots" by implementing robust guardrails, human-in-the-loop (HITL) workflows, and automated reliability patterns.

## 2. Core Pillars

### A. RAG (Retrieval-Augmented Generation)
- Uses ChromaDB to store and retrieve IT knowledge base (KB) articles.
- Implements semantic search to provide the agent with grounded, company-specific context.

### B. Observability (Arize Phoenix)
- Full OpenTelemetry-based tracing for every agent turn.
- Detailed breakdown of latency, token usage, and tool call accuracy.

### C. Security Guardrails
- **Prompt Injection Guard**: Heuristic and LLM-based scans for adversarial inputs.
- **Tool Scoping**: Role-based access control (RBAC) ensuring analysts can only read data, while supervisors can execute "high-stakes" actions.

## 3. Enterprise Features (The "Reliability Layer")

- **KB Freshness Pipeline**: A scheduled job that re-ingests changed KB articles. It uses **Chunk Versioning** (v1.x -> v2.x) to allow rolling back to a known-good embedding set if retrieval quality degrades.
- **Graceful Degradation**: 
    - **Database Failure**: If ChromaDB is empty or unreachable, the agent returns a scripted fallback: *"I'm having trouble right now — please call ext. 4357"*.
    - **LLM Failure**: If the LLM provider returns a rate-limit (429) or overload (503) error, the agent defaults to the same fallback instead of crashing.
- **Persistent Audit Log**: Every Human-in-the-Loop decision (approved/rejected) is recorded to an append-only Postgres store with row-level security. This is a mandatory requirement for **SOC 2** and other compliance frameworks.
- **Identity & Auth**: Planned integration with enterprise IdPs (Okta/Azure AD) to derive `SessionScope` roles from verified JWTs.
- **Rate Limiting**: Per-user token budgets and request quotas to prevent abuse or compromised accounts from flooding the system.
- **Retrieval Monitoring**: Tracking confidence score distribution to detect "embedding drift" or KB staleness before users encounter errors.
- **Red-Team Testing**: Automated CI suite targeting guardrails with known injection payloads.

## 4. User Stories

### US-001: Automated Knowledge Refresh
**Description:** As an IT Manager, I want the agent's knowledge to be updated automatically every night so that it doesn't provide outdated VPN or password reset instructions.
**Acceptance Criteria:**
- [ ] Pipeline runs on a schedule (Cron/Job).
- [ ] New articles are embedded and versioned.
- [ ] Rollback strategy is defined (can point to version N-1).

### US-002: Service Interruption Fallback
**Description:** As an Employee, I want the agent to give me a phone number if it's "feeling sick" rather than hallucinating a wrong answer or crashing with a technical error.
**Acceptance Criteria:**
- [ ] If ChromaDB query fails/returns empty, respond with fallback message.
- [ ] If LLM rate limit is hit, respond with fallback message.

### US-003: Compliance Evidence (Audit Log)
**Description:** As a Compliance Officer, I want to see a log of every time a human approved a "high-stakes" tool call so we can pass our SOC 2 audit.
**Acceptance Criteria:**
- [ ] Record Decision, Timestamp, Tool, Reviewer, and Reason.
- [ ] Data stored in a persistent, append-only database.

## 5. Functional Requirements

- **FR-1:** The system must perform a semantic search before answering any IT-related query.
- **FR-2:** Every tool call requiring a "write" action (e.g., `create_ticket`) must pause for HITL approval.
- **FR-3:** All HITL decisions must be logged to the persistent `audit_log` table.
- **FR-4:** The `AgentNode` must catch `RateLimitException` and return the `FALLBACK_RESPONSE`.

## 6. Non-Goals
- Real-time indexing of file changes (staying with scheduled/manual ingestion for stability).
- End-user identity verification within this prototype (handled via hardcoded session roles).
- Support for non-English KB articles in the initial version.

```mermaid
graph TD
    %% Node Definitions
    User(["👤 Employee / user input"])
    JWT["🔑 JWT Token<br>(Okta/Azure AD)"]
    
    subgraph KFP_Sub ["🔄 KB FRESHNESS PIPELINE"]
        Note_1["<b>Freshness:</b> Scheduled job re-ingests KB docs.<br>Uses Chunk Versioning v1.x -> v2.x for rollbacks."]
        Source[(Source Docs)] --> Ingest[Scheduled Ingest]
        Ingest --> Embed[Chunk & Embed]
        Embed --> Ver_Store["Versioned Store"]
    end
  
    Ver_Store -.-> SD_Search

    subgraph PIG_Sub ["🛡️ PROMPT INJECTION GUARD"]
        Note_2["<b>Security:</b> Heuristic & LLM-based scans check for adversarial intent."]
        HS_Scan[Heuristic scan] --> LC_Class[LLM classifier]
    end

    subgraph TS_Sub ["🔐 TOOL SCOPING"]
        Note_3["<b>Identity:</b> Role extracted from JWT maps to a set of allowed tool names."]
        Note_9["<b>Entitlements:</b> Permissions looked up from Okta FGA / OPA Service."]
        Note_8["<b>Persistence:</b> Scope cached in Redis for stateless multi-user clustering."]
        JWT --> Role["Role Extraction"]
        Role --> Auth_Svc[[Auth Service]]
        Auth_Svc --> Redis[(Redis Cache)]
        Redis --> Scope["SessionScope"]
    end

    subgraph RAG_Sub ["📚 RAG — CHROMADB"]
	    Note_4["<b>Reliability:</b> If ChromaDB is offline, triggers Scripted Fallback."]
        EQ_Embed[Embed query] --> SD_Search[Vector similarity search]
        SD_Search -->|KB Empty| FB_1["Scripted Fallback"]
        Conf_Gate{"Confidence<br/>&ge; threshold?"}
        SD_Search -->|Found| Conf_Gate
    end

    subgraph HITL_Sub ["🙋 HUMAN IN THE LOOP"]
        Note_5["<b>Asynchronous:</b> Agent suspends state to DB while awaiting human approval."]
        UG_Gate[Uncertainty gate]
        AG_Gate[Authorization gate]
        State_DB[(💾 State DB)]
        UG_Gate --> State_DB
        AG_Gate --> State_DB
        Audit_Store[("📁 Audit Log")]
        State_DB --> Audit_Store
    end

    subgraph LOOP_Sub ["🤖 LANGGRAPH AGENT LOOP"]
        Note_7["<b>Follow-up:</b> Flow returns to reasoning node after tool results for next steps."]
        Note_10["<b>Binding:</b> LLM only sees authorized tools; others are logically invisible."]
        Scope --> Filter["Filter Tools"]
        Filter --> Bind["Bind to LLM"]
        Bind --> MS_Reason["Multi-step reasoning"]
        TC_Gate{"High-stakes?"}
        MS_Reason --> TC_Gate
    end

    subgraph OBS_Sub ["🔭 OBSERVABILITY"]
	    Note_6["<b>Monitoring:</b> Tracking confidence drift to detect KB staleness."]
        FT_Trace[Full trace] --> LT_Metrics[Latency + Tokens]
    end

    %% Global Connections
    User --> PIG_Sub
    PIG_Sub -->|clean| TS_Sub
    PIG_Sub -->|injection| Block_Node(["🚫 Block + log"])
    TS_Sub --> LOOP_Sub
    LOOP_Sub --> OBS_Sub
    
    MS_Reason --> RAG_Sub
    Conf_Gate -->|low score| UG_Gate
    Conf_Gate -->|confident| LOOP_Sub
    
    State_DB -->|Resume| Tools_Node["🛠️ Exec Approved Action"]
    TC_Gate -->|write| AG_Gate
    TC_Gate -->|read| Tools_Node
    
    Tools_Node -->|Result Fed Back| MS_Reason
    OBS_Sub --> Done_Node(["📄 Final response"])

    %% Styling
    classDef container fill:#fff4dd,stroke:#d4a017,stroke-width:2px;
    classDef fallback fill:#ffebee,stroke:#c62828,stroke-width:1px;
    class PIG_Sub,TS_Sub,RAG_Sub,HITL_Sub,LOOP_Sub,OBS_Sub,KFP_Sub container;
    class FB_1 fallback;
```

## 7. Microservice Decomposition

In a production environment, this architecture is distributed across independent services to ensure scalability and fault isolation.

| Service | Component Responsibility | Communications |
| :--- | :--- | :--- |
| **Gateway / Security** | Prompt Injection (PIG), JWT Auth, and Entitlements (Okta/OPA). | Synchronous REST / gRPC |
| **Agent Orchestrator** | Multi-step reasoning (LangGraph), Tool steering, and State Management. | Websockets / Async REST |
| **Knowledge (RAG)** | ChromaDB retrieval, Confidence scoring, and Ingestion (KFP). | Internal API |
| **HITL Service** | Approval Dashboard, State Checkpointing (Redis), and Audit Logging. | Event-driven (Webhooks/MQ) |

```mermaid
graph TD
    %% Entry Point
    User(["👤 User"]) --> Gateway

    subgraph SG_GW ["🛡️ SECURITY & AUTH GATEWAY"]
        Gateway[API Gateway] --> JWT_Val[JWT Validation]
        JWT_Val --> PIG_Service[Prompt Injection Guard]
        PIG_Service --> Entitlements[Entitlement Lookup<br>Okta FGA / OPA]
    end

    subgraph SC_CORE ["🤖 AGENT CORE SERVICE"]
        Orchestrator["Graph Orchestrator<br>(LangGraph)"]
        Tool_Binding[Dynamic Tool Binding]
        Orchestrator --> Tool_Binding
    end

    subgraph SC_KNOW ["📚 KNOWLEDGE SERVICE (RAG)"]
        Search_API[Retrieval API] --> Vector_DB[(Vector DB<br>ChromaDB)]
        Freshness_Job[Ingestion Worker] --> Vector_DB
    end

    subgraph SC_HITL ["🙋 HITL SERVICE"]
        Pending_Queue[Approval Queue]
        State_Checkpoint[(State Persistence<br>Postgres/Redis)]
        Audit_Log[("📁 Archive Event Log")]
        
        Pending_Queue --> State_Checkpoint
        State_Checkpoint --> Audit_Log
    end

    %% Communication Flow
    Entitlements -->|Authorized & Clean| Orchestrator
    Orchestrator <-->|Context Retrieval| Search_API
    Orchestrator -->|High-Stakes Tool| Pending_Queue
    
    %% Post-Approval Signal
    Human_Reviewer(["👔 Supervisor"]) --> Pending_Queue
    Pending_Queue -->|Resume Signal| Orchestrator
    
    %% Execution & Final Output
    Orchestrator --> Tools[Tools Execution]
    Orchestrator --> Response(["📄 Final Response"])

    %% Global Observability
    OBS[["🔭 Centralized Tracing<br>(Arize Phoenix)"]]
    Orchestrator -.-> OBS
    Search_API -.-> OBS
    Gateway -.-> OBS

    %% Styling Boundaries
    style SG_GW fill:#e1f5fe,stroke:#01579b
    style SC_CORE fill:#fff3e0,stroke:#e65100
    style SC_KNOW fill:#e8f5e9,stroke:#1b5e20
    style SC_HITL fill:#f3e5f5,stroke:#4a148c
```