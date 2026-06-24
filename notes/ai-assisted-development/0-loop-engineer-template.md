https://github.com/JayZeeDesign/loop-engineer-template

**High‑level: what you want the loop to do** 
You’re basically asking for a **self‑healing code loop** around a Spring app:
1. Claude Code edits the repo to implement a change you describe.
2. It runs:
- `mvn install`
- `mvn spring-boot:run` (or your `spring:run` equivalent)
- `curl http://localhost:8080/…` and asserts `200` and expected JSON/text.
1. It also runs `mvn test`.
2. If any step fails, Claude reads the errors, fixes code or config, and tries again.
3. It repeats until the full check pipeline passes, then commits/PRs.
The loop‑engineer template already gives you the harness pattern; you just need to plug in “Spring checks” as the verification gate and wrap it in a Claude Code loop.

**Step 1: Prepare the knowledge base + harness** 
In your **loop-engineer-template** repo:
1. Fill in `CLAUDE.md` with:
- A short description of your Spring app (domain, tech stack).
- Where the main module lives (e.g., `backend/app-service`).
- How to run it:  
    `mvn install && mvn spring-boot:run`
- How to check health: endpoint + expected response (e.g. `/api/health` returns `{"status":"OK"}`).
1. In `ARCHITECTURE.md`, keep the default structure; just make sure it mentions:
- That there will be a _“spring-app”_ domain (loop) responsible for code changes.
- That verifying a change = build + run + curl + tests pass.
1. Run the `/setup-codebase-harness` skill in Claude Code, pointed at your Spring repo (not the loop repo) so it:

- Sets up dev scripts (`dev.local`, etc.) if needed.
- Makes the repo “legible” and “executable” for the agent.

(If you don’t want to touch `ship-change.js` yet, the harness still helps a lot even with manual loops.)

**Step 2: Define a “Spring change loop” domain** 

In the loop repo, under `domains/`, create e.g. `domains/spring-change/README.md` with a simple contract, something like:

```md
# Spring Change Loop

## Goal
Autonomously implement small application changes in the Spring Boot app,
then verify them end-to-end:
- mvn install succeeds
- app starts with mvn spring-boot:run
- HTTP check returns 200 + expected payload
- mvn test succeeds

## Workflow
1. Read latest backlog/task from tasks/ (or take human prompt).
2. Modify the Spring repo to satisfy the request.
3. Run verification script (build, run, curl, tests).
4. If any step fails:
   - Inspect logs and test failures.
   - Apply a minimal fix.
   - Re-run verification.
5. When all checks pass:
   - Commit and/or open a PR.
   - Log the work into LOG.md and relevant artifacts.

## Timeline
- 2026-06-20: Initial loop defined.
```

  

You can ask Claude Code (inside the loop repo) to scaffold this for you:

“Create a new loop domain called `spring-change` that does what I described above and write the contract into `domains/spring-change/README.md` following this repo’s conventions.”

**Step 3: Add a verification script in the application repo** 

In your Spring app repo, create a script the agent can call that **does exactly the checks you want**, for example `scripts/verify_spring_change.sh`:

```bash
#!/usr/bin/env bash
# scripts/verify_spring_change.sh

set -euo pipefail

APP_PORT=${APP_PORT:-8080}
HEALTH_PATH=${HEALTH_PATH:-/api/health}

echo "[verify] mvn install"
mvn -q clean install

echo "[verify] start app"
# Run app in background
mvn -q spring-boot:run \
  -Dspring-boot.run.jvmArguments="-Dserver.port=${APP_PORT}" \
  > target/app.log 2>&1 &
APP_PID=$!

# Ensure cleanup
cleanup() {
  echo "[verify] stopping app (pid=${APP_PID})"
  kill "${APP_PID}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# Wait for app to become healthy
echo "[verify] waiting for app..."
for i in $(seq 1 30); do
  if curl -s "http://localhost:${APP_PORT}${HEALTH_PATH}" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

echo "[verify] curl health endpoint"
HTTP_CODE=$(curl -s -o /tmp/health_body -w "%{http_code}" \
  "http://localhost:${APP_PORT}${HEALTH_PATH}")

if [ "${HTTP_CODE}" != "200" ]; then
  echo "[verify] expected 200, got ${HTTP_CODE}"
  echo "Body:"
  cat /tmp/health_body || true
  exit 1
fi

echo "[verify] mvn test"
mvn -q test

echo "[verify] SUCCESS"
```

  

