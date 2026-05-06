# Object-Oriented Python

## Classes and `__init__`

```python
class Model:
    # Class variable — shared across all instances
    default_lr = 0.001

    def __init__(self, name: str, lr: float = None):
        # Instance variables
        self.name = name
        self.lr = lr or self.default_lr
        self._weights = []       # convention: "private"
        self.__internal = True   # name-mangled to _Model__internal
```

## Dunder Methods That Matter

```python
class Vector:
    def __init__(self, x, y):
        self.x = x
        self.y = y

    def __repr__(self):
        return f"Vector({self.x}, {self.y})"

    def __add__(self, other):
        return Vector(self.x + other.x, self.y + other.y)

    def __eq__(self, other):
        return self.x == other.x and self.y == other.y

    def __hash__(self):
        return hash((self.x, self.y))

    def __len__(self):
        return 2

    def __getitem__(self, idx):
        return (self.x, self.y)[idx]
```

| Method | Purpose |
|--------|---------|
| `__repr__` | Unambiguous string (for developers) |
| `__str__` | User-friendly string |
| `__eq__`, `__hash__` | Equality and set/dict membership |
| `__lt__`, `__le__`, etc. | Ordering (use `@total_ordering`) |
| `__len__`, `__getitem__` | Make objects behave like sequences |
| `__call__` | Make instances callable (used in PyTorch modules) |
| `__enter__`, `__exit__` | Context manager protocol |

## Inheritance and MRO

```python
class BaseModel:
    def predict(self, X):
        raise NotImplementedError

class LinearModel(BaseModel):
    def predict(self, X):
        return X @ self.weights + self.bias

class RegularizedModel(LinearModel):
    def predict(self, X):
        pred = super().predict(X)  # calls LinearModel.predict
        return pred
```

**Method Resolution Order (MRO):**
```python
RegularizedModel.__mro__
# (RegularizedModel, LinearModel, BaseModel, object)
```

> [!note] Diamond problem
> Python uses C3 linearization to resolve MRO in multiple inheritance. Know that it exists; avoid complex MI in practice.

## Abstract Base Classes

```python
from abc import ABC, abstractmethod

class DataLoader(ABC):
    @abstractmethod
    def load(self, path: str) -> list:
        """Must be implemented by subclasses."""
        ...

    def validate(self, data):
        """Concrete method available to all subclasses."""
        return len(data) > 0

# Can't instantiate ABC directly
# loader = DataLoader()  # TypeError!

class CSVLoader(DataLoader):
    def load(self, path: str) -> list:
        import csv
        with open(path) as f:
            return list(csv.DictReader(f))
```

## Dataclasses (Python 3.7+)

```python
from dataclasses import dataclass, field

@dataclass
class Experiment:
    name: str
    model: str
    lr: float = 0.001
    metrics: dict = field(default_factory=dict)

    def __post_init__(self):
        self.name = self.name.lower().replace(" ", "_")

# Auto-generates __init__, __repr__, __eq__
exp = Experiment("My Experiment", "transformer")
print(exp)  # Experiment(name='my_experiment', model='transformer', ...)

# Frozen (immutable) dataclass
@dataclass(frozen=True)
class Config:
    batch_size: int = 32
    epochs: int = 10
```

## Properties and Descriptors

```python
class Dataset:
    def __init__(self, data: list):
        self._data = data
        self._index = None

    @property
    def size(self):
        return len(self._data)

    @property
    def index(self):
        return self._index

    @index.setter
    def index(self, value):
        if not isinstance(value, list):
            raise TypeError("Index must be a list")
        self._index = value
```

## Class Methods and Static Methods

```python
class Model:
    def __init__(self, config: dict):
        self.config = config

    @classmethod
    def from_file(cls, path: str):
        """Alternative constructor — loads config from file."""
        import json
        with open(path) as f:
            config = json.load(f)
        return cls(config)

    @staticmethod
    def validate_config(config: dict) -> bool:
        """Utility — doesn't need instance or class."""
        return 'model_type' in config and 'params' in config
```

## Protocols (Structural Subtyping) — Python 3.8+

```python
from typing import Protocol

class Predictable(Protocol):
    def predict(self, X) -> list:
        ...

def evaluate(model: Predictable, X_test, y_test):
    """Accepts ANY object with a .predict() method."""
    preds = model.predict(X_test)
    return accuracy(preds, y_test)
```

No inheritance required — just implement the method. This is how sklearn and other frameworks do duck typing with type safety.

---

**Related:** [[01 - Python Fundamentals]] | [[04 - NumPy Essentials]]
