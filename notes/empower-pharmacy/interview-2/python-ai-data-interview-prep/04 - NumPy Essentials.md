# NumPy Essentials

## Why NumPy Over Pure Python

```python
import numpy as np

# Pure Python: ~500ms for 1M elements
result = [x ** 2 for x in range(1_000_000)]

# NumPy: ~2ms — 250x faster
arr = np.arange(1_000_000)
result = arr ** 2
```

NumPy is fast because:
1. Contiguous memory layout (cache-friendly)
2. Vectorized C operations (no Python loop overhead)
3. BLAS/LAPACK for linear algebra

## Array Creation

```python
np.array([1, 2, 3])                  # from list
np.zeros((3, 4))                      # 3x4 zeros
np.ones((2, 3))                       # 2x3 ones
np.full((3, 3), 7)                    # 3x3 filled with 7
np.eye(4)                             # 4x4 identity
np.arange(0, 10, 2)                   # [0, 2, 4, 6, 8]
np.linspace(0, 1, 5)                  # [0, 0.25, 0.5, 0.75, 1.0]
np.random.randn(3, 4)                 # 3x4 standard normal
np.random.randint(0, 10, size=(3, 3)) # 3x3 random ints
```

## Shapes and Reshaping

```python
arr = np.arange(12)
arr.shape          # (12,)
arr.reshape(3, 4)  # 3 rows, 4 cols
arr.reshape(3, -1) # infer columns: (3, 4)
arr.reshape(-1, 1) # column vector: (12, 1)

# Flatten
arr.reshape(3, 4).ravel()   # returns view when possible
arr.reshape(3, 4).flatten() # always returns copy

# Transpose
mat = np.arange(6).reshape(2, 3)
mat.T  # shape (3, 2)

# Add dimension
arr = np.array([1, 2, 3])        # shape (3,)
arr[np.newaxis, :]                 # shape (1, 3)
arr[:, np.newaxis]                 # shape (3, 1)
# or equivalently:
np.expand_dims(arr, axis=0)        # shape (1, 3)
```

## Broadcasting

Broadcasting lets NumPy operate on arrays of different shapes:

```python
# Rule: dimensions are compared right-to-left.
# Compatible if: equal, or one of them is 1.

a = np.ones((3, 4))    # shape (3, 4)
b = np.array([1, 2, 3, 4])  # shape (4,)
a + b  # b broadcasts to (3, 4) ✓

a = np.ones((3, 1))    # shape (3, 1)
b = np.ones((1, 4))    # shape (1, 4)
a + b  # result shape (3, 4) ✓

# Common use: normalize columns
data = np.random.randn(100, 5)
mean = data.mean(axis=0)     # shape (5,)
std = data.std(axis=0)       # shape (5,)
normalized = (data - mean) / std  # broadcasts correctly
```

> [!warning] Common pitfall
> `(3,)` and `(3, 1)` are different shapes. A 1D array broadcasts differently than a column vector.

## Indexing and Slicing

```python
arr = np.arange(20).reshape(4, 5)

# Basic
arr[1, 3]          # element at row 1, col 3
arr[1:3, :]        # rows 1-2, all cols
arr[:, 2]          # all rows, col 2

# Boolean indexing
mask = arr > 10
arr[mask]           # flat array of elements > 10
arr[arr % 2 == 0]   # even elements

# Fancy indexing
rows = [0, 2, 3]
arr[rows]           # select specific rows
arr[[0, 2], [1, 3]] # elements at (0,1) and (2,3)

# np.where — conditional selection
np.where(arr > 10, arr, 0)  # keep if > 10, else 0
```

## Key Operations

```python
# Aggregations
arr.sum(), arr.mean(), arr.std(), arr.min(), arr.max()
arr.sum(axis=0)    # sum along rows → column totals
arr.sum(axis=1)    # sum along columns → row totals
arr.argmax(axis=1) # index of max per row

# Linear algebra
A = np.random.randn(3, 3)
b = np.array([1, 2, 3])

A @ b                    # matrix-vector product
A @ A.T                  # matrix multiply
np.linalg.inv(A)         # inverse
np.linalg.det(A)         # determinant
eigenvalues, eigenvectors = np.linalg.eig(A)
U, S, Vt = np.linalg.svd(A)  # SVD
x = np.linalg.solve(A, b)    # solve Ax = b

# Stacking
np.vstack([a, b])    # vertical stack
np.hstack([a, b])    # horizontal stack
np.concatenate([a, b], axis=0)
```

## Views vs Copies

```python
arr = np.arange(10)

# Slicing creates a VIEW (shared memory)
view = arr[2:5]
view[0] = 99
arr[2]  # 99 — original changed!

# .copy() creates independent data
copy = arr[2:5].copy()
copy[0] = 0
arr[2]  # still 99
```

> [!tip] Interview question
> "When does NumPy create a view vs a copy?" — Slicing → view. Fancy indexing (with arrays/lists) → copy. Boolean indexing → copy.

## Performance Tips

```python
# Avoid Python loops — use vectorized ops
# BAD
result = np.zeros(len(arr))
for i in range(len(arr)):
    result[i] = arr[i] ** 2

# GOOD
result = arr ** 2

# Use np.vectorize only as last resort (it's NOT truly vectorized)
# For complex element-wise ops, prefer explicit broadcasting or ufuncs

# Pre-allocate arrays instead of growing them
# BAD
results = []
for i in range(1000):
    results.append(compute(i))
results = np.array(results)

# GOOD
results = np.empty(1000)
for i in range(1000):
    results[i] = compute(i)
```

---

**Related:** [[05 - Pandas Essentials]] | [[06 - Scikit-Learn and ML Pipelines]]
