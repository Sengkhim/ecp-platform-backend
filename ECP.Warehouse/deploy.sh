#!/bin/bash
echo "=========================================="
echo "Deleting ecp-dev namespace and all resources"
echo "=========================================="

# Delete the namespace (this will delete all resources within it)
#kubectl delete namespace ecp-dev

echo ""
echo "=========================================="
echo "Rebuild image and loading local Docker image to Minikube"
echo "=========================================="

docker build -t ecp-warehouse:dev .

# Load the local image into Minikube
sudo minikube image load ecp-warehouse:dev

echo ""
echo "Verifying image is loaded:"
minikube image ls | grep ecp-warehouse

# Apply the updated deployment
kubectl apply -f deployment.yaml

#echo ""
#echo "Waiting for rollout to complete..."
#kubectl rollout status deployment/order-analy-service -n ecp-dev --timeout=120s

echo ""
echo "=========================================="
echo "Current Status"
echo "=========================================="

echo ""
echo "Pods:"
kubectl get pods -n ecp-dev

echo ""
echo "To watch pod status in real-time:"
echo "  kubectl get pods -n ecp-dev -w"
echo ""
echo "To view logs:"
echo "  kubectl logs -f -n ecp-dev -l app=order-analy-service"
echo ""