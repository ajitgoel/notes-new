##### **CloudWatch** 
==CloudWatch is AWS’s monitoring and observability service. As a developer, you use it to collect metrics, logs, and traces from your applications.==  
**Metrics**: ==Every AWS service emits metrics (CPU usage, request count, error rates).== You can publish custom metrics from your app using `PutMetricData` API or the embedded metric format (EMF) in structured logs.  
**Logs**: ==Your app logs go to CloudWatch Log Groups. Each log group contains log streams. Use Log Insights to query logs with a SQL-like syntax:==

```
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
| limit 50
```
**Structured Logging**: Emit logs as JSON so Log Insights can parse and filter on specific fields without regex.  
**Metric Filters**: Create metrics from log patterns — e.g., count occurrences of “OutOfMemoryError” and graph them over time.  
**Retention**: Set log retention policies (1 day to 10 years) to control costs. Logs are expensive at scale.

###### **CloudWatch Alarms** 
Alarms evaluate a metric against a threshold and trigger actions (SNS notification, Auto Scaling, Lambda).  
**Anatomy of an alarm**:

- **Metric**: what you’re watching (e.g., `CPUUtilization`)
- **Statistic**: how to aggregate (Average, Sum, p99)
- **Period**: evaluation window (e.g., 60 seconds)
- **Threshold**: the trigger value
- **Datapoints to alarm**: e.g., 3 out of 5 periods breaching before firing  
    **Composite Alarms**: Combine multiple alarms with AND/OR logic. Useful to reduce noise — only alert when both high error rate AND high latency occur together.  
    **Anomaly Detection**: Instead of static thresholds, CloudWatch can learn normal patterns and alarm on deviations. Good for metrics with variable baselines like request counts.  
    **OK Actions**: Trigger actions when an alarm returns to OK state — useful for auto-remediation or “all clear” notifications.

###### **CloudWatch Dashboards** 

Dashboards are customizable visual displays of your metrics and logs.  
**Widgets**: Line charts, numbers, text, log query results, alarm status. You can mix metrics from multiple AWS accounts and regions on one dashboard.  
**Automatic Dashboards**: AWS auto-generates dashboards per service (EC2, Lambda, etc.) showing key metrics. Good starting point before building custom ones.  
**Dashboard as Code**: Define dashboards in JSON or through CDK/CloudFormation. This is the right approach — don’t click around in the console.

```json
{
  "widgets": [{
    "type": "metric",
    "properties": {
      "metrics": [["AWS/ECS", "CPUUtilization", "ClusterName", "my-cluster"]],
      "period": 300,
      "stat": "Average"
    }
  }]
}
```

  **Cross-Account Dashboards**: Set up a monitoring account that aggregates metrics from multiple workload accounts using CloudWatch cross-account observability.
###### **CloudWatch Synthetic Canaries** 
==Canaries are configurable scripts that run on a schedule to monitor your endpoints and APIs. They use a headless Chromium browser (for UI tests) or HTTP calls (for API tests).==  
**Use cases**: Verify login flows work, check API response times, validate SSL certificates, detect visual regressions.  
**How they work**: You write a Node.js or Python script using the Synthetics runtime. AWS runs it every N minutes. If it fails, it triggers a CloudWatch alarm.

```javascript
const synthetics = require('Synthetics');
const log = require('SyntheticsLogger');
const pageLoadBlueprint = async function () {
  const page = await synthetics.getPage();
  const response = await page.goto('https://myapp.com/health', {
    waitUntil: 'domcontentloaded', timeout: 30000
  });
  if (response.status() !== 200) {
    throw 'Health check failed with status: ' + response.status();
  }
};
exports.handler = async () => {
  return await pageLoadBlueprint();
};
```

**Visual Monitoring**: Canaries can take screenshots and compare them against baselines to detect UI regressions.  
**HAR Files**: Canaries generate HTTP Archive files so you can inspect every network request, timing, and response code.  
**Cost tip**: Run canaries every 5-10 minutes, not every minute, unless you need sub-minute detection.
##### **ECS (Elastic Container Service)** 
ECS runs your Docker containers on AWS. Two launch types:
- **Fargate**: Serverless — you don’t manage EC2 instances. You specify CPU/memory per task and AWS handles the rest. Start here.
- **EC2**: You manage the underlying instances. Use when you need GPU, specific instance types, or cost optimization at scale.  
    **Core concepts**:
