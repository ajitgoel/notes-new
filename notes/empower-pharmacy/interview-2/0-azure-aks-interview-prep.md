# Azure AKS Interview Prep

## Core Concepts

### What is AKS?
- Azure Kubernetes Service — managed K8s control plane provided by Azure
- Microsoft manages the API server, etcd, scheduler, and controller manager
- You manage (and pay for) the worker nodes, networking, and storage
- Free control plane; you only pay for node VMs, storage, and networking

### AKS Architecture
- **Control Plane**: Managed by Azure — API server, etcd, cloud-controller-manager
- **Node Pools**: Groups of VMs (VMSS) running kubelet + container runtime
  - **System node pool**: Runs system pods (CoreDNS, metrics-server). At least one required.
  - **User node pools**: Your workloads. Can have different VM sizes, OS, scaling rules.
- **Virtual Network**: Each cluster gets a VNet (or uses your own via BYO VNet)
- **Azure AD Integration**: RBAC tied to AAD identities

---

## Networking

### Network Models
| Model | Pod IPs | Key Trait |
|-------|---------|-----------|
| **kubenet** | NAT'd behind node IP | Simple, limited to 400 nodes |
| **Azure CNI** | VNet IPs assigned to pods | Pods are first-class VNet citizens |
| **Azure CNI Overlay** | Overlay network for pods | Scales better, pods not directly on VNet |
| **Azure CNI with Dynamic IP** | From subnet, allocated on demand | Efficient IP usage |

### Key Networking Features
- **Ingress Controllers**: NGINX (self-managed) or Application Gateway Ingress Controller (AGIC)
- **Internal Load Balancer**: `service.beta.kubernetes.io/azure-load-balancer-internal: "true"`
- **Network Policies**: Calico (Azure or open-source) or Azure NPM for L4 traffic filtering
- **Private Clusters**: API server gets a private endpoint, no public IP
- **Service Mesh**: Istio-based add-on available as managed option

---

## Scaling

### Cluster Autoscaler
- Watches for pending pods that can't be scheduled due to resource constraints
- Adds nodes to the node pool (backed by VMSS)
- Configurable via `--min-count`, `--max-count` per node pool
- Respects PodDisruptionBudgets during scale-down

### KEDA (Kubernetes Event-Driven Autoscaling)
- AKS add-on for scaling based on external event sources
- Scales from/to zero — useful for queue-driven workloads
- ScaledObject CRD ties a deployment to a trigger (Service Bus queue depth, HTTP rate, etc.)

### Horizontal Pod Autoscaler (HPA)
- Scales pods based on CPU, memory, or custom metrics
- `kubectl autoscale deployment myapp --cpu-percent=50 --min=3 --max=10`

### Virtual Nodes (ACI burst)
- Burst to Azure Container Instances for sudden spikes
- Backed by the virtual kubelet — pods schedule to ACI as "virtual" nodes
- Linux only; no persistent volumes, daemonsets, or host networking

---

## Security

### Identity & Access
- **Azure AD Integration**: Authenticate `kubectl` via AAD, map groups to K8s ClusterRoles
- **Managed Identity**: System-assigned or user-assigned MI for cluster → Azure resource access
- **Workload Identity**: Federate K8s service accounts with AAD — pods get AAD tokens without secrets
- **Azure RBAC for K8s**: Manage K8s authorization directly through Azure role assignments

### Secrets & Config
- **Azure Key Vault Provider for Secrets Store CSI Driver**: Mounts Key Vault secrets as volumes
- Never store secrets in ConfigMaps or environment variables in plaintext
- **External Secrets Operator**: Alternative for syncing Key Vault → K8s Secrets

### Network Security
- Network Policies (Calico/Azure NPM)
- Private clusters + authorized IP ranges on API server
- Azure Firewall / NSGs on node subnets

### Pod Security
- **Pod Security Admission (PSA)**: Enforce, audit, or warn on pod security standards
- **Azure Policy for AKS (Gatekeeper/OPA)**: Built-in policies — no privileged containers, enforce resource limits, require labels, etc.
- Container image scanning via Microsoft Defender for Containers

---

## Storage

| Storage Type | Access Mode | Use Case |
|-------------|-------------|----------|
| Azure Disk (managed) | ReadWriteOnce | Databases, stateful single-pod |
| Azure Files (SMB/NFS) | ReadWriteMany | Shared file access across pods |
| Azure Blob (NFS/BlobFuse) | ReadWriteMany | Large unstructured data |

- **StorageClasses**: `managed-premium`, `managed-standard`, `azurefile-csi`, `azurefile-csi-premium`
- **CSI Drivers**: Disk, File, Blob, Key Vault — all first-class AKS add-ons
- **Persistent Volume Claims**: Dynamic provisioning via StorageClass is the norm

---

## Deployment & Operations

### Upgrade Strategies
- **Node image upgrade**: OS patches without K8s version change (`az aks nodepool upgrade --node-image-only`)
- **K8s version upgrade**: `az aks upgrade --kubernetes-version 1.29.0`
- **Blue-green node pools**: Add new pool → cordon/drain old pool → delete
- **Max surge**: Control upgrade speed (`maxSurge: 33%` on node pool)

### Observability
- **Azure Monitor / Container Insights**: Node and pod metrics, log collection to Log Analytics
- **Prometheus + Grafana**: Azure Managed Prometheus + Managed Grafana
- **Diagnostic settings**: API server logs, audit logs → Log Analytics or Event Hub

### CI/CD Patterns
- **GitOps with Flux v2**: AKS extension, reconciles cluster state from Git
- **Bridge to Kubernetes**: Debug services locally while connected to cluster
- **GitHub Actions / Azure DevOps**: Build → push ACR → deploy manifest/Helm chart

---

## Common Interview Questions

### Q: How do you handle zero-downtime deployments in AKS?
Rolling updates with `maxUnavailable: 0`, readiness probes, PodDisruptionBudgets, and pre-stop hooks to drain connections gracefully.

### Q: How would you secure an AKS cluster for production?
Private cluster + authorized IPs, AAD integration with RBAC, workload identity (no static creds), network policies, Azure Policy (Gatekeeper), Defender for Containers, Key Vault CSI for secrets, node auto-upgrade enabled.

### Q: Explain the difference between Azure CNI and kubenet.
Kubenet uses a bridge network — pods get IPs from a private range and NAT through the node. Azure CNI assigns each pod a real VNet IP, making pods directly routable from other VNet resources. CNI uses more IP space but simplifies connectivity.

### Q: When would you use Virtual Nodes?
Bursty workloads where you need fast scale-out without pre-provisioning VMs. E.g., batch processing jobs, CI runners, or handling flash traffic. Limitations: Linux only, no DaemonSets, limited volume support.

### Q: How does workload identity differ from pod identity?
Pod identity (aad-pod-identity) used NMI DaemonSets to intercept IMDS calls — deprecated. Workload identity uses K8s service account token federation with AAD (OIDC), is lighter, and works with Azure RBAC natively.

---

## Tags
#azure #aks #kubernetes #interview #devops