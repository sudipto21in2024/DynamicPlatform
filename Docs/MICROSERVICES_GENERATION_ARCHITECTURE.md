# Microservices Generation Architecture

## 1. Executive Summary

This document outlines the architecture and implementation strategy for enabling **DynamicPlatform** to generate applications in **two deployment modes**:

1. **Monolithic Architecture** (Current): Single deployable unit with all features bundled together
2. **Microservices Architecture** (New): Distributed system with independently deployable services

The platform will analyze the customer's application metadata and intelligently decompose it into appropriate microservices based on domain boundaries, entity relationships, and workflow dependencies.

---

## 2. Vision & Goals

### 2.1 Vision
Enable customers to choose their deployment architecture at **publish time**, allowing them to:
- Start with a monolith for rapid prototyping
- Evolve to microservices as complexity grows
- Generate production-ready, containerized microservices with complete infrastructure

### 2.2 Goals
- **Flexibility**: Support both architectures from the same metadata
- **Intelligence**: Auto-suggest optimal service boundaries based on domain analysis
- **Completeness**: Generate all infrastructure code (Docker, K8s, API Gateway, Service Discovery)
- **Best Practices**: Follow microservices patterns (Circuit Breaker, Saga, Event-Driven)
- **Zero Lock-in**: Generated code is fully owned and customizable

---

## 3. Architecture Decision Framework

### 3.1 When to Choose Monolith vs Microservices

The platform will provide **intelligent recommendations** based on:

| Criteria | Monolith | Microservices |
|----------|----------|---------------|
| **Number of Entities** | < 20 entities | > 20 entities |
| **Team Size** | 1-5 developers | > 5 developers |
| **Deployment Frequency** | Weekly/Monthly | Daily/Multiple per day |
| **Scalability Needs** | Uniform load | Variable load per domain |
| **Domain Complexity** | Single bounded context | Multiple bounded contexts |
| **Workflows** | Simple, synchronous | Complex, long-running, distributed |

### 3.2 User Experience Flow

```
[Platform Studio] 
    ↓
[Design Application] (Entities, Workflows, Pages, etc.)
    ↓
[Click "Publish"] 
    ↓
[Architecture Selector Modal]
    ├─→ [Monolithic] → Generate single ASP.NET Core app
    └─→ [Microservices] → Analyze & Decompose
                              ↓
                    [Service Boundary Designer]
                    (Visual canvas showing suggested services)
                              ↓
                    [Configure Infrastructure]
                    (API Gateway, Message Bus, Database per Service)
                              ↓
                    [Generate & Export]
```

---

## 4. Microservices Decomposition Strategy

### 4.1 Service Boundary Identification

The platform will use **Domain-Driven Design (DDD)** principles to identify service boundaries:

#### 4.1.1 Entity Clustering Algorithm
```
1. Build Entity Relationship Graph
2. Identify Aggregates (entities with strong relationships)
3. Group by Business Capability
4. Apply Bounded Context Rules
5. Suggest Service Boundaries
```

#### 4.1.2 Example Decomposition

**Sample Application**: E-Commerce Platform

**Entities**:
- Customer, Address, PaymentMethod
- Product, Category, Inventory
- Order, OrderItem, Payment
- Shipment, Tracking

**Suggested Services**:

1. **Customer Service**
   - Entities: Customer, Address, PaymentMethod
   - Responsibilities: Customer management, authentication
   - Database: CustomerDB

2. **Catalog Service**
   - Entities: Product, Category
   - Responsibilities: Product catalog, search
   - Database: CatalogDB

3. **Inventory Service**
   - Entities: Inventory
   - Responsibilities: Stock management
   - Database: InventoryDB

4. **Order Service**
   - Entities: Order, OrderItem
   - Responsibilities: Order processing, orchestration
   - Database: OrderDB

5. **Payment Service**
   - Entities: Payment
   - Responsibilities: Payment processing
   - Database: PaymentDB

6. **Shipping Service**
   - Entities: Shipment, Tracking
   - Responsibilities: Shipment management
   - Database: ShippingDB

### 4.2 Cross-Service Communication Patterns

#### 4.2.1 Synchronous Communication (REST/gRPC)
- **Use Case**: Real-time queries, simple request-response
- **Implementation**: Auto-generated API clients using Refit/gRPC
- **Pattern**: API Gateway routes external requests

