# Microservices Generation - Granular Implementation Plan

## Overview

This document provides a **step-by-step implementation plan** for adding microservices generation capability to the DynamicPlatform. The plan is broken down into **actionable tasks** with clear deliverables and dependencies.

---

## Phase 1: Foundation & Metadata (Weeks 1-3)

### Task 1.1: Extend Project Metadata Schema
**Duration**: 2 days  
**Owner**: Backend Team  
**Dependencies**: None

**Objective**: Add architecture configuration to project metadata

**Steps**:
1. Create new model: `ArchitectureConfiguration.cs` in `Platform.Core/Models/`
   ```csharp
   public class ArchitectureConfiguration
   {
       public ArchitectureType Type { get; set; } // Monolithic | Microservices
       public ServiceDecompositionStrategy Strategy { get; set; } // Auto | Manual
       public List<ServiceBoundary> Services { get; set; }
       public InfrastructureConfiguration Infrastructure { get; set; }
   }
   ```

2. Add to `ProjectMetadata.cs`:
   ```csharp
   public ArchitectureConfiguration? Architecture { get; set; }
   ```

3. Update database migration to add `Architecture` JSON column to `Projects` table

4. Create enums:
   - `ArchitectureType.cs`
   - `ServiceDecompositionStrategy.cs`
   - `MessagingType.cs` (RabbitMQ, Kafka)
   - `ApiGatewayType.cs` (Ocelot, YARP)

**Deliverables**:
- ✅ New models in `Platform.Core`
- ✅ Database migration script
- ✅ Unit tests for serialization/deserialization

**Validation**:
```bash
dotnet test Platform.Core.Tests --filter "Category=ArchitectureMetadata"
```

---

### Task 1.2: Create Service Boundary Models
**Duration**: 1 day  
**Owner**: Backend Team  
**Dependencies**: Task 1.1

**Objective**: Define data structures for service boundaries

**Steps**:
1. Create `ServiceBoundary.cs`:
   ```csharp
   public class ServiceBoundary
   {
       public string ServiceName { get; set; }
       public List<string> EntityIds { get; set; } // References to EntityMetadata
       public int Port { get; set; }
       public DatabaseConfiguration Database { get; set; }
       public List<string> DependsOn { get; set; } // Other service names
       public bool EnableMessaging { get; set; }
       public bool EnableCaching { get; set; }
   }
   ```

2. Create `DatabaseConfiguration.cs`:
   ```csharp
   public class DatabaseConfiguration
   {
       public string Type { get; set; } // PostgreSQL, MySQL, SQLServer
       public string Name { get; set; }
       public string ConnectionStringTemplate { get; set; }
   }
   ```

3. Create `InfrastructureConfiguration.cs`:
   ```csharp
   public class InfrastructureConfiguration
   {
       public ApiGatewayConfig ApiGateway { get; set; }
       public MessageBusConfig MessageBus { get; set; }
       public ContainerizationConfig Containerization { get; set; }
   }
   ```

**Deliverables**:
- ✅ Service boundary models
- ✅ Validation logic (no circular dependencies)
- ✅ JSON schema for validation

---

### Task 1.3: Build Service Decomposition Analyzer
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: Task 1.2

**Objective**: Implement algorithm to suggest service boundaries

**Steps**:
1. Create `ServiceDecompositionAnalyzer.cs` in `Platform.Engine/Services/`

2. Implement entity relationship graph builder:
   ```csharp
   private EntityGraph BuildEntityGraph(List<EntityMetadata> entities)
   {
       var graph = new EntityGraph();
       foreach (var entity in entities)
       {
           graph.AddNode(entity);
           foreach (var relation in entity.Relations)
           {
               graph.AddEdge(entity, relation.TargetEntity, relation.Type);
           }
       }
       return graph;
   }
   ```

