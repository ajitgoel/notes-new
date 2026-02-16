You are an expert ML mentor and senior engineer. Guide me end‑to‑end to build a production‑ready machine learning application with a medium‑complexity example. Use the example: a Loan Default Risk Scoring app that predicts the probability a borrower will default and exposes this model via an API and lightweight dashboard for loan officers. The stack should be: Python, scikit‑learn + XGBoost, FastAPI, PostgreSQL, Docker, and basic CI/CD. Include privacy, fairness, and monitoring considerations. Drive the process in clearly defined phases, with concrete deliverables and code scaffolding at each step.

Constraints:

- Prioritize clarity and actionability; avoid long theory.

- At each phase, produce: a short plan, commands or code, and a checklist of outputs/artifacts.

- Assume I’m on macOS/Linux and using VS Code.

- Favor open datasets (e.g., UCI German Credit) and reproducible scripts.

- Use small but realistic samples to keep iterations fast.

- Include lightweight explainability (e.g., SHAP) and calibration.

- Provide template code blocks I can paste and run.

PHASE 0 — Project Setup

- Goal: Initialize repo, environment, and structure.

- Deliver:

 a. Repo layout (src/, notebooks/, data/, configs/, tests/, api/, docker/, ci/)

 b. pyproject.toml or requirements.txt

 c. Makefile for common tasks

- Provide commands to create and activate a virtual environment, install dependencies (numpy, pandas, scikit‑learn, xgboost, shap, fastapi, uvicorn, pydantic, sqlalchemy, psycopg2-binary, python-dotenv, pytest, black, isort, pre-commit).

- Output a minimal README with project goals and architecture diagram (ASCII okay).

PHASE 1 — Problem Framing & Requirements

- Goal: Precisely define the objective, stakeholders, and constraints.

- Deliver:

 ▫ Clear target: binary classification with calibrated default probability within 12 months.

 ▫ Decision thresholding policy (e.g., optimize F1 subject to minimum recall).

 ▫ Business KPIs: approval rate, expected loss reduction, false positive/negative costs.

 ▫ Ethical/fairness note: avoid disparate impact; audit performance across sensitive groups.

- Provide a requirements.md template and fill with this project’s specifics.

PHASE 2 — Data Acquisition & Schema

- Goal: Obtain dataset (e.g., UCI German Credit), define schema and data dictionary.

- Deliver:

 ▫ Script to download/cache data in data/raw/.

 ▫ Structured parquet/csv in data/processed/ with typed columns.

 ▫ Data dictionary (feature name, type, description, allowed values).

- Provide code to load and split train/validation/test with stratification and temporal logic if timestamp exists.

- Include basic PII handling (no direct identifiers; anonymize or exclude).

PHASE 3 — EDA & Data Quality

- Goal: Understand distributions, leakage, missingness, imbalance.

- Deliver:

 ▫ Notebook/script that computes summary stats, class balance, correlations.

 ▫ Data quality report (missing rates, outliers, invalid values).

 ▫ Leakage check (features that trivially reveal default).

- Provide code to generate plots (histograms, boxplots), and a concise markdown report.

PHASE 4 — Feature Engineering

- Goal: Build a reproducible pipeline.

- Deliver:

 ▫ scikit‑learn Pipeline: imputation, scaling, one‑hot for categoricals, ordinal handling if needed.

 ▫ Feature selection sanity (mutual information or model-based).

 ▫ Save fitted encoder artifacts.

- Provide code for trainable preprocessing with fit/transform and versioned artifacts in artifacts/preprocessing/.

PHASE 5 — Baseline Model

- Goal: Establish a simple, explainable baseline.

- Deliver:

 ▫ Logistic Regression with class_weight or simple resampling.

 ▫ Metrics: ROC‑AUC, PR‑AUC, accuracy, recall, precision, F1.

 ▫ Calibration curve and Brier score.

- Provide evaluation script that outputs metrics.json and plots in reports/.

PHASE 6 — Advanced Model & Hyperparameter Tuning

- Goal: Train XGBoost classifier.

- Deliver:

 ▫ Optuna/RandomizedSearchCV tuning on ROC‑AUC/PR‑AUC.

 ▫ Early stopping with validation set.

 ▫ Save best model and config to artifacts/models/.

- Provide reproducible training script with seeds, logging, and checkpointing.

PHASE 7 — Fairness & Explainability

- Goal: Audit subgroup performance; add local/global explanations.

- Deliver:

 ▫ Subgroup metrics by sensitive attributes (if available).

 ▫ SHAP summary plot and example-level explanations.

 ▫ Document any tradeoffs and mitigation strategies.

- Provide code producing subgroup_metrics.json and shap plots; include brief fairness.md.

PHASE 8 — Thresholding & Policy

- Goal: Turn probabilities into decisions.

- Deliver:

 ▫ Threshold selection using validation set considering asymmetric costs.

 ▫ Calibrated probabilities (Platt or isotonic) if needed.

 ▫ Decision function returning approve/decline plus reason codes.

- Provide a small policy module and serialize final threshold/config.

PHASE 9 — Packaging & API

- Goal: Serve the model via FastAPI.

- Deliver:

 ▫ Endpoint: POST /score with JSON payload → returns probability, decision, reasons.

 ▫ Pydantic schemas for request/response validation.

 ▫ Load model and preprocessing artifacts on startup; thread-safe inference.

- Provide FastAPI app.py, uvicorn launch command, and example curl.

PHASE 10 — Persistence & Audit Trail

- Goal: Log requests, predictions, and outcomes.

- Deliver:

 ▫ PostgreSQL schema: requests, predictions, outcomes, model_version.

 ▫ SQLAlchemy ORM models and migrations.

 ▫ Write‑behind logging with error handling.

- Provide docker-compose for Postgres and app network; .env handling.

PHASE 11 — Containerization & CI

- Goal: Dockerize and add basic CI.

- Deliver:

 ▫ Dockerfile for API; multi-stage for slimmer images.

 ▫ docker-compose to run API + DB locally.

 ▫ GitHub Actions: lint, test, build, push image.

- Provide YAML workflow and Make targets.

PHASE 12 — Monitoring & Drift

- Goal: Track performance post-deployment.

- Deliver:

 ▫ Metrics collection (latency, error rate, prediction distribution).

 ▫ Data drift checks (PSI/KL divergence) and alert thresholds.

 ▫ Simple dashboard (e.g., Streamlit or Grafana if available).

- Provide monitoring script and weekly report template.

PHASE 13 — Security & Privacy

- Goal: Basic hardening and compliance.

- Deliver:

 ▫ Input validation, rate limiting, confidential config storage.

 ▫ Document retention policy and anonymization.

 ▫ Minimal RBAC for dashboard if included.

- Provide security.md with justifications and TODOs.

PHASE 14 — Documentation & Handoff

- Goal: Clear user/dev docs.

- Deliver:

 ▫ README with architecture diagram, quickstart, API spec, and ops playbook.

 ▫ Model card describing data, metrics, limitations, ethical considerations.

 ▫ Versioned changelog and release notes.

START NOW. For each phase:

1. Brief plan.

2. Paste‑ready code/config snippets (keep concise).

3. Exact commands to run.

4. Checklist of artifacts I should see.

5. Short risks/notes specific to this phase.

Seed me with Phase 0 outputs immediately, then proceed in order. Keep responses structured and compact. Where helpful, use bold on short key terms only (e.g., Pipeline, Threshold, Drift).