#### 4.2.2 Asynchronous Communication (Message Bus)
- **Use Case**: Event-driven workflows, eventual consistency
- **Implementation**: MassTransit + RabbitMQ/Kafka
- **Pattern**: Publish-Subscribe, Saga Orchestration

### 4.3 Data Management Strategy

#### 4.3.1 Database Per Service Pattern
Each microservice gets its own database instance:
```
CustomerService → PostgreSQL (CustomerDB)
CatalogService  → PostgreSQL (CatalogDB)
OrderService    → PostgreSQL (OrderDB)
```

#### 4.3.2 Handling Cross-Service Queries
**Problem**: How to query data across services (e.g., "Get Orders with Customer Details")?

**Solutions**:
1. **API Composition**: Order Service calls Customer Service API
2. **CQRS + Read Models**: Materialized views in Order Service
3. **Event Sourcing**: Rebuild state from events

**Platform Approach**: Generate API composition by default, with option for CQRS

---

## 5. Generated Microservices Architecture

### 5.1 Infrastructure Components

```
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway (Ocelot/YARP)              │
│  - Routing, Load Balancing, Rate Limiting, Authentication   │
└─────────────────────────────────────────────────────────────┘
                              ↓
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   Customer   │      │   Catalog    │      │    Order     │
│   Service    │      │   Service    │      │   Service    │
│              │      │              │      │              │
│ - API        │      │ - API        │      │ - API        │
│ - Business   │      │ - Business   │      │ - Business   │
│ - Data       │      │ - Data       │      │ - Data       │
└──────────────┘      └──────────────┘      └──────────────┘
        ↓                     ↓                     ↓
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│ CustomerDB   │      │  CatalogDB   │      │   OrderDB    │
└──────────────┘      └──────────────┘      └──────────────┘
                              ↓
        ┌─────────────────────┴─────────────────────┐
        ↓                                           ↓
┌──────────────────┐                    ┌──────────────────┐
│  Message Bus     │                    │  Service         │
│  (RabbitMQ)      │                    │  Discovery       │
│                  │                    │  (Consul)        │
└──────────────────┘                    └──────────────────┘
```

### 5.2 Generated Project Structure

```
/GeneratedApp_Microservices
│
├── /src
│   ├── /Services
│   │   ├── /CustomerService
│   │   │   ├── /Controllers
│   │   │   ├── /Domain (Entities, Repositories)
│   │   │   ├── /Application (Business Logic)
│   │   │   ├── /Infrastructure (DbContext, Messaging)
│   │   │   ├── Program.cs
│   │   │   ├── Dockerfile
│   │   │   └── CustomerService.csproj
│   │   │
│   │   ├── /CatalogService
│   │   ├── /OrderService
│   │   └── ... (other services)
│   │
│   ├── /ApiGateway
│   │   ├── ocelot.json (routing configuration)
│   │   ├── Program.cs
│   │   └── ApiGateway.csproj
│   │
│   ├── /Shared
│   │   ├── /Contracts (Shared DTOs, Events)
│   │   ├── /Common (Utilities, Extensions)
│   │   └── Shared.csproj
│   │
│   └── /Frontend
│       └── /platform-studio-app (Angular SPA)
│
├── /infrastructure
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── /kubernetes
│   │   ├── namespace.yaml
│   │   ├── customer-service-deployment.yaml
│   │   ├── catalog-service-deployment.yaml
│   │   ├── api-gateway-deployment.yaml
│   │   ├── rabbitmq-deployment.yaml
│   │   └── ingress.yaml
│   │
│   └── /terraform (optional)
│       ├── main.tf
│       └── variables.tf
│
├── GeneratedApp.sln
└── README.md
```

---

## 6. Code Generation Enhancements

### 6.1 New Metadata: Architecture Configuration

