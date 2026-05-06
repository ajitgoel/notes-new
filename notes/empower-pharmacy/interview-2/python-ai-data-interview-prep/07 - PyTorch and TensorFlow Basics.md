# PyTorch and TensorFlow Basics

## PyTorch Fundamentals

### Tensors

```python
import torch

# Creation
x = torch.tensor([1, 2, 3])
x = torch.zeros(3, 4)
x = torch.randn(3, 4)           # normal distribution
x = torch.ones_like(other_tensor)

# From NumPy (shared memory!)
import numpy as np
np_arr = np.array([1, 2, 3])
t = torch.from_numpy(np_arr)    # shares memory
t_copy = torch.tensor(np_arr)   # copies data

# Device management
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
x = x.to(device)
```

### Autograd — Automatic Differentiation

```python
x = torch.tensor(3.0, requires_grad=True)
y = x ** 2 + 2 * x + 1
y.backward()     # compute gradients
print(x.grad)    # dy/dx = 2x + 2 = 8.0

# Stop tracking gradients
with torch.no_grad():
    # inference / evaluation code
    pred = model(x)
```

### Building a Neural Network

```python
import torch.nn as nn

class SimpleNet(nn.Module):
    def __init__(self, input_dim, hidden_dim, output_dim):
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(hidden_dim, output_dim)
        )

    def forward(self, x):
        return self.net(x)

model = SimpleNet(784, 128, 10).to(device)
```

### Training Loop

```python
from torch.utils.data import DataLoader, TensorDataset

# Data
dataset = TensorDataset(X_tensor, y_tensor)
loader = DataLoader(dataset, batch_size=32, shuffle=True)

# Setup
criterion = nn.CrossEntropyLoss()
optimizer = torch.optim.Adam(model.parameters(), lr=1e-3)

# Training
model.train()
for epoch in range(num_epochs):
    total_loss = 0
    for X_batch, y_batch in loader:
        X_batch, y_batch = X_batch.to(device), y_batch.to(device)

        optimizer.zero_grad()
        output = model(X_batch)
        loss = criterion(output, y_batch)
        loss.backward()
        optimizer.step()

        total_loss += loss.item()

    print(f"Epoch {epoch+1}, Loss: {total_loss/len(loader):.4f}")

# Evaluation
model.eval()
with torch.no_grad():
    predictions = model(X_test.to(device))
```

> [!tip] Common interview question
> "Walk through a training loop step by step."
> 1. `optimizer.zero_grad()` — clear old gradients
> 2. Forward pass — compute predictions
> 3. Compute loss
> 4. `loss.backward()` — compute gradients
> 5. `optimizer.step()` — update weights

### Custom Dataset

```python
from torch.utils.data import Dataset

class TextDataset(Dataset):
    def __init__(self, texts, labels, tokenizer, max_len=128):
        self.texts = texts
        self.labels = labels
        self.tokenizer = tokenizer
        self.max_len = max_len

    def __len__(self):
        return len(self.texts)

    def __getitem__(self, idx):
        encoding = self.tokenizer(
            self.texts[idx],
            max_length=self.max_len,
            padding='max_length',
            truncation=True,
            return_tensors='pt'
        )
        return {
            'input_ids': encoding['input_ids'].squeeze(),
            'attention_mask': encoding['attention_mask'].squeeze(),
            'label': torch.tensor(self.labels[idx])
        }
```

### Saving and Loading

```python
# Save
torch.save(model.state_dict(), 'model.pth')

# Load
model = SimpleNet(784, 128, 10)
model.load_state_dict(torch.load('model.pth'))
model.eval()

# Save full checkpoint (for resuming training)
torch.save({
    'epoch': epoch,
    'model_state_dict': model.state_dict(),
    'optimizer_state_dict': optimizer.state_dict(),
    'loss': loss,
}, 'checkpoint.pth')
```

## TensorFlow / Keras Basics

```python
import tensorflow as tf
from tensorflow import keras

# Sequential model
model = keras.Sequential([
    keras.layers.Dense(128, activation='relu', input_shape=(784,)),
    keras.layers.Dropout(0.3),
    keras.layers.Dense(10, activation='softmax')
])

model.compile(
    optimizer='adam',
    loss='sparse_categorical_crossentropy',
    metrics=['accuracy']
)

# Training
history = model.fit(
    X_train, y_train,
    epochs=10,
    batch_size=32,
    validation_split=0.2
)

# Evaluation
model.evaluate(X_test, y_test)
```

## PyTorch vs TensorFlow — Key Differences

| Aspect | PyTorch | TensorFlow/Keras |
|--------|---------|-----------------|
| Execution | Eager (dynamic graph) | Eager default, `@tf.function` for graph |
| Debugging | Standard Python debugger | Harder in graph mode |
| API style | More Pythonic, explicit | Higher-level, more abstracted |
| Research | Dominant in academia | Strong in production |
| Deployment | TorchServe, ONNX | TF Serving, TFLite, TF.js |
| Ecosystem | HuggingFace default | Vertex AI, TFX pipelines |

## Loss Functions Reference

| Task | PyTorch | Keras |
|------|---------|-------|
| Binary classification | `nn.BCEWithLogitsLoss()` | `'binary_crossentropy'` |
| Multi-class | `nn.CrossEntropyLoss()` | `'sparse_categorical_crossentropy'` |
| Regression | `nn.MSELoss()` | `'mse'` |
| Ranking | `nn.MarginRankingLoss()` | custom |

## Optimizers Reference

| Optimizer | When to Use |
|-----------|-------------|
| SGD | Simple, with momentum for CNNs |
| Adam | Default for most tasks |
| AdamW | Adam with proper weight decay (transformers) |
| Learning rate schedulers | ReduceLROnPlateau, CosineAnnealing |

---

**Related:** [[06 - Scikit-Learn and ML Pipelines]] | [[08 - NLP and LLM Concepts in Python]]
