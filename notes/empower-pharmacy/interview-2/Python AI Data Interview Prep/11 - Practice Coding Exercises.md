# Practice Coding Exercises

Work through these exercises without looking at the solutions. They cover the most common patterns in AI/Data Python interviews.

---

## Section A: Core Python

### A1. Flatten a Nested List

Write a function that flattens an arbitrarily nested list.

```python
def flatten(lst):
    """
    >>> flatten([1, [2, [3, 4], 5], [6, 7]])
    [1, 2, 3, 4, 5, 6, 7]
    >>> flatten([[1, 2], [3, [4, [5]]]])
    [1, 2, 3, 4, 5]
    """
    pass
```

### A2. Word Frequency Counter

Given a string, return a dictionary of word frequencies sorted by frequency (descending), then alphabetically for ties.

```python
def word_freq(text: str) -> list[tuple[str, int]]:
    """
    >>> word_freq("the cat sat on the mat the cat")
    [('the', 3), ('cat', 2), ('mat', 1), ('on', 1), ('sat', 1)]
    """
    pass
```

### A3. Decorator — Retry with Backoff

Write a decorator that retries a function up to `n` times with exponential backoff.

```python
def retry(max_attempts=3, backoff_factor=2):
    """
    @retry(max_attempts=3, backoff_factor=2)
    def unreliable_api_call():
        # might raise an exception
        ...
    """
    pass
```

### A4. LRU Cache from Scratch

Implement a Least Recently Used cache with O(1) get and put.

```python
class LRUCache:
    """
    cache = LRUCache(capacity=2)
    cache.put(1, 'a')
    cache.put(2, 'b')
    cache.get(1)       # returns 'a'
    cache.put(3, 'c')  # evicts key 2
    cache.get(2)       # returns -1
    """
    def __init__(self, capacity: int):
        pass

    def get(self, key: int):
        pass

    def put(self, key: int, value) -> None:
        pass
```

### A5. Generator — Batch Iterator

Write a generator that yields batches of a given size from an iterable.

```python
def batch_iter(iterable, batch_size):
    """
    >>> list(batch_iter(range(10), 3))
    [[0, 1, 2], [3, 4, 5], [6, 7, 8], [9]]
    """
    pass
```

---

## Section B: NumPy

### B1. Moving Average

Compute a moving average of a 1D array using only NumPy (no loops).

```python
import numpy as np

def moving_average(arr: np.ndarray, window: int) -> np.ndarray:
    """
    >>> moving_average(np.array([1, 2, 3, 4, 5]), 3)
    array([2., 3., 4.])
    """
    pass
```

### B2. One-Hot Encoding

Given a 1D array of integer labels, return a one-hot encoded 2D array using only NumPy.

```python
def one_hot(labels: np.ndarray) -> np.ndarray:
    """
    >>> one_hot(np.array([0, 2, 1, 0]))
    array([[1, 0, 0],
           [0, 0, 1],
           [0, 1, 0],
           [1, 0, 0]])
    """
    pass
```

### B3. Cosine Similarity Matrix

Compute the pairwise cosine similarity matrix for a set of vectors.

```python
def cosine_sim_matrix(X: np.ndarray) -> np.ndarray:
    """
    X: shape (n_samples, n_features)
    Returns: shape (n_samples, n_samples)
    """
    pass
```

### B4. Softmax

Implement a numerically stable softmax function.

```python
def softmax(x: np.ndarray) -> np.ndarray:
    """Works for 1D or 2D input (softmax along last axis)."""
    pass
```

---

## Section C: Pandas

### C1. Clean and Analyze Sales Data

Given a DataFrame with columns `['date', 'product', 'quantity', 'price', 'region']`:

```python
def analyze_sales(df: pd.DataFrame) -> dict:
    """
    Return a dict with:
    - 'total_revenue': total quantity * price
    - 'top_product': product with highest total revenue
    - 'monthly_trend': Series of monthly revenue (index = month)
    - 'region_summary': DataFrame with avg revenue per order by region
    """
    pass
```

### C2. Merge and Deduplicate