3. Implement aggregate identification (Tarjan's algorithm for strongly connected components)

4. Apply domain heuristics:
   - **Naming patterns**: Group entities with common prefixes (e.g., "Order*" → OrderService)
   - **Relationship strength**: OneToMany/ManyToOne = strong bond, ManyToMany = weak
   - **Workflow analysis**: Entities used together in workflows → same service
   - **Security boundaries**: Different RBAC rules → different services

5. Optimize service size (target: 5-15 entities per service)

6. Validate decomposition:
   - No circular dependencies
   - Each entity belongs to exactly one service
   - All foreign keys resolvable

**Deliverables**:
- ✅ `ServiceDecompositionAnalyzer` class
- ✅ Unit tests with sample entity graphs
- ✅ Performance test (handle 100+ entities)

**Test Cases**:
- Simple e-commerce (Customer, Product, Order) → 3 services
- Complex ERP (50+ entities) → 8-12 services
- Single-domain app (5 entities) → 1 service (recommend monolith)

---

### Task 1.4: Create API Endpoint for Architecture Analysis
**Duration**: 2 days  
**Owner**: Backend Team  
**Dependencies**: Task 1.3

**Objective**: Expose service decomposition via REST API

**Steps**:
1. Create `ArchitectureController.cs`:
   ```csharp
   [ApiController]
   [Route("api/projects/{projectId}/architecture")]
   public class ArchitectureController : ControllerBase
   {
       [HttpPost("analyze")]
       public async Task<ActionResult<ArchitectureAnalysisResult>> AnalyzeDecomposition(
           Guid projectId)
       {
           var project = await _projectService.GetByIdAsync(projectId);
           var analysis = await _decompositionAnalyzer.AnalyzeAsync(project);
           return Ok(analysis);
       }

       [HttpPost("save")]
       public async Task<ActionResult> SaveArchitectureConfig(
           Guid projectId, 
           ArchitectureConfiguration config)
       {
           await _projectService.UpdateArchitectureAsync(projectId, config);
           return Ok();
       }
   }
   ```

2. Create DTOs:
   - `ArchitectureAnalysisResult` (suggested services, warnings, metrics)
   - `ServiceBoundaryDto`

**Deliverables**:
- ✅ REST API endpoints
- ✅ Swagger documentation
- ✅ Integration tests

---

## Phase 2: Code Generation (Weeks 4-6)

### Task 2.1: Create Microservice Project Generator
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: Phase 1 complete

**Objective**: Generate individual service projects

**Steps**:
1. Create `MicroserviceProjectGenerator.cs` in `Platform.Engine/Generators/`

2. Implement service project structure generation:
   ```
   /CustomerService
   ├── /Controllers
   ├── /Domain
   │   ├── /Entities
   │   └── /Repositories
   ├── /Application
   │   └── /Services
   ├── /Infrastructure
   │   ├── /Data (DbContext)
   │   └── /Messaging
   ├── Program.cs
   ├── Dockerfile
   └── CustomerService.csproj
   ```

3. Create Scriban templates:
   - `Service.Program.cs.scriban`
   - `Service.csproj.scriban`
   - `Service.Dockerfile.scriban`
   - `Service.DbContext.cs.scriban`

4. Implement entity filtering (only include entities for this service)

5. Generate service-specific `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=postgres;Database=CustomerDB;..."
     },
     "ServiceDiscovery": {
       "ServiceName": "CustomerService",
       "Port": 5001
     }
   }
   ```

**Deliverables**:
- ✅ `MicroserviceProjectGenerator` class
- ✅ Scriban templates for service structure
- ✅ Generated sample service compiles successfully

**Validation**:
```bash
# Generate sample CustomerService
dotnet build CustomerService/CustomerService.csproj
# Should succeed
```

---

### Task 2.2: Generate Shared Contracts Library
**Duration**: 3 days  
**Owner**: Backend Team  
**Dependencies**: Task 2.1

**Objective**: Create shared DTOs and event contracts

**Steps**:
1. Create `SharedContractsGenerator.cs`

2. Generate `/Shared/Contracts` project:
   ```
   /Shared.Contracts
   ├── /DTOs
   │   ├── CustomerDto.cs
   │   ├── OrderDto.cs
   │   └── ...
   ├── /Events
   │   ├── CustomerCreatedEvent.cs
   │   ├── OrderSubmittedEvent.cs
   │   └── ...
   └── Shared.Contracts.csproj
   ```

3. Create templates:
   - `Dto.cs.scriban` (generate from EntityMetadata)
   - `Event.cs.scriban` (generate from workflow triggers)

4. Each service references `Shared.Contracts` project

**Deliverables**:
- ✅ Shared contracts library
- ✅ All services reference it
- ✅ No circular dependencies

---

### Task 2.3: Generate API Gateway
**Duration**: 4 days  
**Owner**: Backend Team  
**Dependencies**: Task 2.1

**Objective**: Create API Gateway with Ocelot

**Steps**:
1. Create `ApiGatewayGenerator.cs`

2. Generate `/ApiGateway` project:
   ```
   /ApiGateway
   ├── Program.cs
   ├── ocelot.json
   ├── Dockerfile
   └── ApiGateway.csproj
   ```

3. Create `ocelot.json.scriban` template:
   ```json
   {
     "Routes": [
       {{~ for service in services ~}}
       {{~ for entity in service.entities ~}}
       {
         "DownstreamPathTemplate": "/api/{{ entity.name | string.downcase }}/{everything}",
         "DownstreamScheme": "http",
         "DownstreamHostAndPorts": [
           { "Host": "{{ service.name | string.downcase }}", "Port": {{ service.port }} }
         ],
         "UpstreamPathTemplate": "/api/{{ entity.name | string.downcase }}/{everything}",
         "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
       }{{ if !for.last }},{{ end }}
       {{~ end ~}}
       {{~ end ~}}
     ],
     "GlobalConfiguration": {
       "BaseUrl": "http://localhost:5000"
     }
   }
   ```

4. Add authentication configuration (JWT validation)

5. Add rate limiting and caching policies

**Deliverables**:
- ✅ API Gateway project
- ✅ Routing configuration
- ✅ Successfully routes to services

**Validation**:
```bash
# Start all services + gateway
docker-compose up
# Test routing
curl http://localhost:5000/api/customers
# Should route to CustomerService
```

---

### Task 2.4: Generate Docker Compose Configuration
**Duration**: 2 days  
**Owner**: DevOps Team  
**Dependencies**: Task 2.1, 2.3

**Objective**: Orchestrate all services locally

**Steps**:
1. Create `DockerComposeGenerator.cs`

2. Create `docker-compose.yml.scriban`:
   ```yaml
   version: '3.8'
   services:
     {{~ for service in services ~}}
     {{ service.name | string.downcase }}:
       build:
         context: ./src/Services/{{ service.name }}
         dockerfile: Dockerfile
       ports:
         - "{{ service.port }}:80"
       environment:
         - ConnectionStrings__DefaultConnection=Host=postgres;Database={{ service.database.name }};...
         - MessageBus__Host=rabbitmq
       depends_on:
         - postgres
         - rabbitmq
     {{~ end ~}}

     apigateway:
       build:
         context: ./src/ApiGateway
       ports:
         - "5000:80"
       depends_on:
         {{~ for service in services ~}}
         - {{ service.name | string.downcase }}
         {{~ end ~}}

     postgres:
       image: postgres:15
       environment:
         - POSTGRES_PASSWORD=postgres
       volumes:
         - postgres-data:/var/lib/postgresql/data

     rabbitmq:
       image: rabbitmq:3-management
       ports:
         - "5672:5672"
         - "15672:15672"

   volumes:
     postgres-data:
   ```

3. Generate `docker-compose.override.yml` for development

**Deliverables**:
- ✅ docker-compose.yml
- ✅ All services start successfully
- ✅ Health checks pass

---

## Phase 3: Inter-Service Communication (Weeks 7-9)

### Task 3.1: Generate Typed HTTP Clients (Refit)
**Duration**: 3 days  
**Owner**: Backend Team  
**Dependencies**: Phase 2 complete

**Objective**: Enable type-safe service-to-service calls

**Steps**:
1. Create `ServiceClientGenerator.cs`

2. For each service, generate Refit interfaces:
   ```csharp
   // In OrderService, generate client for CustomerService
   public interface ICustomerServiceClient
   {
       [Get("/api/customers/{id}")]
       Task<CustomerDto> GetCustomerAsync(Guid id);

       [Get("/api/customers")]
       Task<List<CustomerDto>> GetCustomersAsync();
   }
   ```

3. Register clients in DI with Polly policies:
   ```csharp
   services.AddRefitClient<ICustomerServiceClient>()
       .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://customerservice:80"))
       .AddTransientHttpErrorPolicy(policy => 
           policy.WaitAndRetryAsync(3, retryAttempt => 
               TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
       .AddTransientHttpErrorPolicy(policy => 
           policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
   ```

4. Generate client registration in `Program.cs` for each service

**Deliverables**:
- ✅ Refit clients generated
- ✅ Resilience policies applied
- ✅ Integration test: OrderService calls CustomerService

---

### Task 3.2: Implement Message Bus Integration (MassTransit)
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: Task 2.2

**Objective**: Enable asynchronous event-driven communication

**Steps**:
1. Create `MessageBusGenerator.cs`

2. Generate event contracts in `Shared.Contracts/Events`:
   ```csharp
   public record CustomerCreatedEvent
   {
       public Guid CustomerId { get; init; }
       public string Name { get; init; }
       public DateTime CreatedAt { get; init; }
   }
   ```

3. Generate event publishers:
   ```csharp
   // In CustomerService
   public class CustomerService
   {
       private readonly IPublishEndpoint _publishEndpoint;

       public async Task CreateCustomerAsync(Customer customer)
       {
           await _dbContext.Customers.AddAsync(customer);
           await _dbContext.SaveChangesAsync();

           await _publishEndpoint.Publish(new CustomerCreatedEvent
           {
               CustomerId = customer.Id,
               Name = customer.Name,
               CreatedAt = DateTime.UtcNow
           });
       }
   }
   ```

4. Generate event consumers:
   ```csharp
   // In OrderService (if it needs to react to CustomerCreated)
   public class CustomerCreatedConsumer : IConsumer<CustomerCreatedEvent>
   {
       public async Task Consume(ConsumeContext<CustomerCreatedEvent> context)
       {
           // Cache customer data locally or perform other actions
           _logger.LogInformation("Customer {CustomerId} created", context.Message.CustomerId);
       }
   }
   ```

5. Configure MassTransit in each service:
   ```csharp
   services.AddMassTransit(x =>
   {
       x.AddConsumers(Assembly.GetExecutingAssembly());
       
       x.UsingRabbitMq((context, cfg) =>
       {
           cfg.Host("rabbitmq", "/", h =>
           {
               h.Username("guest");
               h.Password("guest");
           });
           
           cfg.ConfigureEndpoints(context);
       });
   });
   ```

**Deliverables**:
- ✅ Event contracts generated
- ✅ Publishers and consumers generated
- ✅ Integration test: Event published and consumed

---

### Task 3.3: Generate Saga Orchestrators for Workflows
**Duration**: 7 days  
**Owner**: Backend Team  
**Dependencies**: Task 3.2

**Objective**: Convert Elsa workflows to MassTransit Sagas

**Steps**:
1. Create `SagaGenerator.cs`

2. Analyze Elsa workflow definitions to identify:
   - Cross-service activities
   - Compensation logic
   - State transitions

3. Generate Saga state machine:
   ```csharp
   public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
   {
       public OrderSagaStateMachine()
       {
           InstanceState(x => x.CurrentState);

           Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
           Event(() => InventoryReserved, x => x.CorrelateById(m => m.Message.OrderId));
           Event(() => PaymentProcessed, x => x.CorrelateById(m => m.Message.OrderId));

           Initially(
               When(OrderSubmitted)
                   .Then(context => context.Instance.OrderId = context.Data.OrderId)
                   .Publish(context => new ReserveInventory(context.Instance.OrderId))
                   .TransitionTo(AwaitingInventory)
           );

           During(AwaitingInventory,
               When(InventoryReserved)
                   .Publish(context => new ProcessPayment(context.Instance.OrderId))
                   .TransitionTo(AwaitingPayment),
               When(InventoryReservationFailed)
                   .Publish(context => new CancelOrder(context.Instance.OrderId))
                   .TransitionTo(Failed)
           );

           During(AwaitingPayment,
               When(PaymentProcessed)
                   .Publish(context => new CreateShipment(context.Instance.OrderId))
                   .TransitionTo(Completed),
               When(PaymentFailed)
                   .Publish(context => new ReleaseInventory(context.Instance.OrderId))
                   .Publish(context => new CancelOrder(context.Instance.OrderId))
                   .TransitionTo(Failed)
           );
       }

       public State AwaitingInventory { get; private set; }
       public State AwaitingPayment { get; private set; }
       public State Completed { get; private set; }
       public State Failed { get; private set; }

       public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; }
       public Event<InventoryReservedEvent> InventoryReserved { get; private set; }
       public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; }
   }
   ```

4. Generate saga state persistence (EF Core)

5. Register saga in DI

**Deliverables**:
- ✅ Saga state machines generated from workflows
- ✅ Compensation logic included
- ✅ Integration test: Full saga execution

---

## Phase 4: Data Management (Weeks 10-11)

### Task 4.1: Implement Database-per-Service Pattern
**Duration**: 3 days  
**Owner**: Backend Team  
**Dependencies**: Phase 2 complete

**Objective**: Each service has isolated database

**Steps**:
1. Update `MicroserviceProjectGenerator` to create service-specific DbContext:
   ```csharp
   public class CustomerServiceDbContext : DbContext
   {
       public DbSet<Customer> Customers { get; set; }
       public DbSet<Address> Addresses { get; set; }
       // Only entities for this service
   }
   ```

2. Generate migration scripts per service:
   ```bash
   /CustomerService/Migrations/
   /OrderService/Migrations/
   /CatalogService/Migrations/
   ```

3. Update docker-compose to create multiple databases:
   ```yaml
   postgres:
     image: postgres:15
     environment:
       - POSTGRES_MULTIPLE_DATABASES=CustomerDB,OrderDB,CatalogDB
   ```

4. Generate database initialization scripts

**Deliverables**:
- ✅ Each service has own DbContext
- ✅ Migrations generated per service
- ✅ Databases created on startup

---

### Task 4.2: Generate API Composition for Cross-Service Queries
**Duration**: 4 days  
**Owner**: Backend Team  
**Dependencies**: Task 3.1

**Objective**: Handle queries spanning multiple services

**Steps**:
1. Create `ApiCompositionGenerator.cs`

2. Analyze frontend requirements (from page metadata) to identify cross-service queries

3. Generate BFF (Backend-for-Frontend) endpoints in API Gateway:
   ```csharp
   // In ApiGateway
   [HttpGet("api/orders/{id}/details")]
   public async Task<OrderDetailsDto> GetOrderDetails(Guid id)
   {
       var order = await _orderServiceClient.GetOrderAsync(id);
       var customer = await _customerServiceClient.GetCustomerAsync(order.CustomerId);
       var products = await _catalogServiceClient.GetProductsByIdsAsync(order.ProductIds);

       return new OrderDetailsDto
       {
           OrderId = order.Id,
           OrderDate = order.CreatedAt,
           CustomerName = customer.Name,
           CustomerEmail = customer.Email,
           Products = products.Select(p => new ProductSummaryDto
           {
               Name = p.Name,
               Price = p.Price
           }).ToList()
       };
   }
   ```

4. Implement parallel fetching with `Task.WhenAll` for performance

5. Add caching for frequently accessed data

**Deliverables**:
- ✅ BFF endpoints generated
- ✅ Cross-service queries working
- ✅ Performance test: < 200ms response time

---

## Phase 5: Frontend Integration (Weeks 12-13)

### Task 5.1: Build Service Boundary Designer UI
**Duration**: 7 days  
**Owner**: Frontend Team  
**Dependencies**: Task 1.4 (API ready)

**Objective**: Visual canvas for designing service boundaries

**Steps**:
1. Create new Angular component: `service-boundary-designer.component.ts`

2. Use Konva.js or D3.js for visual graph:
   - Nodes = Services (rectangles)
   - Labels = Entity names inside service nodes
   - Arrows = Dependencies between services

3. Implement drag-and-drop:
   - Drag entity from one service to another
   - Real-time validation (show warnings for circular dependencies)

4. Add service configuration panel:
   - Service name
   - Port number
   - Database type
   - Enable/disable messaging

5. Integrate with backend API:
   ```typescript
   async analyzeDecomposition() {
     const result = await this.architectureService.analyze(this.projectId);
     this.suggestedServices = result.services;
     this.renderGraph();
   }

   async saveConfiguration() {
     await this.architectureService.save(this.projectId, this.architectureConfig);
   }
   ```

6. Add "Auto-Optimize" button (re-run analyzer)

7. Add "Validate" button (check for issues)

**Deliverables**:
- ✅ Service Boundary Designer component
- ✅ Visual graph rendering
- ✅ Drag-and-drop working
- ✅ Save/load configuration

---

### Task 5.2: Add Architecture Selection to Publish Flow
**Duration**: 3 days  
**Owner**: Frontend Team  
**Dependencies**: Task 5.1

**Objective**: Let users choose architecture when publishing

**Steps**:
1. Update "Publish" button flow in Platform Studio

2. Show modal dialog:
   ```
   ┌────────────────────────────────────────┐
   │  Choose Architecture                   │
   ├────────────────────────────────────────┤
   │                                        │
   │  ○ Monolithic (Recommended for < 10    │
   │     entities)                          │
   │     ✓ Simple deployment                │
   │     ✓ Easy local development           │
   │                                        │
   │  ○ Microservices (Recommended for      │
   │     complex apps)                      │
   │     ✓ Independent scaling              │
   │     ✓ Team autonomy                    │
   │                                        │
   │  [Cancel]  [Next →]                    │
   └────────────────────────────────────────┘
   ```

3. If "Microservices" selected → Navigate to Service Boundary Designer

4. After configuration → Trigger generation

5. Show progress indicator:
   ```
   Generating microservices...
   ✓ Analyzing service boundaries
   ✓ Generating CustomerService
   ✓ Generating OrderService
   ✓ Generating API Gateway
   ✓ Creating Docker Compose
   ⏳ Packaging ZIP...
   ```

**Deliverables**:
- ✅ Architecture selection modal
- ✅ Integration with publish flow
- ✅ Progress tracking

---

## Phase 6: Infrastructure & Deployment (Weeks 14-16)

### Task 6.1: Generate Kubernetes Manifests
**Duration**: 5 days  
**Owner**: DevOps Team  
**Dependencies**: Phase 2 complete

**Objective**: Production-ready K8s deployment

**Steps**:
1. Create `KubernetesManifestGenerator.cs`

2. Generate manifests for each service:
   ```yaml
   # customer-service-deployment.yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: customer-service
   spec:
     replicas: 3
     selector:
       matchLabels:
         app: customer-service
     template:
       metadata:
         labels:
           app: customer-service
       spec:
         containers:
         - name: customer-service
           image: myregistry/customer-service:latest
           ports:
           - containerPort: 80
           env:
           - name: ConnectionStrings__DefaultConnection
             valueFrom:
               secretKeyRef:
                 name: customer-service-secrets
                 key: db-connection
           livenessProbe:
             httpGet:
               path: /health
               port: 80
             initialDelaySeconds: 30
           readinessProbe:
             httpGet:
               path: /ready
               port: 80
   ---
   apiVersion: v1
   kind: Service
   metadata:
     name: customer-service
   spec:
     selector:
       app: customer-service
     ports:
     - port: 80
       targetPort: 80
   ```

3. Generate ConfigMaps and Secrets

4. Generate Ingress for API Gateway

5. Generate Helm chart (optional, advanced)

**Deliverables**:
- ✅ K8s manifests for all services
- ✅ Deployment scripts
- ✅ Successfully deploys to local K8s (minikube/kind)

---

### Task 6.1.5: Build Kubernetes Configuration Wizard (UI)
**Duration**: 5 days  
**Owner**: Frontend Team + DevOps Team  
**Dependencies**: Task 6.1

**Objective**: Visual configurator for customizing Kubernetes deployment

**Steps**:
1. Create new Angular component: `kubernetes-config-wizard.component.ts`

2. Implement multi-step wizard (7 steps):
   - **Step 1**: Cluster Target (Azure AKS, AWS EKS, GCP GKE, On-Premise)
   - **Step 2**: Global Settings (namespace, registry, resource quotas)
   - **Step 3**: Service Configuration (replicas, resources, auto-scaling)
   - **Step 4**: Ingress & Networking (domain, TLS, rate limiting)
   - **Step 5**: Databases & Messaging (in-cluster vs external)
   - **Step 6**: Monitoring & Logging (Prometheus, Grafana, logging)
   - **Step 7**: Review & Generate

3. Create Kubernetes configuration service:
   ```typescript
   export class KubernetesConfigService {
     async getConfiguration(projectId: string): Promise<KubernetesConfiguration> {
       return this.http.get<KubernetesConfiguration>(
         `/api/projects/${projectId}/kubernetes/config`
       ).toPromise();
     }

     async saveConfiguration(
       projectId: string, 
       config: KubernetesConfiguration
     ): Promise<void> {
       return this.http.post(
         `/api/projects/${projectId}/kubernetes/config`,
         config
       ).toPromise();
     }

     async generateManifests(projectId: string): Promise<GeneratedManifests> {
       return this.http.post<GeneratedManifests>(
         `/api/projects/${projectId}/kubernetes/generate`,
         {}
       ).toPromise();
     }

     async estimateCost(
       projectId: string,
       cloudProvider: string
     ): Promise<CostEstimate> {
       return this.http.post<CostEstimate>(
         `/api/projects/${projectId}/kubernetes/estimate-cost`,
         { cloudProvider }
       ).toPromise();
     }
   }
   ```

4. Add cost estimator component:
   ```typescript
   // Display estimated monthly cost based on configuration
   calculateCost() {
     const nodeCount = Math.ceil(this.totalReplicas / this.podsPerNode);
     const storageGb = this.services.reduce((sum, s) => 
       sum + (s.database?.storageSize || 0), 0);
     
     this.estimatedCost = {
       clusterManagement: 73,
       compute: nodeCount * 70,
       storage: storageGb * 2.5,
       loadBalancer: 20,
       total: 0
     };
     this.estimatedCost.total = 
       this.estimatedCost.clusterManagement +
       this.estimatedCost.compute +
       this.estimatedCost.storage +
       this.estimatedCost.loadBalancer;
   }
   ```

5. Add validation logic:
   ```typescript
   validateConfiguration(): ValidationResult {
     const errors = [];
     const warnings = [];

     // Check resource limits
     if (this.totalCpuRequests > this.clusterCpuCapacity) {
       errors.push('Total CPU requests exceed cluster capacity');
     }

     // Check replica count
     this.services.forEach(service => {
       if (service.replicas < 2 && service.critical) {
         warnings.push(`${service.name} has only ${service.replicas} replica(s)`);
       }
     });

     // Check storage class
     if (this.config.databases.some(db => !db.storageClass)) {
       errors.push('Storage class not specified for databases');
     }

     return { errors, warnings, isValid: errors.length === 0 };
   }
   ```

6. Integrate with publish flow:
   - After Service Boundary Designer
   - Before final generation
   - Optional: Use defaults if customer skips

7. Add preset templates:
   ```typescript
   const presets = {
     development: {
       replicas: 1,
       resources: { requests: { cpu: '50m', memory: '128Mi' } },
       autoscaling: { enabled: false }
     },
     staging: {
       replicas: 2,
       resources: { requests: { cpu: '100m', memory: '256Mi' } },
       autoscaling: { enabled: true, minReplicas: 2, maxReplicas: 5 }
     },
     production: {
       replicas: 3,
       resources: { requests: { cpu: '200m', memory: '512Mi' } },
       autoscaling: { enabled: true, minReplicas: 3, maxReplicas: 10 }
     }
   };
   ```

**Deliverables**:
- ✅ Kubernetes Configuration Wizard component
- ✅ 7-step wizard with validation
- ✅ Cost estimator working
- ✅ Preset templates (dev, staging, prod)
- ✅ Integration with publish flow
- ✅ Save/load configuration

**Validation**:
- User can configure K8s settings visually
- Cost estimate updates in real-time
- Configuration saves and loads correctly
- Generated manifests reflect custom settings

---

### Task 6.2: Add Observability Stack
**Duration**: 5 days  
**Owner**: DevOps Team  
**Dependencies**: Task 6.1

**Objective**: Logging, tracing, metrics

**Steps**:
1. Add OpenTelemetry to each service:
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddEntityFrameworkCoreInstrumentation()
           .AddJaegerExporter(options =>
           {
               options.AgentHost = "jaeger";
               options.AgentPort = 6831;
           }));
   ```

2. Add Serilog for structured logging:
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .Enrich.WithProperty("ServiceName", "CustomerService")
       .Enrich.WithProperty("Environment", "Production")
       .WriteTo.Console()
       .WriteTo.Seq("http://seq:5341")
       .CreateLogger();
   ```

