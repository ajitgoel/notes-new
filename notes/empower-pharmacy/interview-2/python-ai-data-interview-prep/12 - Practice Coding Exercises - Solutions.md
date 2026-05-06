# Practice Coding Exercises — Solutions

---

## Section A: Core Python

### A1. Flatten a Nested List

```python
def flatten(lst):
    result = []
    for item in lst:
        if isinstance(item, list):
            result.extend(flatten(item))
        else:
            result.append(item)
    return result

# Generator version (memory-efficient)
def flatten_gen(lst):
    for item in lst:
        if isinstance(item, list):
            yield from flatten_gen(item)
        else:
            yield item
```

### A2. Word Frequency Counter

```python
from collections import Counter

def word_freq(text: str) -> list[tuple[str, int]]:
    words = text.lower().split()
    counts = Counter(words)
    return sorted(counts.items(), key=lambda x: (-x[1], x[0]))
```

### A3. Decorator — Retry with Backoff

```python
import time
import functools

def retry(max_attempts=3, backoff_factor=2):
    def decorator(func):
        @functools.wraps(func)
        def wrapper(*args, **kwargs):
            for attempt in range(max_attempts):
                try:
                    return func(*args, **kwargs)
                except Exception as e:
                    if attempt == max_attempts - 1:
                        raise
                    wait = backoff_factor ** attempt
                    print(f"Attempt {attempt+1} failed: {e}. Retrying in {wait}s...")
                    time.sleep(wait)
        return wrapper
    return decorator
```

### A4. LRU Cache from Scratch

```python
from collections import OrderedDict

class LRUCache:
    def __init__(self, capacity: int):
        self.capacity = capacity
        self.cache = OrderedDict()

    def get(self, key: int):
        if key not in self.cache:
            return -1
        self.cache.move_to_end(key)
        return self.cache[key]

    def put(self, key: int, value) -> None:
        if key in self.cache:
            self.cache.move_to_end(key)
        self.cache[key] = value
        if len(self.cache) > self.capacity:
            self.cache.popitem(last=False)  # remove oldest
```

### A5. Generator — Batch Iterator

```python
def batch_iter(iterable, batch_size):
    batch = []
    for item in iterable:
        batch.append(item)
        if len(batch) == batch_size:
            yield batch
            batch = []
    if batch:
        yield batch
```

---

## Section B: NumPy

### B1. Moving Average

```python
import numpy as np

def moving_average(arr: np.ndarray, window: int) -> np.ndarray:
    cumsum = np.cumsum(arr)
    cumsum[window:] = cumsum[window:] - cumsum[:-window]
    return cumsum[window - 1:] / window
```

### B2. One-Hot Encoding

```python
def one_hot(labels: np.ndarray) -> np.ndarray:
    n_classes = labels.max() + 1
    result = np.zeros((len(labels), n_classes), dtype=int)
    result[np.arange(len(labels)), labels] = 1
    return result
```

### B3. Cosine Similarity Matrix

```python
def cosine_sim_matrix(X: np.ndarray) -> np.ndarray:
    norms = np.linalg.norm(X, axis=1, keepdims=True)
    X_normalized = X / norms
    return X_normalized @ X_normalized.T
```

### B4. Softmax

```python
def softmax(x: np.ndarray) -> np.ndarray:
    # Subtract max for numerical stability
    if x.ndim == 1:
        e_x = np.exp(x - np.max(x))
        return e_x / e_x.sum()
    else:
        e_x = np.exp(x - np.max(x, axis=-1, keepdims=True))
        return e_x / e_x.sum(axis=-1, keepdims=True)
```

---

## Section C: Pandas

### C1. Clean and Analyze Sales Data

```python
import pandas as pd

def analyze_sales(df: pd.DataFrame) -> dict:
    df['revenue'] = df['quantity'] * df['price']

    total_revenue = df['revenue'].sum()

    top_product = (
        df.groupby('product')['revenue']
        .sum()
        .idxmax()
    )

    df['date'] = pd.to_datetime(df['date'])
    monthly_trend = df.set_index('date').resample('M')['revenue'].sum()

    region_summary = (
        df.groupby('region')['revenue']
        .mean()
        .reset_index()
        .rename(columns={'revenue': 'avg_revenue_per_order'})
    )

    return {
        'total_revenue': total_revenue,
        'top_product': top_product,
        'monthly_trend': monthly_trend,
        'region_summary': region_summary,
    }
```

### C2. Merge and Deduplicate

