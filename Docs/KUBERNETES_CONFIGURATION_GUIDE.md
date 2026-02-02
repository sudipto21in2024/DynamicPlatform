# Kubernetes Configuration Guide

## Overview

This document describes the **Kubernetes Configuration** capability in DynamicPlatform, which allows customers to customize how their microservices applications are deployed to Kubernetes clusters.

---

## 1. Configuration Levels

The platform provides **three levels** of Kubernetes configuration:

### Level 1: Quick Start (Default)
**Target Audience**: Developers new to Kubernetes  
**Effort**: 0 configuration, use platform defaults  
**Result**: Production-ready K8s manifests with sensible defaults

### Level 2: Guided Configuration (Recommended)
**Target Audience**: DevOps engineers  
**Effort**: 5-10 minutes using visual configurator  
**Result**: Customized deployment with organization-specific settings

### Level 3: Advanced (Expert)
**Target Audience**: Kubernetes experts  
**Effort**: Full control via YAML editor or API  
**Result**: Highly customized deployment with all K8s features

---

## 2. Configuration Metadata Schema

### 2.1 Kubernetes Configuration Model

```json
{
  "kubernetesConfig": {
    "enabled": true,
    "clusterTarget": "azure-aks",  // "azure-aks" | "aws-eks" | "gcp-gke" | "on-premise" | "local"
    "namespace": "ecommerce-prod",
    "global": {
      "imageRegistry": "myregistry.azurecr.io",
      "imagePullSecrets": ["acr-secret"],
      "resourceQuotas": {
        "enabled": true,
        "limits": {
          "cpu": "20",
          "memory": "40Gi",
          "pods": "50"
        }
      },
      "networkPolicies": {
        "enabled": true,
        "defaultDeny": true
      }
    },
    "services": [
      {
        "serviceName": "customer-service",
        "deployment": {
          "replicas": 3,
          "strategy": {
            "type": "RollingUpdate",
            "rollingUpdate": {
              "maxSurge": 1,
              "maxUnavailable": 0
            }
          },
          "resources": {
            "requests": {
              "cpu": "100m",
              "memory": "256Mi"
            },
            "limits": {
              "cpu": "500m",
              "memory": "512Mi"
            }
          },
          "autoscaling": {
            "enabled": true,
            "minReplicas": 2,
            "maxReplicas": 10,
            "targetCPUUtilization": 70,
            "targetMemoryUtilization": 80
          },
          "healthChecks": {
            "livenessProbe": {
              "httpGet": {
                "path": "/health/live",
                "port": 80
              },
              "initialDelaySeconds": 30,
              "periodSeconds": 10,
              "timeoutSeconds": 5,
              "failureThreshold": 3
            },
            "readinessProbe": {
              "httpGet": {
                "path": "/health/ready",
                "port": 80
              },
              "initialDelaySeconds": 10,
              "periodSeconds": 5,
              "timeoutSeconds": 3,
              "failureThreshold": 3
            }
          },
          "affinity": {
            "podAntiAffinity": {
              "preferredDuringSchedulingIgnoredDuringExecution": [
                {
                  "weight": 100,
                  "podAffinityTerm": {
                    "labelSelector": {
                      "matchExpressions": [
                        {
                          "key": "app",
                          "operator": "In",
                          "values": ["customer-service"]
                        }
                      ]
                    },
                    "topologyKey": "kubernetes.io/hostname"
                  }
                }
              ]
            }
          }
        },
        "service": {
          "type": "ClusterIP",
          "port": 80,
          "targetPort": 80,
          "sessionAffinity": "None"
        },
        "configMaps": [
          {
            "name": "customer-service-config",
            "data": {
              "appsettings.json": "{ ... }"
            }
          }
        ],
        "secrets": [
          {
            "name": "customer-service-secrets",
            "type": "Opaque",
            "data": {
              "db-password": "base64-encoded-value"
            }
          }
        ],
        "persistentVolumes": []
      }
    ],
    "ingress": {
      "enabled": true,
      "className": "nginx",
      "annotations": {
        "cert-manager.io/cluster-issuer": "letsencrypt-prod",
        "nginx.ingress.kubernetes.io/ssl-redirect": "true",
        "nginx.ingress.kubernetes.io/rate-limit": "100"
      },
      "tls": {
        "enabled": true,
        "secretName": "ecommerce-tls",
        "hosts": ["api.mycompany.com"]
      },
      "rules": [
        {
          "host": "api.mycompany.com",
          "http": {
            "paths": [
              {
                "path": "/",
                "pathType": "Prefix",
                "backend": {
                  "service": {
                    "name": "api-gateway",
                    "port": 80
                  }
                }
              }
            ]
          }
        }
      ]
    },
    "monitoring": {
      "prometheus": {
        "enabled": true,
        "serviceMonitor": true,
        "scrapeInterval": "30s"
      },
      "grafana": {
        "enabled": true,
        "dashboards": ["default", "custom"]
      }
    },
    "logging": {
      "fluentd": {
        "enabled": true,
        "outputType": "elasticsearch",
        "elasticsearchHost": "elasticsearch.logging.svc.cluster.local"
      }
    },
    "serviceMesh": {
      "enabled": false,
      "type": "istio",  // "istio" | "linkerd" | "consul"
      "mtls": {
        "enabled": true,
        "mode": "STRICT"
      }
    },
    "databases": [
      {
        "name": "customer-db",
        "type": "postgresql",
        "deployment": {
          "enabled": true,
          "replicas": 1,
          "storageClass": "managed-premium",
          "storageSize": "20Gi",
          "resources": {
            "requests": {
              "cpu": "250m",
              "memory": "512Mi"
            },
            "limits": {
              "cpu": "1",
              "memory": "2Gi"
            }
          }
        },
        "externalConnection": {
          "enabled": false,
          "host": "postgres.external.com",
          "port": 5432
        }
      }
    ],
    "messageBus": {
      "name": "rabbitmq",
      "deployment": {
        "enabled": true,
        "replicas": 3,
        "storageClass": "managed-premium",
        "storageSize": "10Gi",
        "resources": {
          "requests": {
            "cpu": "200m",
            "memory": "512Mi"
          },
          "limits": {
            "cpu": "1",
            "memory": "2Gi"
          }
        }
      },
      "externalConnection": {
        "enabled": false,
        "host": "rabbitmq.external.com",
        "port": 5672
      }
    }
  }
}
```