3. Add Prometheus metrics:
   ```csharp
   app.UseHttpMetrics();
   app.MapMetrics();
   ```

4. Update docker-compose to include:
   - Jaeger (distributed tracing)
   - Seq (centralized logging)
   - Prometheus (metrics collection)
   - Grafana (visualization)

5. Generate pre-configured Grafana dashboards

**Deliverables**:
- ✅ Observability integrated in all services
- ✅ Jaeger UI shows traces
- ✅ Seq shows logs
- ✅ Grafana dashboards working

---

### Task 6.3: Generate CI/CD Pipelines
**Duration**: 4 days  
**Owner**: DevOps Team  
**Dependencies**: Phase 6 complete

**Objective**: Automated build and deployment

**Steps**:
1. Create `CiCdPipelineGenerator.cs`

2. Generate GitHub Actions workflow:
   ```yaml
   # .github/workflows/build-and-deploy.yml
   name: Build and Deploy

   on:
     push:
       branches: [ main ]

   jobs:
     build-services:
       runs-on: ubuntu-latest
       strategy:
         matrix:
           service: [customer-service, order-service, catalog-service]
       steps:
         - uses: actions/checkout@v2
         - name: Build ${{ matrix.service }}
           run: |
             cd src/Services/${{ matrix.service }}
             dotnet build
             dotnet test
         - name: Build Docker image
           run: |
             docker build -t myregistry/${{ matrix.service }}:${{ github.sha }} .
         - name: Push to registry
           run: |
             docker push myregistry/${{ matrix.service }}:${{ github.sha }}

     deploy:
       needs: build-services
       runs-on: ubuntu-latest
       steps:
         - name: Deploy to Kubernetes
           run: |
             kubectl set image deployment/customer-service customer-service=myregistry/customer-service:${{ github.sha }}
             kubectl set image deployment/order-service order-service=myregistry/order-service:${{ github.sha }}
   ```

