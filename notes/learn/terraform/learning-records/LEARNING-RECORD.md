# Terraform Learning Record — Ajit

## Curriculum: Terraform for Empower PMS (Azure)
**Target:** Production-grade Terraform for HIPAA-compliant pharmacy platform on AKS

---

## Lesson Completion

| # | Lesson | Status | Score | Notes |
|---|--------|--------|-------|-------|
| 1 | HCL Fundamentals & Project Structure | ⬜ | —/4 | |
| 2 | Providers, State & Azure Backend | ⬜ | —/3 | |
| 3 | Variables, Outputs & Locals | ⬜ | —/4 | |
| 4 | Expressions, Functions & Meta-Arguments | ⬜ | —/4 | |
| 5 | Modules: Reusable Infrastructure | ⬜ | —/4 | |
| 6 | Workspaces & Multi-Env Patterns | ⬜ | —/3 | |
| 7 | Azure Core Resources for Empower PMS | ⬜ | —/4 | |
| 8 | Testing, Linting & Best Practices | ⬜ | —/4 | |

**Total:** —/30

---

## Key Decisions to Resolve

- [ ] State backend storage account name: ________________
- [ ] Multi-env strategy: ⬜ Workspaces  ⬜ Directory-per-env
- [ ] Module repo strategy: ⬜ Monorepo  ⬜ Separate module repo
- [ ] Terraform version pin: ________________
- [ ] azurerm provider version pin: ________________

---

## Open Questions

1.
2.
3.

---

## Post-Course Build Phases

1. Phase 2 — modules/networking: VNet, subnets, service endpoints
2. Phase 3 — modules/aks: system + user pools, AAD RBAC, OMS
3. Phase 4 — modules/key-vault + AKS Workload Identity
4. Phase 5 — modules/service-bus: 9 bounded-context topics/subs
5. Phase 6 — modules/sql-server: one DB per bounded context
6. Phase 7 — Azure DevOps pipeline: plan artifact + approval gate
7. Phase 8 — checkov + OPA HIPAA policy enforcement