-------------
Excellent, let's begin your journey to building a production-ready ML application. As your mentor, I will guide you through each phase with clear, actionable steps.

### **PHASE 0 — Project Setup**

#### **1. Plan**
Our first step is to create a clean, organized foundation for our project. This involves creating a standardized directory structure, initializing a Git repository for version control, setting up an isolated Python environment to manage dependencies, and creating a `Makefile` for automating common commands like running tests or formatting code. This initial setup is crucial for long-term maintainability.

#### **2. Code & Config Snippets**

Here are the commands and file contents to set up your project structure.

**a. Repository Layout**
This command creates the entire directory structure.

```bash
mkdir -p loan-risk-app/{src,notebooks,data/raw,data/processed,configs,tests,api,docker,ci,artifacts/preprocessing,artifacts/models,reports}
cd loan-risk-app
git init
touch notebooks/.gitkeep tests/.gitkeep configs/.gitkeep docker/.gitkeep ci/.gitkeep
```

**b. Python Dependencies (`requirements.txt`)**
Create a file named `requirements.txt` in the root of your `loan-risk-app` directory.

```text
# requirements.txt

# Core ML & Data
numpy
pandas
scikit-learn
xgboost
shap
optuna

# API & Serving
fastapi
uvicorn[standard]
pydantic

# Database & Persistence
sqlalchemy
psycopg2-binary
python-dotenv

# Tooling & Quality
pytest
black
isort
pre-commit
```

**c. Makefile for Common Tasks**
Create a file named `Makefile` in the root directory. This will be our command center.

```makefile
# Makefile

.PHONY: help install-dev format test run-api

# Default target
help:
	@echo "Makefile for Loan Default Risk Scoring App"
	@echo ""
	@echo "Usage:"
	@echo "  make install-dev    Install development dependencies"
	@echo "  make format         Format code with black and isort"
	@echo "  make test           Run pytest"
	@echo "  make run-api        Run the FastAPI server locally"
	@echo ""

# Environment setup
install-dev:
	python3 -m venv venv
	@echo "Virtual environment 'venv' created."
	@echo "Activate it with: source venv/bin/activate"
	@echo "Then run: pip install -r requirements.txt"

# Code quality
format:
	black .
	isort .

# Testing
test:
	pytest

# API
run-api:
	uvicorn api.main:app --host 0.0.0.0 --port 8000 --reload
```

**d. Minimal README**
Create a `README.md` file in the root directory.

```markdown
# Loan Default Risk Scoring Application

This project is an end-to-end machine learning application that predicts the probability of a loan applicant defaulting on their payments. It includes a trained model, a FastAPI for serving predictions, and infrastructure for deployment and monitoring.

## Project Goal
To provide loan officers with a reliable risk score to aid in their decision-making process, while ensuring fairness and transparency.

## High-Level Architecture

(Loan Officer) --> [Dashboard/UI] --> [FastAPI] --> [ML Model] --> [PostgreSQL]
                                         ^                |
                                         |                v
                                         +---------- [Audit Log]
```

#### **3. Commands to Run**
Execute these commands from your terminal within the `loan-risk-app` directory.

```bash
# 1. Create a Python virtual environment
python3 -m venv venv

# 2. Activate the virtual environment
source venv/bin/activate

# 3. Install the dependencies
pip install -r requirements.txt
```

#### **4. Checklist of Artifacts**
After completing this phase, your directory should look like this:
```
loan-risk-app/
├── Makefile
├── README.md
├── requirements.txt
├── venv/
├── api/
├── artifacts/
│   ├── models/
│   └── preprocessing/
├── ci/
├── configs/
├── data/
│   ├── processed/
│   └── raw/
├── docker/
├── notebooks/
├── reports/
├── src/
└── tests/
```

#### **5. Risks & Notes**
*   **Dependency Conflicts**: Using a `requirements.txt` is simple, but for more complex projects, consider a lock file (`poetry.lock` or `Pipfile.lock`) to ensure perfectly reproducible builds.
*   **Environment Drift**: Always remember to activate your virtual environment (`source venv/bin/activate`) before working on the project. This isolates your dependencies and prevents polluting your global Python installation.

---

With our project structure in place, we are ready to formally define the problem we're solving. Let's proceed to **Phase 1**.

Excellent. Let's move on to defining the problem with precision.

### **PHASE 1 — Problem Framing & Requirements**

#### **1. Plan**
This phase is about translating the business need into a concrete machine learning task. We will define our primary objective, identify who will use the model's output (stakeholders), and determine how we'll measure success both technically and in terms of business value. Crucially, we will also establish our ethical guardrails upfront, focusing on fairness. The output of this phase is a single, vital document: `requirements.md`.

#### **2. Code & Config Snippets**

Create a file named `requirements.md` in the root of your project. This document will serve as our project's charter.

```markdown
# requirements.md

# Project: Loan Default Risk Scoring

## 1. Objective & ML Formulation

- **Business Goal**: Reduce financial losses from loan defaults by providing loan officers with an accurate and fair risk assessment tool.
- **ML Objective**: Predict the probability that a borrower will default on a loan within the next 12 months.
- **Task Type**: **Binary Classification**.
  - **Class 0**: No Default (Loan is paid back).
  - **Class 1**: Default (Loan is not paid back).
- **Model Output**: A calibrated probability score between 0.0 and 1.0.

## 2. Stakeholders & Users

- **Primary Users**: Loan Officers. They will use the model's output (score, decision recommendation) to inform their final lending decision.
- **Secondary Stakeholders**:
  - **Risk Management Team**: Monitors overall portfolio risk and model performance.
  - **Compliance & Legal**: Ensures the model is fair, non-discriminatory, and adheres to regulations.
  - **Business Leadership**: Tracks the impact on key business metrics.

## 3. Decision-Making Policy

- The model's probability score must be translated into a decision (**Approve** / **Decline**).
- **Thresholding Strategy**: We will select a probability threshold that optimizes for the **F1-score** on a validation set, subject to a minimum **Recall of 0.85**.
- **Rationale**: We want to correctly identify a high percentage of actual defaulters (high recall) to minimize credit losses, while still maintaining a reasonable balance with precision (not declining too many good applicants). The exact threshold will be determined in a later phase.

## 4. Business KPIs & Success Metrics

- **Approval Rate**: The percentage of loan applications that are approved. We aim for this to not decrease significantly for qualified applicants.
- **Default Rate**: The percentage of approved loans that default. This should decrease.
- **Expected Loss Reduction**: The primary financial metric. Calculated as the monetary value of defaults prevented by the model.
- **Cost of Errors**:
  - **False Positive Cost (Low)**: We decline a loan to a good applicant. The cost is the lost revenue from that loan's interest.
  - **False Negative Cost (High)**: We approve a loan to a defaulting applicant. The cost is the entire outstanding loan principal.

## 5. Constraints & Ethical Considerations

- **Fairness**: The model must not exhibit significant disparate impact across sensitive groups (e.g., gender, age). Model performance (e.g., False Positive Rate) will be audited and compared across these groups.
- **Explainability**: Loan officers must be provided with reason codes for high-risk scores to facilitate transparency and potential customer feedback. We will use SHAP for this.
- **Privacy**: The system must not store or log any raw Personally Identifiable Information (PII) like names or social security numbers. Input features should be scrubbed of direct identifiers.
- **Latency**: The API prediction response time should be < 200ms for a single request.

```

#### **3. Commands to Run**