3. Generate Azure DevOps pipeline (alternative)

4. Generate deployment scripts

**Deliverables**:
- ✅ CI/CD pipeline files
- ✅ Automated build on commit
- ✅ Automated deployment to K8s

---

## Phase 7: Testing & Documentation (Weeks 17-18)

### Task 7.1: Generate Unit Tests
**Duration**: 4 days  
**Owner**: Backend Team  
**Dependencies**: Phase 2 complete

**Objective**: Auto-generate tests for each service

**Steps**:
1. Create `UnitTestGenerator.cs`

2. Generate tests for each entity:
   ```csharp
   public class CustomerServiceTests
   {
       [Fact]
       public async Task CreateCustomer_ShouldReturnCreatedCustomer()
       {
           // Arrange
           var options = new DbContextOptionsBuilder<CustomerServiceDbContext>()
               .UseInMemoryDatabase(databaseName: "TestDb")
               .Options;
           var context = new CustomerServiceDbContext(options);
           var service = new CustomerService(context);

           // Act
           var customer = new Customer { Name = "John Doe", Email = "john@example.com" };
           var result = await service.CreateAsync(customer);

           // Assert
           Assert.NotNull(result);
           Assert.Equal("John Doe", result.Name);
       }
   }
   ```

3. Generate tests for API endpoints

