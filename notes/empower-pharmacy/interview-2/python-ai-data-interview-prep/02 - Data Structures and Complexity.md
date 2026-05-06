# Data Structures and Complexity

## Big-O Quick Reference

| Operation | list | dict | set | heapq |
|-----------|------|------|-----|-------|
| Access by index | O(1) | — | — | — |
| Search | O(n) | O(1) avg | O(1) avg | O(n) |
| Insert (end/add) | O(1)* | O(1) avg | O(1) avg | O(log n) |
| Insert (middle) | O(n) | — | — | — |
| Delete | O(n) | O(1) avg | O(1) avg | O(n) |
| Sort | O(n log n) | — | — | — |

\* amortized — occasional O(n) for resize

## Lists vs Tuples vs Arrays

```python
# List: general-purpose, dynamic
data = [1, 2, 3]

# Tuple: immutable, hashable, slightly faster
point = (3.0, 4.5)

# array.array: typed, memory-efficient (rarely used — use NumPy)
import array
arr = array.array('f', [1.0, 2.0, 3.0])

# NumPy array: vectorized ops, the real workhorse
import numpy as np
arr = np.array([1.0, 2.0, 3.0])
```

> [!tip] Interview insight
> "When would you use a tuple over a list?" — When data is fixed (e.g., coordinates, RGB values), when you need hashability (dict keys, set elements), or when you want to signal immutability.

## Dictionaries — Deep Dive

```python
# OrderedDict vs dict (Python 3.7+)
# Regular dicts maintain insertion order since 3.7
# OrderedDict still useful for: move_to_end(), equality considers order

from collections import OrderedDict
od = OrderedDict()
od['a'] = 1
od['b'] = 2
od.move_to_end('a')  # moves 'a' to the end

# defaultdict — auto-initialize missing keys
from collections import defaultdict
graph = defaultdict(list)
graph['A'].append('B')  # no KeyError

# Counter — frequency counting
from collections import Counter
words = ["the", "cat", "sat", "on", "the", "mat"]
freq = Counter(words)
freq.most_common(2)  # [('the', 2), ('cat', 1)]
```

### How Python dicts work internally
- Hash table with open addressing
- Load factor triggers resize (usually at 2/3 full)
- Hash collisions resolved via probing
- Keys must be hashable (implement `__hash__` and `__eq__`)

## Stacks and Queues

```python
# Stack (LIFO) — use a list
stack = []
stack.append(1)    # push
stack.pop()        # pop — O(1)

# Queue (FIFO) — use deque, NOT list
from collections import deque
queue = deque()
queue.append(1)     # enqueue
queue.popleft()     # dequeue — O(1) vs O(n) for list.pop(0)

# Priority Queue — use heapq
import heapq
heap = []
heapq.heappush(heap, (priority, item))
_, item = heapq.heappop(heap)  # lowest priority first

# Get top-k elements efficiently
top_3 = heapq.nlargest(3, data, key=lambda x: x['score'])
```

## Trees and Graphs (Interview Patterns)

Python doesn't have built-in tree/graph types. Know how to implement them:

```python
# Binary tree node
class TreeNode:
    def __init__(self, val=0, left=None, right=None):
        self.val = val
        self.left = left
        self.right = right

# Graph as adjacency list
graph = defaultdict(list)
graph['A'].append('B')
graph['B'].append('C')

# BFS
def bfs(graph, start):
    visited = set()
    queue = deque([start])
    visited.add(start)
    while queue:
        node = queue.popleft()
        for neighbor in graph[node]:
            if neighbor not in visited:
                visited.add(neighbor)
                queue.append(neighbor)

# DFS
def dfs(graph, start, visited=None):
    if visited is None:
        visited = set()
    visited.add(start)
    for neighbor in graph[start]:
        if neighbor not in visited:
            dfs(graph, neighbor, visited)
```

## Sorting — What to Know

```python
# Timsort: Python's built-in, O(n log n), stable
sorted_data = sorted(data, key=lambda x: x['date'], reverse=True)

# Multi-key sort
from operator import itemgetter
sorted(employees, key=itemgetter('department', 'salary'))

# Custom objects
from functools import total_ordering

@total_ordering
class Student:
    def __init__(self, name, grade):
        self.name = name
        self.grade = grade
    def __eq__(self, other):
        return self.grade == other.grade
    def __lt__(self, other):
        return self.grade < other.grade
```

## Common Patterns for Data/AI Interviews

### Two pointers
```python
def two_sum_sorted(nums, target):
    left, right = 0, len(nums) - 1
    while left < right:
        s = nums[left] + nums[right]
        if s == target:
            return [left, right]
        elif s < target:
            left += 1
        else:
            right -= 1
```

### Sliding window
```python
def max_sum_subarray(nums, k):
    window_sum = sum(nums[:k])
    max_sum = window_sum
    for i in range(k, len(nums)):
        window_sum += nums[i] - nums[i - k]
        max_sum = max(max_sum, window_sum)
    return max_sum
```

### Hash map counting
```python
def find_duplicates(nums):
    seen = Counter(nums)
    return [x for x, count in seen.items() if count > 1]
```

---

**Related:** [[01 - Python Fundamentals]] | [[03 - Object-Oriented Python]]
