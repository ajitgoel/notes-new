# Data Processing at Scale

## When Pandas Isn't Enough

| Dataset Size | Tool |
|-------------|------|
| < 1 GB | Pandas |
| 1–10 GB | Polars, Pandas with chunking |
| 10–100 GB | Dask, PySpark (single machine) |
| 100 GB+ | PySpark (cluster), distributed tools |

## Polars — Fast DataFrames

```python
import polars as pl

# Read
df = pl.read_csv('data.csv')
df = pl.read_parquet('data.parquet')

# Lazy evaluation (query optimization)
result = (
    pl.scan_parquet('large_data.parquet')
    .filter(pl.col('age') > 30)
    .group_by('department')
    .agg(pl.col('salary').mean().alias('avg_salary'))
    .sort('avg_salary', descending=True)
    .collect()  # execute the plan
)

# Key differences from Pandas:
# - No index
# - Expressions instead of method chaining on Series
# - Lazy evaluation with query optimization
# - True multithreading (no GIL issues)
# - 5-10x faster than Pandas for most operations
```

## PySpark Basics

```python
from pyspark.sql import SparkSession
from pyspark.sql.functions import col, avg, count, when

spark = SparkSession.builder \
    .appName("InterviewPrep") \
    .getOrCreate()

# Read
df = spark.read.parquet('s3://bucket/data/')
df = spark.read.csv('data.csv', header=True, inferSchema=True)

# Transformations (lazy)
result = (
    df.filter(col('age') > 30)
    .groupBy('department')
    .agg(
        avg('salary').alias('avg_salary'),
        count('*').alias('headcount')
    )
    .orderBy(col('avg_salary').desc())
)

# Action (triggers execution)
result.show()
result.collect()
result.write.parquet('output/')

# SQL interface
df.createOrReplaceTempView('employees')
spark.sql('''
    SELECT department, AVG(salary) as avg_salary
    FROM employees
    GROUP BY department
''').show()
```

### PySpark UDFs

```python
from pyspark.sql.functions import udf, pandas_udf
from pyspark.sql.types import StringType
import pandas as pd

# Regular UDF (slow — serializes row by row)
@udf(returnType=StringType())
def categorize(salary):
    if salary > 100000: return 'high'
    elif salary > 60000: return 'mid'
    return 'low'

# Pandas UDF (vectorized — much faster)
@pandas_udf(StringType())
def categorize_vectorized(salary: pd.Series) -> pd.Series:
    return salary.apply(lambda s: 'high' if s > 100000 else ('mid' if s > 60000 else 'low'))

df = df.withColumn('salary_band', categorize_vectorized(col('salary')))
```

> [!tip] Interview insight
> Always prefer Pandas UDFs over regular UDFs in PySpark. Regular UDFs serialize/deserialize every row through Python — Pandas UDFs work on batches using Arrow.

## Chunked Processing with Pandas

```python
# Process large CSV in chunks
chunks = pd.read_csv('huge_file.csv', chunksize=100_000)

results = []
for chunk in chunks:
    processed = chunk.groupby('category')['value'].sum()
    results.append(processed)

final = pd.concat(results).groupby(level=0).sum()
```

## Dask — Parallel Pandas

```python
import dask.dataframe as dd

# Reads lazily, partitions across cores
ddf = dd.read_csv('data_*.csv')  # glob pattern
ddf = dd.read_parquet('large_data.parquet')

result = (
    ddf.groupby('category')['value']
    .mean()
    .compute()  # triggers execution
)
```

## File Formats for Big Data

| Format | Strengths | Use When |
|--------|-----------|----------|
| CSV | Universal, human-readable | Small data, interchange |
| Parquet | Columnar, compressed, fast reads | Analytics, data lakes |
| Avro | Row-based, schema evolution | Streaming, Kafka |
| JSON/JSONL | Flexible schema | APIs, logs |
| HDF5 | Multi-dimensional arrays | Scientific data, ML tensors |

```python
# Parquet with compression
df.to_parquet('data.parquet', compression='snappy')

# Read specific columns (columnar advantage)
df = pd.read_parquet('data.parquet', columns=['name', 'salary'])
```

## Multiprocessing and Concurrency

```python
from concurrent.futures import ProcessPoolExecutor, ThreadPoolExecutor

# CPU-bound work → ProcessPoolExecutor
def process_file(path):
    df = pd.read_csv(path)
    return df.describe()

with ProcessPoolExecutor(max_workers=4) as executor:
    results = list(executor.map(process_file, file_list))

# I/O-bound work → ThreadPoolExecutor or asyncio
import asyncio
import aiohttp

async def fetch_data(url):
    async with aiohttp.ClientSession() as session:
        async with session.get(url) as response:
            return await response.json()

async def main():
    urls = [f"https://api.example.com/data/{i}" for i in range(100)]
    tasks = [fetch_data(url) for url in urls]
    results = await asyncio.gather(*tasks)
```

> [!note] GIL awareness
> Python's GIL prevents true parallel execution of Python bytecode in threads. Use `ProcessPoolExecutor` for CPU-bound work, `ThreadPoolExecutor` for I/O-bound work.

---

**Related:** [[09 - SQL and Database Access in Python]] | [[11 - Practice Coding Exercises]]
