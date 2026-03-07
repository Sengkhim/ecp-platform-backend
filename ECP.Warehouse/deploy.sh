#!/bin/bash
echo "=========================================="
echo "Deleting ecp-dev namespace and all resources"
echo "=========================================="

# Delete the namespace (this will delete all resources within it)
#kubectl delete namespace ecp-dev

echo ""
echo "=========================================="
echo "Rebuild image for OrbStack Kubernetes"
echo "=========================================="

# OrbStack shares local Docker images directly — no image load needed!
docker build -t ecp-warehouse:dev .

echo ""
echo "Verifying image is built:"
docker images | grep ecp-warehouse

# Apply the updated deployment
kubectl apply -f deployment.yaml

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