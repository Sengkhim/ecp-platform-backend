#!/bin/bash
echo "=========================================="
echo "Rebuild image for OrbStack Kubernetes"
echo "=========================================="

# Build from parent folder so Contracts project is accessible
docker build -t ecp-notification:dev .
#docker build -t ecp-warehouse:dev .

if [ $? -ne 0 ]; then
  echo "❌ Docker build failed — aborting"
  exit 1
fi

echo ""
echo "Verifying image is built:"
docker images | grep ecp-notification

echo ""
echo "=========================================="
echo "Applying Kubernetes deployment"
echo "=========================================="

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
echo "  kubectl logs -f -n ecp-dev -l app=ecp-notification"
echo ""