4. Generate tests for business logic

**Deliverables**:
- ✅ Unit tests for all services
- ✅ > 80% code coverage

---

### Task 7.2: Generate Integration Tests
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: Task 7.1

**Objective**: Test cross-service interactions

**Steps**:
1. Create `IntegrationTestGenerator.cs`

2. Use TestContainers to spin up services:
   ```csharp
   public class OrderWorkflowIntegrationTests : IAsyncLifetime
   {
       private readonly IContainer _postgresContainer;
       private readonly IContainer _rabbitmqContainer;

       public async Task InitializeAsync()
       {
           _postgresContainer = new ContainerBuilder()
               .WithImage("postgres:15")
               .WithEnvironment("POSTGRES_PASSWORD", "test")
               .Build();
           await _postgresContainer.StartAsync();

           _rabbitmqContainer = new ContainerBuilder()
               .WithImage("rabbitmq:3")
               .Build();
           await _rabbitmqContainer.StartAsync();
       }

       [Fact]
       public async Task CreateOrder_ShouldTriggerInventoryReservation()
       {
           // Arrange: Start services
           var customerService = new CustomerServiceTestHost(_postgresContainer);
           var orderService = new OrderServiceTestHost(_postgresContainer, _rabbitmqContainer);

           // Act: Create order
           var orderId = await orderService.CreateOrderAsync(new Order());

           // Assert: Verify inventory reservation message published
           var messages = await _rabbitmqContainer.ReadMessagesAsync("inventory-queue");
           Assert.Contains(messages, m => m.OrderId == orderId);
       }
   }
   ```

