# Python Fundamentals

## Data Types and Mutability

| Type | Mutable | Example |
|------|---------|---------|
| `int`, `float`, `bool` | No | `x = 42` |
| `str` | No | `s = "hello"` |
| `list` | Yes | `[1, 2, 3]` |
| `tuple` | No | `(1, 2, 3)` |
| `set` | Yes | `{1, 2, 3}` |
| `frozenset` | No | `frozenset({1, 2})` |
| `dict` | Yes | `{"a": 1}` |

> [!tip] Interview Favorite
> "Why are dictionary keys required to be immutable?" — Because mutable objects can change their hash, breaking the hash table lookup.

## Comprehensions

```python
# List comprehension
squares = [x**2 for x in range(10) if x % 2 == 0]

# Dict comprehension
word_lengths = {w: len(w) for w in ["hello", "world"]}

# Set comprehension
unique_lengths = {len(w) for w in ["hi", "hey", "hello"]}

# Generator expression (lazy — no memory allocation for full list)
total = sum(x**2 for x in range(1_000_000))
```

## Generators and Iterators

```python
def fibonacci():
    a, b = 0, 1
    while True:
        yield a
        a, b = b, a + b

# Usage
fib = fibonacci()
first_10 = [next(fib) for _ in range(10)]
```

**Why generators matter for data work:**
- Process datasets larger than memory
- Lazy evaluation — only compute what you need
- Pipeline composition with `itertools`

## `*args` and `**kwargs`

```python
def flexible_func(*args, **kwargs):
    print(f"Positional: {args}")    # tuple
    print(f"Keyword: {kwargs}")      # dict

flexible_func(1, 2, name="Alice")
# Positional: (1, 2)
# Keyword: {'name': 'Alice'}
```

## Decorators

```python
import functools
import time

def timer(func):
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start = time.perf_counter()
        result = func(*args, **kwargs)
        elapsed = time.perf_counter() - start
        print(f"{func.__name__} took {elapsed:.4f}s")
        return result
    return wrapper

@timer
def train_model(epochs):
    # ...training logic...
    pass
```

> [!note] Common follow-up
> "`functools.wraps` preserves the original function's `__name__` and `__doc__`. Without it, debugging and introspection break."

## Context Managers

```python
# Class-based
class DatabaseConnection:
    def __enter__(self):
        self.conn = connect_to_db()
        return self.conn
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.conn.close()
        return False  # don't suppress exceptions

# Generator-based (preferred for simple cases)
from contextlib import contextmanager

@contextmanager
def temp_seed(seed):
    import numpy as np
    state = np.random.get_state()
    np.random.seed(seed)
    try:
        yield
    finally:
        np.random.set_state(state)
```

## Exception Handling Patterns

```python
# LBYL vs EAFP
# LBYL (Look Before You Leap) — common in other languages
if key in my_dict:
    value = my_dict[key]

# EAFP (Easier to Ask Forgiveness than Permission) — Pythonic
try:
    value = my_dict[key]
except KeyError:
    value = default_value

# Best: just use .get()
value = my_dict.get(key, default_value)
```

## Type Hints (Python 3.10+)

```python
from typing import Optional

def preprocess(
    data: list[dict[str, float]],
    normalize: bool = True,
    fill_value: Optional[float] = None
) -> list[dict[str, float]]:
    ...
```

Type hints don't enforce types at runtime but are critical for:
- Code readability in team settings
- IDE autocompletion
- Static analysis with `mypy`

## Key Built-ins to Know

| Function | Use Case |
|----------|----------|
| `zip()` | Pair elements from multiple iterables |
| `enumerate()` | Loop with index |
| `map()`, `filter()` | Functional transforms (prefer comprehensions) |
| `sorted()` with `key=` | Custom sorting |
| `any()`, `all()` | Boolean aggregation |
| `collections.defaultdict` | Auto-initializing dict |
| `collections.Counter` | Frequency counting |
| `itertools.chain` | Flatten iterables |
| `functools.lru_cache` | Memoization |

---

**Related:** [[02 - Data Structures and Complexity]] | [[03 - Object-Oriented Python]]
