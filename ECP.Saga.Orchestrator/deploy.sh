#!/bin/bash
echo "=========================================="
echo "Rebuild image for OrbStack Kubernetes"
echo "=========================================="

docker build -t ecp-saga-orchestrator:dev -f Dockerfile ..

if [ $? -ne 0 ]; then
  echo "❌ Docker build failed — aborting"
  exit 1
fi

echo ""
echo "Verifying image is built:"
docker images | grep ecp-saga-orchestrator

kubectl apply -f deployment.yaml

echo ""
echo "Pods:"
kubectl get pods -n ecp-dev

echo ""
echo "To watch pod status in real-time:"
echo "  kubectl get pods -n ecp-dev -w"
echo ""
echo "To view logs:"
echo "  kubectl logs -f -n ecp-dev -l app=ecp-saga-orchestrator"