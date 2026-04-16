Here's the full sequence we'll work through together. Everything builds on a single project: a small **Quote of the Day** app — a Python REST API backend + a simple Nginx frontend.

---

**Tutorial 1 — Pods: The Atomic Unit** Run and inspect individual pods manually. Get comfortable with lifecycle, probes, env vars, and debugging crashes before we automate anything.

**Tutorial 2 — Deployments: Reliability and Control** Wrap those pods in a Deployment. Practice scaling, rolling updates, and recovering from a bad rollout — the stuff that actually matters on-call.

**Tutorial 3 — Services: Internal and External Connectivity** Wire the API and frontend together using ClusterIP, then punch a hole to the outside world with NodePort/LoadBalancer. Debug the classic "no endpoints" trap.

**Tutorial 4 — Ingress: One Door, Many Rooms** Put a single Ingress in front of both services. Route `/api` → backend, `/` → frontend. Debug a broken routing config using curl and kubectl.

---

# Tutorial 1 — Pods: The Atomic Unit

## Goal

Manually run the API and frontend as individual pods, instrument them with probes and env vars, and get fluent at inspecting and debugging pod lifecycle events.

---

## Concept Recap

A Pod is the smallest schedulable unit in Kubernetes — one or more containers that share a network namespace and storage. In production you almost never create bare pods directly (Deployments do that for you), but understanding pod lifecycle is essential for debugging _anything_ in K8s. When a Deployment's pod crashes at 2am, you're reading pod events and logs, not Deployment logs.

**Probes matter more than people expect.** A `livenessProbe` tells Kubernetes "restart me if I'm stuck." A `readinessProbe` tells it "don't send me traffic until I'm ready." Getting these wrong causes either endless restart loops (liveness too aggressive) or traffic hitting pods that aren't initialized yet (missing readiness). In production, bad probes are a surprisingly common incident cause.

