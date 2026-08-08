# Kubernetes manifest guide

All resources use the `online-order` namespace and the `app.kubernetes.io/name` label. A Service selects Pods with the same name label; changing either side breaks traffic.

| File | Why it exists and how Kubernetes uses it | Common mistake |
| --- | --- | --- |
| `namespace.yaml` | Creates the isolated `online-order` namespace. Every namespaced manifest explicitly targets it. | Applying a Service to `default` while its Pods are in `online-order`. |
| `configmap.yaml` | Stores non-secret shared configuration and RabbitMQ's learning-cluster configuration. Pods read values through environment variables or the mounted RabbitMQ config file. | Put passwords here; use `secrets.yaml` instead. |
| `secrets.yaml` | Stores SQL, JWT, and RabbitMQ credentials. `stringData` lets Kubernetes encode them when applied. | Treating a Secret as encrypted source control; rotate it and use an external secret manager in production. |
| `sqlserver/pvc.yaml` | Requests 10 GiB of durable storage. The SQL Pod mounts it at SQL Server's data directory. | Using `ReadWriteMany` or multiple SQL replicas with this single-disk configuration. |
| `sqlserver/deployment.yaml` | Runs one SQL Server Pod, injects the SA password, mounts the PVC, and waits for TCP 1433 before declaring it ready. `Recreate` avoids two Pods trying to mount one disk. | Changing `MSSQL_SA_PASSWORD` after data has already initialized without updating SQL Server itself. |
| `sqlserver/service.yaml` | Creates the internal DNS name `sqlserver` on port 1433 used by the three connection strings. | Pointing an in-cluster app to `localhost`; localhost is the app's own Pod. |
| `rabbitmq/pvc.yaml` | Persists queues, definitions, and broker data across Pod recreation. | Deleting the PVC when queued messages must be retained. |
| `rabbitmq/deployment.yaml` | Runs RabbitMQ with AMQP and management ports, Secret-backed credentials, a persisted data directory, and broker diagnostic probes. | The existing application uses `guest/guest`; the ConfigMap deliberately permits it remotely only for this local learning setup. Create a dedicated application user for production. |
| `rabbitmq/service.yaml` | Exposes `rabbitmq:5672` to Pods and `rabbitmq:15672` for management. It is internal (`ClusterIP`). | Exposing the management UI publicly without authentication. |
| `seq/deployment.yaml` and `seq/service.yaml` | Preserve the Compose logging dependency at the existing DNS name `seq:80`. | Assuming this setup persists Seq data; it is intentionally a non-persistent local logging aid. |
| `productservice/*` | Runs the product API and publishes it internally as `productservice:80`. Its database connection is assembled from a Secret and ConfigMap environment values. | The env var order matters: `SQL_SERVER`, `SQL_PORT`, and `SQL_SA_PASSWORD` are defined before the expanded connection string. |
| `orderservice/*` | Runs the order API and exposes `orderservice:80`; it reaches `productservice:80` and `rabbitmq:5672` using the existing application configuration. | Renaming this Service without updating Ocelot and the order HTTP client. |
| `inventoryservice/*` | Runs the inventory consumer/API and exposes `inventoryservice:80`; its readiness probe checks its existing database and RabbitMQ health check. | Scaling this consumer without deliberately choosing queue-consumer semantics. |
| `authservice/*` | Runs the token API as `authservice:80` and injects the JWT signing key. | It currently has no HTTP health route, so the safe probe is TCP; add `/health` in application code before a production rollout. |
| `apigateway/*` | Runs Ocelot at `apigateway:80`; Ocelot's existing downstream names already match `productservice` and `orderservice`. | Docker Compose maps this image to host port 5000 differently, but inside Kubernetes the container listens on port 80. |
| `ingress.yaml` | Routes `online-order.local/api/auth` to AuthService and every other path to the gateway. It requires an installed nginx Ingress controller. | Applying it without an ingress controller or without mapping `online-order.local` to the controller address. |
| `kustomization.yaml` | Gives one ordered entry point for all files: `kubectl apply -k k8s`. | Applying a single app Deployment before its Namespace, Secret, or ConfigMap exists. |

## Probe vocabulary

- A **startup probe** gives slow-starting software time to initialize before liveness checks can restart it.
- A **readiness probe** decides whether a Service may send the Pod traffic.
- A **liveness probe** restarts a process that is alive at the container level but no longer functioning.

The three data-aware API health endpoints are deliberately used for product, order, and inventory readiness. Gateway and AuthService do not expose `/health` today, so their probes use TCP port 80 rather than probing a non-existent URL.