This phase is non-computational. The primary action is creating the file. You can use your editor or this command:

```bash
touch requirements.md
# Now, copy and paste the content above into the file.
```

#### **4. Checklist of Artifacts**

At the end of this phase, you should have one new file in your project root:
```
loan-risk-app/
├── requirements.md  <-- NEW
├── Makefile
├── README.md
...
```

#### **5. Risks & Notes**
*   **Vague Requirements**: The biggest risk in this phase is ambiguity. The `requirements.md` acts as a contract. Having a specific goal (e.g., "predict default within 12 months") and a clear policy (e.g., "optimize F1 with min recall") prevents confusion later.
*   **Stakeholder Buy-in**: In a real-world scenario, this document would be reviewed and approved by all stakeholders. Without this alignment, you risk building a model that doesn't solve the actual business problem or isn't trusted by its users.

Now that we have a clear definition of what we are building and why, we can proceed to get the raw materials. Let's move to **Phase 2**.

------
Alright, let's acquire and structure our data. This phase is about creating a reliable, repeatable process for turning raw data into a clean, usable format for our model.

### **PHASE 2 — Data Acquisition & Schema**

#### **1. Plan**
We will start by programmatically downloading the UCI German Credit dataset. This dataset is classic for credit scoring tasks and is already anonymized, which simplifies our privacy considerations. After downloading, we will write a script to clean it up: we'll add proper column names, map cryptic values to human-readable ones, and convert it to the efficient Parquet format. Finally, we'll create a stratified train-test split to ensure our model is trained and evaluated on representative data, and document our features in a data dictionary.

#### **2. Code & Config Snippets**

**a. Script to Download Data**
Create a new file at `src/data/download_data.py`. This script will fetch the data and save it locally.

```python
# src/data/download_data.py
import requests
from pathlib import Path

def download_german_credit_dataset():
    """Downloads the German Credit dataset if it doesn't exist."""
    
    # Define paths
    data_dir = Path("data/raw")
    output_file = data_dir / "german.data"
    
    # Create directory if it doesn't exist
    data_dir.mkdir(parents=True, exist_ok=True)
    
    # Check if file already exists
    if output_file.exists():
        print("Dataset already exists. Skipping download.")
        return

    # Download the file
    url = "https://archive.ics.uci.edu/ml/machine-learning-databases/statlog/german/german.data"
    print(f"Downloading dataset from {url}...")
    try:
        response = requests.get(url)
        response.raise_for_status()  # Raise an exception for bad status codes
        with open(output_file, 'wb') as f:
            f.write(response.content)
        print(f"Successfully saved dataset to {output_file}")
    except requests.exceptions.RequestException as e:
        print(f"Error downloading the file: {e}")

if __name__ == "__main__":
    download_german_credit_dataset()

```

**b. Script to Process and Split Data**
Create another file at `src/data/make_dataset.py`. This script will handle cleaning and splitting.

```python
# src/data/make_dataset.py
import pandas as pd
from sklearn.model_selection import train_test_split
from pathlib import Path

def create_dataset():
    """Processes the raw German Credit data and creates train/test splits."""
    
    # Define paths
    raw_path = Path("data/raw/german.data")
    processed_dir = Path("data/processed")
    processed_dir.mkdir(parents=True, exist_ok=True)

    # Define column names based on dataset documentation
    column_names = [
        "status_of_existing_checking_account", "duration_in_month", "credit_history", 
        "purpose", "credit_amount", "savings_account_bonds", "present_employment_since",
        "installment_rate_in_percentage_of_disposable_income", "personal_status_and_sex",
        "other_debtors_guarantors", "present_residence_since", "property", "age_in_years",
        "other_installment_plans", "housing", "number_of_existing_credits_at_this_bank",
        "job", "number_of_people_being_liable_to_provide_maintenance_for", "telephone",
        "foreign_worker", "credit_risk"
    ]

    # Load data
    df = pd.read_csv(raw_path, sep=' ', header=None, names=column_names)

    # --- Data Cleaning & Transformation ---
    # The target variable 'credit_risk' is 1 for good, 2 for bad.
    # We map it to the standard 0 (good/no-default) and 1 (bad/default).
    df['credit_risk'] = df['credit_risk'].map({1: 0, 2: 1})
    
    # --- PII Handling Note ---
    # This dataset is anonymized. No direct identifiers are present.

    # Split data
    print("Splitting data into train and test sets...")
    train_df, test_df = train_test_split(
        df, 
        test_size=0.2, 
        random_state=42, 
        stratify=df['credit_risk']
    )

    # Save processed files
    train_df.to_parquet(processed_dir / "train.parquet", index=False)
    test_df.to_parquet(processed_dir / "test.parquet", index=False)
    print(f"Train set shape: {train_df.shape}")
    print(f"Test set shape: {test_df.shape}")
    print(f"Processed data saved in {processed_dir}")

if __name__ == "__main__":
    create_dataset()

```

**c. Data Dictionary**
Create a `data_dictionary.md` file, perhaps in a new `docs/` folder for clarity.

```bash
mkdir docs
touch docs/data_dictionary.md
```

```markdown
# docs/data_dictionary.md

## German Credit Dataset - Data Dictionary

This document describes the features in the processed dataset (`train.parquet`, `test.parquet`).

| Feature Name             | Type        | Description                                       | Example Values                  |
| ------------------------ | ----------- | ------------------------------------------------- | ------------------------------- |
| **credit_risk**          | **Binary**  | **Target Variable**: 1 if default, 0 if no default. | 0, 1                            |
| status_of_existing_...   | Categorical | Status of the customer's checking account.        | `A11`, `A12`, `A13`, `A14`      |
| duration_in_month        | Numerical   | Duration of the credit in months.                 | 12, 24, 36                      |
| credit_history           | Categorical | Customer's past credit history.                   | `A30`, `A31`, `A32`, `A33`, `A34` |
| purpose                  | Categorical | Purpose of the loan.                              | `A40`, `A41`, `A42`, `A43`      |
| credit_amount            | Numerical   | The amount of credit requested.                   | 2500, 5000, 10000               |
| age_in_years             | Numerical   | Age of the applicant in years.                    | 25, 45, 60                      |
| ...                      | ...         | ...                                               | ...                             |

*(Note: The full dictionary can be found at the UCI dataset source page.)*
```

**d. Update Makefile**
Add a convenient command to your `Makefile` to run the entire data pipeline.

```makefile
# Makefile (add this new target)

.PHONY: data
data:
	@echo "Running data pipeline..."
	@python src/data/download_data.py
	@python src/data/make_dataset.py
```

#### **3. Commands to Run**
Now, execute the pipeline with one simple command from your project's root directory.

```bash
make data
```

#### **4. Checklist of Artifacts**
After running the command, your project will contain these new files:
```
loan-risk-app/
├── data/
│   ├── raw/
│   │   └── german.data  <-- NEW
│   └── processed/
│       ├── train.parquet  <-- NEW
│       └── test.parquet   <-- NEW
├── docs/
│   └── data_dictionary.md <-- NEW
├── src/
│   └── data/
│       ├── download_data.py <-- NEW
│       └── make_dataset.py  <-- NEW
...
```