Key thing: this script **fails the process** (`exit 1`) on any problem. That gives your loop a binary “pass/fail” it can trust.

**Step 4: Teach Claude Code to use that script as the gate** 

Now, inside the Spring repo, you want the harness / PR skill to run that script before shipping.

If you open `.claude/skills/pr/` (or whatever `ship-change.js` references), adjust the verification step conceptually like:

```js
// .claude/workflows/ship-change.js (conceptual – adapt to exact file)
module.exports = async function shipChange({ run }) {
  // ... earlier steps: create worktree, apply changes, etc.

  // Verification gate
  await run({
    command: "chmod +x scripts/verify_spring_change.sh && scripts/verify_spring_change.sh",
    description: "Build app, run it, curl health endpoint, run tests",
    onError: "Collect logs, summarize failures for the agent, and fix issues",
  });

  // ... if we reach here, verification passed; proceed to PR creation
};
```

  

Even if your existing workflow file looks a bit different, the key idea is:

- There is **one command** the loop considers “the test suite”.
- Claude sees stdout/stderr from that command.
- On failure, the workflow routes back into “fix mode” (Claude edits code), then re-runs the command.

If you prefer not to edit JS yet, you can do it purely by prompting inside Claude Code:

“For any change you make in this Spring repo, always run `scripts/verify_spring_change.sh`. If it fails, read the logs, fix the code, and re-run the script until it succeeds.”

The template’s benefit is that you can later codify this rule in the `ship-change` workflow so it’s not just prompt‑based.

**Step 5: Running a looped change end‑to‑end** 

With this setup, a typical session in **Claude Code** looks like:

1. Open the **loop-engineer-template** repo in Claude Code.
2. Tell it something like:  
    “Use the `spring-change` loop to update my Spring app (in `../my-spring-app`) so that `/api/health` returns a JSON body that includes a new field `version` with value `1`. Use the codebase harness and verification script.”
3. The loop will:

- Read `CLAUDE.md`, `ARCHITECTURE.md`, and `domains/spring-change/README.md`.
- Open the Spring repo, locate the controller/handler, and modify it.
- Run `scripts/verify_spring_change.sh`.
- If Maven build, app startup, curl, or tests fail, it will:

- Inspect `target/app.log`, Maven output, and test reports.
- Make a targeted fix.
- Re-run the script.

- Once `verify_spring_change.sh` exits with success, it will:

- Commit and/or open a PR (depending on how your `pr` skill is configured).
- Append an entry to `LOG.md` and any relevant artifacts (e.g. “tasks” folder).

From your point of view, you give **one instruction (“change behavior to X and verify with pipeline”)**; the loop takes care of retry/fix cycles until the `mvn install + run + curl + mvn test` pipeline is green.

**Practical tips / gotchas** 

- Make the verification script **fast-ish**. Full `mvn clean install` can be slow; sometimes `mvn -q -DskipTests=false test` plus a lighter `spring-boot:run` is enough.
- Be explicit in `CLAUDE.md` about:

- The health URL.
- Ports.
- Any required env vars / DBs (consider testcontainers or H2 profile).

- Don’t let the same agent both write code and “rubber‑stamp” it purely in text. The **shell script + exit code** is the trustworthy part; Claude’s job is to fix failures, not to declare success.

If you want, you can paste your existing `.claude/workflows/ship-change.js` and I can show the exact modifications to wire `verify_spring_change.sh` into that loop.