---

## 3. Visual Kubernetes Configurator (UI)

### 3.1 Configuration Wizard Flow

```
Step 1: Cluster Target
┌────────────────────────────────────────────────┐
│ Where will you deploy?                        │
├────────────────────────────────────────────────┤
│ ○ Azure Kubernetes Service (AKS)              │
│ ○ Amazon Elastic Kubernetes Service (EKS)     │
│ ○ Google Kubernetes Engine (GKE)              │
│ ○ On-Premise Kubernetes                       │
│ ○ Local (minikube/kind)                       │
│                                                │
│ [Back]  [Next →]                               │
└────────────────────────────────────────────────┘

Step 2: Global Settings
┌────────────────────────────────────────────────┐
│ Global Configuration                           │
├────────────────────────────────────────────────┤
│ Namespace: [ecommerce-prod____________]        │
│                                                │
│ Container Registry:                            │
│ [myregistry.azurecr.io______________]          │
│                                                │
│ Image Pull Secret:                             │
│ [acr-secret_________________________]          │
│                                                │
│ ☑ Enable Resource Quotas                      │
│   CPU Limit: [20___] cores                    │
│   Memory Limit: [40___] Gi                    │
│                                                │
│ ☑ Enable Network Policies                     │
│                                                │
│ [Back]  [Next →]                               │
└────────────────────────────────────────────────┘

Step 3: Service Configuration (Per Service)
┌────────────────────────────────────────────────┐
│ Customer Service Configuration                 │
├────────────────────────────────────────────────┤
│ Replicas: [3___]                               │
│                                                │
│ Resources:                                     │
│   CPU Request: [100m___] Limit: [500m___]     │
│   Memory Request: [256Mi__] Limit: [512Mi__]  │
│                                                │
│ ☑ Enable Auto-Scaling                         │
│   Min Replicas: [2___] Max: [10___]           │
│   Target CPU: [70___]% Memory: [80___]%       │
│                                                │
│ Health Checks:                                 │
│   Liveness Path: [/health/live__________]     │
│   Readiness Path: [/health/ready_________]    │
│                                                │
│ [Configure Advanced] [Next Service →]         │
└────────────────────────────────────────────────┘

Step 4: Ingress & Networking
┌────────────────────────────────────────────────┐
│ Ingress Configuration                          │
├────────────────────────────────────────────────┤
│ ☑ Enable Ingress                               │
│                                                │
│ Ingress Class: [nginx_____▼]                  │
│                                                │
│ Domain: [api.mycompany.com______________]      │
│                                                │
│ ☑ Enable TLS/SSL                               │
│   Certificate Issuer: [letsencrypt-prod__▼]   │
│                                                │
│ Rate Limiting: [100___] requests/minute        │
│                                                │
│ [Back]  [Next →]                               │
└────────────────────────────────────────────────┘

Step 5: Databases & Messaging
┌────────────────────────────────────────────────┐
│ Database Configuration                         │
├────────────────────────────────────────────────┤
│ ○ Deploy databases in Kubernetes              │
│   Storage Class: [managed-premium___▼]        │
│   Storage Size: [20___] Gi                    │
│                                                │
│ ○ Use external databases                      │
│   Connection strings configured separately    │
│                                                │
│ Message Bus (RabbitMQ):                        │
│ ○ Deploy in Kubernetes                        │
│ ○ Use external RabbitMQ                       │
│                                                │
│ [Back]  [Next →]                               │
└────────────────────────────────────────────────┘

Step 6: Monitoring & Logging
┌────────────────────────────────────────────────┐
│ Observability Configuration                    │
├────────────────────────────────────────────────┤
│ ☑ Enable Prometheus Monitoring                │
│   Scrape Interval: [30___] seconds            │
│                                                │
│ ☑ Enable Grafana Dashboards                   │
│   Dashboards: ☑ Default  ☑ Custom             │
│                                                │
│ ☑ Enable Centralized Logging                  │
│   Log Aggregator: [Elasticsearch___▼]         │
│   Retention: [30___] days                     │
│                                                │
│ [Back]  [Next →]                               │
└────────────────────────────────────────────────┘

Step 7: Advanced Features (Optional)
┌────────────────────────────────────────────────┐
│ Advanced Kubernetes Features                   │
├────────────────────────────────────────────────┤
│ ☐ Enable Service Mesh (Istio)                 │
│   ☐ Mutual TLS (mTLS)                         │
│   ☐ Traffic Management                        │
│                                                │
│ ☐ Enable Pod Security Policies                │
│                                                │
│ ☐ Enable Network Policies                     │
│                                                │
│ ☐ Enable Pod Disruption Budgets               │
│   Min Available: [1___]                       │
│                                                │
│ [Back]  [Review & Generate →]                 │
└────────────────────────────────────────────────┘

Step 8: Review & Generate
┌────────────────────────────────────────────────┐
│ Configuration Summary                          │
├────────────────────────────────────────────────┤
│ Cluster: Azure AKS                             │
│ Namespace: ecommerce-prod                      │
│ Services: 4 (Customer, Catalog, Order, Pay)    │
│ Replicas: 12 total pods                        │
│ Auto-scaling: Enabled (2-40 pods max)          │
│ Ingress: api.mycompany.com (TLS enabled)       │
│ Databases: In-cluster PostgreSQL               │
│ Monitoring: Prometheus + Grafana               │
│                                                │
│ Estimated Monthly Cost: $450                   │
│                                                │
│ [Edit] [Generate Manifests] [Export Config]   │
└────────────────────────────────────────────────┘
```