- **Task Definition**: A blueprint describing your container(s) — image, CPU, memory, ports, env vars, log config. Versioned and immutable.
- **Task**: A running instance of a task definition. Ephemeral.
- **Service**: Maintains a desired count of tasks, handles rolling deployments, integrates with load balancers.
- **Cluster**: A logical grouping of services and tasks.  
    **Task Definition essentials**:
- ```json
    {
      "containerDefinitions": [{
        "name": "api",
        "image": "123456789.dkr.ecr.us-east-1.amazonaws.com/my-api:latest",
        "cpu": 256,
        "memory": 512,
        "portMappings": [{ "containerPort": 8080 }],
        "logConfiguration": {
          "logDriver": "awslogs",
          "options": {
            "awslogs-group": "/ecs/my-api",
            "awslogs-region": "us-east-1",
            "awslogs-stream-prefix": "ecs"
          }
        }
      }]
    }
    ```
**Service Connect**: Service-to-service communication using namespaces. Provides load balancing, retries, and observability between ECS services without managing a service mesh.  
**Exec into containers**: `aws ecs execute-command` gives you an interactive shell into a running Fargate task for debugging. Requires enabling `ExecuteCommand` on the service.  
**Capacity Providers**: Control how tasks are placed — Fargate, Fargate Spot (cheaper, can be interrupted), or specific EC2 Auto Scaling groups.
##### **CI/CD on AWS** 
**CodePipeline**: Orchestrates the overall pipeline — source → build → test → deploy. It connects stages together but doesn’t do the actual work.  
**CodeBuild**: Runs your build/test commands in a managed container. Define steps in `buildspec.yml`:

```yaml
version: 0.2
phases:
  install:
    runtime-versions:
      nodejs: 18
  pre_build:
    commands:
      - npm ci
  build:
    commands:
      - npm run test
      - npm run build
      - docker build -t $ECR_REPO:$CODEBUILD_RESOLVED_SOURCE_VERSION .
      - docker push $ECR_REPO:$CODEBUILD_RESOLVED_SOURCE_VERSION
artifacts:
  files:
    - imagedefinitions.json
```
###### **CodeDeploy**: Handles deployment strategies for ECS, EC2, and Lambda:
- **Rolling**: Replace instances in batches
- **Blue/Green**: Spin up new environment, shift traffic, tear down old
- **Canary**: Route a small percentage of traffic to the new version first 
    **For ECS Blue/Green deployments**: CodeDeploy manages two target groups on your ALB. It shifts traffic from blue to green, with optional test listener for validation before cutover.  
    **GitHub Actions alternative**: Many teams skip CodePipeline entirely and use GitHub Actions with OIDC federation to assume an IAM role and deploy directly. This avoids maintaining AWS-native CI/CD infrastructure.  
    **ECR (Elastic Container Registry)**: Store your Docker images here. Enable image scanning for vulnerability detection. Use immutable tags in production — don’t rely on `latest`.

##### **AppConfig** 
==AppConfig is a feature of AWS Systems Manager for managing, deploying, and validating application configuration and feature flags separately from code deployments.==  
**Why use it**: Decouple config changes from deployments. Change a feature flag or timeout value without redeploying your app.  
**Core workflow**:
1. **Create an Application** (logical grouping)
2. **Create an Environment** (dev, staging, prod)
3. **Create a Configuration Profile** — your actual config, stored in AppConfig hosted config, S3, SSM Parameter Store, or SSM Document
4. **Deploy** the config with a deployment strategy  
    **Deployment Strategies**:

- **AllAtOnce**: Immediate. Good for dev.
- **Linear**: Roll out in equal increments (e.g., 20% every 5 minutes).
- **Exponential**: Start small, accelerate (e.g., 1%, 2%, 4%, 8%…).  
    **Validators**: Attach JSON Schema or Lambda validators. AppConfig validates your config before deploying — catches typos and invalid values before they hit production.  
    **Feature Flags**: AppConfig has native feature flag support with a dedicated API. You define flags with attributes, conditions, and default values. No need for third-party tools like LaunchDarkly for basic use cases.  
    **Retrieving config in your app**: Use the AppConfig Agent (a sidecar or Lambda extension) which caches config locally and polls for updates. Don’t call the API directly on every request.
- ```python
    import requests
    # AppConfig Agent runs locally on port 2772
    config = requests.get(
        'http://localhost:2772/applications/my-app/environments/prod/configurations/my-config'
    ).json()
    ```