You have two DataFrames — `users` and `transactions`. Write a function that:
1. Left-joins transactions onto users
2. Removes duplicate transactions (same user, same amount, same date)
3. Adds a column `'days_since_last_txn'` per user

```python
def process_transactions(users: pd.DataFrame, transactions: pd.DataFrame) -> pd.DataFrame:
    pass
```

### C3. Pivot and Reshape

Given a long-format DataFrame with columns `['student', 'subject', 'score']`, write a function that:
1. Pivots to wide format (students as rows, subjects as columns)
2. Adds a column for each student's average score
3. Ranks students by average score

```python
def student_report(df: pd.DataFrame) -> pd.DataFrame:
    pass
```

### C4. Window Functions in Pandas

Given a DataFrame of daily stock prices with columns `['date', 'ticker', 'close']`:

```python
def stock_analysis(df: pd.DataFrame) -> pd.DataFrame:
    """
    Add columns:
    - 'daily_return': percentage change in close price per ticker
    - 'rolling_7d_avg': 7-day rolling average of close per ticker
    - 'cumulative_max': cumulative max close per ticker
    - 'rank': rank of each day's close within its ticker (descending)
    """
    pass
```

---

## Section D: ML / Sklearn

### D1. Build a Full Pipeline

Build a scikit-learn pipeline that:
1. Imputes missing numeric values with median
2. Scales numeric features
3. One-hot encodes categorical features
4. Trains a gradient boosting classifier
5. Evaluates with cross-validation

```python
def build_and_evaluate(X: pd.DataFrame, y: pd.Series,
                       numeric_cols: list, categorical_cols: list) -> dict:
    """
    Returns dict with 'pipeline', 'cv_scores', 'mean_f1'.
    """
    pass
```

### D2. Custom Transformer

Write a custom sklearn transformer that creates polynomial interaction features for specified columns.

```python
from sklearn.base import BaseEstimator, TransformerMixin

class PolynomialInteraction(BaseEstimator, TransformerMixin):
    """
    Creates pairwise product features for specified columns.

    Example: columns ['a', 'b', 'c'] → adds 'a*b', 'a*c', 'b*c'
    """
    def __init__(self, columns: list):
        pass

    def fit(self, X, y=None):
        pass

    def transform(self, X):
        pass
```

---

## Section E: Algorithms for Data/AI

### E1. K-Nearest Neighbors from Scratch

```python
class KNNClassifier:
    def __init__(self, k: int = 5):
        pass

    def fit(self, X: np.ndarray, y: np.ndarray):
        pass

    def predict(self, X: np.ndarray) -> np.ndarray:
        pass
```

### E2. Mini Gradient Descent

Implement gradient descent for linear regression from scratch.

```python
def linear_regression_gd(X: np.ndarray, y: np.ndarray,
                          lr: float = 0.01, epochs: int = 1000):
    """
    Returns (weights, bias, loss_history)
    """
    pass
```

### E3. TF-IDF from Scratch

Implement TF-IDF computation without sklearn.

```python
def compute_tfidf(corpus: list[str]) -> tuple[np.ndarray, list[str]]:
    """
    Args:
        corpus: list of documents (strings)
    Returns:
        tfidf_matrix: shape (n_docs, n_terms)
        vocabulary: list of terms
    """
    pass
```

### E4. Binary Cross-Entropy Loss

Implement binary cross-entropy loss and its gradient.

```python
def bce_loss(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    """Numerically stable binary cross-entropy."""
    pass

def bce_gradient(y_true: np.ndarray, y_pred: np.ndarray) -> np.ndarray:
    """Gradient of BCE with respect to y_pred."""
    pass
```

### E5. Simple Tokenizer (BPE-style)

Implement a simplified byte-pair encoding tokenizer.

```python
class SimpleBPE:
    def __init__(self, vocab_size: int = 100):
        pass

    def train(self, corpus: list[str]):
        """Learn merge rules from corpus."""
        pass

    def encode(self, text: str) -> list[int]:
        """Tokenize text into token IDs."""
        pass

    def decode(self, token_ids: list[int]) -> str:
        """Convert token IDs back to text."""
        pass
```

---

**Solutions:** [[12 - Practice Coding Exercises - Solutions]]