---

## 4. Generated Kubernetes Manifests

### 4.1 Folder Structure

```
/kubernetes
├── /base
│   ├── namespace.yaml
│   ├── resource-quota.yaml
│   ├── network-policy.yaml
│   └── /services
│       ├── customer-service-deployment.yaml
│       ├── customer-service-service.yaml
│       ├── customer-service-hpa.yaml
│       ├── customer-service-configmap.yaml
│       ├── customer-service-secret.yaml
│       ├── catalog-service-deployment.yaml
│       ├── catalog-service-service.yaml
│       └── ... (other services)
│
├── /infrastructure
│   ├── api-gateway-deployment.yaml
│   ├── api-gateway-service.yaml
│   ├── ingress.yaml
│   ├── postgres-statefulset.yaml
│   ├── postgres-service.yaml
│   ├── postgres-pvc.yaml
│   ├── rabbitmq-statefulset.yaml
│   ├── rabbitmq-service.yaml
│   └── rabbitmq-pvc.yaml
│
├── /monitoring
│   ├── prometheus-deployment.yaml
│   ├── prometheus-service.yaml
│   ├── prometheus-servicemonitor.yaml
│   ├── grafana-deployment.yaml
│   └── grafana-service.yaml
│
├── /overlays
│   ├── /dev
│   │   └── kustomization.yaml
│   ├── /staging
│   │   └── kustomization.yaml
│   └── /production
│       └── kustomization.yaml
│
└── kustomization.yaml
```

