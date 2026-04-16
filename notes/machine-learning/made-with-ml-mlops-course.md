# Setup

 [Repository](https://github.com/GokuMohandas/Made-With-ML) ·  [Notebook](https://github.com/GokuMohandas/Made-With-ML/blob/main/notebooks/madewithml.ipynb)

---

In this lesson, we'll setup the development environment that we'll be using in all of our lessons. We'll have instructions for both local laptop and remote scalable clusters ([Anyscale](https://anyscale.com/)). While everything will work locally on your laptop, you can sign up to join one of our upcoming live cohorts where we'll provide **live lessons + QA**, **compute (GPUs)** and **community** to learn everything in one day → [sign up here](https://4190urw86oh.typeform.com/madewithml).

## Cluster

We'll start with defining our cluster, which refers to a group of servers that come together to form one system. Our clusters will have a [head node](https://docs.ray.io/en/latest/cluster/key-concepts.html#head-node) that manages the cluster and it will be connected to a set of [worker nodes](https://docs.ray.io/en/latest/cluster/key-concepts.html#head-node) that will execute workloads for us. These clusters can be fixed in size or [autoscale](https://docs.ray.io/en/latest/cluster/key-concepts.html#cluster-autoscaler) based on our application's compute needs, which makes them highly scalable and performant. We'll create our cluster by defining a compute configuration and an environment.

### Environment

We'll start by defining our cluster environment which will specify the software dependencies that we'll need for our workloads.

💻 Local

Your personal laptop will need to have Python installed and we highly recommend using Python `3.10`. You can use a tool like [pyenv](https://github.com/pyenv/pyenv) (mac) or [pyenv-win](https://github.com/pyenv-win/pyenv-win) (windows) to easily download and switch between Python versions.

`pyenv install 3.10.11  # install pyenv global 3.10.11  # set default`

Once we have our Python version, we can create a virtual environment to install our dependencies. We'll download our Python dependencies _after_ we clone our repository from git [shortly](https://madewithml.com/courses/mlops/setup/#git).

`mkdir madewithml cd madewithml python3 -m venv venv  # create virtual environment source venv/bin/activate  # on Windows: venv\Scripts\activate python3 -m pip install --upgrade pip setuptools wheel`

🚀 Anyscale

Our cluster environment will be defined inside a [`cluster_env.yaml`](https://github.com/GokuMohandas/Made-With-ML/blob/main/deploy/cluster_env.yaml) file. Here we specify some details around our base image ([anyscale/ray:2.6.0-py310-cu118](https://docs.anyscale.com/reference/base-images/ray-260/py310#ray-2-6-0-py310-cu118)) that has our Python version, GPU dependencies, etc.

|   |
|---|
|`base_image: anyscale/ray:2.6.0-py310-cu118 env_vars: {} debian_packages:   - curl  python:   pip_packages: []   conda_packages: []  post_build_cmds:   - python3 -m pip install --upgrade pip setuptools wheel   - python3 -m pip install -r https://raw.githubusercontent.com/GokuMohandas/Made-With-ML/main/requirements.txt`|

We could specify any python packages inside `pip_packages` or `conda_packages` but we're going to use a [`requirements.txt`](https://github.com/GokuMohandas/Made-With-ML/blob/main/requirements.txt) file to load our dependencies under `post_build_cmds`.

### Compute

Next, we'll define our compute configuration, which will specify our hardware dependencies (head and worker nodes) that we'll need for our workloads.

💻 Local

Your personal laptop (single machine) will act as the cluster, where one CPU will be the head node and some of the remaining CPU will be the worker nodes (no GPUs required). All of the code in this course will work in any personal laptop though it will be slower than executing the same workloads on a larger cluster.

🚀 Anyscale

Our cluster compute will be defined inside a [`cluster_compute.yaml`](https://github.com/GokuMohandas/Made-With-ML/blob/main/deploy/cluster_compute.yaml) file. Here we specify some details around where our compute resources will come from (cloud computing platform like AWS), types of nodes and their counts, etc.

|   |
|---|
|`cloud: madewithml-us-east-2 region: us-east2 head_node_type:   name: head_node_type   instance_type: m5.2xlarge  # 8 CPU, 0 GPU, 32 GB RAM worker_node_types: - name: gpu_worker   instance_type: g4dn.xlarge  # 4 CPU, 1 GPU, 16 GB RAM   min_workers: 0   max_workers: 1 ...`|

Our worker nodes will be GPU-enabled so we can train our models faster and we set `min_workers` to 0 so that we can autoscale these workers only when they're needed (up to a maximum of `max_workers`). This will help us significantly reduce our compute costs without having to manage the infrastructure ourselves.

## Workspaces

With our compute and environment defined, we're ready to create our cluster workspace. This is where we'll be developing our ML application on top of our compute, environment and storage.

💻 Local

Your personal laptop will need to have an interactive development environment (IDE) installed, such as [VS Code](https://code.visualstudio.com/). For bash commands in this course, you're welcome to use the terminal on VSCode or a separate one.

🚀 Anyscale

We're going to launch an Anyscale [Workspace](https://docs.anyscale.com/develop/workspaces/get-started) to do all of our development in. Workspaces allow us to use development tools such as VSCode, Jupyter notebooks, web terminal, etc. on top of our cluster compute, environment and [storage](https://docs.anyscale.com/develop/workspaces/storage). This create an "infinite laptop" experience that feels like a local laptop experience but on a powerful, scalable cluster.

![Anyscale Workspaces](https://madewithml.com/static/images/mlops/setup/workspaces.png)

We have the option to create our Workspace using a [CLI](https://docs.anyscale.com/reference/anyscale-cli) but we're going to create it using the [web UI](https://console.anyscale.com/o/madewithml/workspaces/add/blank) (you will receive the required credentials during the cohort). On the UI, we can fill in the following information:

``- Workspace name: `madewithml` - Project: `madewithml` - Cluster environment name: `madewithml-cluster-env` # Toggle `Select from saved configurations` - Compute config: `madewithml-cluster-compute` - Click on the **Start** button to launch the Workspace``

![Anyscale configs](https://madewithml.com/static/images/mlops/setup/configs.png)

We have already saved created our Project, cluster environment and compute config so we can select them from the dropdowns but we could just as easily create new ones / update these using the [CLI](https://docs.anyscale.com/reference/cli/commands).

CLI method

`# Set credentials export ANYSCALE_HOST=https://console.anyscale.com export ANYSCALE_CLI_TOKEN=$YOUR_CLI_TOKEN  # retrieved from Anyscale credentials page  # Create project export PROJECT_NAME="madewithml" anyscale project create --name $PROJECT_NAME  # Cluster environment export CLUSTER_ENV_NAME="madewithml-cluster-env" anyscale cluster-env build deploy/cluster_env.yaml --name $CLUSTER_ENV_NAME  # Compute config export CLUSTER_COMPUTE_NAME="madewithml-cluster-compute" anyscale cluster-compute create deploy/cluster_compute.yaml --name $CLUSTER_COMPUTE_NAME`

## Git

With our development workspace all set up, we're ready to start developing. We'll start by following these instructions to create a repository:

1. [Create a new repository](https://github.com/new)
2. name it `Made-With-ML`
3. Toggle `Add a README file` (**very important** as this creates a `main` branch)
4. Scroll down and click `Create repository`

Now we're ready to clone the [Made With ML repository](https://github.com/GokuMohandas/Made-With-ML)'s contents from GitHub inside our `madewithml` directory.

`export GITHUB_USERNAME="YOUR_GITHUB_UESRNAME"  # <-- CHANGE THIS to your username git clone https://github.com/GokuMohandas/Made-With-ML.git . git remote set-url origin https://github.com/$GITHUB_USERNAME/Made-With-ML.git git checkout -b dev export PYTHONPATH=$PYTHONPATH:$PWD  # so we can import modules from our scripts`

💻 Local

Recall that we created our virtual environment earlier but didn't actually load any Python dependencies yet. We'll clone our repository and then install the packages using the `requirements.txt` file.

`python3 -m pip install -r requirements.txt`

> **Caution**: make sure that we're installing our Python packages inside our virtual environment.

🚀 Anyscale

Our environment with the appropriate Python version and libraries is already all set for us through the cluster environment we used when setting up our Anyscale Workspace. But if we want to install additional Python packages as we develop, we need to do pip install with the [`--user`](https://pip.pypa.io/en/stable/user_guide/#user-installs) flag inside our Workspaces (via terminal) to ensure that our head and all worker nodes receive the package. And then we should also add it to our requirements file so it becomes part of the cluster environment build process next time.

`pip install --user <package_name>:<version>`

## Notebook

Now we're ready to launch our Jupyter notebook to interactively develop our ML application.

💻 Local

We already installed jupyter through our [`requirements.txt`](https://github.com/GokuMohandas/Made-With-ML/blob/main/requirements.txt) file in the previous step, so we can just launch it.

`jupyter lab notebooks/madewithml.ipynb`

🚀 Anyscale

Click on the Jupyter icon  ![](https://upload.wikimedia.org/wikipedia/commons/thumb/3/38/Jupyter_logo.svg/1200px-Jupyter_logo.svg.png)  at the top right corner of our Anyscale Workspace page and this will open up our JupyterLab instance in a new tab. Then navigate to the `notebooks` directory and open up the `madewithml.ipynb` notebook.

![Workspace dev tools](https://madewithml.com/static/images/mlops/setup/devtools.png)

## Ray

We'll be using [Ray](https://github.com/project-ray/ray) to scale and productionize our ML application. Ray consists of a core distributed runtime along with libraries for scaling ML workloads and has companies like [OpenAI](https://www.anyscale.com/blog/ray-summit-2022-stories-large-language-models), [Spotify](https://engineering.atspotify.com/2023/02/unleashing-ml-innovation-at-spotify-with-ray/), [Netflix](https://netflixtechblog.com/scaling-media-machine-learning-at-netflix-f19b400243), [Instacart](https://tech.instacart.com/distributed-machine-learning-at-instacart-4b11d7569423), [Doordash](https://doordash.engineering/2023/06/20/how-doordash-built-an-ensemble-learning-model-for-time-series-forecasting/) + many [more](https://www.anyscale.com/user-stories) using it to develop their ML applications. We're going to start by initializing Ray inside our notebooks:

|   |
|---|
|`import ray`|

|   |
|---|
|`# Initialize Ray if ray.is_initialized():     ray.shutdown() ray.init()`|

We can also view our cluster resources to view the available compute resources:

|   |
|---|
|`ray.cluster_resources()`|

💻 Local

If you are running this on a local laptop (no GPU), use the CPU count from `ray.cluster_resources()` to set your resources. For example if your machine has 10 CPUs:

{'CPU': 10.0,
 'object_store_memory': 2147483648.0,
 'node:127.0.0.1': 1.0}
 

`num_workers = 6  # prefer to do a few less than total available CPU (1 for head node + 1 for background tasks) resources_per_worker={"CPU": 1, "GPU": 0}`

🚀 Anyscale

On our Anyscale Workspace, the `ray.cluster_resources()` command will produce:

{'CPU': 8.0,
'node:**internal_head**': 1.0,
'node:10.0.56.150': 1.0,
'memory': 34359738368.0,
'object_store_memory': 9492578304.0}

These cluster resources only reflect our head node (1 [m5.2xlarge](https://instances.vantage.sh/aws/ec2/m5.2xlarge)). But recall earlier in our [compute configuration](https://madewithml.com/courses/mlops/setup/#compute) that we also added [g4dn.xlarge](https://instances.vantage.sh/aws/ec2/g4dn.xlarge) worker nodes (each has 1 GPU and 4 CPU) to our cluster. But because we set `min_workers=0`, our worker nodes will autoscale ( up to `max_workers`) as they're needed for specific workloads (ex. training). So we can set the # of workers and resources by worker based on this insight:

`# Workers (1 g4dn.xlarge) num_workers = 1 resources_per_worker={"CPU": 3, "GPU": 1}`

Head on over to the next lesson, where we'll motivate the specific application that we're trying to build from a product and systems design perspective. And after that, we're ready to start developing!

# Machine Learning Product Design

 [View all lessons](https://madewithml.com/courses/ml_canvas)

---

An overview of the machine learning product design process.

## Overview

Before we start developing any machine learning models, we need to first motivate and design our application. While this is a technical course, this initial product design process is extremely crucial for creating great products. We'll focus on the product design aspects of our application in this lesson and the systems design aspects in the [next lesson](https://madewithml.com/courses/mlops/systems-design/).

## Template

The template below is designed to guide machine learning product development. It involves both the product and systems design ([next lesson](https://madewithml.com/courses/mlops/systems-design/)) aspects of our application:

[Product design](https://madewithml.com/courses/mlops/product-design/) (_What_ & _Why_) → [Systems design](https://madewithml.com/courses/mlops/systems-design/) (_How_)

[![machine learning canvas](https://madewithml.com/static/images/mlops/design/ml_canvas.png)](https://madewithml.com/static/templates/ml-canvas.pdf)

> 👉   Download a PDF of the ML canvas to use for your own products → [ml-canvas.pdf](https://madewithml.com/static/templates/ml-canvas.pdf) (right click the link and hit "Save Link As...")

## Product design

Motivate the need for the product and outline the objectives and impact.

Note

Each section below has a part called "Our task", which will discuss how the specific topic relates to the application that we will be building.

### Background

Set the scene for what we're trying to do through a user-centric approach:

- `users`: profile/persona of our users
- `goals`: our users' main goals
- `pains`: obstacles preventing our users from achieving their goals

Our task

- `users`: machine learning developers and researchers.
- `goals`: stay up-to-date on ML content for work, knowledge, etc.
- `pains`: too much unlabeled content scattered around the internet.

### Value proposition

Propose the value we can create through a product-centric approach:

- `product`: what needs to be built to help our users reach their goals?
- `alleviates`: how will the product reduce pains?
- `advantages`: how will the product create gains?

Our task

We will build a platform that helps machine learning developers and researchers stay up-to-date on ML content. We'll do this by discovering and categorizing content from popular sources (Reddit, Twitter, etc.) and displaying it on our platform. For simplicity, assume that we already have a pipeline that delivers ML content from popular sources to our platform. We will just focus on developing the ML service that can correctly categorize the content.

- `product`: a service that discovers and categorizes ML content from popular sources.
- `alleviates`: display categorized content for users to discover.
- `advantages`: when users visit our platform to stay up-to-date on ML content, they don't waste time searching for that content themselves in the noisy internet.

![product mockup](https://madewithml.com/static/images/mlops/design/product.png)

### Objectives

Breakdown the product into key objectives that we want to focus on.

Our task

- Discover ML content from trusted sources to bring into our platform.
- Classify incoming content for our users to easily discover. **[OUR FOCUS]**
- Display categorized content on our platform (recent, popular, recommended, etc.)

### Solution

Describe the solution required to meet our objectives, including its:

- `core features`: key features that will be developed.
- `integration`: how the product will integrate with other services.
- `alternatives`: alternative solutions that we should considered.
- `constraints`: limitations that we need to be aware of.
- `out-of-scope.`: features that we will not be developing for now.

Our task

Develop a model that can classify the content so that it can be organized by category (tag) on our platform.

`Core features`:

- predict the correct tag for a given content. **[OUR FOCUS]**
- user feedback process for incorrectly classified content.
- workflows to categorize ML content that our model is incorrect / unsure about.

`Integrations`:

- ML content from reliable sources will be sent to our service for classification.

`Alternatives`:

- allow users to add content manually and classify them (noisy, cold start, etc.)

`Constraints`:

- maintain low latency (>100ms) when classifying incoming content. **[Latency]**
- only recommend tags from our list of approved tags. **[Security]**
- avoid duplicate content from being added to the platform. **[UI/UX]**

`Out-of-scope`:

- identify relevant tags beyond our approved list of tags (`natural-language-processing`, `computer-vision`, `mlops` and `other`).
- using full-text HTML from content links to aid in classification.

### Feasibility

How feasible is our solution and do we have the required resources to deliver it (data, $, team, etc.)?

Our task

We have a [dataset](https://raw.githubusercontent.com/GokuMohandas/Made-With-ML/main/datasets/dataset.csv) with ML content that has been labeled. We'll need to assess if it has the necessary signals to meet our [objectives](https://madewithml.com/courses/mlops/product-design/#objectives).

|Sample data point|   |
|---|---|
|`{     "id": 443,     "created_on": "2020-04-10 17:51:39",     "title": "AllenNLP Interpret",     "description": "A Framework for Explaining Predictions of NLP Models",     "tag": "natural-language-processing" }`|

Now that we've set up the product design requirements for our ML service, let's move on to the systems design requirements in the [next lesson](https://madewithml.com/courses/mlops/systems-design/).

------------------
# Machine Learning Systems Design

---

An overview of the machine learning systems design process.
## Overview

In the [previous lesson](https://madewithml.com/courses/mlops/product-design/), we covered the product design process for our ML application. In this lesson, we'll cover the systems design process where we'll learn how to design the ML system that will address our product objectives.

## Template

The template below is designed to guide machine learning product development. It involves both the product and systems design aspects of our application:

[Product design](https://madewithml.com/courses/mlops/product-design/) (_What_ & _Why_) → [Systems design](https://madewithml.com/courses/mlops/systems-design/) (_How_)

[![machine learning canvas](https://madewithml.com/static/images/mlops/design/ml_canvas.png)](https://madewithml.com/static/templates/ml-canvas.pdf)

> 👉   Download a PDF of the ML canvas to use for your own products → [ml-canvas.pdf](https://madewithml.com/static/templates/ml-canvas.pdf) (right click the link and hit "Save Link As...")

## Systems design

_How_ can we engineer our approach for building the product? We need to account for everything from data ingestion to model serving.

![ML workloads](https://madewithml.com/static/images/mlops/systems-design/workloads.png)

### Data

Describe the training and production (batches/streams) sources of data.

||id|created_on|title|description|tag|
|---|---|---|---|---|---|
|0|6|2020-02-20 06:43:18|Comparison between YOLO and RCNN on real world ...|Bringing theory to experiment is cool. We can ...|computer-vision|
|1|89|2020-03-20 18:17:31|Rethinking Batch Normalization in Transformers|We found that NLP batch statistics exhibit large ...|natural-language-processing|
|2|1274|2020-06-10 05:21:00|Getting Machine Learning to Production|Machine learning is hard and there are a lot, a lot of ...|mlops|
|4|19|2020-03-03 13:54:31|Diffusion to Vector|Reference implementation of Diffusion2Vec ...|other|

Our task

- **training**:
    - access to [training data](https://github.com/GokuMohandas/Made-With-ML/blob/main/datasets/dataset.csv) and [testing (holdout) data](https://github.com/GokuMohandas/Made-With-ML/blob/main/datasets/holdout.csv).
    - was there sampling of any kind applied to create this dataset?
    - are we introducing any data leaks?
- **production**:
    - access to [batches](https://madewithml.com/courses/mlops/serving/#batch-inference) or [real-time](https://madewithml.com/courses/mlops/serving/#online-inference) streams of ML content from various sources
    - how can we trust that this stream only has data that is consistent with what we have historically seen?

|Assumption|Reality|Reason|
|---|---|---|
|All of our incoming data is only machine learning related (no spam).|We would need a filter to remove spam content that's not ML related.|To simplify our ML task, we will assume all the data is ML content.|

#### Labeling

Describe the labeling process (ingestions, QA, etc.) and how we decided on the features and labels.

![labeling workflow](https://madewithml.com/static/images/mlops/labeling/workflow.png)

Our task

**Labels**: categories of machine learning (for simplification, we've restricted the label space to the following tags: `natural-language-processing`, `computer-vision`, `mlops` and `other`).

**Features**: text features (title and description) that describe the content.

|Assumption|Reality|Reason|
|---|---|---|
|Content can only belong to one category (multiclass).|Content can belong to more than one category (multilabel).|For simplicity and many libraries don't support or complicate multilabel scenarios.|

### Metrics

One of the hardest challenges with ML systems is tying our core [objectives](https://madewithml.com/courses/mlops/product-design/#objectives), many of which may be qualitative, with quantitative metrics that our model can optimize towards.

Our task

For our task, we want to have both high precision and recall, so we'll optimize for f1 score (weighted combination of precision and recall). We'll determine these metrics for the overall dataset, as well as specific classes or [slices](https://madewithml.com/courses/mlops/evaluation/#slicing) of data.

- **True positives (TP)**: we correctly predicted class X.
- **False positives (FP)**: we incorrectly predicted class X but it was another class.
- **True negatives (TN)**: we correctly predicted that it's wasn't the class X.
- **False negatives (FN)**: we incorrectly predicted that it wasn't the class X but it was.

![metrics](https://madewithml.com/static/images/mlops/evaluation/metrics.png)

What are our priorities

<span style="background:rgba(163, 67, 31, 0.2)">How do we decide which metrics to prioritize?</span>
<span style="background:rgba(163, 67, 31, 0.2)">It entirely depends on the specific task. For example, in an email spam detector, precision is very important because it's better than we some spam then completely miss an important email. Overtime, we need to iterate on our solution so all evaluation metrics improve but it's important to know which one's we can't comprise on from the get-go.</span>

### Evaluation

Once we have our metrics defined, we need to think about when and how we'll evaluate our model.

#### Offline evaluation

<span style="background:rgba(163, 67, 31, 0.2)">[Offline evaluation](https://madewithml.com/courses/mlops/evaluation/) requires a gold standard holdout dataset that we can use to benchmark all of our [models](https://madewithml.com/courses/mlops/systems-design/#modeling).</span>

Our task

We'll be using this [holdout dataset](https://github.com/GokuMohandas/Made-With-ML/blob/main/datasets/holdout.csv) for offline evaluation. We'll also be creating [slices](https://madewithml.com/courses/mlops/evaluation/#slicing) of data that we want to evaluate in isolation.

#### Online evaluation

<span style="background:rgba(163, 67, 31, 0.2)">[Online evaluation](https://madewithml.com/courses/mlops/evaluation/#online-evaluation) ensures that our model continues to perform well in production and can be performed using labels or, in the event we don't readily have labels, [proxy signals](https://madewithml.com/courses/mlops/monitoring/#performance).</span>

Our task

- manually label a subset of incoming data to evaluate periodically.
- asking the initial set of users viewing a newly categorized content if it's correctly classified.
- allow users to report misclassified content by our model.

It's important that we measure real-time performance before committing to replace our existing version of the system.

- Internal canary rollout, monitoring for proxy/actual performance, etc.
- Rollout to the larger internal team for more feedback.
- A/B rollout to a subset of the population to better understand UX, utility, etc.

> Not all releases have to be high stakes and external facing. We can always include internal releases, gather feedback and iterate until we’re ready to increase the scope.

### Modeling

While the specific methodology we employ can differ based on the problem, there are core principles we always want to follow:

- **End-to-end utility**: the end result from every iteration should deliver minimum end-to-end utility so that we can benchmark iterations against each other and plug-and-play with the system.
- **Manual before ML**: try to see how well a simple rule-based system performs before moving onto more [complex](https://madewithml.com/courses/mlops/training/) ones.
- **Augment vs. automate**: allow the system to supplement the decision making process as opposed to making the actual decision.
- **Internal vs. external**: not all early releases have to be end-user facing. We can use early versions for internal validation, feedback, data collection, etc.
- **Thorough**: every approach needs to be well [tested](https://madewithml.com/courses/mlops/testing/) (code, data + models) and [evaluated](https://madewithml.com/courses/mlops/evaluation/), so we can objectively benchmark different approaches.

Our task

1. creating a gold-standard labeled dataset that is representative of the problem space.
2. rule-based text matching approaches to categorize content.
3. predict labels (probabilistic) from content title and description.

|Assumption|Reality|Reason|
|---|---|---|
|Solution needs to involve ML due to unstructured data and ineffectiveness of rule-based systems for this task.|An iterative approach where we start with simple rule-based solutions and slowly add complexity.|This course is about responsibly delivering value with ML, so we'll jump to it right away.|

Utility in starting simple

<span style="background:rgba(163, 67, 31, 0.2)">Some of the earlier, simpler, approaches may not deliver on a certain performance objective. What are some advantages of still starting simple?</span>

Show answer

<span style="background:rgba(163, 67, 31, 0.2)">- get internal feedback on end-to-end utility.</span>
<span style="background:rgba(163, 67, 31, 0.2)">- perform A/B testing to understand UI/UX design.</span>
<span style="background:rgba(163, 67, 31, 0.2)">- deployed locally to start generating more data required for more complex approaches.</span>

### Inference

Once we have a model we're satisfied with, we need to think about whether we want to perform batch (offline) or real-time (online) inference.

### Batch inference

We can use our models to make batch predictions on a finite set of inputs which are then written to a database for low latency inference. When a user or downstream service makes an inference request, cached results from the database are returned. In this scenario, our trained model can directly be loaded and used for inference in the code. It doesn't have to be served as a separate service.

[![batch inference](https://madewithml.com/static/images/mlops/design/batch_inference.png)](https://www.anyscale.com/blog/offline-batch-inference-comparing-ray-apache-spark-and-sagemaker)

- ✅  generate and cache predictions for very fast inference for users.
- ✅  the model doesn't need to be spun up as it's own service since it's never used in real-time.
- ❌  predictions can become stale if user develops new interests that aren’t captured by the old data that the current predictions are based on.

Batch serving tasks

What are some tasks where batch serving is ideal?

Show answer

Recommend content that _existing_ users will like based on their viewing history. However, _new_ users may just receive some generic recommendations based on their explicit interests until we process their history the next day. And even if we're not doing batch serving, it might still be useful to cache very popular sets of input features (ex. combination of explicit interests leads to certain recommended content) so that we can serve those predictions faster.

### Online inference

We can also serve real-time predictions where input features are fed to the model to retrieve predictions. In this scenario, our model will need to be served as a separate service (ex. [api endpoint](https://madewithml.com/courses/mlops/jobs-and-services/#services)) that can handle incoming requests.

![batch inference](https://madewithml.com/static/images/mlops/design/online_inference.png)

- ✅  can yield more up-to-date predictions which may yield a more meaningful user experience, etc.
- ❌  requires managed microservices to handle request traffic.
- ❌  requires real-time monitoring since input space in unbounded, which could yield erroneous predictions.

Online inference tasks

In our example task for batch inference above, how can online inference significantly improve content recommendations?

Show answer

With batch processing, we generate content recommendations for users offline using their history. These recommendations won't change until we process the batch the next day using the updated user features. But what is the user's taste significantly changes during the day (ex. user is searching for horror movies to watch). With real-time serving, we can use these recent features to recommend highly relevant content based on the immediate searches.

Our task

For our task, we'll be serving our model as a separate service to handle real-time requests. We want to be able to perform [online inference](https://madewithml.com/courses/mlops/serving/#online-inference) so that we can quickly categorize ML content as they become available. However, we will also demonstrate how to do [batch inference](https://madewithml.com/courses/mlops/serving/#batch-inference) for the sake of completeness.

### Feedback

How do we receive feedback on our system and incorporate it into the next iteration? This can involve both human-in-the-loop feedback as well as automatic feedback via [monitoring](https://madewithml.com/courses/mlops/monitoring/), etc.

Our task

- enforce human-in-loop checks when there is low confidence in classifications.
- allow users to report issues related to misclassification.

Always return to the value proposition

While it's important to iterate and optimize on our models, it's even more important to ensure that our ML systems are actually making an impact. We need to constantly engage with our users to iterate on why our ML system exists and how it can be made better.

![product development cycle](https://madewithml.com/static/images/mlops/systems-design/development_cycle.png)

-------------------------------------
# Data Preparation

---

Preparing our dataset by ingesting and splitting it.

## Intuition

We'll start by first preparing our data by ingesting it from source and splitting it into training, validation and test data splits.

### Ingestion

Our data could reside in many different places (databases, files, etc.) and exist in different formats (CSV, JSON, Parquet, etc.). For our application, we'll load the data from a CSV file to a [Pandas DataFrame](https://pandas.pydata.org/docs/reference/api/pandas.DataFrame.html) using the [`read_csv`](https://pandas.pydata.org/docs/reference/api/pandas.read_csv.html) function.

> Here is a quick refresher on the [Pandas](https://madewithml.com/courses/foundations/pandas/) library.

|   |
|---|
|`import pandas as pd`|

|   |
|---|
|`# Data ingestion DATASET_LOC = "https://raw.githubusercontent.com/GokuMohandas/Made-With-ML/main/datasets/dataset.csv" df = pd.read_csv(DATASET_LOC) df.head()`|

||id|created_on|title|description|tag|
|---|---|---|---|---|---|
|0|6|2020-02-20 06:43:18|Comparison between YOLO and RCNN on real world...|Bringing theory to experiment is cool. We can ...|computer-vision|
|1|7|2020-02-20 06:47:21|Show, Infer & Tell: Contextual Inference for C...|The beauty of the work lies in the way it arch...|computer-vision|
|2|9|2020-02-24 16:24:45|Awesome Graph Classification|A collection of important graph embedding, cla...|other|
|3|15|2020-02-28 23:55:26|Awesome Monte Carlo Tree Search|A curated list of Monte Carlo tree search pape...|other|
|4|25|2020-03-07 23:04:31|AttentionWalk|A PyTorch Implementation of "Watch Your Step: ...|other|

> In our [data engineering lesson](https://madewithml.com/courses/mlops/data-engineering/) we'll look at how to continually ingest data from more complex sources (ex. data warehouses)

### Splitting

Next, we need to split our training dataset into `train` and `val` data splits.

1. Use the `train` split to train the model.
    
    > Here the model will have access to both inputs (features) and outputs (labels) to optimize its internal weights.
    
2. After each iteration (epoch) through the training split, we will use the `val` split to determine the model's performance.
    
    > Here the model will not use the labels to optimize its weights but instead, we will use the validation performance to optimize training hyperparameters such as the learning rate, etc.
    
3. Finally, we will use a separate holdout [`test` dataset](https://github.com/GokuMohandas/Made-With-ML/blob/main/datasets/holdout.csv) to determine the model's performance after training.
    
    > This is our best measure of how the model may behave on new, unseen data that is from a similar distribution to our training dataset.
    

Tip

For our application, we will have a [training dataset](https://raw.githubusercontent.com/GokuMohandas/Made-With-ML/main/datasets/dataset.csv) to split into `train` and `val` splits and a **separate** [testing dataset](https://github.com/GokuMohandas/Made-With-ML/blob/main/datasets/holdout.csv) for the `test` set. While we could have one large dataset and split that into the three splits, it's a good idea to have a separate test dataset. Over time, our training data may grow and our test splits will look different every time. This will make it difficult to compare models against other models and against each other.

We can view the class counts in our dataset by using the [`pandas.DataFrame.value_counts`](https://pandas.pydata.org/docs/reference/api/pandas.DataFrame.value_counts.html) function:

|   |
|---|
|`from sklearn.model_selection import train_test_split`|

|   |
|---|
|`# Value counts df.tag.value_counts()`|

tag
natural-language-processing    310
computer-vision                285
other                          106
mlops                           63
Name: count, dtype: int64

For our multi-class task (where each project has exactly one tag), we want to ensure that the data splits have similar class distributions. We can achieve this by specifying how to stratify the split by using the [`stratify`](https://scikit-learn.org/stable/modules/generated/sklearn.model_selection.train_test_split.html) keyword argument with sklearn's [`train_test_split()`](https://scikit-learn.org/stable/modules/generated/sklearn.model_selection.train_test_split.html) function.

Creating proper data splits

What are the criteria we should focus on to ensure proper data splits?

Show answer

- the dataset (and each data split) should be representative of data we will encounter
- equal distributions of output values across all splits
- shuffle your data if it's organized in a way that prevents input variance
- avoid random shuffles if your task can suffer from data leaks (ex. `time-series`)

|   |
|---|
|`# Split dataset test_size = 0.2 train_df, val_df = train_test_split(df, stratify=df.tag, test_size=test_size, random_state=1234)`|

How can we validate that our data splits have similar class distributions? We can view the frequency of each class in each split:

|   |
|---|
|`# Train value counts train_df.tag.value_counts()`|

tag
natural-language-processing    248
computer-vision                228
other                           85
mlops                           50
Name: count, dtype: int64

Before we view our validation split's class counts, recall that our validation split is only `test_size` of the entire dataset. So we need to adjust the value counts so that we can compare it to the training split's class counts.

|   |
|---|
|`# Validation (adjusted) value counts val_df.tag.value_counts() * int((1-test_size) / test_size)`|

tag
natural-language-processing    248
computer-vision                228
other                           84
mlops                           52
Name: count, dtype: int64

These adjusted counts looks very similar to our train split's counts. Now we're ready to [explore](https://madewithml.com/courses/mlops/exploratory-data-analysis/) our dataset!
