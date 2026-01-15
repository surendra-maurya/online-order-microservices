Online Order Microservices System - .NET 8

This project demonstrates a production-grade microservices architecture built using ASP.NET Core (.NET 8) with the following features:

- Clean Architecture
- Independent microservices
- SQL Server (Docker)
- RabbitMQ messaging
- Polly fault tolerance
- Event-driven design
- Docker Compose orchestration
- Persistent SQL data using Docker volumes

--------------------------------------------------

System Architecture

Services:
- ProductService - manages products
- OrderService - places orders and communicates with ProductService
- InventoryService - listens to order events and updates inventory
- SQL Server - database per service
- RabbitMQ - asynchronous messaging

Flow:
Client -> OrderService -> ProductService
OrderService -> RabbitMQ -> InventoryService

--------------------------------------------------

Tech Stack

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core
- SQL Server 2022 (Docker)
- RabbitMQ
- Polly (Retry and Circuit Breaker)
- Docker and Docker Compose
- Clean Architecture

--------------------------------------------------

Running the Application with Docker

Prerequisites:
- Docker Desktop
- .NET SDK 8
- SQL Server Management Studio (optional for viewing database)

Step 1: Start the system

From the solution root folder:

- docker compose up --build

This will start:
- ProductService: http://localhost:5185/swagger
- OrderService: http://localhost:5141/swagger
- InventoryService: http://localhost:5150/swagger
- RabbitMQ UI: http://localhost:15672
  username: guest
  password: guest

--------------------------------------------------

SQL Server Connection

Connect using SQL Server Management Studio:

Server: localhost,1433
Authentication: SQL Server Authentication
Login: sa
Password: Surendra@123

Databases:
- ProductDB
- OrderDB
- InventoryDB

SQL data is stored persistently in Docker volume named: sql_data

--------------------------------------------------

Test Flow

1. Create a product
2. Create an order
3. InventoryService automatically updates stock using RabbitMQ events

--------------------------------------------------

Fault Tolerance

- HTTP calls protected with Polly retry and circuit breaker
- Asynchronous messaging with RabbitMQ
- Fully containerized deployment

--------------------------------------------------

Stopping the System

To stop containers:

- docker compose down

To stop containers and delete all data:

- docker compose down -v

--------------------------------------------------

Author

Surendra Maurya
Senior .NET Full Stack Developer
Microservices, AWS, Docker, Clean Architecture
