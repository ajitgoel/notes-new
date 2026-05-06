# Pandas Essentials

## Creating DataFrames

```python
import pandas as pd

# From dict
df = pd.DataFrame({
    'name': ['Alice', 'Bob', 'Charlie'],
    'age': [30, 25, 35],
    'salary': [90000, 75000, 120000]
})

# From CSV
df = pd.read_csv('data.csv', parse_dates=['date_col'])

# From SQL
from sqlalchemy import create_engine
engine = create_engine('sqlite:///db.sqlite')
df = pd.read_sql('SELECT * FROM users', engine)
```

## Inspection

```python
df.shape              # (rows, cols)
df.dtypes             # column types
df.info()             # memory usage + types
df.describe()         # summary stats for numeric cols
df.head(10)           # first 10 rows
df.columns.tolist()   # column names
df.nunique()          # unique values per column
df.isnull().sum()     # missing values per column
```

## Selection and Filtering

```python
# Single column
df['name']              # Series
df[['name', 'age']]     # DataFrame

# loc: label-based
df.loc[0:2, 'name':'age']      # rows 0-2, cols name through age
df.loc[df['age'] > 30, 'name'] # filter + select

# iloc: integer position
df.iloc[0:2, 0:2]              # first 2 rows, first 2 cols

# Boolean filtering
seniors = df[df['age'] >= 30]
high_earners = df[(df['salary'] > 80000) & (df['age'] < 35)]

# .query() — cleaner for complex filters
df.query('salary > 80000 and age < 35')

# isin
df[df['name'].isin(['Alice', 'Bob'])]
```

## Handling Missing Data

```python
df.isnull().sum()                # count nulls per column
df.dropna()                       # drop rows with any null
df.dropna(subset=['salary'])      # drop only if salary is null
df.fillna(0)                      # fill with constant
df['salary'].fillna(df['salary'].median(), inplace=True)  # fill with median

# Interpolation (time series)
df['value'].interpolate(method='linear')
```

## GroupBy and Aggregation

```python
# Basic groupby
df.groupby('department')['salary'].mean()

# Multiple aggregations
df.groupby('department').agg(
    avg_salary=('salary', 'mean'),
    max_salary=('salary', 'max'),
    headcount=('name', 'count')
)

# Transform — returns same-shaped output
df['salary_z'] = df.groupby('dept')['salary'].transform(
    lambda x: (x - x.mean()) / x.std()
)

# Apply — flexible, but slower
df.groupby('dept').apply(lambda g: g.nlargest(3, 'salary'))
```

> [!tip] Interview question
> "Difference between `agg`, `transform`, and `apply`?"
> - `agg`: reduces → one row per group
> - `transform`: broadcasts → same shape as input
> - `apply`: flexible → any shape, but slowest

## Merging and Joining

```python
# merge (SQL-style)
pd.merge(df1, df2, on='user_id', how='left')   # left join
pd.merge(df1, df2, on='user_id', how='inner')  # inner join
pd.merge(df1, df2, left_on='id', right_on='user_id')  # different col names

# concat (stacking)
pd.concat([df1, df2], axis=0)  # vertical (union)
pd.concat([df1, df2], axis=1)  # horizontal

# Indicator column to debug joins
pd.merge(df1, df2, on='id', how='outer', indicator=True)
# _merge column: 'both', 'left_only', 'right_only'
```

## Pivot Tables and Reshaping

```python
# Pivot table
df.pivot_table(
    values='revenue',
    index='region',
    columns='quarter',
    aggfunc='sum',
    fill_value=0
)

# Melt (wide → long)
pd.melt(df, id_vars=['name'], value_vars=['q1', 'q2', 'q3'],
         var_name='quarter', value_name='revenue')

# Crosstab
pd.crosstab(df['department'], df['gender'], margins=True)
```

## String and DateTime Operations

```python
# String methods
df['name'].str.lower()
df['name'].str.contains('ali', case=False)
df['email'].str.split('@').str[1]  # extract domain

# DateTime
df['date'] = pd.to_datetime(df['date_str'])
df['year'] = df['date'].dt.year
df['month'] = df['date'].dt.month
df['day_of_week'] = df['date'].dt.day_name()

# Resample (time series)
df.set_index('date').resample('M')['value'].mean()  # monthly average
```

## Window Functions

```python
# Rolling
df['rolling_avg'] = df['price'].rolling(window=7).mean()

# Expanding (cumulative)
df['cumulative_max'] = df['price'].expanding().max()

# Rank
df['salary_rank'] = df.groupby('dept')['salary'].rank(ascending=False)

# Shift (lag/lead)
df['prev_day'] = df['price'].shift(1)    # lag
df['next_day'] = df['price'].shift(-1)   # lead
df['daily_return'] = df['price'].pct_change()
```

## Performance Tips

```python
# Use categorical for low-cardinality string columns
df['status'] = df['status'].astype('category')  # huge memory savings

# Use vectorized ops, not apply
# BAD
df['upper'] = df['name'].apply(lambda x: x.upper())
# GOOD
df['upper'] = df['name'].str.upper()

# Use query() over chained boolean indexing for readability

# For large datasets, consider:
# - polars (Rust-based, much faster)
# - dask (parallel pandas)
# - read_csv with usecols= and dtype= to limit memory
```

---

**Related:** [[04 - NumPy Essentials]] | [[06 - Scikit-Learn and ML Pipelines]]