#### **5. Risks & Notes**
*   **Data Provenance**: Always know where your data comes from. The script makes this clear. In a corporate environment, you would likely be connecting to a database or data warehouse, but the principle of a dedicated, version-controlled script is the same.
*   **Static Dataset**: We are using a fixed, historical dataset. In a real production system, you must plan for how new data will be ingested, processed, and used for retraining. We'll touch on this in the monitoring phase.
*   **Temporal Splits**: Our dataset lacks timestamps, so a random stratified split is appropriate. If we had dates, we would perform a **temporal split** (e.g., train on 2023 data, test on 2024 data) to better simulate how the model will perform on future, unseen data.

We now have clean, split, and documented data. The next logical step is to explore it to understand its characteristics. Let's proceed to **Phase 3**.

--------------
Excellent. Now that our data is clean and structured, we need to understand it. This is a crucial diagnostic step to inform our feature engineering and modeling strategies.

### **PHASE 3 — EDA & Data Quality**

#### **1. Plan**
We will perform Exploratory Data Analysis (EDA) exclusively on our **training dataset** (`train.parquet`) to avoid data leakage from the test set. Our goals are to:
1.  Compute summary statistics for a high-level overview.
2.  Check for missing values and other data quality issues.
3.  Analyze the class balance of our target variable (`credit_risk`), as this will heavily influence our modeling approach.
4.  Visualize the distributions of key numerical and categorical features.
5.  Check for any obvious features that might leak information about the target.

We'll use a Jupyter Notebook for this interactive work.

#### **2. Code & Config Snippets**

First, let's update our dependencies to include plotting libraries. Add these lines to your `requirements.txt`:

```text
# requirements.txt (add these)
matplotlib
seaborn
jupyterlab
```

Now, run this command to install them:
```bash
# Make sure your virtual env is active: source venv/bin/activate
pip install -r requirements.txt
```

Next, create a new Jupyter Notebook at `notebooks/01-EDA.ipynb`. Below are the code blocks to add to the notebook cells.

**Cell 1: Imports and Setup**
```python
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path

# Set plot style
sns.set_theme(style="whitegrid")

# Load the training data
data_path = Path("../data/processed/train.parquet")
df_train = pd.read_parquet(data_path)

print("Training data loaded successfully.")
print(f"Shape of the data: {df_train.shape}")
```

**Cell 2: High-Level Overview & Data Quality**
```python
print("--- Data Info ---")
df_train.info()

print("\n--- Missing Values ---")
print(df_train.isnull().sum().to_string())

print("\n--- Summary Statistics (Numerical) ---")
print(df_train.describe().T)

print("\n--- Summary Statistics (Categorical) ---")
print(df_train.describe(include='object').T)
```

**Cell 3: Target Variable Analysis (Class Balance)**
```python
plt.figure(figsize=(8, 5))
sns.countplot(x='credit_risk', data=df_train)
plt.title('Class Distribution of Credit Risk (0: No Default, 1: Default)')
plt.xlabel('Credit Risk')
plt.ylabel('Count')
plt.show()

# Print the exact numbers
balance = df_train['credit_risk'].value_counts(normalize=True)
print(f"Class Balance:\n{balance}")
```

**Cell 4: Numerical Feature Distributions**
```python
numerical_features = df_train.select_dtypes(include=np.number).columns.tolist()
# Remove target and irrelevant IDs if any
numerical_features.remove('credit_risk')

df_train[numerical_features].hist(bins=30, figsize=(15, 10), layout=(-1, 3))
plt.suptitle('Distributions of Numerical Features')
plt.tight_layout(rect=[0, 0, 1, 0.96])
plt.show()
```

**Cell 5: Categorical Feature Distributions**
```python
categorical_features = df_train.select_dtypes(include='object').columns.tolist()

# Plotting counts for a few key categorical features
fig, axes = plt.subplots(2, 2, figsize=(16, 12))
sns.countplot(y='purpose', data=df_train, ax=axes[0,0], order=df_train['purpose'].value_counts().index)
axes[0,0].set_title('Loan Purpose')

sns.countplot(y='credit_history', data=df_train, ax=axes[0,1], order=df_train['credit_history'].value_counts().index)
axes[0,1].set_title('Credit History')

sns.countplot(y='housing', data=df_train, ax=axes[1,0], order=df_train['housing'].value_counts().index)
axes[1,0].set_title('Housing Situation')

sns.countplot(y='status_of_existing_checking_account', data=df_train, ax=axes[1,1], order=df_train['status_of_existing_checking_account'].value_counts().index)
axes[1,1].set_title('Checking Account Status')

plt.tight_layout()
plt.show()
```

**Cell 6: Data Leakage Check (Conceptual)**
```python
# --- Conceptual Data Leakage Check ---
# We need to ensure no feature is a direct proxy for the target or would be unavailable at prediction time.
# Example of a leaky feature: 'date_of_default'.
# By inspecting the feature names from the data dictionary, we can see they are all attributes
# of the applicant at the time of the application (e.g., age, credit history, purpose).
# Conclusion: No obvious data leakage is present in this dataset.
print("Leakage Check: No features that would be unknown at prediction time were found.")
```

#### **3. Commands to Run**
Launch Jupyter Lab from your project's root directory:
```bash
jupyter lab
```
This will open a new tab in your browser. From there, navigate to the `notebooks/` directory, create a new notebook named `01-EDA.ipynb`, and paste the code from the cells above.

#### **4. Checklist of Artifacts**
Your project structure should now include the new notebook:
```
loan-risk-app/
├── notebooks/
│   └── 01-EDA.ipynb   <-- NEW
...
```
**Key Findings from EDA (Summary Report):**
*   **Data Quality**: The dataset is complete, with **zero missing values**. This simplifies our preprocessing needs.
*   **Class Imbalance**: The dataset is **imbalanced**. Approximately 70% of loans are "No Default" (class 0) and 30% are "Default" (class 1). This must be addressed during modeling (e.g., using `class_weight` or resampling techniques).
*   **Distributions**: Numerical features like `credit_amount` and `duration_in_month` are right-skewed, which is common in financial data. This suggests that scaling will be important for linear models.
*   **Leakage**: No features that would cause data leakage were identified.

#### **5. Risks & Notes**
*   **DO NOT TOUCH THE TEST SET**: Repeating this crucial point: all exploration, visualization, and decision-making must happen **only on the training data**. Using the test set for EDA will invalidate your final evaluation metrics, giving you a false sense of your model's real-world performance.
*   **Actionability**: EDA is not just for making plots. Each finding should lead to a decision. The finding of class imbalance directly leads to a modeling requirement. The finding of skewed data leads to a preprocessing requirement (scaling).

We now have a solid understanding of our data. The next step is to build a robust, reproducible pipeline to prepare this data for modeling. Let's proceed to **Phase 4**.

----------------

Of course. Let's build the engine of our ML application: the feature engineering pipeline. This phase is about creating a reproducible and robust process to transform raw data into a format suitable for machine learning models.

### **PHASE 4 — Feature Engineering**

#### **1. Plan**
We will use `scikit-learn`'s `Pipeline` and `ColumnTransformer` to build a single, cohesive preprocessing object. This object will apply different transformations to different types of columns:
*   **Numerical Features**: We will apply median imputation (to robustly handle potential missing values in future data) followed by standard scaling.
*   **Categorical Features**: We will use most-frequent imputation and then apply one-hot encoding. Using `handle_unknown='ignore'` is critical to ensure our API doesn't fail if it encounters a new category in production.

Finally, we will fit this pipeline on our training data and save the fitted object as a versioned artifact. This ensures that the exact same transformations are applied during training, evaluation, and inference.

#### **2. Code & Config Snippets**

First, create the directory for our new script:
```bash
mkdir -p src/features
```

