#!/bin/bash

set -e

echo "=========================================="
echo "   Building & Running ALL ECP Services"
echo "=========================================="

SERVICES=(
  "ECP.ApiGateway"
  "ECP.NotificationService"
  "ECP.OrderService"
  "ECP.PaymentService"
#  "ECP.ProductService"
  "ECP.Saga.Orchestrator"
  "ECP.Warehouse"
)

# Stay in the root directory!
ROOT_DIR=$(pwd)

for SERVICE in "${SERVICES[@]}"
do
  echo ""
  echo "=========================================="
  echo " Processing: $SERVICE"
  echo "=========================================="

  if [ ! -d "$SERVICE" ]; then
    echo "❌ Folder not found: $SERVICE"
    exit 1
  fi

  # Convert service name to lowercase for the image tag
  IMAGE_NAME=$(echo "$SERVICE" | tr '[:upper:]' '[:lower:]')

  echo "🐳 Building image: $IMAGE_NAME"
  
  # FIX: Set context to '.' (root) and point to the Dockerfile inside the service folder
  docker build -t "$IMAGE_NAME:dev" -f "$SERVICE/Dockerfile" .

  echo "🚀 Running container: $IMAGE_NAME"

  docker rm -f "$IMAGE_NAME" 2>/dev/null || true

  docker run -d \
    --name "$IMAGE_NAME" \
    "$IMAGE_NAME:latest"
done

echo ""
echo "=========================================="
echo "   ALL SERVICES STARTED SUCCESSFULLY"
echo "=========================================="