**Rollback**: If CloudWatch alarms fire during deployment, AppConfig automatically rolls back to the previous config version.
##### **AWS CDK (Cloud Development Kit)** 
==CDK lets you define infrastructure using TypeScript, Python, Java, Go, or C#. It synthesizes your code into CloudFormation templates.==  
**Why CDK over raw CloudFormation**: Real programming constructs — loops, conditionals, composition, type checking. CloudFormation YAML is painful at scale.  
**Core concepts**:
- **App**: The root of your CDK application
- **Stack**: Maps 1:1 to a CloudFormation stack. Unit of deployment.
- **Construct**: A building block. Three levels:
- **L1 (Cfn)**: Raw CloudFormation resources. Prefixed with `Cfn`. Full control, no opinions.
- **L2**: Opinionated defaults with sensible security. e.g., `new s3.Bucket()` creates a bucket with encryption enabled by default.
- **L3 (Patterns)**: Multi-resource patterns. e.g., `ApplicationLoadBalancedFargateService` sets up an ALB, ECS service, target group, security groups, and log group in one construct.  
    **Example**:
- ```typescript
    import * as cdk from 'aws-cdk-lib';
    import * as ecs from 'aws-cdk-lib/aws-ecs';
    import * as ecsPatterns from 'aws-cdk-lib/aws-ecs-patterns';
    export class MyStack extends cdk.Stack {
      constructor(scope: cdk.App, id: string) {
        super(scope, id);
        new ecsPatterns.ApplicationLoadBalancedFargateService(this, 'Api', {
          taskImageOptions: {
            image: ecs.ContainerImage.fromAsset('./api'),
            containerPort: 8080,
            environment: { NODE_ENV: 'production' },
          },
          desiredCount: 2,
          cpu: 256,
          memoryLimitMiB: 512,
        });
      }
    }
    ```
**Key commands**:

- `cdk synth` — generate CloudFormation template
- `cdk diff` — preview changes before deploying
- `cdk deploy` — deploy the stack
- `cdk destroy` — tear down the stack  
    **cdk.context.json**: CDK caches lookup values (VPC IDs, AMI IDs) here. Commit this file — it ensures deterministic builds.  
    **Aspects**: Apply cross-cutting rules to all resources. e.g., enforce tagging or ensure all S3 buckets have encryption:
- ```typescript
    class BucketEncryption implements cdk.IAspect {
      visit(node: IConstruct) {
        if (node instanceof s3.CfnBucket) {
          node.bucketEncryption = { /* ... */ };
        }
      }
    }
    cdk.Aspects.of(app).add(new BucketEncryption());
    ```
**Testing**: Use `assertions` module to unit test your stacks — verify resources exist, have correct properties, and reference each other properly.
##### **CloudTrail** 
==CloudTrail records API calls made in your AWS account.== Every `CreateBucket`, `RunTask`, `AssumeRole` — all logged.  
**Management Events**: Control plane operations (creating/deleting resources, IAM changes). Enabled by default on all accounts.  
**Data Events**: Data plane operations (S3 `GetObject`, Lambda `Invoke`, DynamoDB `GetItem`). Not enabled by default — must opt in. Can be high volume and expensive.  
**Event History**: The console shows 90 days of management events for free. For longer retention, create a Trail that delivers events to S3.  
**Trail setup**: Create an organization trail to capture events across all accounts in your AWS Organization. Deliver to a central S3 bucket in your logging account.  
**CloudTrail Lake**: Query events with SQL instead of parsing JSON files in S3. Useful for investigations:

```sql
SELECT eventTime, eventName, userIdentity.arn, sourceIPAddress
FROM my_event_data_store
WHERE eventName = 'DeleteBucket'
AND eventTime > '2026-04-01'
```

**Developer use cases**:
- **Debugging permission errors**: Find the exact API call that got denied, see which policy was evaluated.
- **Auditing**: Who changed that security group? When was that Lambda function last updated?
- **Incident response**: Trace what an compromised credential did — every API call with timestamps and source IPs.  
    **Insights**: CloudTrail can detect unusual API activity (e.g., a sudden spike in `DeleteObject` calls) and raise events.  
    **Integration with EventBridge**: Route specific CloudTrail events to Lambda for automated response — e.g., auto-remediate a public S3 bucket.