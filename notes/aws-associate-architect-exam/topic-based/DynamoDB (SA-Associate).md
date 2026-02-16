##### . Question

Category: CSAA – Design High-Performing Architectures

A popular social network is hosted in AWS and is using a Amazon DynamoDB table as its database. There is a requirement to implement a ‘follow’ feature where users can subscribe to certain updates made by a particular user and be notified via email.

Which of the following is the most suitable solution to implement to meet the requirement?

- ==Enable DynamoDB Stream and create an AWS Lambda trigger, as well as the IAM role which contains all of the permissions that the Lambda function will need at runtime. The data from the stream record will be processed by the Lambda function which will then publish a message to Amazon SNS Topic that will notify the subscribers via email.==
- Using the Amazon Kinesis Client Library (KCL), write an application that leverages on DynamoDB Streams Kinesis Adapter that will fetch data from the DynamoDB Streams endpoint. When there are updates made by a particular user, notify the subscribers via email using Amazon SNS.
- Create an AWS Lambda function that uses DynamoDB Streams Amazon Kinesis Adapter which will fetch data from the DynamoDB Streams endpoint. Set up an Amazon SNS Topic that will notify the subscribers via email when there is an update made by a particular user.
- Set up a DAX cluster to access the source DynamoDB table. Create a new DynamoDB trigger and an AWS Lambda function. For every update made in the user data, the trigger will send data to the Lambda function which will then notify the subscribers via email using Amazon SNS.

Correct

A **DynamoDB stream** is an ordered flow of information about changes to items in an Amazon DynamoDB table. When you enable a stream on a table, DynamoDB captures information about every modification to data items in the table.

Whenever an application creates, updates, or deletes items in the table, DynamoDB Streams writes a stream record with the primary key attribute(s) of the items that were modified. A _stream record_ contains information about a data modification to a single item in a DynamoDB table. You can configure the stream so that the stream records capture additional information, such as the “before” and “after” images of modified items.

Amazon DynamoDB is integrated with AWS Lambda so that you can create _triggers_—pieces of code that automatically respond to events in DynamoDB Streams. With triggers, you can build applications that react to data modifications in DynamoDB tables.

If you enable DynamoDB Streams on a table, you can associate the stream ARN with a Lambda function that you write. Immediately after an item in the table is modified, a new record appears in the table’s stream. AWS Lambda polls the stream and invokes your Lambda function synchronously when it detects new stream records. The Lambda function can perform any actions you specify, such as sending a notification or initiating a workflow.

![Streams and Triggers](https://media.tutorialsdojo.com/StreamsAndTriggers.png)

Hence, the correct answer is: **Enable DynamoDB Stream and create an AWS Lambda trigger, as well as the IAM role which contains all of the permissions that the Lambda function will need at runtime. The data from the stream record will be processed by the Lambda function which will then publish a message to Amazon SNS Topic that will notify the subscribers via email**.

The option that says: **Using the Amazon Kinesis Client Library (KCL), write an application that leverages on DynamoDB Streams Kinesis Adapter that will fetch data from the DynamoDB Streams endpoint. When there are updates made by a particular user, notify the subscribers via email using Amazon SNS** is incorrect. Although this is a valid solution, it is missing a vital step which is to enable DynamoDB Streams. With the DynamoDB Streams Kinesis Adapter in place, you can begin developing applications via the KCL interface, with the API calls seamlessly directed at the DynamoDB Streams endpoint. Remember that the DynamoDB Stream feature is not enabled by default.

The option that says: **Create an AWS Lambda function that uses DynamoDB Streams Amazon Kinesis Adapter which will fetch data from the DynamoDB Streams endpoint. Set up an Amazon SNS Topic that will notify the subscribers via email when there is an update made by a particular user** is incorrect because just like in the above, you have to manually enable DynamoDB Streams first before you can use its endpoint.

The option that says: **Set up a DAX cluster to access the source DynamoDB table. Create a new DynamoDB trigger and an AWS Lambda function. For every update made in the user data, the trigger will send data to the Lambda function which will then notify the subscribers via email using Amazon SNS** is incorrect because the DynamoDB Accelerator (DAX) feature is primarily used to significantly improve the in-memory read performance of your database, and not to capture the time-ordered sequence of item-level modifications. You should use DynamoDB Streams in this scenario instead.

**References:**

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html)

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.Lambda.Tutorial.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.Lambda.Tutorial.html)

