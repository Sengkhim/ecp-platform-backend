#!/bin/bash
echo "=========================================="
echo "Create deployment all"
echo "=========================================="

# Delete the namespace (this will delete all resources within it)
#kubectl delete namespace ecp-dev

kubectl apply -f 1_zookeeper.yaml
kubectl apply -f 2_kafka.yaml
kubectl apply -f 3_mongo_db.yaml
kubectl apply -f 4_postgres_db.yaml
kubectl apply -f 5_redis.yaml

echo ""
echo "=========================================="
echo "Completed create deployment infra"
echo "=========================================="

echo "=========================================="
echo "Start deployment all applications"
echo "=========================================="