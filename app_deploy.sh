#!/bin/bash

set -e


echo ""
echo "=========================================="
echo "   1. Deploy Infrastructure"
echo "=========================================="

# Ensure the namespace exists first if you use one
# kubectl apply -f namespace.yaml 

#kubectl apply -f 1_zookeeper.yaml
#kubectl apply -f 2_kafka.yaml
#kubectl apply -f 3_mongo_db.yaml
#kubectl apply -f 4_postgres_db.yaml
#kubectl apply -f 5_redis.yaml

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
#  "ECP.ProductService"
#  "ECP.NotificationService"
#  "ECP.OrderService"
#  "ECP.PaymentService"
#  "ECP.Saga.Orchestrator"
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
#  docker build -t "$IMAGE_NAME:dev" -f "$SERVICE/Dockerfile" .

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


echo ""
echo "=========================================="
echo "   DEPLOYMENT SERVICES"
echo "=========================================="

# Deploy all services
kubectl apply -f ECP.ApiGateway/deployment.yaml
#kubectl apply -f ECP.ProductService/deployment.yaml
kubectl apply -f ECP.Warehouse/deployment.yaml
#kubectl apply -f ECP.NotificationService/deployment.yaml
#kubectl apply -f ECP.OrderService/deployment.yaml
#kubectl apply -f ECP.PaymentService/deployment.yaml
#kubectl apply -f ECP.Saga.Orchestrator/deployment.yaml

# Watch gateway discover external routes
#kubectl logs -n ecp-dev deployment/api-gateway -f

# Expected routes:
# /api/ecp-warehouse/{**catch-all} → http://ecp-warehouse.ecp-dev.svc.cluster.local
# /api/notification/{**catch-all} → http://ecp-notification.ecp-dev.svc.cluster.local
# /api/order/{**catch-all} → http://ecp-order.ecp-dev.svc.cluster.local
# /api/payment/{**catch-all} → http://ecp-payment.ecp-dev.svc.cluster.local

echo ""
echo "=========================================="
echo "   DEPLOYMENT FINISHED"
echo "=========================================="