**Check out this Amazon DynamoDB Cheat Sheet:** 

[https://tutorialsdojo.com/amazon-dynamodb/](https://tutorialsdojo.com/amazon-dynamodb/)

##### 2. Question

Category: CSAA – Design High-Performing Architectures

A leading IT consulting company has an application which processes a large stream of financial data by an Amazon ECS Cluster then stores the result to a DynamoDB table. You have to design a solution to detect new entries in the DynamoDB table then automatically trigger a Lambda function to run some tests to verify the processed data.

What solution can be easily implemented to alert the Lambda function of new entries while requiring minimal configuration change to your architecture?

- Invoke the Lambda functions using SNS each time that the ECS Cluster successfully processed financial data.
- ==Enable DynamoDB Streams to capture table activity and automatically trigger the Lambda function.==
- Use CloudWatch Alarms to trigger the Lambda function whenever a new entry is created in the DynamoDB table.
- Use Systems Manager Automation to detect new entries in the DynamoDB table then automatically invoke the Lambda function for processing.

Amazon DynamoDB is integrated with AWS Lambda so that you can create _triggers_—pieces of code that automatically respond to events in DynamoDB Streams. With triggers, you can build applications that react to data modifications in DynamoDB tables.

If you enable DynamoDB Streams on a table, you can associate the stream ARN with a Lambda function that you write. Immediately after an item in the table is modified, a new record appears in the table’s stream. AWS Lambda polls the stream and invokes your Lambda function synchronously when it detects new stream records.

![](https://media.tutorialsdojo.com/StreamsAndTriggers.png)

You can create a Lambda function which can perform a specific action that you specify, such as sending a notification or initiating a workflow. For instance, you can set up a Lambda function to simply copy each stream record to persistent storage, such as EFS or S3, to create a permanent audit trail of write activity in your table.

Suppose you have a mobile gaming app that writes to a `TutorialsDojoCourses` table. Whenever the `TopCourse` attribute of the `TutorialsDojoScores` table is updated, a corresponding stream record is written to the table’s stream. This event could then trigger a Lambda function that posts a congratulatory message on a social media network. (The function would simply ignore any stream records that are not updated to `TutorialsDojoCourses` or that do not modify the `TopCourse` attribute.)

Hence, **enabling DynamoDB Streams to capture table activity and automatically trigger the Lambda function** is the correct answer because the requirement can be met with minimal configuration change using DynamoDB streams, which can automatically trigger Lambda functions whenever there is a new entry.

**Using CloudWatch Alarms to trigger the Lambda function whenever a new entry is created in the DynamoDB table** is incorrect because ==CloudWatch Alarms only monitor service metrics, not changes in DynamoDB table data.==

**Invoking the Lambda functions using SNS each time that the ECS Cluster successfully processed financial data** is incorrect because you don’t need to create an SNS topic just to invoke Lambda functions. You can enable DynamoDB streams instead to meet the requirement with less configuration.

**Using Systems Manager Automation to detect new entries in the DynamoDB table then automatically invoking the Lambda function for processing** is incorrect because the ==Systems Manager Automation service is primarily used to simplify common maintenance and deployment tasks of Amazon EC2 instances and other AWS resources.== It does not have the capability to detect new entries in a DynamoDB table.

**References:**

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.Lambda.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.Lambda.html)

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html)

**Check out this Amazon DynamoDB cheat sheet:**

