##### 1. Question

Category: CSAA – Design Resilient Architectures

An accounting application uses an Amazon RDS database configured with Multi-AZ deployments to improve availability. What would happen to RDS if the primary database instance fails?

- The primary database instance will reboot.
- A new database instance is created in the standby Availability Zone.
- ==The canonical name record (CNAME) is switched from the primary to standby instance.==
- The IP address of the primary DB instance is switched to the standby DB instance.

Correct

In **Amazon RDS**, failover is automatically handled so that you can resume database operations as quickly as possible without administrative intervention in the event that your primary database instance goes down. When failing over, Amazon RDS simply flips the canonical name record (CNAME) for your DB instance to point at the standby, which is in turn promoted to become the new primary.

![Amazon RDS](https://media.tutorialsdojo.com/rds_ha_5.png)

Hence, the correct answer is: **The canonical name record (CNAME) is switched from the primary to standby instance.**

The option that says: **The IP address of the primary DB instance is switched to the standby DB instance** is incorrect because IP addresses are per subnet, and subnets simply cannot span multiple AZs.

The option that says: **The primary database instance will reboot** is incorrect because, in the event of a failure, there is typically no database to reboot with.

The option that says: **A new database instance is created in the standby Availability Zone** is incorrect because with multi-AZ enabled, you already have a standby database in another AZ.

**References:**

[https://aws.amazon.com/rds/details/multi-az/](https://aws.amazon.com/rds/details/multi-az/)

[https://aws.amazon.com/rds/faqs/](https://aws.amazon.com/rds/faqs/)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 2. Question

Category: CSAA – Design Secure Architectures

A financial application consists of an Auto Scaling group of Amazon EC2 instances, an Application Load Balancer, and a MySQL RDS instance set up in a Multi-AZ Deployment configuration. To protect customers’ confidential data, it must be ensured that the Amazon RDS database is only accessible using an authentication token specific to the profile credentials of EC2 instances.

Which of the following actions should be taken to meet this requirement?

- Create an IAM Role and assign it to your EC2 instances which will grant exclusive access to your RDS instance.
- Configure SSL in your application to encrypt the database connection to RDS.
- Use a combination of IAM and STS to enforce restricted access to your RDS instance using a temporary authentication token.
- ==Enable the IAM DB Authentication.==

You can authenticate to your DB instance using AWS Identity and Access Management (IAM) database authentication. IAM database authentication works with MySQL and PostgreSQL. With this authentication method, you don’t need to use a password when you connect to a DB instance. Instead, you use an authentication token.

An **_authentication token_** is a unique string of characters that Amazon RDS generates on request. Authentication tokens are generated using AWS Signature Version 4. Each token has a lifetime of 15 minutes. You don’t need to store user credentials in the database, because authentication is managed externally using IAM. You can also still use standard database authentication.

![IAM database authentication](https://media.tutorialsdojo.com/2019-01-13_07-04-06-a2157247b0fa129795001208504fcb51.png)

IAM database authentication provides the following benefits:

1. Network traffic to and from the database is encrypted using Secure Sockets Layer (SSL).
2. You can use IAM to centrally manage access to your database resources, instead of managing access individually on each DB instance.
3. For applications running on Amazon EC2, you can use profile credentials specific to your EC2 instance to access your database instead of a password, for greater security

Hence, the correct answer is: **Enable the IAM DB Authentication**.

The option that says: **Configuring SSL in your application to encrypt the database connection to RDS** is incorrect because an SSL connection is not just using an authentication token from IAM. Although configuring SSL to your application can improve the security of your data in flight, it is still not a suitable option to use in this scenario.

The option that says: **Creating an IAM Role and assigning it to your EC2 instances which will grant exclusive access to your RDS instance** is incorrect because although you can create and assign an IAM Role to your EC2 instances, you still need to configure your RDS to use IAM DB Authentication.

The option that says: **Use a combination of IAM and STS to enforce restricted access to your RDS instance using a temporary authentication token** is incorrect because you have to use IAM DB Authentication for this scenario, and not simply a combination of an IAM and STS. Although ==STS is used to send temporary tokens for authentication==, this is not a compatible use case for RDS.

**References:**

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.IAMDBAuth.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.IAMDBAuth.html)

[https://aws.amazon.com/rds/](https://aws.amazon.com/rds/%C2%A0) 

**Check out this Amazon RDS cheat sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 3. Question

Category: CSAA – Design Resilient Architectures

An application that records weather data every minute is deployed in a fleet of Amazon EC2 Spot instances and uses a MySQL RDS database instance. Currently, there is only one Amazon RDS instance running in one Availability Zone. The database needs to be improved to ensure high availability by enabling synchronous data replication to another RDS instance.

Which of the following performs synchronous data replication in RDS?

- RDS Read Replica
- ==RDS DB instance running as a Multi-AZ deployment==
- Amazon DynamoDB Read Replica

When you create or modify your DB instance to run as a Multi-AZ deployment, Amazon RDS automatically provisions and maintains a synchronous **standby** replica in a different Availability Zone. Updates to your DB Instance are synchronously replicated across Availability Zones to the standby in order to keep both in sync and protect your latest database updates against DB instance failure.

![Amazon RDS DB instance types comparison](https://media.tutorialsdojo.com/2019-06-07_10-00-40-e7c750751ea701ec7b91cbeeb464f364.png)

Therefore, the correct answer is: **RDS DB instance running as a Multi-AZ deployment**

**RDS Read Replica** is incorrect as a ==Read Replica primarily provides an asynchronous replication instead of synchronous.==

==**Amazon DynamoDB Read Replica**== is incorrect since it does not offer a Read Replica feature. It typically ==uses global tables to replicate data across multiple AWS Regions.==

**Amazon CloudFront running as a Multi-AZ deployment** is incorrect as it also does not have a Read Replica feature. It simply caches content at edge locations rather than replicating data in the database.

**References:**

[https://aws.amazon.com/rds/details/multi-az/](https://aws.amazon.com/rds/details/multi-az/)

[https://aws.amazon.com/rds/features/multi-az/](https://aws.amazon.com/rds/features/multi-az/)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 1. Question

Category: CSAA – Design High-Performing Architectures

Due to the large volume of query requests, the database performance of an online reporting application significantly slowed down. The Solutions Architect is trying to convince her client to use Amazon RDS Read Replica for their application instead of setting up a Multi-AZ Deployments configuration.

What are two benefits of using Read Replicas over Multi-AZ that the Architect should point out? (Select TWO.)

- ==Provides asynchronous replication and improves the performance of the primary database by taking read-heavy database workloads from it.==
- Allows both read and write operations on the read replica to complement the primary database.
- It enhances the read performance of your primary database by increasing its IOPS and accelerates its query processing via AWS Global Accelerator.
- Provides synchronous replication and automatic failover in the case of Availability Zone service failures.
- ==It elastically scales out beyond the capacity constraints of a single DB instance for read-heavy database workloads.==

Correct

Amazon RDS Read Replicas provide enhanced performance and durability for database (DB) instances. This feature makes it easy to elastically scale out beyond the capacity constraints of a single DB instance for read-heavy database workloads.

You can create one or more replicas of a given source DB Instance and serve high-volume application read traffic from multiple copies of your data, thereby increasing aggregate read throughput. Read replicas can also be promoted when needed to become standalone DB instances.

For the MySQL, MariaDB, PostgreSQL, and Oracle database engines, Amazon RDS creates a second DB instance using a snapshot of the source DB instance. It then uses the engines’ native asynchronous replication to update the read replica whenever there is a change to the source DB instance. The read replica operates as a DB instance that allows only read-only connections; applications can connect to a read replica just as they would to any DB instance. Amazon RDS replicates all databases in the source DB instance.

![](https://media.tutorialsdojo.com/2020-02-28_01-52-40-4fa2635076a98c44c28464d31d793a21.png)

When you create a read replica for Amazon RDS for MySQL, MariaDB, PostgreSQL, and Oracle, Amazon RDS sets up a secure communications channel using public-key encryption between the source DB instance and the read replica, even when replicating across regions. Amazon RDS establishes any AWS security configurations, such as adding security group entries needed to enable the secure channel.

You can also create read replicas within a Region or between Regions for your Amazon RDS for MySQL, MariaDB, PostgreSQL, and Oracle database instances encrypted at rest with AWS Key Management Service (KMS).

Hence, the correct answers are:

    **– It elastically scales out beyond the capacity constraints of a single DB instance for read-heavy database workloads.**

    **– Provides asynchronous replication and improves the performance of the primary database by taking read-heavy database workloads from it.**

The option that says: **Allows both read and write operations on the read replica to complement the primary database** is incorrect, as Read Replicas are primarily used to offload read-only operations from the primary database instance. By default, you can’t do a write operation to your Read Replica.

The option that says: **Provides synchronous replication and automatic failover in the case of Availability Zone service failures** is incorrect as this is a benefit of Multi-AZ and not of a Read Replica. Moreover, Read Replicas provide an asynchronous type of replication and not synchronous replication.

The option that says: **It enhances the read performance of your primary database by increasing its IOPS and accelerates its query processing via AWS Global Accelerator** is incorrect because Read Replicas do not do anything to upgrade or increase the read throughput on the primary DB instance per se, but it provides a way for your application to fetch data from replicas. In this way, it improves the overall performance of your entire database tier (and not just the primary DB instance). It doesn’t increase the IOPS nor use AWS Global Accelerator to accelerate the compute capacity of your primary database. AWS Global Accelerator is a networking service not related to RDS that directs user traffic to the nearest application endpoint to the client, thus reducing internet latency and jitter. It simply routes the traffic to the closest edge location via Anycast.

**References:**

[https://aws.amazon.com/rds/details/read-replicas/](https://aws.amazon.com/rds/details/read-replicas/)

[https://aws.amazon.com/rds/features/multi-az/](https://aws.amazon.com/rds/features/multi-az/)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 2. Question

Category: CSAA – Design High-Performing Architectures

A company launched a global news website that is deployed to AWS and is using MySQL RDS. The website has millions of viewers from all over the world, which means that the website has a read-heavy database workload. All database transactions must be ACID compliant to ensure data integrity.

In this scenario, which of the following is the best option to use to increase the read-throughput on the MySQL database?

- Enable Amazon RDS Standby Replicas
- Use SQS to queue up the requests
- Enable Multi-AZ deployments
- ==Enable Amazon RDS Read Replicas==

**Amazon RDS Read Replicas** provide enhanced performance and durability for database (DB) instances. This feature makes it easy to elastically scale out beyond the capacity constraints of a single DB instance for read-heavy database workloads. You can create one or more replicas of a given source DB Instance and serve high-volume application read traffic from multiple copies of your data, thereby increasing aggregate read throughput. Read replicas can also be promoted when needed to become standalone DB instances. Read replicas are available in Amazon RDS for MySQL, MariaDB, Oracle, and PostgreSQL as well as Amazon Aurora.

![](https://media.tutorialsdojo.com/public/MySQLConnector.png)

**Enabling Multi-AZ deployments** is incorrect because the ==Multi-AZ deployments feature is mainly used to achieve high availability and failover support for your database.==

**Enabling Amazon RDS Standby Replicas** is incorrect because a ==Standby replica is used in Multi-AZ deployments== and hence, it is not a solution to reduce read-heavy database workloads.

**Using SQS to queue up the requests** is incorrect. Although an SQS queue can effectively manage the requests, it won’t be able to entirely improve the read-throughput of the database by itself.

**References:**

[https://aws.amazon.com/rds/details/read-replicas/](https://aws.amazon.com/rds/details/read-replicas/)

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_ReadRepl.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_ReadRepl.html)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 3. Question

Category: CSAA – Design Secure Architectures

An application is hosted in an Auto Scaling group of EC2 instances and a Microsoft SQL Server on Amazon RDS. There is a requirement that all in-flight data between your web servers and RDS should be secured.

Which of the following options is the MOST suitable solution that you should implement? (Select TWO.)

- Enable the IAM DB authentication in RDS using the AWS Management Console.
- Specify the TDE option in an RDS option group that is associated with that DB instance to enable transparent data encryption (TDE).
- Configure the security groups of your EC2 instances and RDS to only allow traffic to and from port 443.
- ==Download the Amazon RDS Root CA certificate. Import the certificate to your servers and configure your application to use SSL to encrypt the connection to RDS.==
- ==Force all connections to your DB instance to use SSL by setting the `rds.force_ssl` parameter to true. Once done, reboot your DB instance.==

You can use Secure Sockets Layer (SSL) to encrypt connections between your client applications and your Amazon RDS DB instances running Microsoft SQL Server. SSL support is available in all AWS regions for all supported SQL Server editions.

When you create an SQL Server DB instance, Amazon RDS creates an SSL certificate for it. The SSL certificate includes the DB instance endpoint as the Common Name (CN) for the SSL certificate to guard against spoofing attacks.

There are 2 ways to use SSL to connect to your SQL Server DB instance:

– Force SSL for all connections — this happens transparently to the client, and the client doesn’t have to do any work to use SSL.

– Encrypt specific connections — this sets up an SSL connection from a specific client computer, and you must do work on the client to encrypt connections.

![](https://media.tutorialsdojo.com/public/rds_sql_ssl_cert.png)

You can force all connections to your DB instance to use SSL, or you can encrypt connections from specific client computers only. To use SSL from a specific client, you must obtain certificates for the client computer, import certificates on the client computer, and then encrypt the connections from the client computer.

If you want to force SSL, use the `rds.force_ssl` parameter. By default, the `rds.force_ssl` parameter is set to `false`. Set the `rds.force_ssl` parameter to `true` to force connections to use SSL. The `rds.force_ssl` parameter is static, so after you change the value, you must reboot your DB instance for the change to take effect.

Hence, the correct answers for this scenario are the options that say:

**– Force all connections to your DB instance to use SSL by setting the `rds.force_ssl` parameter to true. Once done, reboot your DB instance.**

**– Download the Amazon RDS Root CA certificate. Import the certificate to your servers and configure your application to use SSL to encrypt the connection to RDS.**

**Specifying the TDE option in an RDS option group that is associated with that DB instance to enable transparent data encryption (TDE)** is incorrect because ==transparent data encryption (TDE) is primarily used to encrypt stored data on your DB instances running Microsoft SQL Server and not the data that is in transit.==

**Enabling the IAM DB authentication in RDS using the AWS Management Console** is incorrect because ==IAM database authentication is only supported in MySQL and PostgreSQL database engines==. With IAM database authentication, you don’t need to use a password when you connect to a DB instance but instead, you use an authentication token.

**Configuring the security groups of your EC2 instances and RDS to only allow traffic to and from port 443** is incorrect because it is not enough to do this. You need to either force all connections to your DB instance to use SSL, or you can encrypt connections from specific client computers, just as mentioned above.

**References:**

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/SQLServer.Concepts.General.SSL.Using.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/SQLServer.Concepts.General.SSL.Using.html)

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Appendix.SQLServer.Options.TDE.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Appendix.SQLServer.Options.TDE.html)

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.IAMDBAuth.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.IAMDBAuth.html)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 6. Question

Category: CSAA – Design Secure Architectures

An online events registration system is hosted in AWS and uses ECS to host its front-end tier and an RDS configured with Multi-AZ for its database tier. What are the events that will make Amazon RDS automatically perform a failover to the standby replica? (Select TWO.)

- Storage failure on secondary DB instance
- Compute unit failure on secondary DB instance
- ==Storage failure on primary==
- ==Loss of availability in primary Availability Zone==
- In the event of Read Replica failure

**Amazon RDS** provides high availability and failover support for DB instances using Multi-AZ deployments. Amazon RDS uses several different technologies to provide failover support. Multi-AZ deployments for Oracle, PostgreSQL, MySQL, and MariaDB DB instances use Amazon’s failover technology. SQL Server DB instances use SQL Server Database Mirroring (DBM).

In a Multi-AZ deployment, Amazon RDS automatically provisions and maintains a synchronous standby replica in a different Availability Zone. The primary DB instance is synchronously replicated across Availability Zones to a standby replica to provide data redundancy, eliminate I/O freezes, and minimize latency spikes during system backups. Running a DB instance with high availability can enhance availability during planned system maintenance and help protect your databases against DB instance failure and Availability Zone disruption.

Amazon RDS detects and automatically recovers from the most common failure scenarios for Multi-AZ deployments so that you can resume database operations as quickly as possible without administrative intervention.

![](https://media.tutorialsdojo.com/con-multi-AZ.png)

The high-availability feature is not a scaling solution for read-only scenarios; you cannot use a standby replica to serve read traffic. To service read-only traffic, you should use a Read Replica.

Amazon RDS automatically performs a failover in the event of any of the following:

1. Loss of availability in primary Availability Zone.
2. Loss of network connectivity to primary.
3. Compute unit failure on primary.
4. Storage failure on primary.

Hence, the correct answers are:

**– Loss of availability in primary Availability Zone**  
**– Storage failure on primary**

The following options are incorrect because all these scenarios do not affect the primary database. Automatic failover only occurs if the primary database is the one that is affected.

**– Storage failure on secondary DB instance**

**– In the event of Read Replica failure**

**– Compute unit failure on secondary DB instance**

**References:**

[https://aws.amazon.com/rds/details/multi-az/](https://aws.amazon.com/rds/details/multi-az/)

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.MultiAZ.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.MultiAZ.html)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)

##### 8. Question

Category: CSAA – Design Resilient Architectures

A Forex trading platform, which frequently processes and stores global financial data every minute, is hosted in an on-premises data center and uses an Oracle database. Due to a recent cooling problem in its data center, the company urgently needs to migrate its infrastructure to AWS to improve the performance of its applications. As the Solutions Architect, the responsibility is to ensure that the database is properly migrated and remains available in case of database server failure in the future, following AWS Prescriptive Guidance for database migration and high availability.

Which combination of actions would meet the requirement? (Select TWO.)

- Convert the database schema using the AWS Schema Conversion Tool.
- Launch an Oracle database instance in Amazon RDS with Recovery Manager (RMAN) enabled.
- ==Create an Oracle database in Amazon RDS with Multi-AZ deployments.==
- ==Migrate the Oracle database to AWS using the AWS Database Migration Service==
- Migrate the Oracle database to a non-cluster Amazon Aurora with a single instance.

Incorrect

Amazon RDS Multi-AZ deployments provide enhanced availability and durability for Database (DB) Instances, making them a natural fit for production database workloads. When you provision a Multi-AZ DB Instance, Amazon RDS automatically creates a primary DB Instance and synchronously replicates the data to a standby instance in a different Availability Zone (AZ). Each AZ runs on its own physically distinct, independent infrastructure, and is engineered to be highly reliable.

![Amazon RDS Multi-AZ deployments](https://media.tutorialsdojo.com/con-multi-AZ.png)

In case of an infrastructure failure, Amazon RDS performs an automatic failover to the standby (or to a read replica in the case of Amazon Aurora) so that you can resume database operations as soon as the failover is complete. Since the endpoint for your DB Instance remains the same after a failover, your application can resume database operation without the need for manual administrative intervention.

In this scenario, the best RDS configuration to use is an Oracle database in RDS with Multi-AZ deployments to ensure high availability even if the primary database instance goes down. You can use AWS DMS to move the on-premises database to AWS with minimal downtime and zero data loss. It supports over 20 engines, including Oracle to Aurora MySQL, MySQL to RDS for MySQL, SQL Server to Aurora PostgreSQL, MongoDB to DocumentDB, Oracle to Redshift, and S3.

Hence, the correct answers are:

**– Create an Oracle database in Amazon RDS with Multi-AZ deployments.**

– **Migrate the Oracle database to AWS using the AWS Database Migration Service.**

The option that says: **Launch an Oracle database instance in Amazon RDS with Recovery Manager (RMAN) enabled** is incorrect because Oracle RMAN is not supported in RDS.

The option that says: **Convert the database schema using the AWS Schema Conversion Tool** is incorrect. ==AWS Schema Conversion Tool is typically used for heterogeneous migrations where you’re moving from one type of database to another== (e.g., Oracle to PostgreSQL). In the scenario, the migration is homogenous, meaning it’s an Oracle-to-Oracle migration. As a result, there’s no need to convert the schema since you’re staying within the same database type.

The option that says: **Migrate the Oracle database to a non-cluster Amazon Aurora with a single instance** is incorrect. While a single-instance Aurora can be a feasible solution for non-critical applications or environments like development or testing, it is typically not suitable for applications that demand high availability.

**References:**

[https://aws.amazon.com/rds/details/multi-az/](https://aws.amazon.com/rds/details/multi-az/)

[https://aws.amazon.com/dms/](https://aws.amazon.com/dms/)

[https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.MultiAZ.html](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.MultiAZ.html)

**Check out this Amazon RDS Cheat Sheet:**

[https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/](https://tutorialsdojo.com/amazon-relational-database-service-amazon-rds/)