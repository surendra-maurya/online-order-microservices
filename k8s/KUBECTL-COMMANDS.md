# Kubernetes command reference

Run these commands from the solution root. The `-k k8s` form applies the whole configuration in dependency-safe Kubernetes reconciliation; it does not replace Docker Compose.

```powershell
kubectl apply -k k8s
kubectl get pods,svc,pvc,ingress -n online-order
kubectl get pods -n online-order -w
kubectl describe pod <pod-name> -n online-order
kubectl logs deployment/orderservice -n online-order --tail=100
kubectl logs deployment/inventoryservice -n online-order -f
kubectl logs deployment/rabbitmq -n online-order
kubectl port-forward service/rabbitmq 15672:15672 -n online-order
kubectl port-forward service/apigateway 5000:80 -n online-order
kubectl port-forward service/sqlserver 1433:1433 -n online-order
kubectl exec -it deployment/rabbitmq -n online-order -- rabbitmqctl status
kubectl get events -n online-order --sort-by=.lastTimestamp
kubectl rollout status deployment/productservice -n online-order
kubectl rollout restart deployment/productservice -n online-order
kubectl delete -k k8s
```

Useful first checks: `kubectl get pods -n online-order`, then `kubectl describe pod` for scheduling, image, probe, or Secret errors, and `kubectl logs` for application startup errors. A `Pending` PVC normally means the Kubernetes cluster has no default storage class.