```python
def process_transactions(users: pd.DataFrame, transactions: pd.DataFrame) -> pd.DataFrame:
    # Left join
    merged = pd.merge(users, transactions, on='user_id', how='left')

    # Remove duplicates
    merged = merged.drop_duplicates(subset=['user_id', 'amount', 'date'])

    # Sort by date for shift calculation
    merged['date'] = pd.to_datetime(merged['date'])
    merged = merged.sort_values(['user_id', 'date'])

    # Days since last transaction
    merged['prev_txn_date'] = merged.groupby('user_id')['date'].shift(1)
    merged['days_since_last_txn'] = (merged['date'] - merged['prev_txn_date']).dt.days
    merged = merged.drop(columns=['prev_txn_date'])

    return merged
```

### C3. Pivot and Reshape

```python
def student_report(df: pd.DataFrame) -> pd.DataFrame:
    # Pivot to wide
    wide = df.pivot(index='student', columns='subject', values='score')

    # Average score
    wide['average'] = wide.mean(axis=1)

    # Rank by average
    wide['rank'] = wide['average'].rank(ascending=False).astype(int)

    return wide.sort_values('rank')
```

### C4. Window Functions in Pandas

```python
def stock_analysis(df: pd.DataFrame) -> pd.DataFrame:
    df = df.sort_values(['ticker', 'date']).copy()

    df['daily_return'] = df.groupby('ticker')['close'].pct_change()
    df['rolling_7d_avg'] = df.groupby('ticker')['close'].transform(
        lambda x: x.rolling(7).mean()
    )
    df['cumulative_max'] = df.groupby('ticker')['close'].cummax()
    df['rank'] = df.groupby('ticker')['close'].rank(ascending=False)

    return df
```

---

## Section D: ML / Sklearn

### D1. Build a Full Pipeline

```python
from sklearn.pipeline import Pipeline
from sklearn.compose import ColumnTransformer
from sklearn.impute import SimpleImputer
from sklearn.preprocessing import StandardScaler, OneHotEncoder
from sklearn.ensemble import GradientBoostingClassifier
from sklearn.model_selection import cross_val_score

def build_and_evaluate(X, y, numeric_cols, categorical_cols):
    numeric_transformer = Pipeline([
        ('imputer', SimpleImputer(strategy='median')),
        ('scaler', StandardScaler())
    ])

    categorical_transformer = Pipeline([
        ('imputer', SimpleImputer(strategy='most_frequent')),
        ('encoder', OneHotEncoder(handle_unknown='ignore'))
    ])

    preprocessor = ColumnTransformer([
        ('num', numeric_transformer, numeric_cols),
        ('cat', categorical_transformer, categorical_cols)
    ])

    pipeline = Pipeline([
        ('preprocessor', preprocessor),
        ('classifier', GradientBoostingClassifier(random_state=42))
    ])

    scores = cross_val_score(pipeline, X, y, cv=5, scoring='f1_weighted')

    return {
        'pipeline': pipeline,
        'cv_scores': scores,
        'mean_f1': scores.mean()
    }
```

### D2. Custom Transformer

```python
from sklearn.base import BaseEstimator, TransformerMixin
from itertools import combinations
import pandas as pd

class PolynomialInteraction(BaseEstimator, TransformerMixin):
    def __init__(self, columns: list):
        self.columns = columns

    def fit(self, X, y=None):
        self.pairs_ = list(combinations(self.columns, 2))
        return self

    def transform(self, X):
        X = X.copy()
        for col_a, col_b in self.pairs_:
            X[f'{col_a}*{col_b}'] = X[col_a] * X[col_b]
        return X
```

---

## Section E: Algorithms for Data/AI

### E1. K-Nearest Neighbors from Scratch

```python
import numpy as np
from collections import Counter

class KNNClassifier:
    def __init__(self, k: int = 5):
        self.k = k

    def fit(self, X: np.ndarray, y: np.ndarray):
        self.X_train = X
        self.y_train = y
        return self

    def predict(self, X: np.ndarray) -> np.ndarray:
        predictions = []
        for x in X:
            # Compute distances to all training points
            distances = np.linalg.norm(self.X_train - x, axis=1)
            # Get k nearest indices
            k_indices = distances.argsort()[:self.k]
            # Majority vote
            k_labels = self.y_train[k_indices]
            most_common = Counter(k_labels).most_common(1)[0][0]
            predictions.append(most_common)
        return np.array(predictions)
```

### E2. Mini Gradient Descent