### 4.2 Sample Generated Manifests

#### Deployment with Full Configuration

```yaml
# customer-service-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: customer-service
  namespace: ecommerce-prod
  labels:
    app: customer-service
    version: v1
    tier: backend
  annotations:
    generated-by: "DynamicPlatform"
    generated-at: "2026-02-02T20:15:00Z"
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: customer-service
  template:
    metadata:
      labels:
        app: customer-service
        version: v1
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "80"
        prometheus.io/path: "/metrics"
    spec:
      # Security Context
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 2000
      
      # Image Pull Secrets
      imagePullSecrets:
        - name: acr-secret
      
      # Anti-Affinity (spread pods across nodes)
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                labelSelector:
                  matchExpressions:
                    - key: app
                      operator: In
                      values:
                        - customer-service
                topologyKey: kubernetes.io/hostname
      
      # Containers
      containers:
        - name: customer-service
          image: myregistry.azurecr.io/customer-service:latest
          imagePullPolicy: Always
          
          ports:
            - name: http
              containerPort: 80
              protocol: TCP
          
          # Environment Variables
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: customer-service-secrets
                  key: db-connection
            - name: MessageBus__Host
              value: "rabbitmq"
            - name: MessageBus__Port
              value: "5672"
            - name: MessageBus__Username
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-secrets
                  key: username
            - name: MessageBus__Password
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-secrets
                  key: password
          
          # Resource Limits
          resources:
            requests:
              cpu: 100m
              memory: 256Mi
            limits:
              cpu: 500m
              memory: 512Mi
          
          # Liveness Probe
          livenessProbe:
            httpGet:
              path: /health/live
              port: http
            initialDelaySeconds: 30
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
          
          # Readiness Probe
          readinessProbe:
            httpGet:
              path: /health/ready
              port: http
            initialDelaySeconds: 10
            periodSeconds: 5
            timeoutSeconds: 3
            failureThreshold: 3
          
          # Startup Probe (for slow-starting apps)
          startupProbe:
            httpGet:
              path: /health/startup
              port: http
            initialDelaySeconds: 0
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 30
          
          # Volume Mounts
          volumeMounts:
            - name: config
              mountPath: /app/config
              readOnly: true
            - name: logs
              mountPath: /app/logs
      
      # Volumes
      volumes:
        - name: config
          configMap:
            name: customer-service-config
        - name: logs
          emptyDir: {}
```

#### Horizontal Pod Autoscaler

```yaml
# customer-service-hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: customer-service-hpa
  namespace: ecommerce-prod
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: customer-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
        - type: Percent
          value: 100
          periodSeconds: 30
        - type: Pods
          value: 2
          periodSeconds: 30
      selectPolicy: Max
```

#### Service with Session Affinity

```yaml
# customer-service-service.yaml
apiVersion: v1
kind: Service
metadata:
  name: customer-service
  namespace: ecommerce-prod
  labels:
    app: customer-service
  annotations:
    service.beta.kubernetes.io/azure-load-balancer-internal: "true"
spec:
  type: ClusterIP
  selector:
    app: customer-service
  ports:
    - name: http
      port: 80
      targetPort: http
      protocol: TCP
  sessionAffinity: None
```

#### Ingress with TLS

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: api-gateway-ingress
  namespace: ecommerce-prod
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/rate-limit: "100"
    nginx.ingress.kubernetes.io/proxy-body-size: "10m"
    nginx.ingress.kubernetes.io/proxy-connect-timeout: "60"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "60"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "60"
spec:
  tls:
    - hosts:
        - api.mycompany.com
      secretName: ecommerce-tls
  rules:
    - host: api.mycompany.com
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: api-gateway
                port:
                  number: 80
```

#### StatefulSet for PostgreSQL

```yaml
# postgres-statefulset.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: ecommerce-prod
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
        - name: postgres
          image: postgres:15
          ports:
            - containerPort: 5432
              name: postgres
          env:
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: postgres-secrets
                  key: password
            - name: PGDATA
              value: /var/lib/postgresql/data/pgdata
          resources:
            requests:
              cpu: 250m
              memory: 512Mi
            limits:
              cpu: 1
              memory: 2Gi
          volumeMounts:
            - name: postgres-storage
              mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
    - metadata:
        name: postgres-storage
      spec:
        accessModes: ["ReadWriteOnce"]
        storageClassName: managed-premium
        resources:
          requests:
            storage: 20Gi
