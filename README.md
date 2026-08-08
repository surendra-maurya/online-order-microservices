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
Health Checks

Each service exposes health endpoints to monitor dependencies:

- Database health
- RabbitMQ connectivity
- Application liveness

Example:
- GET http://localhost:5001/health
- GET http://localhost:5002/health
- GET http://localhost:5003/health

Healthy response:

{
  "status": "Healthy"
}

--------------------------------------------------

Stopping the System

To stop containers:

- docker compose down

To stop containers and delete all data:

- docker compose down -v

--------------------------------------------------

# Running with Kubernetes

The Kubernetes configuration is additive: the Docker Compose workflow above remains unchanged. Kubernetes uses the same Dockerfiles, but replaces Compose networking with Kubernetes Services. Inside the cluster, services call DNS names such as `productservice`, `sqlserver`, and `rabbitmq` instead of `localhost`.

## Prerequisites

- Docker Desktop with Kubernetes enabled (Docker Desktop Settings -> Kubernetes -> **Enable Kubernetes**, then wait until it is running).
- `kubectl` configured for the Docker Desktop cluster: `kubectl config current-context` should show `docker-desktop`.
- .NET SDK 8 if you need to apply EF Core migrations to a new database.
- An nginx Ingress controller only if you want to use the optional Ingress hostname. Docker Desktop may require enabling or installing one separately.

Check the cluster before continuing:

```powershell
kubectl cluster-info
kubectl get nodes
```

## Build the local images

Docker Desktop Kubernetes can use images built in the same Docker Desktop engine. Build each existing Dockerfile from the solution root:

```powershell
docker build -t online-order-apigateway:local -f ApiGateway/Dockerfile .
docker build -t online-order-authservice:local -f AuthService.API/Dockerfile .
docker build -t online-order-productservice:local -f ProductService.API/Dockerfile .
docker build -t online-order-orderservice:local -f OrderService.API/Dockerfile .
docker build -t online-order-inventoryservice:local -f InventoryService.API/Dockerfile .
```

The manifests use `imagePullPolicy: IfNotPresent`, so Kubernetes uses these local images. For a remote cluster, push the same tags to your container registry and change each `image:` value; do not rely on Docker Desktop's local image cache there.

## Deploy step by step

### Step 1: apply the manifests

Run this from the solution root. Kustomize creates the Namespace, Secret, ConfigMap, storage, dependencies, APIs, Services, and optional Ingress:

```powershell
kubectl apply -k k8s
```

### Step 2: verify Pods and storage

Wait until every application shows `1/1 Running` and every PVC is `Bound`:

```powershell
kubectl get pods,svc,pvc,ingress -n online-order
```

If a Pod is not ready, inspect it before continuing:

```powershell
kubectl describe pod <pod-name> -n online-order
kubectl logs deployment/<service-name> -n online-order --tail=100
```

### Step 3: apply EF Core migrations to a new SQL volume

The applications do not automatically change database schemas at startup. Install the EF tool once:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.23
```

Keep this SQL port-forward running in its own PowerShell window:

```powershell
kubectl port-forward service/sqlserver 1433:1433 -n online-order
```

Apply each service's existing migrations from another PowerShell window. `127.0.0.1` is intentional because it avoids Windows `localhost` IPv6 resolution issues:

```powershell
dotnet ef database update --project ProductService.Infrastructure --startup-project ProductService.API --connection "Server=127.0.0.1,1433;Database=ProductDB;User Id=sa;Password=Surendra@123;TrustServerCertificate=True"
dotnet ef database update --project OrderService.Infrastructure --startup-project OrderService.API --connection "Server=127.0.0.1,1433;Database=OrderDB;User Id=sa;Password=Surendra@123;TrustServerCertificate=True"
dotnet ef database update --project InventoryService.Infrastructure --startup-project InventoryService.API --connection "Server=127.0.0.1,1433;Database=InventoryDB;User Id=sa;Password=Surendra@123;TrustServerCertificate=True"
```

The ProductService, OrderService, and InventoryService startup projects include the EF Design package required by these commands.

## Accessing APIs from Postman

The API Services are `ClusterIP` Services. That means they are intentionally private to Kubernetes; `http://localhost:5000` will not work from Postman unless you either port-forward or install/configure an Ingress controller. Port-forwarding is the simplest local method and does not change the cluster security model.

### Step 4: expose the Gateway

Run this in a dedicated PowerShell window and leave it running:

```powershell
kubectl port-forward service/apigateway 5000:80 -n online-order
```

In Postman, request a token first:

- Method: `POST`
- URL: `http://localhost:5004/api/auth/login` (after forwarding AuthService below)

Then call the Gateway:

- Product create: `POST http://localhost:5000/products`
- Order create: `POST http://localhost:5000/orders`
- Header: `Authorization: Bearer <token>`
- Header: `ClientId: local-user` (required by Ocelot rate limiting)
- Header: `Content-Type: application/json`

Product body example:

```json
{
  "name": "Keyboard",
  "price": 49.99,
  "stock": 10
}
```

Order body example:

```json
{
  "productId": 1,
  "quantity": 2
}
```

### Step 5: expose AuthService directly

Run this in another PowerShell window:

```powershell
kubectl port-forward service/authservice 5004:80 -n online-order
```