**Deliverables**:
- ✅ Integration tests for key workflows
- ✅ Tests pass in CI/CD pipeline

---

### Task 7.3: Generate Documentation
**Duration**: 3 days  
**Owner**: Technical Writer  
**Dependencies**: All phases

**Objective**: Comprehensive documentation for generated apps

**Steps**:
1. Create `DocumentationGenerator.cs`

2. Generate README.md for each service:
   ```markdown
   # Customer Service

   ## Overview
   Manages customer data, addresses, and payment methods.

   ## Architecture
   - **Database**: PostgreSQL (CustomerDB)
   - **Port**: 5001
   - **Dependencies**: None

   ## API Endpoints
   - `GET /api/customers` - List all customers
   - `POST /api/customers` - Create customer
   - `GET /api/customers/{id}` - Get customer by ID
   - `PUT /api/customers/{id}` - Update customer
   - `DELETE /api/customers/{id}` - Delete customer

   ## Running Locally
   ```bash
   docker-compose up customer-service
   ```

   ## Environment Variables
   - `ConnectionStrings__DefaultConnection` - Database connection string
   - `MessageBus__Host` - RabbitMQ host
   ```

3. Generate architecture diagrams (PlantUML)

4. Generate API documentation (Swagger/OpenAPI)

5. Generate deployment guide