Now, create the feature engineering script at `src/features/build_features.py`.

```python
# src/features/build_features.py
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.pipeline import Pipeline
from sklearn.impute import SimpleImputer
from sklearn.preprocessing import StandardScaler, OneHotEncoder
import joblib
from pathlib import Path

def build_feature_pipeline():
    """Builds and saves the feature engineering pipeline."""

    # --- 1. Define Paths & Create Directories ---
    train_data_path = Path("data/processed/train.parquet")
    artifacts_dir = Path("artifacts/preprocessing")
    preprocessor_path = artifacts_dir / "preprocessor-v1.joblib"
    
    artifacts_dir.mkdir(parents=True, exist_ok=True)

    # --- 2. Load Data ---
    print("Loading training data...")
    df_train = pd.read_parquet(train_data_path)
    X_train = df_train.drop('credit_risk', axis=1)

    # --- 3. Identify Feature Types ---
    numerical_features = X_train.select_dtypes(include=['int64', 'float64']).columns.tolist()
    categorical_features = X_train.select_dtypes(include=['object']).columns.tolist()
    print(f"Found {len(numerical_features)} numerical features.")
    print(f"Found {len(categorical_features)} categorical features.")

    # --- 4. Create Preprocessing Pipelines for Each Type ---
    # Numerical pipeline: impute missing values with the median and scale features.
    numeric_transformer = Pipeline(steps=[
        ('imputer', SimpleImputer(strategy='median')),
        ('scaler', StandardScaler())
    ])

    # Categorical pipeline: impute missing values with the most frequent and one-hot encode.
    categorical_transformer = Pipeline(steps=[
        ('imputer', SimpleImputer(strategy='most_frequent')),
        ('onehot', OneHotEncoder(handle_unknown='ignore', sparse_output=False))
    ])

    # --- 5. Combine into a Single ColumnTransformer ---
    preprocessor = ColumnTransformer(
        transformers=[
            ('num', numeric_transformer, numerical_features),
            ('cat', categorical_transformer, categorical_features)
        ],
        remainder='passthrough'
    )

    # --- 6. Fit the Preprocessor on Training Data ---
    print("Fitting the preprocessing pipeline...")
    preprocessor.fit(X_train)

    # --- 7. Save the Artifact ---
    print(f"Saving preprocessor artifact to {preprocessor_path}...")
    joblib.dump(preprocessor, preprocessor_path)
    print("Preprocessing pipeline created and saved successfully.")


if __name__ == "__main__":
    build_feature_pipeline()

```

Next, let's update our `Makefile` to include a command for this step.

```makefile
# Makefile (add this new target)

.PHONY: features
features:
	@echo "Building feature engineering pipeline..."
	@python src/features/build_features.py
```

#### **3. Commands to Run**

Execute the feature engineering script using our new `make` command:
```bash
make features
```

#### **4. Checklist of Artifacts**

After the script runs, you will have these new artifacts:
```
loan-risk-app/
├── src/
│   └── features/
│       └── build_features.py       <-- NEW SCRIPT
└── artifacts/
    └── preprocessing/
        └── preprocessor-v1.joblib  <-- NEW ARTIFACT
...
```

#### **5. Risks & Notes**
*   **Artifact Versioning**: Notice we saved the artifact as `preprocessor-v1.joblib`. This is crucial. If you change your feature engineering logic, you should save a new version (e.g., `v2`). Your model will be tightly coupled to a specific preprocessor version.
*   **The Golden Rule of Preprocessing**: We **fit** the preprocessor **only on the training data**. For our validation set, test set, and all future live data, we will only call **transform**. This prevents data leakage and ensures consistent feature representation.
*   **Feature Selection**: For this medium-complexity project, we are keeping all features. In a larger project, you might add a feature selection step to the pipeline (e.g., `SelectKBest` with mutual information) to reduce noise and model complexity.

With a robust feature pipeline in place, we are ready to train our first model. Let's proceed to **Phase 5** and establish a simple, solid baseline.

-------
Excellent. It's time to build our first model. A simple baseline is a critical step to establish a performance benchmark that any more complex model must beat.

### **PHASE 5 — Baseline Model**

#### **1. Plan**
We will train a **Logistic Regression** model. This model is a great baseline because it's fast, interpretable, and its performance is often surprisingly strong. To address the class imbalance we discovered during EDA (70/30 split), we will configure the model with `class_weight='balanced'`. This setting automatically adjusts weights inversely proportional to class frequencies, penalizing mistakes on the minority class (defaulters) more heavily.

Our evaluation will be comprehensive. We'll measure standard classification metrics (ROC-AUC, PR-AUC, F1-Score, etc.) and, importantly, assess the model's **calibration** using a calibration curve and the Brier score. For a risk scoring app, having probabilities that are trustworthy is just as important as getting the binary classification right.

#### **2. Code & Config Snippets**

First, let's create the necessary directories and the training script.

```bash
mkdir -p src/models reports
touch src/models/train_baseline.py
```

Now, here is the content for `src/models/train_baseline.py`.

```python
# src/models/train_baseline.py
import pandas as pd
import joblib
import json
import matplotlib.pyplot as plt
from pathlib import Path
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    roc_auc_score, 
    average_precision_score, 
    accuracy_score, 
    precision_score, 
    recall_score, 
    f1_score, 
    brier_score_loss,
)
from sklearn.calibration import CalibrationDisplay

def train_baseline():
    """Trains and evaluates a baseline Logistic Regression model."""

    # --- 1. Load Data and Preprocessor ---
    print("Loading data and preprocessor...")
    train_df = pd.read_parquet("data/processed/train.parquet")
    test_df = pd.read_parquet("data/processed/test.parquet")
    preprocessor = joblib.load("artifacts/preprocessing/preprocessor-v1.joblib")

    # Separate features and target
    X_train = train_df.drop("credit_risk", axis=1)
    y_train = train_df["credit_risk"]
    X_test = test_df.drop("credit_risk", axis=1)
    y_test = test_df["credit_risk"]

    # --- 2. Preprocess Data ---
    print("Preprocessing data...")
    X_train_processed = preprocessor.transform(X_train)
    X_test_processed = preprocessor.transform(X_test)

    # --- 3. Train Model ---
    print("Training baseline Logistic Regression model...")
    model = LogisticRegression(
        class_weight='balanced', 
        random_state=42, 
        solver='liblinear'
    )
    model.fit(X_train_processed, y_train)

    # --- 4. Make Predictions ---
    y_pred_proba = model.predict_proba(X_test_processed)[:, 1] # Probability of default
    y_pred = model.predict(X_test_processed)

    # --- 5. Evaluate Metrics ---
    print("Evaluating model...")
    metrics = {
        "roc_auc": roc_auc_score(y_test, y_pred_proba),
        "pr_auc": average_precision_score(y_test, y_pred_proba),
        "brier_score": brier_score_loss(y_test, y_pred_proba),
        "accuracy": accuracy_score(y_test, y_pred),
        "precision": precision_score(y_test, y_pred),
        "recall": recall_score(y_test, y_pred),
        "f1_score": f1_score(y_test, y_pred),
    }
    
    print("Baseline Metrics:")
    print(json.dumps(metrics, indent=2))
    
    # Save metrics
    reports_dir = Path("reports")
    reports_dir.mkdir(exist_ok=True)
    with open(reports_dir / "baseline_metrics.json", "w") as f:
        json.dump(metrics, f, indent=2)

    # --- 6. Plot and Save Calibration Curve ---
    fig, ax = plt.subplots()
    CalibrationDisplay.from_predictions(y_test, y_pred_proba, n_bins=10, ax=ax)
    ax.set_title("Baseline Model Calibration Curve")
    plt.savefig(reports_dir / "baseline_calibration_curve.png")
    print(f"Calibration curve saved to {reports_dir / 'baseline_calibration_curve.png'}")

    # --- 7. Save Model Artifact ---
    model_dir = Path("artifacts/models")
    model_dir.mkdir(exist_ok=True)
    model_path = model_dir / "logistic-regression-v1.joblib"
    joblib.dump(model, model_path)
    print(f"Model saved to {model_path}")


if __name__ == "__main__":
    train_baseline()

```