This makes `POST http://localhost:5004/api/auth/login` available to Postman. Keep the window open while testing.

### Step 6: access individual APIs

Use a separate port-forward window for each API you want to call directly:

```powershell
kubectl port-forward service/productservice 5001:80 -n online-order
kubectl port-forward service/orderservice 5002:80 -n online-order
kubectl port-forward service/inventoryservice 5003:80 -n online-order
```

Direct URLs:

- ProductService Swagger: `http://localhost:5001/swagger`
- ProductService health: `http://localhost:5001/health`
- OrderService Swagger: `http://localhost:5002/swagger`
- OrderService health: `http://localhost:5002/health`
- InventoryService health: `http://localhost:5003/health`

### Can Postman connect without port-forwarding?

Not to the current `ClusterIP` Services directly. For host access without port-forwarding, install an nginx Ingress controller and map `online-order.local` to its address, or deliberately change a Service to `NodePort`. Port-forwarding is recommended for local learning because it is temporary and requires no public exposure.

## RabbitMQ Management UI

### Step 7: expose RabbitMQ Management

Run:

```powershell
kubectl port-forward service/rabbitmq 15672:15672 -n online-order
```

Open `http://localhost:15672` in a browser and log in with:

```text
Username: guest
Password: guest
```

This `guest` setup is for the local learning cluster only.

### Step 8: verify an order in RabbitMQ

Open **Queues and Streams** and select `order-created`. The application consumes messages quickly, so `Messages` may be `0` even after a successful order. A healthy queue normally shows one consumer.

You can also check from PowerShell:

```powershell
kubectl exec deployment/rabbitmq -n online-order -- rabbitmqctl list_queues name messages consumers
```

To verify the complete flow, create an order through the Gateway, confirm `order-created` briefly receives it, then inspect InventoryDB or InventoryService logs. An empty queue after the order is normally evidence that InventoryService consumed it.

## SQL Server Management Studio (SSMS)

### Step 9: expose Kubernetes SQL Server

Keep this command running in its own PowerShell window:

```powershell
kubectl port-forward service/sqlserver 1433:1433 -n online-order
```

Verify the port before opening SSMS:

```powershell
Test-NetConnection 127.0.0.1 -Port 1433
```

The result must say `TcpTestSucceeded : True`.

### Step 10: connect in SSMS

Use these settings:

- Server name: `127.0.0.1,1433`
- Authentication: `SQL Server Authentication`
- Login: `sa`
- Password: `Surendra@123`
- Encryption: `Mandatory` or `Optional`
- **Trust server certificate**: checked

Use `127.0.0.1,1433` instead of `localhost,1433` because Windows may resolve `localhost` to IPv6 while the port-forward is using IPv4.

If the port-forward reports that port 1433 is already in use by your Windows SQL Server, use another local port:

```powershell
kubectl port-forward service/sqlserver 11433:1433 -n online-order
```

Then connect in SSMS to `127.0.0.1,11433`.

The databases should be visible under the server:

- `ProductDB`
- `OrderDB`
- `InventoryDB`

If the password is rejected, confirm that you are connecting through the Kubernetes port-forward and not to your separate Windows SQL Server instance named `SURENDRA`. The Kubernetes SQL Server password is initialized when its PVC is first created; changing the Kubernetes Secret later does not change an already-initialized SQL Server password.

## Troubleshooting

- `ImagePullBackOff`: build the local image with the exact tag above, or update `image:` for your registry.
- `Pending` PVC: inspect `kubectl get storageclass`; your cluster needs a default storage class.
- `CrashLoopBackOff`: use `kubectl logs deployment/<service> -n online-order --tail=100` and `kubectl describe pod <pod> -n online-order`.
- `0/1 Ready`: check SQL/RabbitMQ first, then verify the EF migrations were applied.
- Postman `502`: Gateway cannot find a ready downstream Service; inspect `kubectl get endpoints -n online-order`.
- Postman `405`: the route exists, but the HTTP method is not implemented by that controller.
- Postman rate-limit error: add `ClientId: local-user`.
- SSMS connection refused: keep the SQL port-forward running and test `127.0.0.1` with `Test-NetConnection`.
- Ingress has no address: install/enable an nginx Ingress controller, or use port-forwarding.

## Delete Kubernetes resources

### Step 11: remove the Kubernetes application

```powershell
kubectl delete -k k8s
```

Deleting the manifests also deletes the PVC objects; depending on the StorageClass reclaim policy, the underlying disk may be deleted. Back up SQL data before cleanup.

Stop each running `kubectl port-forward` window with `Ctrl+C` after testing.

## Delete Kubernetes resources

```powershell
kubectl delete -k k8s
```

Deleting the manifests also deletes the PVC objects; depending on the StorageClass reclaim policy, the underlying disk may be deleted. Back up SQL data before cleanup if you need it.

For a concise command reference, see [k8s/KUBECTL-COMMANDS.md](k8s/KUBECTL-COMMANDS.md). For a file-by-file explanation, see [k8s/README.md](k8s/README.md).

--------------------------------------------------

Author

Surendra Maurya | 
Senior .NET Full Stack Developer | 
Microservices, .NET, Docker, AWS, Clean Architecture
