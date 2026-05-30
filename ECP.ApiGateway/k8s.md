🚀 ECP Production Setup
Kubernetes + .NET 8 YARP + Cloudflare Tunnel
OrbStack Ubuntu VM → ecpws-uat.notcode.uk
📋 Architecture
Internet
   ↓
Cloudflare Edge
   ↓
Cloudflare Tunnel
   ↓
cloudflared Pod
   ↓
YARP API Gateway (ClusterIP)
   ↓
Internal Microservices
✅ Benefits
No public inbound ports
No MetalLB required
No NGINX ingress required
Cloudflare SSL + WAF + DDoS protection
Kubernetes internal-only networking
Production-grade secure architecture
Zero downtime rolling deployments
Auto-scaling support
📦 Prerequisites

Inside Ubuntu VM:

kubectl version --client
docker --version

Install Helm:

curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
⚠️ Ubuntu Questing Fix (OrbStack)

Ubuntu questing may have IPv6 issues.

Run:

sudo nano /etc/gai.conf

Find:

#precedence ::ffff:0:0/96  100

Uncomment:

precedence ::ffff:0:0/96  100

Save file.

☁️ Install cloudflared
Add Cloudflare Repository
curl -fsSL https://pkg.cloudflare.com/cloudflare-main.gpg | \
sudo tee /usr/share/keyrings/cloudflare-main.gpg >/dev/null
echo "deb [signed-by=/usr/share/keyrings/cloudflare-main.gpg] \
https://pkg.cloudflare.com/cloudflared noble main" | \
sudo tee /etc/apt/sources.list.d/cloudflared.list
Install
sudo apt update
sudo apt install cloudflared -y

Verify:

cloudflared --version
🔐 Login to Cloudflare
cloudflared tunnel login

Browser opens.

Authorize:

notcode.uk
🌐 Create Tunnel
cloudflared tunnel create ecpws-uat

Save:

Tunnel ID

Example:

a1b2c3d4-e5f6-7890-abcd-123456789abc
🌍 Route DNS
cloudflared tunnel route dns ecpws-uat ecpws-uat.notcode.uk

Verify inside Cloudflare DNS:

CNAME
ecpws-uat
→
<TUNNEL_ID>.cfargotunnel.com

Proxy status:

Enabled ✅
📁 Project Structure
k8s/
├── namespace.yaml
├── secret.yaml
├── api-gateway.yaml
├── cloudflared-config.yaml
├── cloudflared-deployment.yaml
└── hpa.yaml
📂 namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: ecp-prod

Apply:

kubectl apply -f namespace.yaml
🔑 Generate JWT Secret
openssl rand -base64 64
📂 secret.yaml

Replace YOUR_SECRET.

apiVersion: v1
kind: Secret
metadata:
  name: api-gateway-secret
  namespace: ecp-prod
type: Opaque
stringData:
  JWT_SECRET: "YOUR_SECRET"

Apply:

kubectl apply -f secret.yaml
📂 api-gateway.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: ecp-prod

spec:
  replicas: 3

  revisionHistoryLimit: 5

  selector:
    matchLabels:
      app: api-gateway

  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 0
      maxSurge: 1

  template:
    metadata:
      labels:
        app: api-gateway

    spec:
      terminationGracePeriodSeconds: 30

      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                topologyKey: kubernetes.io/hostname
                labelSelector:
                  matchLabels:
                    app: api-gateway

      containers:
        - name: api-gateway

          image: devkhim/ecp.apigateway:1.0.0

          imagePullPolicy: Always

          ports:
            - containerPort: 8080

          env:
            - name: ASPNETCORE_URLS
              value: "http://+:8080"

            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"

            - name: JWT_SECRET
              valueFrom:
                secretKeyRef:
                  name: api-gateway-secret
                  key: JWT_SECRET

          resources:
            requests:
              cpu: "100m"
              memory: "128Mi"

            limits:
              cpu: "500m"
              memory: "512Mi"

          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10

          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 20
            periodSeconds: 20

          startupProbe:
            httpGet:
              path: /health
              port: 8080
            failureThreshold: 30
            periodSeconds: 5

          securityContext:
            runAsNonRoot: true
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop:
                - ALL

          volumeMounts:
            - name: tmp
              mountPath: /tmp

      volumes:
        - name: tmp
          emptyDir: {}

---

apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: ecp-prod

spec:
  type: ClusterIP

  selector:
    app: api-gateway

  ports:
    - port: 80
      targetPort: 8080

Apply:

kubectl apply -f api-gateway.yaml
📂 cloudflared-config.yaml

Replace:

YOUR_TUNNEL_ID
apiVersion: v1
kind: ConfigMap
metadata:
  name: cloudflared-config
  namespace: ecp-prod

