###### **1. How GitHub Actions Works** 
GitHub Actions uses YAML files in `.github/workflows/` to define automated pipelines. Each workflow triggers on Git events (push, pull request, etc.) and runs jobs on virtual machines.

```
.github/
└── workflows/
    ├── ci.yml           → runs on every push/PR
    ├── deploy.yml       → runs on merge to main
    └── nightly.yml      → runs on a schedule
```

Key concepts:

- **Workflow** — a YAML file defining the entire pipeline
- **Job** — a set of steps that run on the same runner (VM)
- **Step** — a single command or action
- **Action** — a reusable unit (e.g., `actions/checkout@v4`)

###### **2. Basic CI Workflow (Build + Test)** 

```yaml hl:11,14,16,20
# .github/workflows/ci.yml
name: CI Pipeline
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
env:
  DOTNET_VERSION: "8.0.x"
  NODE_VERSION: "20"
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --configuration Release
      - name: Run unit tests
        run: dotnet test --no-build --configuration Release --verbosity normal
```

###### **3. Running Unit Tests with Coverage** 

```yaml
# Job that runs unit tests and uploads coverage reports
unit-tests:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "8.0.x"
    - name: Run unit tests with coverage
      run: |
        dotnet test \
          --configuration Release \
          --collect:"XPlat Code Coverage" \
          --results-directory ./coverage \
          --filter "Category!=Integration"
    - name: Generate coverage report
      uses: danielpalme/ReportGenerator-GitHub-Action@5
      with:
        reports: coverage/**/coverage.cobertura.xml
        targetdir: coverage-report
        reporttypes: HtmlInline;Cobertura
    - name: Upload coverage artifact
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: coverage-report
    - name: Comment coverage on PR
      if: github.event_name == 'pull_request'
      uses: marocchino/sticky-pull-request-comment@v2
      with:
        path: coverage-report/Summary.txt
```

For a **Node.js/Jest** project:
```yaml
unit-tests-node:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: "npm"
    - run: npm ci
    - name: Run tests with coverage
      run: npm test -- --coverage --coverageReporters=text --coverageReporters=lcov
    - name: Enforce coverage threshold
      run: |
        COVERAGE=$(npx coverage-summary coverage/lcov.info)
        if [ "$COVERAGE" -lt 80 ]; then
          echo "Coverage is $COVERAGE%, minimum is 80%"
          exit 1
        fi
```

  ###### **4. Running Integration Tests** 
Integration tests typically need external services (databases, message queues). GitHub Actions supports **service containers** for this.

```yaml
integration-tests:
  runs-on: ubuntu-latest
  needs: unit-tests  # only run after unit tests pass
  services:
    postgres:
      image: postgres:16
      env:
        POSTGRES_USER: testuser
        POSTGRES_PASSWORD: testpass
        POSTGRES_DB: testdb
      ports:
        - 5432:5432
      options: >-
        --health-cmd="pg_isready"
        --health-interval=10s
        --health-timeout=5s
        --health-retries=5
    redis:
      image: redis:7
      ports:
        - 6379:6379
      options: >-
        --health-cmd="redis-cli ping"
        --health-interval=10s
        --health-timeout=5s
        --health-retries=5
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "8.0.x"
    - name: Run integration tests
      env:
        ConnectionStrings__Default: "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass"
        Redis__ConnectionString: "localhost:6379"
      run: |
        dotnet test \
          --configuration Release \
          --filter "Category=Integration" \
          --verbosity normal \
          --logger "trx;LogFileName=integration-results.trx"
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: integration-test-results
        path: "**/integration-results.trx"
```

