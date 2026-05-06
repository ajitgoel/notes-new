# SQL and Database Access in Python

## SQL Refresher — Common Interview Queries

### Aggregation and Grouping

```sql
-- Average salary by department, only departments with 5+ employees
SELECT department, AVG(salary) as avg_salary, COUNT(*) as headcount
FROM employees
GROUP BY department
HAVING COUNT(*) >= 5
ORDER BY avg_salary DESC;
```

### Window Functions

```sql
-- Rank employees by salary within each department
SELECT name, department, salary,
    RANK() OVER (PARTITION BY department ORDER BY salary DESC) as dept_rank,
    LAG(salary) OVER (PARTITION BY department ORDER BY salary) as prev_salary,
    salary - LAG(salary) OVER (PARTITION BY department ORDER BY salary) as diff
FROM employees;

-- Running total
SELECT date, revenue,
    SUM(revenue) OVER (ORDER BY date ROWS UNBOUNDED PRECEDING) as cumulative
FROM daily_sales;
```

### Joins and Subqueries

```sql
-- Find employees with above-average salary in their department
SELECT e.name, e.salary, d.avg_salary
FROM employees e
JOIN (
    SELECT department, AVG(salary) as avg_salary
    FROM employees
    GROUP BY department
) d ON e.department = d.department
WHERE e.salary > d.avg_salary;

-- Same with CTE (cleaner)
WITH dept_avg AS (
    SELECT department, AVG(salary) as avg_salary
    FROM employees GROUP BY department
)
SELECT e.name, e.salary, da.avg_salary
FROM employees e
JOIN dept_avg da ON e.department = da.department
WHERE e.salary > da.avg_salary;
```

### Common Interview Patterns

```sql
-- Second highest salary
SELECT MAX(salary) FROM employees
WHERE salary < (SELECT MAX(salary) FROM employees);

-- Duplicate detection
SELECT email, COUNT(*) FROM users
GROUP BY email HAVING COUNT(*) > 1;

-- Self-join: employees and their managers
SELECT e.name as employee, m.name as manager
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id;
```

## Python Database Access

### SQLite (built-in)

```python
import sqlite3

conn = sqlite3.connect('data.db')
cursor = conn.cursor()

cursor.execute('''
    CREATE TABLE IF NOT EXISTS experiments (
        id INTEGER PRIMARY KEY,
        name TEXT,
        accuracy REAL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    )
''')

cursor.execute('INSERT INTO experiments (name, accuracy) VALUES (?, ?)',
               ('model_v1', 0.95))
conn.commit()

results = cursor.execute('SELECT * FROM experiments').fetchall()
conn.close()
```

### SQLAlchemy (ORM)

```python
from sqlalchemy import create_engine, Column, Integer, String, Float
from sqlalchemy.orm import declarative_base, Session

engine = create_engine('sqlite:///data.db')
Base = declarative_base()

class Experiment(Base):
    __tablename__ = 'experiments'
    id = Column(Integer, primary_key=True)
    name = Column(String)
    accuracy = Column(Float)

Base.metadata.create_all(engine)

# Insert
with Session(engine) as session:
    exp = Experiment(name='model_v2', accuracy=0.97)
    session.add(exp)
    session.commit()

# Query
with Session(engine) as session:
    results = session.query(Experiment).filter(
        Experiment.accuracy > 0.9
    ).all()
```

### Pandas + SQL

```python
import pandas as pd
from sqlalchemy import create_engine

engine = create_engine('postgresql://user:pass@localhost/mydb')

# Read
df = pd.read_sql('SELECT * FROM users WHERE active = true', engine)

# Read with parameterized query
df = pd.read_sql(
    'SELECT * FROM users WHERE department = %(dept)s',
    engine, params={'dept': 'engineering'}
)

# Write DataFrame to SQL
df.to_sql('results', engine, if_exists='replace', index=False)
```

## NoSQL — MongoDB with pymongo

```python
from pymongo import MongoClient

client = MongoClient('mongodb://localhost:27017/')
db = client['ml_experiments']
collection = db['runs']

# Insert
collection.insert_one({
    'model': 'transformer_v3',
    'metrics': {'accuracy': 0.94, 'f1': 0.91},
    'params': {'lr': 0.001, 'epochs': 10}
})

# Query
results = collection.find({'metrics.accuracy': {'$gt': 0.9}})
best = collection.find_one(sort=[('metrics.accuracy', -1)])

# Aggregation pipeline
pipeline = [
    {'$match': {'metrics.accuracy': {'$gt': 0.8}}},
    {'$group': {'_id': '$model', 'avg_acc': {'$avg': '$metrics.accuracy'}}},
    {'$sort': {'avg_acc': -1}}
]
results = list(collection.aggregate(pipeline))
```

---

**Related:** [[08 - NLP and LLM Concepts in Python]] | [[10 - Data Processing at Scale]]
