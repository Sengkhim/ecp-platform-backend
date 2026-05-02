#!/bin/bash

set -e


echo ""
echo "=========================================="
echo "   1. Deploy Infrastructure"
echo "=========================================="

# Ensure the namespace exists first if you use one
# kubectl apply -f namespace.yaml 

kubectl apply -f 1_zookeeper.yaml
kubectl apply -f 2_kafka.yaml
kubectl apply -f 3_mongo_db.yaml
kubectl apply -f 4_postgres_db.yaml
kubectl apply -f 5_redis.yaml

echo ""
echo "=========================================="
echo "   2. Deploy Services to Kubernetes"
echo "=========================================="


echo "=========================================="
echo "   Building & Deploying ECP Services"
echo "=========================================="

# If using Minikube, uncomment the line below to build directly into the cluster
# eval $(minikube docker-env)

SERVICES=(
  "ECP.Warehouse"
  "ECP.ProductService"
  "ECP.NotificationService"
  "ECP.OrderService"
  "ECP.PaymentService"
  "ECP.Saga.Orchestrator"
  "ECP.ApiGateway"
)

ROOT_DIR=$(pwd)

for SERVICE in "${SERVICES[@]}"
do
  echo "------------------------------------------"
  echo " Processing: $SERVICE"
  
  if [ ! -d "$SERVICE" ]; then
    echo "❌ Folder not found: $SERVICE"
    continue
  fi

  # 1. MATCH YAML NAMES: Remove "ECP." prefix and convert to lowercase
  # Example: ECP.Warehouse -> ecp-warehouse
  IMAGE_NAME=$(echo "$SERVICE" | sed 's/ECP\.//' | tr '[:upper:]' '[:lower:]')

  echo "🐳 Building image: $IMAGE_NAME:dev"
  
  # Build from root context to satisfy project references (Contracts)
  docker build -t "$IMAGE_NAME:dev" -f "$SERVICE/Dockerfile" .

  # Note: We skip 'docker run' here because you are deploying to Kubernetes instead.
done


# Applying deployments
for SERVICE in "${SERVICES[@]}"
do
  if [ -f "$SERVICE/deployment.yaml" ]; then
    echo "🚀 Deploying $SERVICE..."
    kubectl apply -f "$SERVICE/deployment.yaml"
  else
    echo "⚠️  Skip: $SERVICE/deployment.yaml not found"
  fi
done

echo ""
echo "=========================================="
echo "   DEPLOYMENT FINISHED"
echo "   Run 'kubectl get pods' to check status"
echo "=========================================="