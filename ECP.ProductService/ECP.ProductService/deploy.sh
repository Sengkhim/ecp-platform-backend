#!/bin/bash
set -e

echo "🐳 Building ECP.ProductService..."
docker build -t ecp-product-service:dev -f Dockerfile .

echo "☸️  Applying Kubernetes manifests..."
kubectl apply -f k8s/deployment.yaml

echo "⏳ Waiting for rollout..."
kubectl rollout status deployment/ecp-product-service -n ecp-dev

echo "✅ Done. Pods:"
kubectl get pods -n ecp-dev -l app=ecp-product-service

echo ""
echo "GraphQL playground: http://ecp-product-service.ecp-dev.svc.cluster.local/graphql/ui"