```python
def linear_regression_gd(X, y, lr=0.01, epochs=1000):
    n_samples, n_features = X.shape
    weights = np.zeros(n_features)
    bias = 0.0
    loss_history = []

    for _ in range(epochs):
        # Forward pass
        y_pred = X @ weights + bias

        # Loss (MSE)
        loss = np.mean((y_pred - y) ** 2)
        loss_history.append(loss)

        # Gradients
        dw = (2 / n_samples) * X.T @ (y_pred - y)
        db = (2 / n_samples) * np.sum(y_pred - y)

        # Update
        weights -= lr * dw
        bias -= lr * db

    return weights, bias, loss_history
```

### E3. TF-IDF from Scratch

```python
import numpy as np
import math
from collections import Counter

def compute_tfidf(corpus: list[str]) -> tuple[np.ndarray, list[str]]:
    # Tokenize
    tokenized = [doc.lower().split() for doc in corpus]

    # Build vocabulary
    vocabulary = sorted(set(w for doc in tokenized for w in doc))
    word_to_idx = {w: i for i, w in enumerate(vocabulary)}

    n_docs = len(corpus)
    n_terms = len(vocabulary)

    # Document frequency
    df = Counter()
    for doc in tokenized:
        for word in set(doc):
            df[word] += 1

    # Build TF-IDF matrix
    tfidf_matrix = np.zeros((n_docs, n_terms))

    for doc_idx, doc in enumerate(tokenized):
        tf = Counter(doc)
        for word, count in tf.items():
            term_freq = count / len(doc)
            inverse_doc_freq = math.log(n_docs / df[word])
            tfidf_matrix[doc_idx, word_to_idx[word]] = term_freq * inverse_doc_freq

    return tfidf_matrix, vocabulary
```

### E4. Binary Cross-Entropy Loss

```python
def bce_loss(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    eps = 1e-15
    y_pred = np.clip(y_pred, eps, 1 - eps)
    return -np.mean(
        y_true * np.log(y_pred) + (1 - y_true) * np.log(1 - y_pred)
    )

def bce_gradient(y_true: np.ndarray, y_pred: np.ndarray) -> np.ndarray:
    eps = 1e-15
    y_pred = np.clip(y_pred, eps, 1 - eps)
    return (y_pred - y_true) / (y_pred * (1 - y_pred)) / len(y_true)
```

### E5. Simple Tokenizer (BPE-style)

```python
from collections import Counter

class SimpleBPE:
    def __init__(self, vocab_size: int = 100):
        self.vocab_size = vocab_size
        self.merges = []
        self.vocab = {}

    def _get_pairs(self, tokens_list):
        pairs = Counter()
        for tokens in tokens_list:
            for i in range(len(tokens) - 1):
                pairs[(tokens[i], tokens[i + 1])] += 1
        return pairs

    def _merge_pair(self, tokens_list, pair):
        merged = pair[0] + pair[1]
        new_tokens_list = []
        for tokens in tokens_list:
            new_tokens = []
            i = 0
            while i < len(tokens):
                if i < len(tokens) - 1 and tokens[i] == pair[0] and tokens[i + 1] == pair[1]:
                    new_tokens.append(merged)
                    i += 2
                else:
                    new_tokens.append(tokens[i])
                    i += 1
            new_tokens_list.append(new_tokens)
        return new_tokens_list

    def train(self, corpus: list[str]):
        # Initialize: split each word into characters
        tokens_list = [list(word) for text in corpus for word in text.split()]

        # Build initial vocab from characters
        base_vocab = set(ch for tokens in tokens_list for ch in tokens)

        while len(base_vocab) < self.vocab_size:
            pairs = self._get_pairs(tokens_list)
            if not pairs:
                break
            best_pair = pairs.most_common(1)[0][0]
            self.merges.append(best_pair)
            tokens_list = self._merge_pair(tokens_list, best_pair)
            base_vocab.add(best_pair[0] + best_pair[1])

        self.vocab = {token: idx for idx, token in enumerate(sorted(base_vocab))}

    def encode(self, text: str) -> list[int]:
        tokens = list(text.replace(" ", "▁"))
        for pair in self.merges:
            new_tokens = []
            i = 0
            while i < len(tokens):
                if i < len(tokens) - 1 and tokens[i] == pair[0] and tokens[i + 1] == pair[1]:
                    new_tokens.append(pair[0] + pair[1])
                    i += 2
                else:
                    new_tokens.append(tokens[i])
                    i += 1
            tokens = new_tokens
        return [self.vocab.get(t, 0) for t in tokens]

    def decode(self, token_ids: list[int]) -> str:
        idx_to_token = {v: k for k, v in self.vocab.items()}
        text = ''.join(idx_to_token.get(i, '') for i in token_ids)
        return text.replace('▁', ' ')
```

---

**Back to exercises:** [[11 - Practice Coding Exercises]]