**Deliverables**:
- ✅ README per service
- ✅ Architecture diagrams
- ✅ Deployment guide

---

## Phase 8: Advanced Features (Weeks 19-20)

### Task 8.1: Implement Strangler Fig Migration Pattern
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: All previous phases

**Objective**: Support gradual migration from monolith to microservices

**Steps**:
1. Create `StranglerFigGenerator.cs`

2. Support hybrid architecture:
   ```json
   {
     "architecture": {
       "type": "hybrid",
       "extractedServices": ["PaymentService"],
       "monolithServices": ["CustomerService", "OrderService"]
     }
   }
   ```

3. Generate API Gateway with smart routing:
   - `/api/payment/*` → PaymentService (microservice)
   - `/api/customers/*` → Monolith
   - `/api/orders/*` → Monolith

4. Generate data synchronization scripts (if needed)

**Deliverables**:
- ✅ Hybrid architecture support
- ✅ Gradual migration path documented

---

### Task 8.2: Add CQRS Pattern Support
**Duration**: 5 days  
**Owner**: Backend Team  
**Dependencies**: Phase 3 complete

**Objective**: Optimize read-heavy workloads

**Steps**:
1. Create `CqrsGenerator.cs`

2. Generate separate read and write models:
   ```csharp
   // Write Model (Command)
   public class CreateCustomerCommand
   {
       public string Name { get; set; }
       public string Email { get; set; }
   }

   public class CreateCustomerCommandHandler
   {
       public async Task<Guid> Handle(CreateCustomerCommand command)
       {
           var customer = new Customer { Name = command.Name, Email = command.Email };
           await _dbContext.Customers.AddAsync(customer);
           await _dbContext.SaveChangesAsync();
           
           await _publishEndpoint.Publish(new CustomerCreatedEvent { CustomerId = customer.Id });
           
           return customer.Id;
       }
   }

   // Read Model (Query)
   public class GetCustomerQuery
   {
       public Guid CustomerId { get; set; }
   }

   public class GetCustomerQueryHandler
   {
       public async Task<CustomerDto> Handle(GetCustomerQuery query)
       {
           return await _readDbContext.Customers
               .Where(c => c.Id == query.CustomerId)
               .Select(c => new CustomerDto { Id = c.Id, Name = c.Name })
               .FirstOrDefaultAsync();
       }
   }
   ```