```

#### ConfigMap

```yaml
# customer-service-configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: customer-service-config
  namespace: ecommerce-prod
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning"
        }
      },
      "ServiceDiscovery": {
        "ServiceName": "CustomerService",
        "Port": 80
      },
      "Caching": {
        "Enabled": true,
        "Provider": "Redis",
        "ConnectionString": "redis:6379"
      }
    }
```

#### Secret (Base64 Encoded)

```yaml
# customer-service-secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: customer-service-secrets
  namespace: ecommerce-prod
type: Opaque
data:
  db-connection: <base64-encoded-connection-string>
  api-key: <base64-encoded-api-key>
```

---

## 5. Kustomize Support for Multi-Environment

### 5.1 Base Kustomization

```yaml
# /kubernetes/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: ecommerce-prod

resources:
  - base/namespace.yaml
  - base/resource-quota.yaml
  - base/services/customer-service-deployment.yaml
  - base/services/customer-service-service.yaml
  - base/services/customer-service-hpa.yaml
  - infrastructure/api-gateway-deployment.yaml
  - infrastructure/ingress.yaml
  - infrastructure/postgres-statefulset.yaml
  - infrastructure/rabbitmq-statefulset.yaml

configMapGenerator:
  - name: customer-service-config
    files:
      - config/appsettings.json

secretGenerator:
  - name: customer-service-secrets
    literals:
      - db-connection=Host=postgres;Database=CustomerDB;Username=postgres;Password=changeme

images:
  - name: myregistry.azurecr.io/customer-service
    newTag: v1.0.0
```

### 5.2 Development Overlay

```yaml
# /kubernetes/overlays/dev/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

bases:
  - ../../base

namespace: ecommerce-dev

patchesStrategicMerge:
  - deployment-patch.yaml

replicas:
  - name: customer-service
    count: 1

images:
  - name: myregistry.azurecr.io/customer-service
    newTag: dev-latest
```

```yaml
# /kubernetes/overlays/dev/deployment-patch.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: customer-service
spec:
  template:
    spec:
      containers:
        - name: customer-service
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Development"
          resources:
            requests:
              cpu: 50m
              memory: 128Mi
            limits:
              cpu: 200m
              memory: 256Mi
```

### 5.3 Production Overlay

```yaml
# /kubernetes/overlays/production/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

bases:
  - ../../base

namespace: ecommerce-prod

patchesStrategicMerge:
  - deployment-patch.yaml

replicas:
  - name: customer-service
    count: 3

images:
  - name: myregistry.azurecr.io/customer-service
    newTag: v1.0.0
```

---

## 6. Helm Chart Support (Alternative)

### 6.1 Generated Helm Chart Structure

```
/helm
├── Chart.yaml
├── values.yaml
├── values-dev.yaml
├── values-staging.yaml
├── values-prod.yaml
└── /templates
    ├── namespace.yaml
    ├── /services
    │   ├── customer-service-deployment.yaml
    │   ├── customer-service-service.yaml
    │   └── customer-service-hpa.yaml
    ├── /infrastructure
    │   ├── api-gateway-deployment.yaml
    │   ├── ingress.yaml
    │   └── postgres-statefulset.yaml
    └── /monitoring
        ├── prometheus-deployment.yaml
        └── grafana-deployment.yaml
```

### 6.2 values.yaml (Templated)

```yaml
# values.yaml
global:
  imageRegistry: myregistry.azurecr.io
  imagePullSecrets:
    - name: acr-secret
  namespace: ecommerce-prod

services:
  customerService:
    enabled: true
    replicaCount: 3
    image:
      repository: customer-service
      tag: v1.0.0
      pullPolicy: Always
    resources:
      requests:
        cpu: 100m
        memory: 256Mi
      limits:
        cpu: 500m
        memory: 512Mi
    autoscaling:
      enabled: true
      minReplicas: 2
      maxReplicas: 10
      targetCPUUtilization: 70
      targetMemoryUtilization: 80
    service:
      type: ClusterIP
      port: 80
    healthChecks:
      liveness:
        path: /health/live
        initialDelaySeconds: 30
      readiness:
        path: /health/ready
        initialDelaySeconds: 10

  catalogService:
    enabled: true
    # ... similar structure

ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
  hosts:
    - host: api.mycompany.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: ecommerce-tls
      hosts:
        - api.mycompany.com

databases:
  postgres:
    enabled: true
    storageClass: managed-premium
    storageSize: 20Gi
    resources:
      requests:
        cpu: 250m
        memory: 512Mi
      limits:
        cpu: 1
        memory: 2Gi