Finally, add the command to your `Makefile`.

```makefile
# Makefile (add this new target)

.PHONY: train-baseline
train-baseline:
	@echo "Training baseline model..."
	@python src/models/train_baseline.py
```

#### **3. Commands to Run**
Now, run the entire baseline training and evaluation process with a single command:
```bash
make train-baseline
```

#### **4. Checklist of Artifacts**
After the script completes, you will have created the following:
```
loan-risk-app/
├── src/
│   └── models/
│       └── train_baseline.py          <-- NEW SCRIPT
├── artifacts/
│   └── models/
│       └── logistic-regression-v1.joblib <-- NEW MODEL ARTIFACT
└── reports/
    ├── baseline_metrics.json          <-- NEW METRICS FILE
    └── baseline_calibration_curve.png <-- NEW PLOT
...
```

#### **5. Risks & Notes**
*   **Performance Floor**: The metrics in `baseline_metrics.json` are now our official benchmark. Our next model, XGBoost, must clearly beat these numbers (especially ROC-AUC and Brier score) to be considered an improvement.
*   **Calibration Insights**: Look at the `baseline_calibration_curve.png`. A perfectly calibrated model follows the diagonal line. The plot will show if our baseline tends to be over-confident (predicting high probabilities that are actually lower) or under-confident.
*   **Threshold-Independent Metrics**: Note that we are focusing on ROC-AUC, PR-AUC, and Brier score for now. These metrics evaluate the *quality of the probability scores* before a decision threshold is chosen. We'll decide on a threshold in a later phase.

We have a solid, well-evaluated baseline. Now we can bring in a more powerful model to see if we can improve performance. Let's proceed to **Phase 6**.

-------

With our baseline established, it's time to bring in a more powerful and flexible model. We'll use XGBoost, a favorite in many competitions and production systems for its high performance.

### **PHASE 6 — Advanced Model & Hyperparameter Tuning**

#### **1. Plan**
Our goal is to find the best version of an XGBoost model for our specific dataset. We won't just use default settings; we'll perform **hyperparameter tuning** using the **Optuna** library. The process will be:
1.  Split our main training data (`train.parquet`) into a smaller training subset and a validation subset.
2.  Define an "objective function" for Optuna. This function will train an XGBoost model with a specific combination of hyperparameters on the training subset.
3.  The model's performance (ROC-AUC) will be measured on the validation subset. **Early stopping** will be used to prevent overfitting and speed up the process.
4.  Optuna will intelligently search for the hyperparameter combination that yields the best validation score over a set number of trials.
5.  Once the best parameters are found, we will train a final XGBoost model using these parameters on the **entire original training dataset**.
6.  Finally, we will evaluate this model on the held-out test set and save the model, its parameters, and the performance metrics.

#### **2. Code & Config Snippets**

First, create a new script file:
```bash
touch src/models/train_xgboost.py
```

Now, place the following code into `src/models/train_xgboost.py`. This script is more complex as it incorporates the full tuning loop.

```python
# src/models/train_xgboost.py
import pandas as pd
import xgboost as xgb
import optuna
import joblib
import json
from pathlib import Path
from sklearn.model_selection import train_test_split
from sklearn.metrics import roc_auc_score

def train_advanced():
    """Tunes, trains, and evaluates an XGBoost model."""
    
    # --- 1. Load Data & Preprocessor ---
    print("Loading data and preprocessor...")
    train_df = pd.read_parquet("data/processed/train.parquet")
    preprocessor = joblib.load("artifacts/preprocessing/preprocessor-v1.joblib")

    # Separate features and target
    X = train_df.drop("credit_risk", axis=1)
    y = train_df["credit_risk"]

    # Create a train/validation split for hyperparameter tuning
    X_train, X_val, y_train, y_val = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )

    # Preprocess all data splits
    X_train_processed = preprocessor.transform(X_train)
    X_val_processed = preprocessor.transform(X_val)
    
    # --- 2. Hyperparameter Tuning with Optuna ---
    def objective(trial):
        # Calculate scale_pos_weight for handling class imbalance
        scale_pos_weight = (y_train == 0).sum() / (y_train == 1).sum()

        params = {
            "objective": "binary:logistic",
            "eval_metric": "auc",
            "booster": "gbtree",
            "n_estimators": trial.suggest_int("n_estimators", 100, 1000, step=100),
            "max_depth": trial.suggest_int("max_depth", 3, 10),
            "learning_rate": trial.suggest_float("learning_rate", 1e-3, 0.3, log=True),
            "subsample": trial.suggest_float("subsample", 0.5, 1.0),
            "colsample_bytree": trial.suggest_float("colsample_bytree", 0.5, 1.0),
            "gamma": trial.suggest_float("gamma", 0, 5),
            "scale_pos_weight": scale_pos_weight,
            "use_label_encoder": False,
            "random_state": 42,
        }

        model = xgb.XGBClassifier(**params)
        model.fit(
            X_train_processed, y_train,
            eval_set=[(X_val_processed, y_val)],
            early_stopping_rounds=50,
            verbose=False,
        )
        
        preds_proba = model.predict_proba(X_val_processed)[:, 1]
        auc = roc_auc_score(y_val, preds_proba)
        return auc

    print("Starting hyperparameter tuning with Optuna...")
    study = optuna.create_study(direction="maximize", study_name="xgb_tuning")
    study.optimize(objective, n_trials=50) # Use more trials for a real project

    print(f"Best trial found: {study.best_trial.number}")
    print(f"Best ROC-AUC on validation set: {study.best_value}")
    print("Best hyperparameters:")
    print(json.dumps(study.best_params, indent=2))
    
    # --- 3. Train Final Model on Full Training Data ---
    print("\nTraining final model on the full training dataset...")
    best_params = study.best_params
    scale_pos_weight = (y == 0).sum() / (y == 1).sum() # Recalculate for full dataset
    best_params.update({
        "objective": "binary:logistic",
        "eval_metric": "auc",
        "scale_pos_weight": scale_pos_weight,
        "use_label_encoder": False,
        "random_state": 42,
    })
    
    final_model = xgb.XGBClassifier(**best_params)
    
    # Preprocess the full training data
    X_processed_full = preprocessor.transform(X)
    final_model.fit(X_processed_full, y, verbose=False)
    
    # --- 4. Save Artifacts ---
    model_dir = Path("artifacts/models")
    joblib.dump(final_model, model_dir / "xgboost-v1.joblib")
    with open(model_dir / "xgboost-v1-params.json", "w") as f:
        json.dump(best_params, f, indent=2)
    
    print("Advanced model and its parameters have been saved.")

if __name__ == "__main__":
    train_advanced()
    # Note: A full evaluation on the test set will be done in the next phase
    # after we've also considered fairness and explainability.
```