3. Generate read model projections (event handlers that update read database)

4. Support separate read database (e.g., Elasticsearch for search)

**Deliverables**:
- ✅ CQRS pattern generated
- ✅ Read/write separation working

---

## Success Criteria

### Phase 1-2 (Foundation & Code Generation)
- ✅ Generate 3-service microservices app from sample project
- ✅ All services compile successfully
- ✅ Docker Compose starts all services

### Phase 3 (Communication)
- ✅ Services communicate via REST
- ✅ Events published and consumed
- ✅ Saga orchestrates multi-service workflow

### Phase 4-5 (Data & Frontend)
- ✅ Each service has isolated database
- ✅ Cross-service queries work
- ✅ Service Boundary Designer functional

### Phase 6 (Infrastructure)
- ✅ K8s deployment successful
- ✅ Observability stack operational
- ✅ CI/CD pipeline runs

### Phase 7-8 (Testing & Advanced)
- ✅ Tests pass (unit + integration)
- ✅ Documentation complete
- ✅ Advanced patterns (CQRS, Strangler Fig) working

---

## Risk Management

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Complexity underestimated** | High | High | Add 20% buffer to timeline |
| **Service boundary algorithm inaccurate** | Medium | Medium | Manual override in UI |
| **Performance issues (N+1 queries)** | Medium | High | Implement caching, API composition optimization |
| **Distributed transaction failures** | Medium | High | Comprehensive Saga testing, compensation logic |
| **Team learning curve** | High | Medium | Training sessions, documentation |

---

## Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| **Phase 1** | 3 weeks | Metadata, Decomposition Analyzer, API |
| **Phase 2** | 3 weeks | Service Generation, API Gateway, Docker Compose |
| **Phase 3** | 3 weeks | HTTP Clients, Messaging, Sagas |
| **Phase 4** | 2 weeks | Database-per-Service, API Composition |
| **Phase 5** | 2 weeks | Service Boundary Designer UI |
| **Phase 6** | 3 weeks | Kubernetes, Observability, CI/CD |
| **Phase 7** | 2 weeks | Testing, Documentation |
| **Phase 8** | 2 weeks | Advanced Patterns |
| **Total** | **20 weeks** | **Full Microservices Generation Capability** |

---

## Next Steps

1. **Review & Approve**: Stakeholder review of this plan
2. **Team Assignment**: Assign developers to each phase
3. **Kickoff Meeting**: Align team on architecture and goals
4. **Sprint Planning**: Break Phase 1 into 2-week sprints
5. **Begin Implementation**: Start with Task 1.1

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Status**: Ready for Implementation