monitoring:
  prometheus:
    enabled: true
  grafana:
    enabled: true
```

### 6.3 Deployment Commands

```bash
# Development
helm install ecommerce ./helm -f ./helm/values-dev.yaml

# Staging
helm install ecommerce ./helm -f ./helm/values-staging.yaml

# Production
helm install ecommerce ./helm -f ./helm/values-prod.yaml

# Upgrade
helm upgrade ecommerce ./helm -f ./helm/values-prod.yaml
```

---

## 7. Cloud-Specific Optimizations

### 7.1 Azure AKS

```yaml
# Azure-specific annotations and configurations
metadata:
  annotations:
    service.beta.kubernetes.io/azure-load-balancer-internal: "true"
    service.beta.kubernetes.io/azure-load-balancer-resource-group: "my-rg"

spec:
  storageClassName: managed-premium  # Azure Premium SSD
  
  # Azure AD Pod Identity
  podLabels:
    aadpodidbinding: customer-service-identity
```

### 7.2 AWS EKS

```yaml
# AWS-specific annotations and configurations
metadata:
  annotations:
    service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
    service.beta.kubernetes.io/aws-load-balancer-internal: "true"

spec:
  storageClassName: gp3  # AWS EBS gp3
  
  # IAM Roles for Service Accounts (IRSA)
  serviceAccountName: customer-service-sa
```

### 7.3 GCP GKE

```yaml
# GCP-specific annotations and configurations
metadata:
  annotations:
    cloud.google.com/load-balancer-type: "Internal"

spec:
  storageClassName: standard-rwo  # GCP Persistent Disk
  
  # Workload Identity
  serviceAccountName: customer-service@project-id.iam.gserviceaccount.com
```

---

## 8. Advanced Features Configuration

### 8.1 Service Mesh (Istio)

```yaml
# Enable Istio sidecar injection
apiVersion: v1
kind: Namespace
metadata:
  name: ecommerce-prod
  labels:
    istio-injection: enabled

---
# Virtual Service for traffic management
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: customer-service
spec:
  hosts:
    - customer-service
  http:
    - match:
        - headers:
            canary:
              exact: "true"
      route:
        - destination:
            host: customer-service
            subset: v2
          weight: 20
        - destination:
            host: customer-service
            subset: v1
          weight: 80
    - route:
        - destination:
            host: customer-service
            subset: v1

---
# Destination Rule for mTLS
apiVersion: networking.istio.io/v1beta1
kind: DestinationRule
metadata:
  name: customer-service
spec:
  host: customer-service
  trafficPolicy:
    tls:
      mode: ISTIO_MUTUAL
  subsets:
    - name: v1
      labels:
        version: v1
    - name: v2
      labels:
        version: v2
```

### 8.2 Network Policies

```yaml
# network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: customer-service-netpol
  namespace: ecommerce-prod
spec:
  podSelector:
    matchLabels:
      app: customer-service
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        - podSelector:
            matchLabels:
              app: api-gateway
      ports:
        - protocol: TCP
          port: 80
  egress:
    - to:
        - podSelector:
            matchLabels:
              app: postgres
      ports:
        - protocol: TCP
          port: 5432
    - to:
        - podSelector:
            matchLabels:
              app: rabbitmq
      ports:
        - protocol: TCP
          port: 5672
```

### 8.3 Pod Disruption Budget

```yaml
# customer-service-pdb.yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: customer-service-pdb
  namespace: ecommerce-prod
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: customer-service
```

### 8.4 Pod Security Policy

```yaml
# pod-security-policy.yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: restricted
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
    - ALL
  volumes:
    - 'configMap'
    - 'emptyDir'
    - 'projected'
    - 'secret'
    - 'downwardAPI'
    - 'persistentVolumeClaim'
  hostNetwork: false
  hostIPC: false
  hostPID: false
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'RunAsAny'
  fsGroup:
    rule: 'RunAsAny'
  readOnlyRootFilesystem: false
```

---

## 9. Deployment Scripts

### 9.1 Generated Deployment Script

```bash
#!/bin/bash
# deploy.sh - Generated by DynamicPlatform

set -e

# Configuration
NAMESPACE="ecommerce-prod"
KUBECTL_CONTEXT="aks-production"
HELM_RELEASE="ecommerce"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting deployment to ${NAMESPACE}...${NC}"

# Switch context
echo -e "${YELLOW}Switching to context: ${KUBECTL_CONTEXT}${NC}"
kubectl config use-context ${KUBECTL_CONTEXT}

