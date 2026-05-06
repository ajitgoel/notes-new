# NLP and LLM Concepts in Python

## Text Preprocessing Pipeline

```python
import re
from collections import Counter

def preprocess(text: str) -> list[str]:
    text = text.lower()
    text = re.sub(r'[^a-z\s]', '', text)   # remove non-alpha
    tokens = text.split()
    # Optional: remove stopwords
    stopwords = {'the', 'a', 'an', 'is', 'in', 'on', 'at', 'to', 'and'}
    tokens = [t for t in tokens if t not in stopwords]
    return tokens

# Vocabulary
corpus = ["the cat sat on the mat", "the dog sat on the log"]
all_tokens = [t for doc in corpus for t in preprocess(doc)]
vocab = {word: idx for idx, word in enumerate(sorted(set(all_tokens)))}
```

## Text Representations

### Bag of Words / TF-IDF

```python
from sklearn.feature_extraction.text import TfidfVectorizer

tfidf = TfidfVectorizer(max_features=10000, ngram_range=(1, 2))
X = tfidf.fit_transform(corpus)  # sparse matrix
```

### Word Embeddings

```python
# Word2Vec with gensim
from gensim.models import Word2Vec

sentences = [preprocess(doc) for doc in corpus]
w2v = Word2Vec(sentences, vector_size=100, window=5, min_count=1)
vector = w2v.wv['cat']
similar = w2v.wv.most_similar('cat', topn=5)
```

## Transformers with HuggingFace

### Tokenization

```python
from transformers import AutoTokenizer

tokenizer = AutoTokenizer.from_pretrained('bert-base-uncased')
encoding = tokenizer(
    "Hello, world!",
    padding='max_length',
    truncation=True,
    max_length=128,
    return_tensors='pt'
)
# encoding.input_ids, encoding.attention_mask
```

### Using Pre-trained Models

```python
from transformers import AutoModel, AutoModelForSequenceClassification

# Feature extraction
model = AutoModel.from_pretrained('bert-base-uncased')
outputs = model(**encoding)
last_hidden = outputs.last_hidden_state     # (batch, seq_len, hidden)
cls_embedding = last_hidden[:, 0, :]         # CLS token as sentence repr

# Classification (fine-tuned)
classifier = AutoModelForSequenceClassification.from_pretrained(
    'bert-base-uncased', num_labels=3
)
```

### Fine-tuning with Trainer API

```python
from transformers import Trainer, TrainingArguments

training_args = TrainingArguments(
    output_dir='./results',
    num_train_epochs=3,
    per_device_train_batch_size=16,
    per_device_eval_batch_size=64,
    evaluation_strategy='epoch',
    learning_rate=2e-5,
    weight_decay=0.01,
)

trainer = Trainer(
    model=classifier,
    args=training_args,
    train_dataset=train_dataset,
    eval_dataset=eval_dataset,
)
trainer.train()
```

## LLM Concepts to Know

### The Transformer Architecture (high level)

```
Input → Tokenization → Embedding + Positional Encoding
      → [Multi-Head Self-Attention → Feed-Forward] × N layers
      → Output (logits over vocabulary)
```

**Self-Attention:** Each token attends to all other tokens. Attention score = softmax(QK^T / √d) × V.

### Key Concepts for Interviews

| Concept | One-liner |
|---------|-----------|
| **Attention** | Weighted sum of values based on query-key similarity |
| **Multi-head attention** | Multiple attention heads capture different relationship types |
| **Positional encoding** | Injects sequence order info (sinusoidal or learned) |
| **Causal masking** | Prevents attending to future tokens (autoregressive generation) |
| **Temperature** | Controls randomness in sampling (lower = deterministic) |
| **Top-k / Top-p** | Truncate sampling distribution to top k tokens or cumulative probability p |
| **Tokenization** | BPE, WordPiece, SentencePiece — subword splitting |
| **Fine-tuning** | Continue training a pre-trained model on your task |
| **LoRA** | Low-rank adapter matrices for parameter-efficient fine-tuning |
| **RLHF** | RL with human feedback to align model outputs |
| **RAG** | Retrieve context docs, inject into prompt before generation |

### Using LLM APIs

```python
# OpenAI-style (also compatible with many other providers)
from openai import OpenAI

client = OpenAI(api_key="...")
response = client.chat.completions.create(
    model="gpt-4",
    messages=[
        {"role": "system", "content": "You are a helpful assistant."},
        {"role": "user", "content": "Explain transformers in 3 sentences."}
    ],
    temperature=0.7,
    max_tokens=200
)
answer = response.choices[0].message.content

# Anthropic
from anthropic import Anthropic

client = Anthropic()
message = client.messages.create(
    model="claude-sonnet-4-20250514",
    max_tokens=200,
    messages=[{"role": "user", "content": "Explain transformers."}]
)
```

### Embeddings and Vector Search

```python
# Generate embeddings
from sentence_transformers import SentenceTransformer

model = SentenceTransformer('all-MiniLM-L6-v2')
embeddings = model.encode(["hello world", "goodbye world"])

# Cosine similarity
from sklearn.metrics.pairwise import cosine_similarity
sim = cosine_similarity([embeddings[0]], [embeddings[1]])

# Vector database (FAISS)
import faiss
import numpy as np

dimension = embeddings.shape[1]
index = faiss.IndexFlatL2(dimension)
index.add(np.array(embeddings))

query = model.encode(["greetings"])
distances, indices = index.search(np.array(query), k=2)
```

### Prompt Engineering Patterns

```python
# Few-shot prompting
prompt = """Classify the sentiment: positive, negative, or neutral.

Text: "I love this product!" → positive
Text: "Worst experience ever." → negative
Text: "It arrived on time." → neutral
Text: "The quality exceeded my expectations!" →"""

# Chain-of-thought
prompt = """Q: If a train travels 120 miles in 2 hours, what is its speed?
Let's think step by step:
1. Speed = Distance / Time
2. Speed = 120 miles / 2 hours
3. Speed = 60 mph
The answer is 60 mph.

Q: If a car travels 250 miles in 5 hours, what is its speed?
Let's think step by step:"""
```

---

**Related:** [[07 - PyTorch and TensorFlow Basics]] | [[09 - SQL and Database Access in Python]]