###### **5. SonarQube / SonarCloud Scanning** 
SonarQube analyzes code quality, bugs, vulnerabilities, code smells, and coverage.
**Option A: SonarCloud (hosted, free for open source)** 
```yaml
sonar-analysis:
  runs-on: ubuntu-latest
  needs: unit-tests
  steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # full history needed for blame data
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "8.0.x"
    - name: Install SonarScanner
      run: dotnet tool install --global dotnet-sonarscanner
    - name: Begin SonarCloud analysis
      env:
        SONAR_TOKEN: ${{ secrets. SONAR_TOKEN }}
      run: |
        dotnet sonarscanner begin \
          /k:"my-org_my-project" \
          /o:"my-org" \
          /d:sonar.host.url="https://sonarcloud.io" \
          /d:sonar.token="${SONAR_TOKEN}" \
          /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
          /d:sonar.exclusions="**/Migrations/**,**/wwwroot/**"
    - name: Build
      run: dotnet build --configuration Release
    - name: Run tests with coverage
      run: |
        dotnet test --configuration Release \
          --collect:"XPlat Code Coverage;Format=opencover"
    - name: End SonarCloud analysis
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      run: dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
```

**Option B: Self-hosted SonarQube (Node.js project)** 
```yaml
sonar-self-hosted:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0
    - uses: actions/setup-node@v4
      with:
        node-version: 20
    - run: npm ci
    - run: npm test -- --coverage
    - name: SonarQube Scan
      uses: SonarSource/sonarqube-scan-action@v3
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        SONAR_HOST_URL: ${{ secrets.SONAR_HOST_URL }}
      with:
        args: >
          -Dsonar.projectKey=my-project
          -Dsonar.sources=src
          -Dsonar.tests=tests
          -Dsonar.javascript.lcov.reportPaths=coverage/lcov.info
    - name: Quality Gate check
      uses: SonarSource/sonarqube-quality-gate-action@v1
      timeout-minutes: 5
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

  

###### **6. Code Formatting & Linting** 

**.NET —** `dotnet format` 

```yaml
formatting:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "8.0.x"
    - name: Check code formatting
      run: dotnet format --verify-no-changes --verbosity diagnostic
    - name: Check for style violations
      run: dotnet format style --verify-no-changes
    - name: Check analyzers
      run: dotnet format analyzers --verify-no-changes
```

  

**Node.js — Prettier + ESLint** 

```yaml
lint-and-format:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: "npm"
    - run: npm ci
    - name: Check formatting (Prettier)
      run: npx prettier --check "src/**/*.{ts,tsx,js,jsx,json,css}"
    - name: Lint (ESLint)
      run: npx eslint "src/**/*.{ts,tsx}" --max-warnings 0
```

  

**Auto-fix and commit (use on push, not on PRs from forks)** 

```yaml
auto-format:
  runs-on: ubuntu-latest
  if: github.event_name == 'push'
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: "npm"
    - run: npm ci
    - name: Auto-fix formatting
      run: npx prettier --write "src/**/*.{ts,tsx,js,jsx}"
    - name: Commit changes
      uses: stefanzweifel/git-auto-commit-action@v5
      with:
        commit_message: "style: auto-format code"
```

  

###### **7. Full Production Pipeline (Everything Together)** 

```yaml
# .github/workflows/pipeline.yml
name: Full CI/CD Pipeline
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
permissions:
  contents: read
  pull-requests: write
  checks: write
