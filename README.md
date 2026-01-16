Online Order Microservices System - .NET 8

This project demonstrates a production-grade microservices architecture built using ASP.NET Core (.NET 8) with modern enterprise patterns such as API Gateway, JWT Authentication, Event-driven communication, and fault tolerance.

--------------------------------------------------

Key Features

- Clean Architecture per microservice
- API Gateway using Ocelot
- Centralized JWT Authentication
- Secure inter-service communication
- SQL Server per service (Docker)
- RabbitMQ event-driven messaging
- Polly retry and circuit breaker
- Startup resiliency for RabbitMQ consumers
- Docker Compose orchestration
- Persistent SQL data using Docker volumes

--------------------------------------------------

System Architecture

Services:
- ApiGateway        : Single entry point for clients
- AuthService       : Issues JWT tokens
- ProductService    : Manages products
- OrderService      : Places orders and validates product availability
- InventoryService  : Consumes order events and updates stock
- SQL Server        : Database per service
- RabbitMQ          : Asynchronous messaging

Flow:
Client
  -> ApiGateway
      -> AuthService (Login / Token)
      -> ProductService
      -> OrderService
          -> ProductService (JWT propagated)
          -> RabbitMQ
              -> InventoryService

--------------------------------------------------

Service Ports

- API Gateway     : http://localhost:5000
- ProductService  : http://localhost:5001/swagger
- OrderService    : http://localhost:5002/swagger
- InventoryService: http://localhost:5003/swagger
- AuthService     : http://localhost:5004/swagger
- RabbitMQ UI     : http://localhost:15672

RabbitMQ credentials:
- Username: guest
- Password: guest

--------------------------------------------------

Tech Stack

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core
- SQL Server 2022 (Docker)
- RabbitMQ
- Ocelot API Gateway
- JWT Authentication
- Polly (Retry and Circuit Breaker)
- Docker and Docker Compose
- Clean Architecture

--------------------------------------------------

Running the Application with Docker

Prerequisites:
- Docker Desktop
- .NET SDK 8
- SQL Server Management Studio (optional)

Step 1: Start the system

From the solution root folder:

- docker compose up --build

All services will start automatically with proper dependency handling.

--------------------------------------------------

Authentication Flow (JWT)

Step 1: Generate JWT token

POST http://localhost:5004/api/auth/login

Response:
{
  "token": "<jwt-token>"
}

Step 2: Call APIs via Gateway

Add HTTP header:

Authorization: Bearer <jwt-token>

Example:
- POST http://localhost:5000/products
- POST http://localhost:5000/orders

Without token:
- Request will return 401 Unauthorized

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

End-to-End Test Flow

1. Generate JWT token using AuthService
2. Create product via ApiGateway
3. Create order via ApiGateway
4. OrderService validates product availability
5. OrderService publishes OrderCreated event
6. InventoryService consumes event and updates stock

--------------------------------------------------

Fault Tolerance and Resiliency

- HTTP calls protected with Polly retry and circuit breaker
- RabbitMQ consumers retry connection on startup
- Manual ACK/NACK for message processing
- Docker restart policy enabled for consumers
- Fully containerized and production-ready setup

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
Microservices, .NET, Docker, AWS, Clean Architecture