data:
  config.yml: |
    tunnel: YOUR_TUNNEL_ID

    credentials-file: /etc/cloudflared/credentials.json

    ingress:
      - hostname: ecpws-uat.notcode.uk
        service: http://api-gateway:80

      - service: http_status:404

Apply:

kubectl apply -f cloudflared-config.yaml
🔐 Create Tunnel Credentials Secret

Replace:

YOUR_TUNNEL_ID
kubectl create secret generic cloudflared-credentials \
  --from-file=credentials.json=$HOME/.cloudflared/YOUR_TUNNEL_ID.json \
  -n ecp-prod
📂 cloudflared-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cloudflared
  namespace: ecp-prod

spec:
  replicas: 2

  selector:
    matchLabels:
      app: cloudflared

  template:
    metadata:
      labels:
        app: cloudflared

    spec:
      containers:
        - name: cloudflared

          image: cloudflare/cloudflared:2026.5.0

          args:
            - tunnel
            - --config
            - /etc/cloudflared/config.yml
            - run

          resources:
            requests:
              cpu: "50m"
              memory: "64Mi"

            limits:
              cpu: "200m"
              memory: "256Mi"

          securityContext:
            runAsNonRoot: true
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop:
                - ALL

          volumeMounts:
            - name: config
              mountPath: /etc/cloudflared/config.yml
              subPath: config.yml

            - name: creds
              mountPath: /etc/cloudflared/credentials.json
              subPath: credentials.json

      volumes:
        - name: config
          configMap:
            name: cloudflared-config

        - name: creds
          secret:
            secretName: cloudflared-credentials

Apply:

kubectl apply -f cloudflared-deployment.yaml
📂 hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler

metadata:
  name: api-gateway-hpa
  namespace: ecp-prod

spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: api-gateway

  minReplicas: 3
  maxReplicas: 10

  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70

Apply:

kubectl apply -f hpa.yaml
📊 Install Metrics Server

Required for HPA.

kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

For OrbStack:

kubectl patch deployment metrics-server -n kube-system \
--type='json' \
-p='[
  {
    "op":"add",
    "path":"/spec/template/spec/containers/0/args/-",
    "value":"--kubelet-insecure-tls"
  }
]'

Verify:

kubectl top pods -A
🩺 Verify Deployment
Pods
kubectl get pods -n ecp-prod

Expected:

api-gateway-xxx      Running
cloudflared-xxx      Running
📜 View Logs
Gateway Logs
kubectl logs -l app=api-gateway -n ecp-prod -f
Tunnel Logs
kubectl logs -l app=cloudflared -n ecp-prod -f

Look for:

Connection registered
🔍 Internal Health Check
kubectl run curl-test \
  --image=curlimages/curl \
  -n ecp-prod \
  --rm -it --restart=Never -- \
  curl http://api-gateway/health
🌐 Public Health Check
curl https://ecpws-uat.notcode.uk/health
🔒 Recommended Cloudflare Settings

Cloudflare Dashboard:

Setting	Value
SSL Mode	Full (Strict)
Always HTTPS	ON
WAF	ON
Bot Fight Mode	ON
HTTP/3	ON
Minimum TLS	1.2
🚀 Deploy New Version
kubectl set image deployment/api-gateway \
api-gateway=devkhim/ecp.apigateway:1.0.1 \
-n ecp-prod

Watch rollout:

kubectl rollout status deployment/api-gateway -n ecp-prod
⏪ Rollback
kubectl rollout undo deployment/api-gateway -n ecp-prod
📈 Scale Manually
kubectl scale deployment/api-gateway \
--replicas=5 \
-n ecp-prod
📊 Resource Usage
kubectl top pods -n ecp-prod
✅ Final Production Architecture
Cloudflare Edge
   ↓
Cloudflare Tunnel
   ↓
cloudflared
   ↓
YARP API Gateway
   ↓
Internal Services

WITHOUT:

MetalLB
NGINX ingress
Public node ports
Open inbound firewall ports

Production-grade setup for:

OrbStack
Kubernetes
.NET 8
YARP
Cloudflare Tunnel





 ea58d41f-3f85-4fa5-a32e-a04c53a7a0e3
 2026-05-27T15:21:23Z INF Added CNAME ecpws-uat.notcode.uk which will route to this tunnel tunnelID=ea58d41f-3f85-4fa5-a32e-a04c53a7a0e3


sudo kubectl create secret generic cloudflared-credentials --from-file=credentials.json=$HOME/.cloudflared/ea58d41f-3f85-4fa5-a32e-a04c53a7a0e3.json -n ecp-prod