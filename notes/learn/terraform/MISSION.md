# Mission: Terraform

## Why
Ajit is standing up production-grade cloud infrastructure for Empower PMS on Azure — AKS clusters, Service Bus, Key Vault, SQL Server, and a full HIPAA-compliant landing zone. Terraform is the tool that turns that intent into repeatable, auditable, version-controlled infrastructure that can be promoted from dev → staging → prod without manual portal clicks.

## Success looks like
- Write a complete Terraform project from scratch: providers, variables, outputs, modules
- Model real Azure resources: Resource Groups, Key Vault, AKS, Service Bus, SQL Server
- Use workspaces and remote state (Azure Storage backend) to manage dev/staging/prod
- Structure modules the way a senior platform engineer would (reusable, composable)
- Run the full plan → apply → destroy lifecycle with confidence
- Read and reason about someone else's Terraform codebase

## Constraints
- Learning is tied to Azure; AWS/GCP examples are secondary
- Ajit has 30 years of Java/engineering experience — skip beginner hand-holding, go direct
- Lessons should be completable in 20–40 minutes each
- Practical over theoretical — every lesson should produce runnable `.tf` files

## Out of scope
- Pulumi, CDK, Bicep, or ARM template comparisons (for now)
- Terragrunt (revisit after modules are solid)
- Terraform Cloud / Enterprise licensing details