Next, add the new command to your `Makefile`.

```makefile
# Makefile (add this new target)

.PHONY: train-advanced
train-advanced:
	@echo "Training advanced XGBoost model with Optuna..."
	@python src/models/train_xgboost.py
```

#### **3. Commands to Run**
Execute the tuning and training process. This will take a few minutes.
```bash
make train-advanced
```

#### **4. Checklist of Artifacts**
Your project will now contain the powerful XGBoost model and its configuration:
```
loan-risk-app/
├── src/
│   └── models/
│       └── train_xgboost.py        <-- NEW SCRIPT
└── artifacts/
    └── models/
        ├── xgboost-v1.joblib       <-- NEW MODEL ARTIFACT
        └── xgboost-v1-params.json  <-- NEW PARAMS FILE
...
```

#### **5. Risks & Notes**
*   **Tuning Overfitting**: It's possible to "overfit" to your validation set if you run too many trials or have a very small validation set. Optuna is good at avoiding this, but it's a risk to be aware of.
*   **Reproducibility**: We've set `random_state=42` where possible to make the process repeatable. Small variations can still occur due to the nature of some algorithms, but the results should be very close on subsequent runs.
*   **Computation Time**: Hyperparameter tuning is computationally expensive. We used 50 trials, which is fine for a demo. For a critical production model, you might run hundreds or even thousands of trials.

We now have a highly-tuned, powerful model. But a powerful model isn't necessarily a fair or trustworthy one. Let's address that next in **Phase 7**.

-------------
Absolutely. A model that is powerful but opaque or biased is a liability in production. This phase is about building trust and ensuring our model behaves responsibly.

### **PHASE 7 — Fairness & Explainability**

#### **1. Plan**
We will now evaluate our trained XGBoost model from two critical angles:
1.  **Fairness**: We will audit the model's performance across different demographic subgroups. The German Credit dataset contains a `personal_status_and_sex` feature. We'll use this to compare key error metrics (like the False Positive Rate) between male and female applicants to check for disparate treatment.
2.  **Explainability**: We will use the SHAP (SHapley Additive exPlanations) library to understand *why* our model makes the decisions it does. We will generate a **global** summary plot to see which features are most influential overall, and a **local** waterfall plot to explain a single, specific prediction—a feature essential for providing "reason codes" to loan officers.

#### **2. Code & Config Snippets**

First, create a new script for this combined evaluation.
```bash
touch src/models/evaluate_and_explain.py
```

Now, populate `src/models/evaluate_and_explain.py` with the following code.

```python
# src/models/evaluate_and_explain.py
import pandas as pd
import joblib
import json
import shap
import matplotlib.pyplot as plt
from pathlib import Path
from sklearn.metrics import confusion_matrix

def evaluate_and_explain():
    """Audits the model for fairness and generates SHAP explanations."""
    
    # --- 1. Load Artifacts & Data ---
    print("Loading artifacts and test data...")
    model = joblib.load("artifacts/models/xgboost-v1.joblib")
    preprocessor = joblib.load("artifacts/preprocessing/preprocessor-v1.joblib")
    test_df = pd.read_parquet("data/processed/test.parquet")
    
    # To get feature names after one-hot encoding for SHAP plots
    train_df = pd.read_parquet("data/processed/train.parquet")
    X_train = train_df.drop("credit_risk", axis=1)
    
    X_test = test_df.drop("credit_risk", axis=1)
    y_test = test_df["credit_risk"]
    
    # Preprocess data
    X_train_processed = preprocessor.transform(X_train)
    X_test_processed = preprocessor.transform(X_test)
    processed_feature_names = preprocessor.get_feature_names_out()

    y_pred = model.predict(X_test_processed)

    # --- 2. Fairness Audit ---
    print("Performing fairness audit...")
    fairness_metrics = {}
    
    # Define sensitive attribute: 'sex'
    # A21: male, divorced/separated | A22: female, divorced/separated/married | A23: male, single | A24: male, married/widowed
    X_test['sex'] = X_test['personal_status_and_sex'].apply(lambda x: 'male' if x in ['A91', 'A93', 'A94'] else 'female')
    
    for group in ['male', 'female']:
        group_mask = (X_test['sex'] == group)
        y_test_group = y_test[group_mask]
        y_pred_group = y_pred[group_mask]
        
        tn, fp, fn, tp = confusion_matrix(y_test_group, y_pred_group).ravel()
        
        fairness_metrics[group] = {
            "num_samples": len(y_test_group),
            "accuracy": (tp + tn) / (tp + tn + fp + fn),
            "false_positive_rate": fp / (fp + tn),
            "false_negative_rate": fn / (fn + tp),
        }
    
    print("Subgroup Metrics:")
    print(json.dumps(fairness_metrics, indent=2))
    
    reports_dir = Path("reports")
    with open(reports_dir / "subgroup_metrics.json", "w") as f:
        json.dump(fairness_metrics, f, indent=2)

    # --- 3. Explainability with SHAP ---
    print("\nGenerating SHAP explanations...")
    
    # Use a sample of the training set for the explainer background
    background_data = pd.DataFrame(X_train_processed, columns=processed_feature_names).sample(100, random_state=42)
    explainer = shap.TreeExplainer(model, data=background_data)
    
    # Calculate SHAP values for the test set
    shap_values = explainer(pd.DataFrame(X_test_processed, columns=processed_feature_names))

    # Global explanation: SHAP Summary Plot
    plt.figure()
    shap.summary_plot(shap_values, show=False, plot_type="bar", max_display=15)
    plt.title("SHAP Feature Importance Summary")
    plt.tight_layout()
    plt.savefig(reports_dir / "shap_summary.png")
    plt.close()
    print(f"SHAP summary plot saved to {reports_dir / 'shap_summary.png'}")
    
    # Local explanation: Waterfall plot for the first prediction
    plt.figure()
    shap.plots.waterfall(shap_values[0], show=False, max_display=15)
    plt.title("SHAP Explanation for a Single Prediction")
    plt.tight_layout()
    plt.savefig(reports_dir / "shap_local_example.png")
    plt.close()
    print(f"SHAP local plot saved to {reports_dir / 'shap_local_example.png'}")


if __name__ == "__main__":
    evaluate_and_explain()
```

Create a new markdown file `fairness.md` in the `docs/` directory.
```bash
touch docs/fairness.md
``````markdown
# docs/fairness.md

## Fairness Audit Report

This document outlines the fairness analysis conducted on the `xgboost-v1` model.

### 1. Sensitive Attribute
The audit was performed on the `sex` attribute, derived from the `personal_status_and_sex` feature in the original dataset.

### 2. Audit Results
The performance metrics for each subgroup on the test set are available in `reports/subgroup_metrics.json`.

**Key Findings (Example):**
- **False Positive Rate (FPR)**: This metric indicates the rate at which non-defaulters are incorrectly flagged as high-risk. A significant disparity here could mean one group is being unfairly denied loans more often.
- **False Negative Rate (FNR)**: This metric shows the rate at which actual defaulters are missed. A disparity could indicate the model is less effective at identifying risk for a certain group.

*(After running the script, you would analyze the output JSON and write a concrete summary here.)*

### 3. Mitigation Strategies & Next Steps
Based on the audit, if significant biases are found, the following steps could be considered:
- **Data Augmentation**: Collect more data for the underperforming subgroup.
- **Feature Engineering**: Re-evaluate or transform features that may be acting as strong proxies for the sensitive attribute.
- **Algorithmic Debiasing**: Employ techniques like re-weighting training samples or applying post-processing fairness constraints.

This audit serves as a baseline. Continuous monitoring for fairness drift is required post-deployment.
```