[https://tutorialsdojo.com/amazon-dynamodb/](https://tutorialsdojo.com/amazon-dynamodb/)

##### 3. Question

Category: CSAA – Design Secure Architectures

A GraphQL API hosted is hosted in an Amazon EKS cluster with AWS Fargate launch type and deployed using AWS SAM. The API is connected to an Amazon DynamoDB table with DynamoDB Accelerator (DAX) as its data store. Both resources are hosted in the us-east-1 region.

The AWS IAM authenticator for Kubernetes is integrated into the EKS cluster for role-based access control (RBAC) and cluster authentication. A solutions architect must improve network security by preventing database calls from traversing the public internet. An automated cross-account backup for the DynamoDB table is also required for long-term retention.

Which of the following should the solutions architect implement to meet the requirement?

- Create a DynamoDB gateway endpoint. Set up a Network Access Control List (NACL) rule that allows outbound traffic to the `dynamodb.us-east-1.amazonaws.com` gateway endpoint. Use the built-in on-demand DynamoDB backups for cross-account backup and recovery.
- ==Create a DynamoDB gateway endpoint. Associate the endpoint to the appropriate route table. Use AWS Backup to automatically copy the on-demand DynamoDB backups to another AWS account for disaster recovery.==
- Create a DynamoDB interface endpoint. Associate the endpoint to the appropriate route table. Enable Point-in-Time Recovery (PITR) to restore the DynamoDB table to a particular point in time on the same or a different AWS account.
- Create a DynamoDB interface endpoint. Set up a stateless rule using AWS Network Firewall to control all outbound traffic to only use the `dynamodb.us-east-1.amazonaws.com` endpoint. Integrate the DynamoDB table with Amazon Timestream to allow point-in-time recovery from a different AWS account.

==Since DynamoDB tables are public resources, applications within a VPC rely on an Internet Gateway to route traffic to/from Amazon DynamoDB. You can use a Gateway endpoint if you want to keep the traffic between your VPC and Amazon DynamoDB within the Amazon network. This way, resources residing in your VPC can use their private IP addresses to access DynamoDB with no exposure to the public internet.==

When you create a DynamoDB Gateway endpoint, you specify the VPC where it will be deployed as well as the route table that will be associated with the endpoint. The route table will be updated with an Amazon DynamoDB prefix list (list of CIDR blocks) as the destination and the endpoint’s ID as the target.

![amazon dynamodb gateway endpoint](https://media.tutorialsdojo.com/amazon-dynamodb-gateway-endpoint.jpg)

DynamoDB on-demand backups are available at no additional cost beyond the normal pricing that’s associated with backup storage size. ==DynamoDB on-demand backups cannot be copied to a different account or Region. To create backup copies across AWS accounts and Regions and for other advanced features, you should use AWS Backup.==

With AWS Backup, you can configure backup policies and monitor activity for your AWS resources and on-premises workloads in one place. Using DynamoDB with AWS Backup, you can copy your on-demand backups across AWS accounts and Regions, add cost allocation tags to on-demand backups, and transition on-demand backups to cold storage for lower costs. To use these advanced features, you must opt into AWS Backup. Opt-in choices apply to the specific account and AWS Region, so you might have to opt into multiple Regions using the same account.

Hence, the correct answer is: **Create a DynamoDB gateway endpoint. Associate the endpoint to the appropriate route table. Use AWS Backup to automatically copy the on-demand DynamoDB backups to another AWS account for disaster recovery.**

The option that says: **Create a DynamoDB interface endpoint. Associate the endpoint to the appropriate route table. Enable Point-in-Time Recovery (PITR) to restore the DynamoDB table to a particular point in time on the same or a different AWS account** is incorrect. While this option addresses the network security requirement, Point-in-Time Recovery (PITR) is only used for restoring a DynamoDB table to a specific point in time within the same AWS account and region. It does not support cross-account backups or long-term retention. If this functionality is needed, you have to use the AWS Backup service instead.

The option that says: **Create a DynamoDB gateway endpoint. Set up a Network Access Control List (NACL) rule that allows outbound traffic to the `dynamodb.us-east-1.amazonaws.com` gateway endpoint. Use the built-in on-demand DynamoDB backups for cross-account backup and recovery** is incorrect because using a Network Access Control List alone is not enough to prevent traffic traversing to the public Internet. Moreover, you cannot copy DynamoDB on-demand backups to a different account or Region.

The option that says: **Create a DynamoDB interface endpoint. Set up a stateless rule using AWS Network Firewall to control all outbound traffic to only use the `dynamodb.us-east-1.amazonaws.com` endpoint. Integrate the DynamoDB table with Amazon Timestream to allow point-in-time recovery from a different AWS account** is incorrect. Keep in mind that the **`dynamodb.us-east-1.amazonaws.com`** is a public service endpoint for Amazon DynamoDB. Since the application is able to communicate with Amazon DynamoDB prior to the required architectural change, it’s implied that no firewalls (security group, NACL, etc.) are blocking traffic to/from Amazon DynamoDB, hence, adding an NACL rule to allow outbound traffic to DynamoDB is unnecessary. Furthermore, the use of the AWS Network Firewall in this solution is simply incorrect as you have to integrate this with your Amazon VPC. The use of Amazon Timestream is also wrong since this is a time series database service in AWS for IoT and operational applications. You cannot directly integrate DynamoDB and Amazon Timestream for the purpose of point-in-time data recovery.

**References:**

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/vpc-endpoints-dynamodb.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/vpc-endpoints-dynamodb.html)

[https://aws.amazon.com/blogs/database/how-to-configure-a-private-network-environment-for-amazon-dynamodb-using-vpc-endpoints/](https://aws.amazon.com/blogs/database/how-to-configure-a-private-network-environment-for-amazon-dynamodb-using-vpc-endpoints/)

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/BackupRestore.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/BackupRestore.html)

**Check out this Amazon DynamoDB Cheat sheet:**

[https://tutorialsdojo.com/amazon-dynamodb](https://tutorialsdojo.com/amazon-dynamodb)

##### 4. Question

Category: CSAA – Design High-Performing Architectures

A Docker application, which is running on an Amazon ECS cluster behind a load balancer, is heavily using Amazon DynamoDB. The application requires improved database performance by distributing the workload evenly and utilizing the provisioned throughput efficiently.

Which of the following should be implemented for the DynamoDB table?

- Reduce the number of partition keys in the DynamoDB table.
- ==Use partition keys with high-cardinality attributes, which have a large number of distinct values for each item.==
- Avoid using a composite primary key, which is composed of a partition key and a sort key.
- Use partition keys with low-cardinality attributes, which have a few number of distinct values for each item.

The partition key portion of a table’s primary key determines the logical partitions in which a table’s data is stored. This in turn affects the underlying physical partitions. Provisioned I/O capacity for the table is divided evenly among these physical partitions. Therefore a partition key design that doesn’t distribute I/O requests evenly can create “hot” partitions that result in throttling and use your provisioned I/O capacity inefficiently.

![DynamoDB Adaptive Capacity](https://media.tutorialsdojo.com/public/DynamoDB-adaptive-capacity.png)

The optimal usage of a table’s provisioned throughput depends not only on the workload patterns of individual items, but also on the partition-key design. This doesn’t mean that you must access all partition key values to achieve an efficient throughput level, or even that the percentage of accessed partition key values must be high. It does mean that the more distinct partition key values that your workload accesses, the more those requests will be spread across the partitioned space. In general, you will use your provisioned throughput more efficiently as the ratio of partition key values accessed to the total number of partition key values increases.

Hence, the correct answer is: **Use partition keys with high-cardinality attributes, which have a large number of distinct values for each item**.

The option that says: **Reducing the number of partition keys in the DynamoDB table** is incorrect. Instead of doing this, you should actually add more to improve its performance to distribute the I/O requests evenly and not simply avoid “hot” partitions.

The option that says: **Using partition keys with low-cardinality attributes, which have a few number of distinct values for each item** is incorrect because this is only the exact opposite of the correct answer. Remember that the more distinct partition key values your workload accesses, the more those requests will be spread across the partitioned space. Conversely, the less distinct partition key values, the less evenly spread it would be across the partitioned space, which effectively slows the performance.

The option that says: **Avoid using a composite primary key, which is composed of a partition key and a sort key** is incorrect because as mentioned, a composite primary key will provide more partition for the table and in turn, improves the performance. Hence, it should be used and not avoided.

**References:**

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-partition-key-uniform-load.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-partition-key-uniform-load.html)

[https://aws.amazon.com/blogs/database/choosing-the-right-dynamodb-partition-key/](https://aws.amazon.com/blogs/database/choosing-the-right-dynamodb-partition-key/)

**Check out this Amazon DynamoDB Cheat Sheet:**

[https://tutorialsdojo.com/amazon-dynamodb/](https://tutorialsdojo.com/amazon-dynamodb/)

**Amazon DynamoDB Overview:**

##### 5. Question

Category: CSAA – Design Resilient Architectures

A company is running a web application on AWS. The application is made up of an Auto-Scaling group that sits behind an Application Load Balancer and an Amazon DynamoDB table where user data is stored. The solutions architect must design the application to remain available in the event of a regional failure. A solution to automatically monitor the status of your workloads across your AWS account, conduct architectural reviews and check for AWS best practices.

Which configuration meets the requirement with the least amount of downtime possible?

- In a secondary region, create a global secondary index of the DynamoDB table and replicate the auto-scaling group and application load balancer. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up the AWS Compute Optimizer to automatically get recommendations for improving your workloads based on the AWS best practices
- Write a CloudFormation template that includes the auto-scaling group, application load balancer, and DynamoDB table. In the event of a failure, deploy the template in a secondary region. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up and configure the Amazon Managed Service for Prometheus service to receive insights for improving your workloads based on the AWS best practices.
- Write a CloudFormation template that includes the auto-scaling group, application load balancer, and DynamoDB table. In the event of a failure, deploy the template in a secondary region. Configure Amazon EventBridge (Amazon CloudWatch Events) to trigger a Lambda function that updates the application’s Route 53 DNS record. Launch an Amazon Managed Grafana workspace to automatically receive tips and action items for improving your workloads based on the AWS best practices
- ==In a secondary region, create a global table of the DynamoDB table and replicate the auto-scaling group and application load balancer. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up the AWS Well-Architected Tool to easily get recommendations for improving your workloads based on the AWS best practices==

==When you have more than one resource performing the same function—for example, more than one HTTP serve—you can configure Amazon Route 53 to check the health of your resources and respond to DNS queries using only the healthy resources. For example, suppose your website, example.com, is hosted on six servers, two each in three data centers around the world. You can configure Route 53 to check the health of those servers and to respond to DNS queries for example.com using only the servers that are currently healthy.==

**![](https://media.tutorialsdojo.com/amazon-route-53-dns-failover.jpg)**

In this scenario, you can replicate the process layer (EC2 instances, Application Load Balancer) to a different region and create a global table based on the existing DynamoDB table (data layer). Amazon DynamoDB will handle data synchronization between the tables in different regions. This way, the state of the application is preserved even in the event of an outage. Lastly, configure Route 53 DNS failover and set the DNS name of the backup application load balancer as a target.

You can also use the ==Well-Architected Tool== to automatically monitor the status of your workloads across your AWS account, conduct architectural reviews and check for AWS best practices.

![](https://media.tutorialsdojo.com/well-architected-tool-aws-saa-c03.png)

This tool is based on the AWS Well-Architected Framework, which was developed to help cloud architects build secure, high-performing, resilient, and efficient application infrastructures. The Framework has been used in tens of thousands of workload reviews by AWS solutions architects, and it ==provides a consistent approach for evaluating your cloud architecture and implementing designs that will scale with your application needs over time.==

Hence, the correct answer is: **In a secondary region, create a global table of the DynamoDB table and replicate the auto-scaling group and application load balancer. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up the AWS Well-Architected Tool to easily get recommendations for improving your workloads based on the AWS best practices**

The option that says: **In a secondary region, create a global secondary index of the DynamoDB table and replicate the auto-scaling group and application load balancer. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up the AWS Compute Optimizer to automatically get recommendations for improving your workloads based on the AWS best practices** is incorrect because this configuration is impossible to implement. A global secondary index can only be created in the region where its parent table resides. Moreover, the ==AWS Compute Optimizer simply helps you to identify the optimal AWS resource configurations, such as Amazon Elastic Compute Cloud (EC2) instance types, Amazon Elastic Block Store (EBS) volume configurations, and AWS Lambda function memory sizes. It is not capable of providing recommendations to improve your workloads based on AWS best practices.==

The option that says: **Write a CloudFormation template that includes the auto-scaling group, application load balancer, and DynamoDB table. In the event of a failure, deploy the template in a secondary region. Use Route 53 DNS failover to automatically route traffic to the resources in the secondary region. Set up and configure the Amazon Managed Service for Prometheus service to receive insights for improving your workloads based on the AWS best practices** is incorrect. This solution describes a situation in which the environment is provisioned only after a regional failure occurs. It won’t work because to enable Route 53 DNS failover, you’d need to target an existing environment. The use of the Amazon Managed Service for Prometheus service is irrelevant as well. This is just a serverless, Prometheus-compatible monitoring service for container metrics that makes it easier to securely monitor container environments at scale.

The option that says: **Write a CloudFormation template that includes the auto-scaling group, application load balancer, and DynamoDB table. In the event of a failure, deploy the template in a secondary region. Configure Amazon EventBridge (Amazon CloudWatch Events) to trigger a Lambda function that updates the application’s Route 53 DNS record. Launch an Amazon Managed Grafana workspace to automatically receive tips and action items for improving your workloads based on the AWS best practices** is incorrect. This could work, but it won’t deliver the shortest downtime possible since resource provisioning takes minutes to complete. Switching traffic to a standby environment is a faster method, albeit more expensive. Amazon Managed Grafana is a fully managed service with rich, interactive data visualizations to help customers analyze, monitor, and alarm on metrics, logs, and traces across multiple data sources. This service does not provide recommendations based on AWS best practices. You have to use the AWS Well-Architected Tool instead.

**References:**

[https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/dns-failover-configuring.html](https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/dns-failover-configuring.html)

[https://aws.amazon.com/blogs/networking-and-content-delivery/creating-disaster-recovery-mechanisms-using-amazon-route-53/](https://aws.amazon.com/blogs/networking-and-content-delivery/creating-disaster-recovery-mechanisms-using-amazon-route-53/)

[https://aws.amazon.com/well-architected-tool](https://aws.amazon.com/well-architected-tool)

**Check out this Amazon Route 53 Cheat Sheet:**

[https://tutorialsdojo.com/amazon-route-53/](https://tutorialsdojo.com/amazon-route-53/)

##### 6. Question

Category: CSAA – Design High-Performing Architectures

A company currently has an Augment Reality (AR) mobile game that has a serverless backend. It is using a DynamoDB table which was launched using the AWS CLI to store all the user data and information gathered from the players and a Lambda function to pull the data from DynamoDB. The game is being used by millions of users each day to read and store data.

How would you design the application to improve its overall performance and make it more scalable while keeping the costs low? (Select TWO

- ==Enable DynamoDB Accelerator (DAX) and ensure that the Auto Scaling is enabled and increase the maximum provisioned read and write capacity.==
- Configure CloudFront with DynamoDB as the origin; cache frequently accessed data on the client device using ElastiCache.
- Since Auto Scaling is enabled by default, the provisioned read and write capacity will adjust automatically. Also enable DynamoDB Accelerator (DAX) to improve the performance from milliseconds to microseconds.
- ==Use API Gateway in conjunction with Lambda and turn on the caching on frequently accessed data and enable DynamoDB global replication.==
- Use AWS IAM Identity Center to authenticate users and have them directly access DynamoDB using single sign-on. Manually set the provisioned read and write capacity to a higher RCU and WCU.

Correct

**Amazon DynamoDB Accelerator (DAX)** is a fully managed, highly available, in-memory cache for DynamoDB that delivers up to a 10x performance improvement – from milliseconds to microseconds – even at millions of requests per second. DAX does all the heavy lifting required to add in-memory acceleration to your DynamoDB tables, without requiring developers to manage cache invalidation, data population, or cluster management.

![](https://media.tutorialsdojo.com/ddb_as_set_read_1.png)

**Amazon API Gateway** lets you create an API that acts as a “front door” for applications to access data, business logic, or functionality from your back-end services, such as code running on AWS Lambda. Amazon API Gateway handles all of the tasks involved in accepting and processing up to hundreds of thousands of concurrent API calls, including traffic management, authorization, and access control, monitoring, and API version management. Amazon API Gateway has no minimum fees or startup costs.

**AWS Lambda** scales your functions automatically on your behalf. Every time an event notification is received for your function, AWS Lambda quickly locates free capacity within its compute fleet and runs your code. Since your code is stateless, AWS Lambda can start as many copies of your function as needed without lengthy deployment and configuration delays.

The correct answers are the options that say:

**– Enable DynamoDB Accelerator (DAX) and ensure that the Auto Scaling is enabled and increase the maximum provisioned read and write capacity.**

**– Use API Gateway in conjunction with Lambda and turn on the caching on frequently accessed data and enable DynamoDB global replication.**

The option that says: **Configure CloudFront with DynamoDB as the origin; cache frequently accessed data on the client device using ElastiCache** is incorrect. Although CloudFront delivers content faster to your users using edge locations, you still cannot integrate DynamoDB table with CloudFront as these two are incompatible.

The option that says: **Use AWS IAM Identity Center to authenticate users and have them directly access DynamoDB using single sign-on. Manually set the provisioned read and write capacity to a higher RCU and WCU** is incorrect because AWS IAM Identity Center is a service that just makes it easy to centrally manage access to multiple AWS accounts and business applications. This will not be of much help to the scalability and performance of the application. It is costly to manually set the provisioned read and write capacity to a higher RCU and WCU because this capacity will run round the clock and will still be the same even if the incoming traffic is stable and there is no need to scale.

The option that says: **Since Auto Scaling is enabled by default, the provisioned read and write capacity will adjust automatically. Also enable DynamoDB Accelerator (DAX) to improve the performance from milliseconds to microseconds** is incorrect because by default, Auto Scaling is not enabled in a DynamoDB table, which is created using the AWS CLI.

**References:**

[https://aws.amazon.com/lambda/faqs/](https://aws.amazon.com/lambda/faqs/)

[https://aws.amazon.com/api-gateway/faqs/](https://aws.amazon.com/api-gateway/faqs/)

[https://aws.amazon.com/dynamodb/dax/](https://aws.amazon.com/dynamodb/dax/)

**Tutorials Dojo’s AWS Certified Solutions Architect Associate Exam Study Guide:**

[https://tutorialsdojo.com/aws-certified-solutions-architect-associate/](https://tutorialsdojo.com/aws-certified-solutions-architect-associate/)

##### 7. Question

Category: CSAA – Design High-Performing Architectures

A healthcare organization wants to build a system that can predict drug prescription abuse. The organization will gather real-time data from multiple sources, which include Personally Identifiable Information (PII). It’s crucial that this sensitive information is anonymized prior to landing in a NoSQL database for further processing.

Which solution would meet the requirements?

-   
    Stream the data in an Amazon DynamoDB table. Enable DynamoDB Streams, and configure an AWS Lambda function with `AmazonDynamoDBFullAccess` permissions to perform anonymization on newly written items.
- Create a data lake in Amazon S3 and use it as the primary storage for patient health data. Use an S3 trigger to run an AWS Lambda function that performs anonymization. Send the anonymized data to Amazon DynamoDB.
- Deploy an Amazon Data Firehose stream to capture and transform the streaming data. Deliver the anonymized data to Amazon Redshift for analysis.
- ==Ingest real-time data using Amazon Kinesis Data Stream. Use an AWS Lambda function to anonymize the PII, then store it in Amazon DynamoDB.==

**Amazon Kinesis Data Streams (KDS)** is a massively scalable and durable real-time data streaming service. KDS can continuously capture gigabytes of data per second from hundreds of thousands of sources.

![How Amazon Kinesis Data Streams works](https://media.tutorialsdojo.com/product-page-diagram_Amazon-Kinesis-Data-Streams.074de94302fd60948e1ad070e425eeda73d350e7.png "How Amazon Kinesis Data Streams works")

Kinesis Data Streams integrates seamlessly with AWS Lambda, which can be utilized to transform and anonymize Personally Identifiable Information (PII) in transit before it is stored in any system. This ensures that sensitive information is anonymized immediately, preventing unanonymized PII from being stored in any storage system, as required. The anonymized data is then stored in Amazon DynamoDB, a NoSQL database suitable for handling the processed data for further analysis, such as predicting drug prescription abuse.

Hence, the correct answer is: **Ingest real-time data using Amazon Kinesis Data Stream. Use an AWS Lambda function to anonymize the PII, then store it in Amazon DynamoDB.**

The option that says: **Create a data lake in Amazon S3 and use it as the primary storage for patient health data. Use an S3 trigger to run an AWS Lambda function that performs anonymization. Send the anonymized data to Amazon DynamoDB** is incorrect. This approach stores unanonymized PII in Amazon S3 before the Lambda function anonymizes it. This simply violates the requirement that PII be anonymized before landing in any storage system. Storing sensitive data in S3, even temporarily, only increases the risk of exposure and does not comply with the privacy requirements.

The option that says: **Stream the data in an Amazon DynamoDB table. Enable DynamoDB Streams, and configure an AWS Lambda function with `AmazonDynamoDBFullAccess` permissions to perform anonymization on newly written items** is incorrect. DynamoDB Streams processes changes to already written data, meaning unanonymized PII would be stored in DynamoDB before anonymization, violating the requirement. Additionally, using `AmazonDynamoDBFullAccess` violates the principle of least privilege, as it primarily grants more permissions than necessary.

The option that says: **Deploy an Amazon Data Firehose stream to capture and transform the streaming data. Deliver the anonymized data to Amazon Redshift for analysis** is incorrect. The requirement specifies that the anonymized data must be stored in a NoSQL database. Amazon Redshift is a relational data warehousing solution, not a NoSQL database, making this option unsuitable.

**References:**

[https://aws.amazon.com/kinesis/data-streams/](https://aws.amazon.com/kinesis/data-streams/)

[https://docs.aws.amazon.com/lambda/latest/dg/with-kinesis.html](https://docs.aws.amazon.com/lambda/latest/dg/with-kinesis.html)

**Check out this Amazon Kinesis Cheat Sheet:**

[https://tutorialsdojo.com/amazon-kinesis/](https://tutorialsdojo.com/amazon-kinesis/)

##### 8. Question

Category: CSAA – Design High-Performing Architectures

A popular augmented reality (AR) mobile game is heavily using a RESTful API which is hosted in AWS. The API uses Amazon API Gateway and a DynamoDB table with a preconfigured read and write capacity. Based on your systems monitoring, the DynamoDB table begins to throttle requests during high peak loads which causes the slow performance of the game.

Which of the following can you do to improve the performance of your app?

- Add the DynamoDB table to an Auto Scaling Group.
- Create an SQS queue in front of the DynamoDB table.
- Integrate an Application Load Balancer with your DynamoDB table.
- ==Use DynamoDB Auto Scaling==

**DynamoDB auto scaling** uses the AWS Application Auto Scaling service to dynamically adjust provisioned throughput capacity on your behalf, in response to actual traffic patterns. This enables a table or a global secondary index to increase its provisioned read and write capacity to handle sudden increases in traffic, without throttling. When the workload decreases, Application Auto Scaling decreases the throughput so that you don’t pay for unused provisioned capacity.

**Using DynamoDB Auto Scaling** is the best answer. ==DynamoDB Auto Scaling uses the AWS Application Auto Scaling service to dynamically adjust provisioned throughput capacity on your behalf.==

**Integrating an Application Load Balancer with your DynamoDB table** is incorrect because an Application Load Balancer is not suitable to be used with DynamoDB and in addition, this will not increase the throughput of your DynamoDB table.

**Adding the DynamoDB table to an Auto Scaling Group** is incorrect because you usually put EC2 instances on an Auto Scaling Group, and not a DynamoDB table.

**Creating an SQS queue in front of the DynamoDB table** is incorrect because this is not a design principle for high throughput DynamoDB table. Using SQS is for handling queuing and polling the request. This will not increase the throughput of DynamoDB which is required in this situation.

**Reference:**

[https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/AutoScaling.html](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/AutoScaling.html)

**Check out this Amazon DynamoDB Cheat Sheet:**

[https://tutorialsdojo.com/amazon-dynamodb/](https://tutorialsdojo.com/amazon-dynamodb/)

**Amazon DynamoDB Overview:**