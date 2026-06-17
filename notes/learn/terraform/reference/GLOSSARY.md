# Terraform Glossary

## Core Concepts

**Backend** — Where Terraform stores its state file. For production Azure, this is Azure Blob Storage with AAD auth and blob leasing for concurrency control.

**Child Module** — Any module called via a `module {}` block. Should never contain provider or backend blocks.

**Data Source** — A `data {}` block that reads existing infra without creating, modifying, or destroying anything. Reference: `data.TYPE.NAME.ATTRIBUTE`.

**Dependency Graph** — The DAG Terraform builds from resource references. Determines creation order. Visualize: `terraform graph | dot -Tpng > graph.png`.

**for_each** — Meta-argument accepting `map` or `set(string)`. Creates one resource per key. Preferred over `count` for named resources.

**count** — Meta-argument accepting a number. Creates N instances, indexed 0-based. Use for toggle (0/1) or truly numeric repetition.

**depends_on** — Explicit dependency override. Use only when automatic reference-based deps aren't enough (e.g., IAM propagation delays).

**Dynamic Block** — Generates repeated nested blocks (not resources) within a resource using `for_each`.

**HCL** — HashiCorp Configuration Language. The declarative DSL for Terraform config.

**Idempotent** — Running `terraform apply` on an already-applied config produces zero changes.

**Implicit Dependency** — When resource A references `resource_b.attr`, Terraform automatically orders: create B, then A.

**Local Value (locals)** — A named expression computed inside a module. Accessed as `local.name`. Cannot be set by callers.

**Module** — Any directory of `.tf` files. The one you run `apply` in is the root module.

**Output** — A value exported from a module. Accessed as `module.NAME.output_name` from a parent.

**Provider** — Plugin that translates HCL → API calls. Downloaded by `terraform init`. Lock with `.terraform.lock.hcl`.

**State** — JSON mapping HCL to real cloud resource IDs. The source of truth. Never commit to git — store in remote backend.

**State Locking** — Prevents concurrent applies from corrupting state. Azure Blob uses blob lease acquisition.

**Workspace** — Isolated state file within the same config. `terraform workspace new staging`.

---

## CLI Commands

| Command | Purpose |
|---------|---------|
| `terraform init` | Download providers, initialize backend |
| `terraform init -upgrade` | Force refresh providers/modules |
| `terraform plan` | Preview changes |
| `terraform plan -out=tfplan` | Save plan artifact |
| `terraform apply` | Apply changes (prompts confirmation) |
| `terraform apply tfplan` | Apply a saved plan |
| `terraform apply -auto-approve` | Apply without prompt (CI only) |
| `terraform apply -target=resource.name` | Apply single resource |
| `terraform destroy` | Destroy all managed resources |
| `terraform validate` | Check syntax and type correctness |
| `terraform fmt -recursive .` | Format all .tf files |
| `terraform fmt -check -recursive .` | Check formatting (CI gate) |
| `terraform state list` | List all tracked resources |
| `terraform state show resource.name` | Full state for one resource |
| `terraform state mv old new` | Rename in state |
| `terraform state rm resource.name` | Remove from state (leaves Azure resource intact) |
| `terraform import resource.name /azure/id` | Import existing resource |
| `terraform output` | Show all outputs |
| `terraform output -raw name` | Single output as plain text |
| `terraform output -json` | All outputs as JSON |
| `terraform workspace list/new/select` | Manage workspaces |
| `terraform test` | Run .tftest.hcl test files |
| `terraform graph` | Print dependency graph (DOT) |

---

## Type Quick Reference

```hcl
string  number  bool                    # primitives
list(string)   set(string)  map(string) # collections
object({ key = type })                  # structural
optional(string, "default")             # TF 1.3+ optional object attr
any   null                              # special
```

## Version Constraint Operators

| Operator | Meaning |
|----------|---------|
| `= 3.100.0` | Exact only |
| `>= 3.100.0` | At least |
| `~> 3.100` | >= 3.100, < 4.0 ← recommended |
| `~> 3.100.0` | >= 3.100.0, < 3.101.0 (patch only) |
| `>= 1.6, < 2.0` | Range |