**Environment variables** are how you inject config without baking it into images — think database URLs, feature flags, API keys (via Secrets, but we'll keep it simple here). The pattern you learn here scales directly to ConfigMaps and Secrets later.

**Pod status vocabulary** — `Pending`, `Running`, `CrashLoopBackOff`, `OOMKilled`, `ImagePullBackOff` — each tells you something specific. You'll see several of these today.

---

## Step 1 — Run the API Pod

We'll use a real public image: `kennethreitz/httpbin` — a handy HTTP testing API. It'll play the role of our "Quote API" for now.

```yaml
# api-pod.yaml
apiVersion: v1
kind: Pod
metadata:
  name: quote-api
  labels:
    app: quote
    component: api
spec:
  containers:
    - name: api
      image: kennethreitz/httpbin
      ports:
        - containerPort: 80
      env:
        - name: APP_ENV
          value: "development"
        - name: MAX_RESULTS
          value: "10"
      livenessProbe:
        httpGet:
          path: /status/200
          port: 80
        initialDelaySeconds: 10
        periodSeconds: 15
        failureThreshold: 3
      readinessProbe:
        httpGet:
          path: /status/200
          port: 80
        initialDelaySeconds: 5
        periodSeconds: 10
        failureThreshold: 2
```

```bash
kubectl apply -f api-pod.yaml
```

**Verify:**

```bash
kubectl get pod quote-api
```

Expected output after ~15 seconds:

```
NAME        READY   STATUS    RESTARTS   AGE
quote-api   1/1     Running   0          20s
```

`1/1` means 1 container running out of 1 defined. If you catch it early you might see `0/1 Running` — that's the readiness probe not yet passing.

---

## Step 2 — Inspect It Properly

```bash
# High-level status + node placement
kubectl get pod quote-api -o wide

# Full details: env vars, probe config, events
kubectl describe pod quote-api

# Logs from the container
kubectl logs quote-api

# Follow logs live
kubectl logs quote-api -f

# Exec into it (like docker exec)
kubectl exec -it quote-api -- /bin/bash
```

Inside the shell, verify your env vars landed:

```bash
echo $APP_ENV
echo $MAX_RESULTS
exit
```

In `kubectl describe`, scroll to the **Events** section at the bottom. You'll see something like:

```
Normal  Scheduled  ...  Successfully assigned default/quote-api to ...
Normal  Pulled     ...  Successfully pulled image
Normal  Created    ...  Created container api
Normal  Started    ...  Started container api
```

This events trail is your first stop when a pod won't start.

---

## Step 3 — Run the Frontend Pod

```yaml
# frontend-pod.yaml
apiVersion: v1
kind: Pod
metadata:
  name: quote-frontend
  labels:
    app: quote
    component: frontend
spec:
  containers:
    - name: frontend
      image: nginx:1.25-alpine
      ports:
        - containerPort: 80
      env:
        - name: APP_ENV
          value: "development"
      readinessProbe:
        httpGet:
          path: /
          port: 80
        initialDelaySeconds: 3
        periodSeconds: 5
```

```bash
kubectl apply -f frontend-pod.yaml
kubectl get pods
```

Expected:

```
NAME             READY   STATUS    RESTARTS   AGE
quote-api        1/1     Running   0          2m
quote-frontend   1/1     Running   0          15s
```

---

## Step 4 — Quick Connectivity Check

Pods get cluster-internal IPs. Let's verify the API is actually responding:

```bash
# Get the pod IP
kubectl get pod quote-api -o jsonpath='{.status.podIP}'

# Curl it from inside the frontend pod
kubectl exec -it quote-frontend -- wget -qO- http://<POD_IP>/json
```

You should get a JSON response back. This is raw pod-to-pod communication — no Service involved yet.

---

## 🔥 Break It: The Crash Scenario

Now let's deliberately create a broken pod. **Before applying this — what do you think will happen? What status do you expect to see?**

```yaml
# broken-pod.yaml
apiVersion: v1
kind: Pod
metadata:
  name: quote-broken
spec:
  containers:
    - name: api
      image: kennethreitz/httpbin
      livenessProbe:
        httpGet:
          path: /status/200
          port: 80
        initialDelaySeconds: 3   # <-- way too short
        periodSeconds: 5
        failureThreshold: 1      # <-- zero tolerance
      readinessProbe:
        httpGet:
          path: /this-path-does-not-exist   # <-- always 404
          port: 80
        initialDelaySeconds: 2
        periodSeconds: 5
```

```bash
kubectl apply -f broken-pod.yaml
```

Watch what happens in real time:

```bash
kubectl get pod quote-broken --watch
```

Give it 30–60 seconds.

**Run these to diagnose:**

```bash
kubectl describe pod quote-broken
kubectl logs quote-broken
```

Look for in `describe`:

- The **Liveness probe failed** and **Readiness probe failed** warning events
- `Reason: Unhealthy` in the events
- Eventually `CrashLoopBackOff` or the container being killed and restarted

**The fix:** The readiness probe path is wrong, and the liveness initialDelay is too aggressive. Here's the corrected version:

```bash
# Delete and reapply with fixes
kubectl delete pod quote-broken
```

Change the broken pod YAML: set `initialDelaySeconds: 10`, `failureThreshold: 3`, and fix the path back to `/status/200`, then reapply.

---

## Cleanup

```bash
kubectl delete pod quote-api quote-frontend quote-broken
```

---

## Reflection Questions

Take a moment and think through these before we move on — I'll pick up on any of them if you want to dig in:

1. **The readiness probe failed but the pod never entered `CrashLoopBackOff` from that alone — why?** What's the functional difference between a failing liveness vs. a failing readiness probe in terms of what Kubernetes _does_ about it?
    
2. **You set `initialDelaySeconds: 5` on the readiness probe.** What happens to incoming traffic during those 5 seconds if this pod were behind a Service? Is that a problem?
    
3. **The pod IP you curled directly — what happens to that IP if the pod is deleted and recreated?** What problem does that create for the frontend trying to talk to the API?
    
4. **You had two separate pod definitions for api and frontend.** Could you have put both containers in the same pod? When would that be the right call vs. wrong call?
    
5. **If `image: kennethreitz/httpbin` were misspelled as `httpbin-kentreitz`, what status would the pod show, and where exactly in `kubectl describe` would you find the error message?**
    

---

**When you're ready, say "next" and we'll move to Tutorial 2 — Deployments.** Or fire any questions/observations from what you just ran. 🚀