# Create namespace if not exists
kubectl get namespace ${NAMESPACE} || kubectl create namespace ${NAMESPACE}

# Apply secrets (from external secret manager)
echo -e "${YELLOW}Applying secrets...${NC}"
kubectl apply -f kubernetes/secrets/ -n ${NAMESPACE}

# Deploy using Kustomize
echo -e "${YELLOW}Deploying with Kustomize...${NC}"
kubectl apply -k kubernetes/overlays/production

# Wait for deployments
echo -e "${YELLOW}Waiting for deployments to be ready...${NC}"
kubectl wait --for=condition=available --timeout=300s \
  deployment/customer-service \
  deployment/catalog-service \
  deployment/order-service \
  deployment/payment-service \
  -n ${NAMESPACE}

# Check pod status
echo -e "${GREEN}Deployment Status:${NC}"
kubectl get pods -n ${NAMESPACE}

# Get ingress URL
echo -e "${GREEN}Ingress URL:${NC}"
kubectl get ingress -n ${NAMESPACE}

echo -e "${GREEN}Deployment completed successfully!${NC}"
```

### 9.2 Rollback Script

```bash
#!/bin/bash
# rollback.sh

set -e

NAMESPACE="ecommerce-prod"
DEPLOYMENT_NAME=$1

if [ -z "$DEPLOYMENT_NAME" ]; then
  echo "Usage: ./rollback.sh <deployment-name>"
  exit 1
fi

echo "Rolling back ${DEPLOYMENT_NAME}..."
kubectl rollout undo deployment/${DEPLOYMENT_NAME} -n ${NAMESPACE}

echo "Waiting for rollback to complete..."
kubectl rollout status deployment/${DEPLOYMENT_NAME} -n ${NAMESPACE}

echo "Rollback completed!"
```

---

## 10. Configuration Templates (Scriban)

### 10.1 Deployment Template

```scriban
# deployment.yaml.scriban
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ service.name | string.downcase }}
  namespace: {{ namespace }}
  labels:
    app: {{ service.name | string.downcase }}
    version: v1
spec:
  replicas: {{ service.deployment.replicas }}
  strategy:
    type: {{ service.deployment.strategy.type }}
    {{~ if service.deployment.strategy.type == "RollingUpdate" ~}}
    rollingUpdate:
      maxSurge: {{ service.deployment.strategy.rolling_update.max_surge }}
      maxUnavailable: {{ service.deployment.strategy.rolling_update.max_unavailable }}
    {{~ end ~}}
  selector:
    matchLabels:
      app: {{ service.name | string.downcase }}
  template:
    metadata:
      labels:
        app: {{ service.name | string.downcase }}
        version: v1
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "80"
        prometheus.io/path: "/metrics"
    spec:
      {{~ if global.image_pull_secrets ~}}
      imagePullSecrets:
        {{~ for secret in global.image_pull_secrets ~}}
        - name: {{ secret }}
        {{~ end ~}}
      {{~ end ~}}
      
      containers:
        - name: {{ service.name | string.downcase }}
          image: {{ global.image_registry }}/{{ service.name | string.downcase }}:{{ service.image_tag }}
          imagePullPolicy: Always
          
          ports:
            - name: http
              containerPort: 80
              protocol: TCP
          
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            {{~ for env in service.environment_variables ~}}
            - name: {{ env.name }}
              {{~ if env.value ~}}
              value: "{{ env.value }}"
              {{~ else if env.secret_ref ~}}
              valueFrom:
                secretKeyRef:
                  name: {{ env.secret_ref.name }}
                  key: {{ env.secret_ref.key }}
              {{~ end ~}}
            {{~ end ~}}
          
          resources:
            requests:
              cpu: {{ service.deployment.resources.requests.cpu }}
              memory: {{ service.deployment.resources.requests.memory }}
            limits:
              cpu: {{ service.deployment.resources.limits.cpu }}
              memory: {{ service.deployment.resources.limits.memory }}
          
          {{~ if service.deployment.health_checks ~}}
          livenessProbe:
            httpGet:
              path: {{ service.deployment.health_checks.liveness_probe.path }}
              port: http
            initialDelaySeconds: {{ service.deployment.health_checks.liveness_probe.initial_delay_seconds }}
            periodSeconds: {{ service.deployment.health_checks.liveness_probe.period_seconds }}
            timeoutSeconds: {{ service.deployment.health_checks.liveness_probe.timeout_seconds }}
            failureThreshold: {{ service.deployment.health_checks.liveness_probe.failure_threshold }}
          
          readinessProbe:
            httpGet:
              path: {{ service.deployment.health_checks.readiness_probe.path }}
              port: http
            initialDelaySeconds: {{ service.deployment.health_checks.readiness_probe.initial_delay_seconds }}
            periodSeconds: {{ service.deployment.health_checks.readiness_probe.period_seconds }}
            timeoutSeconds: {{ service.deployment.health_checks.readiness_probe.timeout_seconds }}
            failureThreshold: {{ service.deployment.health_checks.readiness_probe.failure_threshold }}
          {{~ end ~}}
