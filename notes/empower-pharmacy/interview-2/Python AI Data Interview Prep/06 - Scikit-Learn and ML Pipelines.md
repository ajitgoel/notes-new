# Scikit-Learn and ML Pipelines

## The sklearn API Pattern

Every estimator follows the same interface:

```python
from sklearn.ensemble import RandomForestClassifier

model = RandomForestClassifier(n_estimators=100, random_state=42)
model.fit(X_train, y_train)       # learn from data
predictions = model.predict(X_test)  # predict
probas = model.predict_proba(X_test) # class probabilities
score = model.score(X_test, y_test)  # default metric
```

Three types of objects:
- **Estimators**: `.fit()` — learn parameters from data
- **Transformers**: `.fit()` + `.transform()` — preprocess data
- **Predictors**: `.fit()` + `.predict()` — make predictions

## Train/Test Split and Cross-Validation

```python
from sklearn.model_selection import train_test_split, cross_val_score

# Basic split
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)

# Cross-validation
scores = cross_val_score(model, X, y, cv=5, scoring='accuracy')
print(f"Mean: {scores.mean():.3f} ± {scores.std():.3f}")

# Stratified K-Fold (for imbalanced classes)
from sklearn.model_selection import StratifiedKFold
skf = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
```

> [!warning] Data leakage
> Never fit preprocessors (scaler, encoder, imputer) on the full dataset before splitting. Fit only on training data, then transform both train and test. Pipelines handle this automatically.

## Preprocessing

```python
from sklearn.preprocessing import (
    StandardScaler, MinMaxScaler, LabelEncoder,
    OneHotEncoder, OrdinalEncoder
)
from sklearn.impute import SimpleImputer

# Scaling
scaler = StandardScaler()  # zero mean, unit variance
X_train_scaled = scaler.fit_transform(X_train)
X_test_scaled = scaler.transform(X_test)  # same params!

# Encoding categoricals
ohe = OneHotEncoder(sparse_output=False, handle_unknown='ignore')
X_cat = ohe.fit_transform(X_train[['color', 'size']])

# Imputation
imputer = SimpleImputer(strategy='median')
X_filled = imputer.fit_transform(X_train)
```

## Pipelines — The Right Way

```python
from sklearn.pipeline import Pipeline
from sklearn.compose import ColumnTransformer

# Define feature groups
numeric_features = ['age', 'income', 'hours_per_week']
categorical_features = ['occupation', 'education']

# Build column-specific transformers
numeric_transformer = Pipeline([
    ('imputer', SimpleImputer(strategy='median')),
    ('scaler', StandardScaler())
])

categorical_transformer = Pipeline([
    ('imputer', SimpleImputer(strategy='most_frequent')),
    ('encoder', OneHotEncoder(handle_unknown='ignore'))
])

# Combine into a ColumnTransformer
preprocessor = ColumnTransformer([
    ('num', numeric_transformer, numeric_features),
    ('cat', categorical_transformer, categorical_features)
])

# Full pipeline: preprocessing + model
pipeline = Pipeline([
    ('preprocessor', preprocessor),
    ('classifier', RandomForestClassifier(random_state=42))
])

# Use it
pipeline.fit(X_train, y_train)
pipeline.predict(X_test)
pipeline.score(X_test, y_test)
```

## Hyperparameter Tuning

```python
from sklearn.model_selection import GridSearchCV, RandomizedSearchCV

# Grid search
param_grid = {
    'classifier__n_estimators': [50, 100, 200],
    'classifier__max_depth': [5, 10, None],
    'classifier__min_samples_split': [2, 5]
}

grid = GridSearchCV(
    pipeline, param_grid,
    cv=5, scoring='f1', n_jobs=-1, verbose=1
)
grid.fit(X_train, y_train)
print(grid.best_params_)
print(grid.best_score_)

# Randomized search (better for large param spaces)
from scipy.stats import randint, uniform
param_dist = {
    'classifier__n_estimators': randint(50, 300),
    'classifier__max_depth': [5, 10, 20, None],
    'classifier__min_samples_leaf': randint(1, 10)
}
random_search = RandomizedSearchCV(
    pipeline, param_dist, n_iter=50, cv=5, scoring='f1', random_state=42
)
```

## Evaluation Metrics

```python
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report,
    roc_auc_score, mean_squared_error, r2_score
)

# Classification
print(classification_report(y_test, y_pred))
cm = confusion_matrix(y_test, y_pred)
auc = roc_auc_score(y_test, y_pred_proba[:, 1])

# Regression
mse = mean_squared_error(y_test, y_pred)
rmse = mean_squared_error(y_test, y_pred, squared=False)
r2 = r2_score(y_test, y_pred)
```

| Metric | When to Use |
|--------|------------|
| Accuracy | Balanced classes only |
| Precision | Cost of false positives is high (spam filter) |
| Recall | Cost of false negatives is high (disease detection) |
| F1 | Balance precision/recall |
| AUC-ROC | Ranking quality, threshold-independent |
| RMSE | Regression, penalizes large errors |
| R² | Proportion of variance explained |

## Feature Importance

```python
# Tree-based models
importances = model.feature_importances_
feature_ranking = sorted(zip(feature_names, importances),
                          key=lambda x: x[1], reverse=True)

# Permutation importance (model-agnostic)
from sklearn.inspection import permutation_importance
result = permutation_importance(model, X_test, y_test, n_repeats=10)
```

## Common Models and When to Use Them

| Model | Strengths | When to Use |
|-------|-----------|-------------|
| Logistic Regression | Interpretable, fast | Baseline, linear boundaries |
| Random Forest | Robust, handles mixed types | General purpose, feature importance |
| Gradient Boosting (XGBoost/LightGBM) | Best tabular performance | Competitions, production |
| SVM | Works in high dimensions | Text classification, small datasets |
| KNN | Simple, no training | Quick prototyping |

---

**Related:** [[05 - Pandas Essentials]] | [[07 - PyTorch and TensorFlow Basics]]
