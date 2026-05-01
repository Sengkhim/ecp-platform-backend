#!/bin/bash
set -e

echo "🐳 Building ECP.ProductService..."
docker build --no-cache -t ecp-product-service:dev -f Dockerfile .

echo "☸️  Applying Kubernetes manifests..."
kubectl apply -f deployment.yaml

echo "✅ Done. Pods:"
kubectl get pods -n ecp-dev -l app=ecp-product-service

echo ""
echo "GraphQL playground: http://ecp-product-service.ecp-dev.svc.cluster.local/graphql/ui"