Finally, add the `make` command to your `Makefile`.

```makefile
# Makefile (add this new target)

.PHONY: evaluate
evaluate:
	@echo "Evaluating model fairness and generating explanations..."
	@python src/models/evaluate_and_explain.py
```

#### **3. Commands to Run**
Execute the evaluation and explanation script.
```bash
make evaluate
```

#### **4. Checklist of Artifacts**
This phase generates several important report artifacts:
```
loan-risk-app/
├── src/
│   └── models/
│       └── evaluate_and_explain.py  <-- NEW SCRIPT
├── docs/
│   └── fairness.md                  <-- NEW DOCUMENT
└── reports/
    ├── subgroup_metrics.json        <-- NEW FAIRNESS REPORT
    ├── shap_summary.png             <-- NEW GLOBAL EXPLANATION PLOT
    └── shap_local_example.png       <-- NEW LOCAL EXPLANATION PLOT
...
```

#### **5. Risks & Notes**
*   **Proxy Variables**: A model might not use `sex` directly, but it could learn bias from other features that are correlated with it (e.g., certain job types). This is the "proxy" problem, and it's why auditing by outcome is essential.
*   **Definition of Fairness**: We audited based on error rates (Equal Opportunity, Equalized Odds). There are many other mathematical definitions of fairness. Choosing the right one depends on the social and legal context of the problem.
*   **Actionable Explanations**: The local SHAP plot (`shap_local_example.png`) is the blueprint for our API's "reason codes". The top features in that plot are the primary drivers for that specific prediction.

Our model is now not only powerful but also scrutinized. We understand its biases and have a tool to explain its logic. Now, let's turn its probabilistic output into concrete decisions. Proceed to **Phase 8**.

-----------

With a fair and explainable model in hand, we now need to translate its probability scores into actionable business decisions. This is where we define and apply our decision policy.

### **PHASE 8 — Thresholding & Policy**

#### **1. Plan**
Our model produces a probability of default, a number between 0.0 and 1.0. A loan officer needs a clear recommendation: **Approve** or **Decline**. To achieve this, we need to select a **decision threshold**.

Following our `requirements.md`, we will use the **validation set** (the same one used for tuning) to find the optimal threshold. We will iterate through all possible thresholds and find the one that **maximizes the F1-score**, but only from the set of thresholds that achieve a **Recall of at least 0.85**. This enforces our business constraint of catching a high percentage of actual defaulters. We will then save this optimal threshold to a configuration file, which will be used by our API.

#### **2. Code & Config Snippets**

First, create the script that will determine the optimal threshold.

```bash
touch src/models/set_policy.py```

Now, populate `src/models/set_policy.py` with the following logic.

```python
# src/models/set_policy.py
import pandas as pd
import numpy as np
import joblib
import json
from pathlib import Path
from sklearn.model_selection import train_test_split
from sklearn.metrics import f1_score, recall_score, precision_recall_curve

def set_decision_policy():
    """Finds the optimal probability threshold based on project requirements."""

    # --- 1. Load Artifacts & Data ---
    print("Loading artifacts and data...")
    model = joblib.load("artifacts/models/xgboost-v1.joblib")
    preprocessor = joblib.load("artifacts/preprocessing/preprocessor-v1.joblib")
    train_df = pd.read_parquet("data/processed/train.parquet")
    
    # --- 2. Create the Same Validation Set as in Tuning ---
    X = train_df.drop("credit_risk", axis=1)
    y = train_df["credit_risk"]
    
    # Use the same split to ensure we're optimizing on the same data
    _, X_val, _, y_val = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    
    # Preprocess the validation data
    X_val_processed = preprocessor.transform(X_val)

    # --- 3. Get Probability Predictions for the Validation Set ---
    print("Getting model predictions on the validation set...")
    y_val_proba = model.predict_proba(X_val_processed)[:, 1]

    # --- 4. Find the Optimal Threshold ---
    # Our policy: Maximize F1-score subject to Recall >= 0.85
    MIN_RECALL = 0.85
    
    precisions, recalls, thresholds = precision_recall_curve(y_val, y_val_proba)
    
    # Find thresholds that meet the minimum recall constraint
    valid_threshold_indices = np.where(recalls >= MIN_RECALL)[0]
    
    if len(valid_threshold_indices) == 0:
        raise ValueError(f"No threshold found that satisfies Recall >= {MIN_RECALL}")

    # For each valid threshold, calculate the F1 score
    # Note: pr_curve thresholds array is 1 shorter than precisions/recalls
    f1_scores = (2 * precisions * recalls) / (precisions + recalls)
    valid_f1_scores = f1_scores[valid_threshold_indices]
    
    # Find the index and value of the best F1 score
    best_index = valid_threshold_indices[np.argmax(valid_f1_scores)]
    best_threshold = thresholds[best_index]
    best_f1 = f1_scores[best_index]
    best_recall = recalls[best_index]
    
    print(f"Optimal threshold found: {best_threshold:.4f}")
    print(f"  - F1-score at threshold: {best_f1:.4f}")
    print(f"  - Recall at threshold: {best_recall:.4f}")

    # --- 5. Save the Policy Configuration ---
    policy_config = {
        "model_version": "xgboost-v1",
        "preprocessor_version": "preprocessor-v1",
        "decision_threshold": best_threshold,
        "policy_description": f"Maximize F1-score subject to Recall >= {MIN_RECALL}"
    }
    
    config_path = Path("artifacts/models/policy_config.json")
    with open(config_path, "w") as f:
        json.dump(policy_config, f, indent=2)
    
    print(f"Policy configuration saved to {config_path}")


if __name__ == "__main__":
    set_decision_policy()

```

Next, add the corresponding command to your `Makefile`.

```makefile
# Makefile (add this new target)

.PHONY: set-policy
set-policy:
	@echo "Setting decision policy and threshold..."
	@python src/models/set_policy.py
```

#### **3. Commands to Run**
Execute the script to find and save the best threshold.
```bash
make set-policy
```

#### **4. Checklist of Artifacts**
This phase produces a single but critical configuration file.
```
loan-risk-app/
├── src/
│   └── models/
│       └── set_policy.py        <-- NEW SCRIPT
└── artifacts/
    └── models/
        └── policy_config.json   <-- NEW POLICY CONFIG
...
```

#### **5. Risks & Notes**
*   **Threshold Is a Bet**: The chosen threshold is an optimization based on *past* data. If the applicant pool or economic conditions change, this threshold may no longer be optimal. It's a key parameter that should be monitored and potentially re-evaluated over time.
*   **Calibration Check**: For this guide, we assumed the model's probabilities are well-calibrated. In a real-world scenario, you would plot a calibration curve on the validation set *before* this step. If it's poor, you would wrap your model with `CalibratedClassifierCV` to produce more reliable probabilities before finding the threshold.
*   **Business Impact**: The choice of a threshold has a direct and immediate impact on the business (e.g., approval rates). This decision should always be made in close consultation with business stakeholders, ensuring they understand the trade-off between risk (recall) and business volume (precision).

We have now fully defined our prediction logic, from raw features to a final decision. It's time to expose this logic to the world. Let's proceed to **Phase 9** and build our API.

----------