```

---

## 11. Cost Estimation

### 11.1 Cost Calculator

The platform provides a **cost estimator** based on Kubernetes configuration:

```
┌────────────────────────────────────────────────┐
│ Estimated Monthly Cost (Azure AKS)             │
├────────────────────────────────────────────────┤
│ Cluster Management: $73                        │
│                                                │
│ Compute (Nodes):                               │
│   3 × Standard_D2s_v3: $210                    │
│                                                │
│ Storage:                                       │
│   5 × 20Gi Premium SSD: $50                    │
│                                                │
│ Load Balancer: $20                             │
│                                                │
│ Egress Traffic (100GB): $10                    │
│                                                │
│ ─────────────────────────────────────────────  │
│ Total: ~$363/month                             │
│                                                │
│ [Optimize] [Export Estimate]                   │
└────────────────────────────────────────────────┘
```

---

## 12. Best Practices & Recommendations

### 12.1 Platform-Generated Recommendations

```yaml
# Generated recommendations.md
# Kubernetes Configuration Recommendations

## Security
✅ Enabled: Pod Security Policies
✅ Enabled: Network Policies
✅ Enabled: RBAC
⚠️  Consider: Enable Service Mesh for mTLS
⚠️  Consider: Use external secret manager (Azure Key Vault, AWS Secrets Manager)

## Reliability
✅ Enabled: Liveness and Readiness Probes
✅ Enabled: Pod Disruption Budgets
✅ Enabled: Resource Limits
⚠️  Consider: Increase replica count for critical services

## Performance
✅ Enabled: Horizontal Pod Autoscaling
✅ Enabled: Pod Anti-Affinity
⚠️  Consider: Use node affinity for database pods
⚠️  Consider: Enable cluster autoscaler

## Cost Optimization
⚠️  Warning: Running databases in-cluster may be expensive
    Consider: Use managed database services (Azure Database for PostgreSQL)
⚠️  Consider: Use spot instances for non-critical workloads
✅ Good: Resource requests match actual usage
```

---

## 13. Implementation in Platform

### 13.1 New API Endpoints

```csharp
[ApiController]
[Route("api/projects/{projectId}/kubernetes")]
public class KubernetesConfigController : ControllerBase
{
    [HttpGet("config")]
    public async Task<ActionResult<KubernetesConfiguration>> GetConfiguration(Guid projectId)
    {
        // Return current K8s configuration or defaults
    }

    [HttpPost("config")]
    public async Task<ActionResult> SaveConfiguration(
        Guid projectId, 
        KubernetesConfiguration config)
    {
        // Save K8s configuration
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GeneratedManifests>> GenerateManifests(
        Guid projectId)
    {
        // Generate K8s manifests based on configuration
    }

    [HttpPost("estimate-cost")]
    public async Task<ActionResult<CostEstimate>> EstimateCost(
        Guid projectId,
        string cloudProvider)
    {
        // Calculate estimated monthly cost
    }

    [HttpGet("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateConfiguration(
        Guid projectId)
    {
        // Validate K8s configuration
    }
}
```

---

## 14. Summary

The Kubernetes Configuration capability provides:

✅ **Visual Configurator**: Easy-to-use wizard for K8s settings  
✅ **Production-Ready Manifests**: Complete K8s YAML files  
✅ **Multi-Environment Support**: Kustomize overlays for dev/staging/prod  
✅ **Helm Charts**: Alternative deployment method  
✅ **Cloud-Specific Optimizations**: Azure/AWS/GCP best practices  
✅ **Advanced Features**: Service Mesh, Network Policies, Auto-scaling  
✅ **Cost Estimation**: Predict infrastructure costs  
✅ **Deployment Scripts**: Automated deployment and rollback  

**Next Steps**: Integrate this into Phase 6 of the implementation plan!

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Status**: Ready for Implementation