```json
{
  "projectId": "guid",
  "name": "ECommerceApp",
  "architecture": {
    "type": "microservices",
    "serviceDecomposition": {
      "strategy": "auto",  // "auto" | "manual"
      "services": [
        {
          "serviceName": "CustomerService",
          "entities": ["Customer", "Address", "PaymentMethod"],
          "port": 5001,
          "database": {
            "type": "postgresql",
            "name": "CustomerDB"
          },
          "dependencies": []
        },
        {
          "serviceName": "OrderService",
          "entities": ["Order", "OrderItem"],
          "port": 5002,
          "database": {
            "type": "postgresql",
            "name": "OrderDB"
          },
          "dependencies": ["CustomerService", "CatalogService"]
        }
      ]
    },
    "infrastructure": {
      "apiGateway": {
        "enabled": true,
        "type": "ocelot",  // "ocelot" | "yarp"
        "port": 5000
      },
      "messageBus": {
        "enabled": true,
        "type": "rabbitmq",  // "rabbitmq" | "kafka"
        "host": "localhost",
        "port": 5672
      },
      "serviceDiscovery": {
        "enabled": false,
        "type": "consul"
      },
      "containerization": {
        "enabled": true,
        "orchestration": "docker-compose"  // "docker-compose" | "kubernetes"
      }
    }
  }
}
```

### 6.2 Enhanced Code Generators

#### 6.2.1 New Generators Required

1. **ServiceDecompositionAnalyzer**
   - Analyzes entity relationships
   - Suggests service boundaries
   - Validates decomposition rules

2. **MicroserviceProjectGenerator**
   - Creates individual service projects
   - Generates service-specific DbContext
   - Configures service dependencies

3. **ApiGatewayGenerator**
   - Generates Ocelot/YARP configuration
   - Creates routing rules
   - Configures authentication/authorization

4. **MessageContractGenerator**
   - Generates event/command classes
   - Creates MassTransit consumers
   - Configures message routing

5. **InfrastructureGenerator**
   - Generates docker-compose.yml
   - Creates Kubernetes manifests
   - Generates Terraform scripts

6. **ServiceClientGenerator**
   - Generates typed HTTP clients (Refit)
   - Creates gRPC clients
   - Implements circuit breaker patterns (Polly)

### 6.3 Template Structure

```
/Templates
├── /Microservices
│   ├── /Service
│   │   ├── Program.cs.scriban
│   │   ├── ServiceExtensions.cs.scriban
│   │   ├── Dockerfile.scriban
│   │   └── appsettings.json.scriban
│   │
│   ├── /ApiGateway
│   │   ├── ocelot.json.scriban
│   │   ├── Program.cs.scriban
│   │   └── Dockerfile.scriban
│   │
│   ├── /Messaging
│   │   ├── EventContract.cs.scriban
│   │   ├── EventConsumer.cs.scriban
│   │   └── MassTransitConfig.cs.scriban
│   │
│   └── /Infrastructure
│       ├── docker-compose.yml.scriban
│       ├── kubernetes-deployment.yaml.scriban
│       └── kubernetes-service.yaml.scriban
│
└── /Monolithic (existing templates)
```

---

## 7. Cross-Cutting Concerns

### 7.1 Distributed Transactions (Saga Pattern)

**Problem**: How to handle workflows that span multiple services?

**Example**: Order Creation Workflow
1. Create Order (Order Service)
2. Reserve Inventory (Inventory Service)
3. Process Payment (Payment Service)
4. Create Shipment (Shipping Service)

**Solution**: Generate Saga Orchestrator using MassTransit

```csharp
// Auto-generated Saga State Machine
public class OrderSaga : MassTransitStateMachine<OrderState>
{
    public OrderSaga()
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
                .TransitionTo(AwaitingPayment)
        );

        During(AwaitingPayment,
            When(PaymentProcessed)
                .Publish(context => new CreateShipment(context.Instance.OrderId))
                .TransitionTo(Completed)
        );
    }
}
```

**Platform Capability**: 
- Analyze Elsa workflows
- Detect cross-service activities
- Auto-generate Saga state machines

### 7.2 API Composition for Queries

**Problem**: Frontend needs "Order with Customer Details"

**Solution**: Generate Backend-for-Frontend (BFF) pattern

