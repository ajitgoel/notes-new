# Terraform Cheatsheet — Empower PMS Reference

## Standard File Layout

```
empower-infra/
├── modules/
│   ├── aks/          variables.tf  outputs.tf  main.tf  versions.tf
│   ├── key-vault/
│   ├── service-bus/
│   ├── networking/
│   └── sql-server/
└── environments/
    ├── dev/          main.tf  providers.tf  terraform.tfvars
    ├── staging/
    └── prod/
```

---

## Variable Precedence (last wins)

1. `default` in variable block
2. `terraform.tfvars` (auto-loaded)
3. `*.auto.tfvars` (auto-loaded, alpha order)
4. `-var-file="prod.tfvars"` (CLI flag)
5. `-var="key=value"` (CLI flag)
6. `TF_VAR_name` env var ← highest

---

## Reference Syntax

```hcl
var.name                              # variable
local.name                            # local value
resource_type.resource_name.attribute # resource attribute
data.resource_type.name.attribute     # data source attribute
module.module_name.output_name        # module output
each.key / each.value                 # inside for_each
count.index                           # inside count
```

---

## Common Patterns

### Toggle resource on/off
```hcl
resource "azurerm_monitor_diagnostic_setting" "diag" {
  count = var.enable_diagnostics ? 1 : 0
  # ...
}
```

### Env-specific values via locals
```hcl
locals {
  aks_nodes = { dev = 1, staging = 2, prod = 3 }[var.environment]
  kv_sku    = var.environment == "prod" ? "premium" : "standard"
}
```

### Multi-instance with for_each (map)
```hcl
resource "azurerm_servicebus_topic" "topics" {
  for_each = toset(var.topic_names)
  name     = each.value
  # ...
}
# Reference: azurerm_servicebus_topic.topics["prescription-received"].id
```

### Dynamic nested blocks
```hcl
dynamic "network_acls" {
  for_each = var.ip_rules
  content {
    ip_rules       = [network_acls.value]
    default_action = "Deny"
    bypass         = ["AzureServices"]
  }
}
```

### Merge tags (right wins)
```hcl
locals {
  all_tags = merge(var.tags, {
    ManagedBy   = "Terraform"
    Environment = var.environment
  })
}
```

### Module call pattern
```hcl
module "aks" {
  source = "../../modules/aks"

  name_prefix         = local.name_prefix
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  aks_subnet_id       = module.networking.aks_subnet_id
  tags                = local.common_tags
}

# Access output:
output "cluster_id" { value = module.aks.cluster_id }
```

---

## Most-Used Functions

```hcl
merge(map1, map2)            # merge maps, right wins
concat(list1, list2)         # join lists
flatten([[a,b],[c]])         # → [a, b, c]
toset(list)                  # deduplicate, required for for_each
lookup(map, key, default)    # safe map lookup
coalesce(a, b, c)            # first non-null/non-empty
format("%s-%s", a, b)        # sprintf
lower("Value") → "value"    # string transform
jsonencode({...})            # HCL object → JSON string
file("path/to/file.sh")      # read file as string
templatefile("tmpl", vars)   # render template with vars
try(expr, fallback)          # catch errors, return fallback
```

---

## Azure Backend Config

```hcl
# bootstrap-state.sh — run ONCE before terraform init
az group create --name rg-tfstate --location westus2
az storage account create --name empowerpmsstate \
  --resource-group rg-tfstate --sku Standard_LRS \
  --min-tls-version TLS1_2 --allow-blob-public-access false
az storage container create --name tfstate \
  --account-name empowerpmsstate
az storage account blob-service-properties update \
  --account-name empowerpmsstate --enable-versioning true
```

```hcl
# providers.tf — one per environment, unique key
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-tfstate"
    storage_account_name = "empowerpmsstate"
    container_name       = "tfstate"
    key                  = "dev/core.tfstate"   # unique per env
    use_azuread_auth     = true
  }
}
```

---

## HIPAA-Required Config Checklist

| Resource | Required Setting |
|----------|-----------------|
| Key Vault | `purge_protection_enabled = true`, `soft_delete_retention_days = 90`, `enable_rbac_authorization = true`, `network_acls.default_action = "Deny"` |
| AKS | `local_account_disabled = true`, `azure_policy_enabled = true`, `oms_agent` → Log Analytics |
| SQL Server | `public_network_access_enabled = false`, `minimum_tls_version = "1.2"`, `azuread_administrator` block, extended auditing policy with 90-day retention |
| Service Bus Premium | `zone_redundant = true`, private endpoint for VNet isolation |
| All Resources | `Environment`, `ManagedBy`, `CostCenter` tags |

---

## Testing Commands

```bash
# Static analysis (run locally before every commit)
terraform fmt -recursive -check .
terraform validate
tflint --recursive
checkov -d . --framework terraform --compact

# Plan policy check
terraform plan -out=tfplan.binary
terraform show -json tfplan.binary > tfplan.json
conftest test tfplan.json --policy policy/

# HCL-native tests (TF 1.6+)
terraform test
terraform test -filter=tests/key_vault_test.tftest.hcl -verbose
```

---

## .gitignore for Terraform

```gitignore
# State files — never commit
*.tfstate
*.tfstate.backup
.terraform/

# Saved plans may contain secrets
*.tfplan
*.tfplan.binary

# Sensitive tfvars
*.auto.tfvars
terraform.tfvars

# DO commit: .terraform.lock.hcl
```