jobs:
  # ──────────────── Stage 1: Format & Lint ────────────────
  format-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet format --verify-no-changes
  # ──────────────── Stage 2: Build ────────────────
  build:
    runs-on: ubuntu-latest
    needs: format-check
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - name: Cache build output
        uses: actions/cache@v4
        with:
          path: |
            **/bin/Release
            **/obj
          key: build-${{ github.sha }}
  # ──────────────── Stage 3: Unit Tests ────────────────
  unit-tests:
    runs-on: ubuntu-latest
    needs: build
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - uses: actions/cache@v4
        with:
          path: |
            **/bin/Release
            **/obj
          key: build-${{ github.sha }}
      - name: Run unit tests
        run: |
          dotnet test -c Release --no-build \
            --filter "Category!=Integration" \
            --collect:"XPlat Code Coverage;Format=opencover" \
            --logger "trx"
      - uses: actions/upload-artifact@v4
        with:
          name: coverage
          path: "**/coverage.opencover.xml"
  # ──────────────── Stage 4: Integration Tests ────────────────
  integration-tests:
    runs-on: ubuntu-latest
    needs: build
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
          POSTGRES_DB: testdb
        ports: ["5432:5432"]
        options: --health-cmd="pg_isready" --health-interval=10s --health-timeout=5s --health-retries=5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - uses: actions/cache@v4
        with:
          path: |
            **/bin/Release
            **/obj
          key: build-${{ github.sha }}
      - name: Run integration tests
        env:
          ConnectionStrings__Default: "Host=localhost;Database=testdb;Username=test;Password=test"
        run: |
          dotnet test -c Release --no-build \
            --filter "Category=Integration" \
            --logger "trx"
  # ──────────────── Stage 5: SonarCloud ────────────────
  sonar:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - uses: actions/download-artifact@v4
        with:
          name: coverage
      - run: dotnet tool install --global dotnet-sonarscanner
      - name: Run SonarCloud
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          dotnet sonarscanner begin \
            /k:"my-org_my-project" /o:"my-org" \
            /d:sonar.host.url="https://sonarcloud.io" \
            /d:sonar.token="${SONAR_TOKEN}" \
            /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
          dotnet build -c Release
          dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
  # ──────────────── Stage 6: Deploy (main only) ────────────────
  deploy:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests, sonar]
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    environment: production
    steps:
      - uses: actions/checkout@v4
      - name: Deploy to production
        run: echo "Deploy step — replace with your deployment script"
```

  

###### **8. Branch Protection Rules** 

Enforce the pipeline by configuring branch protection in GitHub:

1. Go to **Settings → Branches → Add rule** for `main`
2. Enable:

- **Require a pull request before merging**
- **Require status checks to pass** — select `format-check`, `unit-tests`, `integration-tests`, `sonar`
- **Require branches to be up to date before merging**
- **Require conversation resolution before merging**  
    This blocks merges to `main` unless all pipeline stages pass.

###### **9. Reusable Workflows** 

Extract shared logic into reusable workflows to avoid duplication across repos.

```yaml
# .github/workflows/reusable-dotnet-test.yml
name: Reusable .NET Test
on:
  workflow_call:
    inputs:
      dotnet-version:
        required: false
        type: string
        default: "8.0.x"
      test-filter:
        required: false
        type: string
        default: "Category!=Integration"
    secrets:
      SONAR_TOKEN:
        required: false
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build --filter "${{ inputs.test-filter }}"
```

Call it from another workflow:
```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]
jobs:
  test:
    uses: ./.github/workflows/reusable-dotnet-test.yml
    with:
      dotnet-version: "8.0.x"
      test-filter: "Category!=Integration"
    secrets:
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```
###### **10. Workflow Triggers Reference** 
```yaml
# Common triggers
on:
  push:
    branches: [main, develop]
    paths: ["src/**", "tests/**"]       # only run when these files change
    paths-ignore: ["docs/**", "*.md"]   # skip for docs-only changes
  pull_request:
    types: [opened, synchronize, reopened]
  schedule:
    - cron: "0 6 * * 1"                 # every Monday at 6 AM UTC
  workflow_dispatch:                     # manual trigger from GitHub UI
    inputs:
      environment:
        description: "Target environment"
        required: true
        default: "staging"
        type: choice
        options: [staging, production]
```
###### **11. Secrets & Environment Variables** 
Store sensitive values in **Settings → Secrets and variables → Actions**.
```yaml
# Access secrets in workflows
env:
  SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
  NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
# Environment-specific secrets
deploy:
  environment: production  # uses secrets scoped to "production" environment
  steps:
    - name: Deploy
      env:
        API_KEY: ${{ secrets.PROD_API_KEY }}
      run: ./deploy.sh
```

Never hardcode tokens, passwords, or connection strings in workflow files.