```csharp
// Auto-generated in API Gateway or dedicated BFF service
public class OrderQueryService
{
    private readonly IOrderServiceClient _orderClient;
    private readonly ICustomerServiceClient _customerClient;

    public async Task<OrderWithCustomerDto> GetOrderWithCustomer(Guid orderId)
    {
        var order = await _orderClient.GetOrder(orderId);
        var customer = await _customerClient.GetCustomer(order.CustomerId);

        return new OrderWithCustomerDto
        {
            OrderId = order.Id,
            OrderDate = order.CreatedAt,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            // ... compose data
        };
    }
}
```

### 7.3 Authentication & Authorization

**Centralized Auth Service**:
- Generate dedicated Identity Service
- JWT token issuance
- Shared secret for token validation across services

**Service-to-Service Auth**:
- Mutual TLS (mTLS)
- API Keys
- Service Mesh (Istio) - future enhancement

### 7.4 Observability

**Generated Observability Stack**:

1. **Distributed Tracing** (OpenTelemetry + Jaeger)
   ```csharp
   // Auto-injected in each service
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddJaegerExporter());
   ```

2. **Centralized Logging** (Serilog + Seq/ELK)
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .Enrich.WithProperty("ServiceName", "CustomerService")
       .WriteTo.Seq("http://seq:5341")
       .CreateLogger();
   ```

3. **Metrics** (Prometheus + Grafana)
   - Auto-expose `/metrics` endpoint
   - Generate Grafana dashboards

### 7.5 Resilience Patterns

**Auto-generated using Polly**:

```csharp
// Circuit Breaker for inter-service calls
services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>()
    .AddTransientHttpErrorPolicy(policy => 
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

---

## 8. Service Boundary Designer (UI Component)

### 8.1 Visual Canvas

A new component in Platform Studio that allows users to:

1. **View Auto-Suggested Services**
   - Visual graph showing entities grouped by service
   - Color-coded by bounded context

2. **Manually Adjust Boundaries**
   - Drag-and-drop entities between services
   - Real-time validation (e.g., "Warning: This creates circular dependency")

3. **Configure Service Properties**
   - Service name, port, database
   - Enable/disable features (caching, messaging)

4. **Visualize Dependencies**
   - Arrow showing service-to-service calls
   - Highlight potential bottlenecks

### 8.2 Mockup (Conceptual)

```
┌────────────────────────────────────────────────────────────┐
│  Service Boundary Designer                        [Export] │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────┐       ┌──────────────────┐         │
│  │ CustomerService  │       │  CatalogService  │         │
│  ├──────────────────┤       ├──────────────────┤         │
│  │ • Customer       │       │ • Product        │         │
│  │ • Address        │       │ • Category       │         │
│  │ • PaymentMethod  │       │                  │         │
│  └──────────────────┘       └──────────────────┘         │
│                                                            │
│  ┌──────────────────┐       ┌──────────────────┐         │
│  │  OrderService    │──────→│ PaymentService   │         │
│  ├──────────────────┤       ├──────────────────┤         │
│  │ • Order          │       │ • Payment        │         │
│  │ • OrderItem      │       │                  │         │
│  └──────────────────┘       └──────────────────┘         │
│         ↓                                                  │
│  [Depends on: CustomerService, CatalogService]            │
│                                                            │
│  [Add Service] [Auto-Optimize] [Validate]                 │
└────────────────────────────────────────────────────────────┘
```

---

## 9. Implementation Plan

### Phase 1: Foundation (Weeks 1-3)
**Goal**: Enable basic microservices generation

**Tasks**:
1. ✅ Design architecture metadata schema
2. ✅ Create Service Decomposition Analyzer
3. ✅ Build MicroserviceProjectGenerator
4. ✅ Generate basic service structure (Controllers, Entities, DbContext)
5. ✅ Generate docker-compose.yml for local development

**Deliverables**:
- Generate 2-3 services from sample project
- Each service runs independently
- Docker Compose orchestration

### Phase 2: Inter-Service Communication (Weeks 4-6)
**Goal**: Enable services to communicate

**Tasks**:
1. ✅ Implement ApiGatewayGenerator (Ocelot)
2. ✅ Generate typed HTTP clients (Refit)
3. ✅ Implement MessageContractGenerator (MassTransit)
4. ✅ Add resilience patterns (Polly)
5. ✅ Generate event-driven workflows

**Deliverables**:
- API Gateway routing requests
- Services calling each other via REST
- Asynchronous messaging for workflows

### Phase 3: Data & Transactions (Weeks 7-9)
**Goal**: Handle distributed data challenges

**Tasks**:
1. ✅ Implement Database-per-Service pattern
2. ✅ Generate Saga orchestrators for workflows
3. ✅ Implement API Composition for queries
4. ✅ Add eventual consistency patterns
5. ✅ Generate migration scripts per service

**Deliverables**:
- Each service has isolated database
- Distributed transactions via Sagas
- Cross-service queries working

### Phase 4: UI & Developer Experience (Weeks 10-12)
**Goal**: Make it easy for customers to use

**Tasks**:
1. ✅ Build Service Boundary Designer (Angular component)
2. ✅ Add architecture selection modal in publish flow
3. ✅ Implement auto-suggestion algorithm
4. ✅ Add validation and warnings
5. ✅ Generate comprehensive README per architecture

**Deliverables**:
- Visual service designer in Platform Studio
- One-click architecture selection
- Intelligent recommendations

### Phase 5: Production Readiness (Weeks 13-16)
**Goal**: Generate production-grade infrastructure

**Tasks**:
1. ✅ Generate Kubernetes manifests
2. ✅ Add observability stack (OpenTelemetry, Seq, Prometheus)
3. ✅ Implement centralized authentication
4. ✅ Add health checks and readiness probes
5. ✅ Generate CI/CD pipelines (GitHub Actions / Azure DevOps)
6. ✅ Create deployment documentation

**Deliverables**:
- K8s-ready microservices
- Full observability
- Automated deployment pipelines

### Phase 6: Advanced Features (Weeks 17-20)
**Goal**: Enterprise-grade capabilities

**Tasks**:
1. ✅ Service mesh integration (Istio/Linkerd)
2. ✅ CQRS pattern generation
3. ✅ Event Sourcing support
4. ✅ Multi-region deployment
5. ✅ Auto-scaling configuration

**Deliverables**:
- Advanced architectural patterns
- Cloud-native deployment options

---

## 10. Technical Specifications

### 10.1 Service Decomposition Algorithm

```csharp
public class ServiceDecompositionAnalyzer
{
    public List<ServiceBoundary> AnalyzeAndSuggest(ProjectMetadata project)
    {
        // Step 1: Build entity relationship graph
        var graph = BuildEntityGraph(project.Entities);

        // Step 2: Identify strongly connected components (aggregates)
        var aggregates = IdentifyAggregates(graph);

        // Step 3: Apply domain heuristics
        var boundedContexts = ApplyDomainHeuristics(aggregates, project);

        // Step 4: Optimize for service size (5-15 entities per service)
        var optimized = OptimizeServiceSize(boundedContexts);

        // Step 5: Validate (no circular dependencies, etc.)
        ValidateDecomposition(optimized);

        return optimized;
    }

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

    private List<Aggregate> IdentifyAggregates(EntityGraph graph)
    {
        // Use Tarjan's algorithm for strongly connected components
        // Entities with OneToMany/ManyToOne are grouped
        // ManyToMany relationships are weak boundaries
    }

    private List<BoundedContext> ApplyDomainHeuristics(
        List<Aggregate> aggregates, 
        ProjectMetadata project)
    {
        // Heuristic 1: Entity naming patterns (e.g., "Customer*" → CustomerService)
        // Heuristic 2: Workflow analysis (entities used together → same service)
        // Heuristic 3: Security boundaries (different RBAC → different service)
        // Heuristic 4: Scalability needs (high-traffic entities → isolated service)
    }
}
```

### 10.2 API Gateway Configuration Generation

```csharp
public class OcelotConfigGenerator
{
    public string GenerateConfiguration(List<ServiceBoundary> services)
    {
        var routes = new List<Route>();

        foreach (var service in services)
        {
            foreach (var entity in service.Entities)
            {
                routes.Add(new Route
                {
                    DownstreamPathTemplate = $"/api/{entity.Name.ToLower()}/{{everything}}",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new[]
                    {
                        new HostAndPort { Host = service.ServiceName.ToLower(), Port = service.Port }
                    },
                    UpstreamPathTemplate = $"/api/{entity.Name.ToLower()}/{{everything}}",
                    UpstreamHttpMethod = new[] { "GET", "POST", "PUT", "DELETE" }
                });
            }
        }

        return JsonSerializer.Serialize(new { Routes = routes }, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
}
```

### 10.3 Message Contract Generation

```csharp
// Template: EventContract.cs.scriban
public class {{ event_name }}
{
    public Guid CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    
    {{~ for property in properties ~}}
    public {{ property.type }} {{ property.name }} { get; set; }
    {{~ end ~}}
}

// Template: EventConsumer.cs.scriban
public class {{ event_name }}Consumer : IConsumer<{{ event_name }}>
{
    private readonly {{ service_name }}DbContext _context;

    public {{ event_name }}Consumer({{ service_name }}DbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<{{ event_name }}> context)
    {
        // Auto-generated handler logic based on workflow metadata
        {{ handler_logic }}
    }
}
```

---

## 11. Comparison: Monolith vs Microservices Output

### 11.1 Monolithic Output (Current)

```
/GeneratedApp
├── /Domain (All entities)
├── /Infrastructure (Single DbContext)
├── /API (All controllers)
├── /Frontend
├── Program.cs
└── appsettings.json
```

**Pros**:
- Simple deployment
- Easy local development
- No network latency
- Transactions are simple

**Cons**:
- Scales as a unit
- Tight coupling
- Large codebase
- Single point of failure

### 11.2 Microservices Output (New)

```
/GeneratedApp_Microservices
├── /Services
│   ├── /CustomerService (Independent deployment)
│   ├── /OrderService
│   └── /CatalogService
├── /ApiGateway
├── /Shared
├── /Infrastructure (Docker, K8s)
└── docker-compose.yml
```

**Pros**:
- Independent scaling
- Technology flexibility
- Team autonomy
- Fault isolation

**Cons**:
- Complex deployment
- Network overhead
- Distributed transactions
- Debugging challenges

---

## 12. Migration Path (Monolith → Microservices)

### 12.1 Strangler Fig Pattern

Allow customers to **start with monolith** and **gradually extract services**:

1. Generate monolith initially
2. Customer identifies service to extract (e.g., Payment)
3. Platform re-generates with Payment as separate service
4. API Gateway routes `/api/payment/*` to new service
5. Repeat for other services

### 12.2 Platform Support

```json
{
  "migrationStrategy": "strangler",
  "extractedServices": ["PaymentService"],
  "monolithServices": ["CustomerService", "OrderService", "CatalogService"]
}
```

Platform generates:
- Hybrid docker-compose (monolith + extracted services)
- API Gateway with smart routing
- Data synchronization scripts

---

## 13. Testing Strategy

### 13.1 Generated Tests

**Unit Tests** (per service):
```csharp
// Auto-generated for each entity
public class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_ShouldReturnCreatedCustomer()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new CustomerService(context);

        // Act
        var result = await service.CreateAsync(new Customer { Name = "John" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }
}
```

**Integration Tests** (cross-service):
```csharp
// Auto-generated for workflows
public class OrderWorkflowIntegrationTests
{
    [Fact]
    public async Task CreateOrder_ShouldReserveInventoryAndProcessPayment()
    {
        // Arrange: Start all services via TestContainers
        await using var environment = await MicroservicesTestEnvironment.CreateAsync();

        // Act: Submit order
        var orderId = await environment.OrderService.CreateOrderAsync(new Order());

        // Assert: Verify saga completed
        var order = await environment.OrderService.GetOrderAsync(orderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }
}
```

### 13.2 Contract Testing

Generate **Pact** contracts for inter-service communication:
```csharp
// Consumer test (OrderService expects CustomerService to return customer)
[Fact]
public async Task GetCustomer_ShouldReturnCustomerDetails()
{
    _mockProvider
        .Given("Customer exists")
        .UponReceiving("A request for customer details")
        .With(new ProviderServiceRequest
        {
            Method = HttpVerb.Get,
            Path = "/api/customers/123"
        })
        .WillRespondWith(new ProviderServiceResponse
        {
            Status = 200,
            Body = new { Id = "123", Name = "John Doe" }
        });

    var client = new CustomerServiceClient(_mockProvider.BaseUri);
    var customer = await client.GetCustomer("123");
    
    Assert.Equal("John Doe", customer.Name);
}
```

---

## 14. Documentation Generation

### 14.1 Architecture Decision Records (ADRs)

Auto-generate ADRs for each project:

```markdown
# ADR 001: Microservices Architecture

## Status
Accepted

## Context
The application has 35 entities across 4 bounded contexts with varying scalability needs.

## Decision
Decompose into 5 microservices: Customer, Catalog, Inventory, Order, Payment.

## Consequences
**Positive**:
- Independent scaling of high-traffic Catalog service
- Team autonomy for Order and Payment domains

**Negative**:
- Increased operational complexity
- Need for distributed transaction management
```

### 14.2 Service Documentation

Each service gets auto-generated README:

```markdown
# Customer Service

## Overview
Manages customer data, addresses, and payment methods.

## API Endpoints
- `GET /api/customers` - List all customers
- `POST /api/customers` - Create customer
- `GET /api/customers/{id}` - Get customer details

## Dependencies
- **Database**: PostgreSQL (CustomerDB)
- **Message Bus**: RabbitMQ
- **Depends On**: None (foundational service)

## Running Locally
```bash
docker-compose up customer-service
```

## Environment Variables
- `DATABASE_CONNECTION`: PostgreSQL connection string
- `RABBITMQ_HOST`: Message bus host
```

---

## 15. Future Enhancements

### 15.1 AI-Powered Decomposition
- Use LLM to analyze entity names and suggest domain boundaries
- Learn from user corrections to improve suggestions

### 15.2 Service Mesh Integration
- Auto-generate Istio/Linkerd configuration
- Traffic management, security, observability

### 15.3 Serverless Option
- Generate Azure Functions / AWS Lambda per service
- Event-driven, auto-scaling

### 15.4 Multi-Cloud Support
- Generate Terraform for AWS, Azure, GCP
- Cloud-agnostic abstractions

---

## 16. Success Metrics

### 16.1 Platform Metrics
- **Adoption Rate**: % of customers choosing microservices
- **Service Count**: Average number of services generated
- **Build Success Rate**: % of generated projects that compile successfully

### 16.2 Generated App Metrics
- **Performance**: API response times (monolith vs microservices)
- **Scalability**: Requests per second under load
- **Reliability**: Uptime, error rates

---

## 17. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Over-decomposition** | Too many services, operational overhead | Enforce minimum entities per service (5+) |
| **Circular dependencies** | Services can't deploy independently | Validation in Service Boundary Designer |
| **Data consistency issues** | Eventual consistency bugs | Generate comprehensive Saga tests |
| **Complexity for small apps** | Overkill for simple use cases | Recommend monolith for < 10 entities |
| **Learning curve** | Customers struggle with microservices | Generate extensive documentation + tutorials |

---

## 18. Conclusion

By enabling **dual-architecture generation**, DynamicPlatform becomes a truly enterprise-grade low-code platform. Customers can:

1. **Start Simple**: Prototype with monolith
2. **Scale Smart**: Migrate to microservices as needs grow
3. **Own Everything**: Full control over generated code
4. **Deploy Anywhere**: Docker, Kubernetes, Cloud

This positions the platform competitively against OutSystems, Mendix, and PowerApps, with a unique differentiator: **true architectural flexibility**.

---

## Appendix A: Technology Stack

### Core Technologies
- **Backend**: ASP.NET Core 9.0
- **API Gateway**: Ocelot / YARP
- **Messaging**: MassTransit + RabbitMQ
- **Databases**: PostgreSQL (per service)
- **Containerization**: Docker + Docker Compose
- **Orchestration**: Kubernetes
- **Observability**: OpenTelemetry, Seq, Prometheus, Grafana
- **Resilience**: Polly
- **Service Clients**: Refit / gRPC

### Infrastructure as Code
- **Local Dev**: Docker Compose
- **Production**: Kubernetes (Helm charts)
- **Cloud**: Terraform (multi-cloud)

---

## Appendix B: References

- [Microservices Patterns by Chris Richardson](https://microservices.io/patterns/)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Building Microservices by Sam Newman](https://www.oreilly.com/library/view/building-microservices-2nd/9781492034018/)
- [MassTransit Documentation](https://masstransit-project.com/)
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Author**: Platform Architecture Team  
**Status**: Draft for Review
