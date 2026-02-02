# Nested Entity Composition Patterns for Microservices

## 1. Executive Summary

This document addresses the **complex challenge of nested entity composition** when decomposing a monolithic application into microservices. It identifies edge cases, provides API patterns, and offers solutions for maintaining data consistency and query performance across service boundaries.

**Key Challenges**:
- Deep entity hierarchies spanning multiple services
- Circular references across service boundaries
- Aggregate consistency in distributed systems
- Query performance for nested data
- Transaction boundaries

---

## 2. Problem Statement

### 2.1 Monolithic Entity Composition (Current State)

In a monolithic application, entities can be freely composed:

```csharp
// Monolithic approach - Single database, easy joins
public class Order
{
    public Guid Id { get; set; }
    public Customer Customer { get; set; }          // Navigation property
    public List<OrderItem> Items { get; set; }      // Collection
    public Address ShippingAddress { get; set; }    // Nested entity
    public Payment Payment { get; set; }            // Related entity
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Product Product { get; set; }            // Navigation to Product
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Category Category { get; set; }          // Nested entity
    public Supplier Supplier { get; set; }          // Related entity
    public List<ProductImage> Images { get; set; }  // Collection
}

// Single query to get everything
var order = dbContext.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Category)
    .Include(o => o.ShippingAddress)
    .Include(o => o.Payment)
    .FirstOrDefault(o => o.Id == orderId);
```

### 2.2 Microservices Challenge (Target State)

After decomposition, entities are split across services:

```
OrderService:
  - Order
  - OrderItem
  - ShippingAddress (value object)

CustomerService:
  - Customer
  - CustomerAddress

CatalogService:
  - Product
  - Category
  - ProductImage

PaymentService:
  - Payment
  - PaymentMethod

SupplierService:
  - Supplier
```

**Problem**: How do we reconstruct the nested `Order` object when data is distributed?

---

## 3. Complex Scenarios & Edge Cases

### Scenario 1: Deep Nested Hierarchies

**Example**: Order → OrderItem → Product → Category → ParentCategory (5 levels deep)

```
Order (OrderService)
  ├── Customer (CustomerService) ❌ Cross-service
  ├── OrderItems (OrderService)
  │   └── Product (CatalogService) ❌ Cross-service
  │       ├── Category (CatalogService)
  │       │   └── ParentCategory (CatalogService)
  │       └── Supplier (SupplierService) ❌ Cross-service
  ├── ShippingAddress (OrderService)
  └── Payment (PaymentService) ❌ Cross-service
```

**Challenges**:
- Multiple cross-service calls (N+1 problem)
- Latency accumulation (5 services × 50ms = 250ms)
- Partial failure handling
- Data consistency

**Edge Cases**:
1. What if CatalogService is down when fetching Order?
2. What if Product was deleted after Order was created?
3. How to handle pagination for nested collections?

---

### Scenario 2: Circular References

**Example**: Customer ↔ Order ↔ Product ↔ Supplier ↔ Product

```csharp
// Monolithic circular reference
public class Customer
{
    public List<Order> Orders { get; set; }  // Customer has Orders
}

public class Order
{
    public Customer Customer { get; set; }    // Order belongs to Customer
    public List<OrderItem> Items { get; set; }
}

public class Product
{
    public Supplier Supplier { get; set; }    // Product has Supplier
    public List<OrderItem> OrderItems { get; set; }  // Product in many Orders
}

public class Supplier
{
    public List<Product> Products { get; set; }  // Supplier has Products
}
```

**Challenges**:
- Infinite loops in API composition
- Serialization issues (JSON circular reference)
- Cache invalidation complexity

**Edge Cases**:
1. Customer → Orders → Products → Supplier → Products (infinite loop)
2. How deep should we traverse?
3. How to represent in DTOs without circular refs?

---

### Scenario 3: Aggregate Boundaries Spanning Services

**Example**: Order aggregate includes OrderItems, but Product is in another service

```
Aggregate: Order
  ├── OrderId (Aggregate Root)
  ├── OrderItems (Part of aggregate)
  │   ├── ProductId (Reference to CatalogService) ❌
  │   ├── ProductName (Denormalized)
  │   └── UnitPrice (Denormalized)
  └── TotalAmount (Calculated)
```

**Challenges**:
- Maintaining aggregate consistency
- Denormalization vs. real-time queries
- Handling product price changes
- Eventual consistency

**Edge Cases**:
1. Product price changes after order is placed
2. Product is deleted but exists in historical orders
3. Product name changes (do we update old orders?)

---

### Scenario 4: Many-to-Many Relationships Across Services

**Example**: Order ↔ Promotion (many-to-many)

```
OrderService:
  - Order
  - OrderPromotion (join table)

PromotionService:
  - Promotion
  - PromotionRule
```

**Challenges**:
- Where does the join table live?
- How to query orders by promotion?
- How to ensure referential integrity?

**Edge Cases**:
1. Promotion deleted while orders reference it
2. Promotion rules change (retroactive application?)
3. Concurrent updates to join table from both services

---

### Scenario 5: Polymorphic Relationships

**Example**: Payment can be CreditCard, PayPal, or BankTransfer

```csharp
// Monolithic polymorphic relationship
public abstract class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
}

public class CreditCardPayment : Payment
{
    public string CardNumber { get; set; }
    public string CVV { get; set; }
}

public class PayPalPayment : Payment
{
    public string PayPalEmail { get; set; }
}

public class Order
{
    public Payment Payment { get; set; }  // Can be any subtype
}
```

**Challenges**:
- How to represent polymorphism across services?
- Type discrimination in DTOs
- Different validation rules per type

**Edge Cases**:
1. New payment type added (extensibility)
2. Payment type changes after order creation
3. Querying orders by payment type

---

### Scenario 6: Temporal Queries (Historical Data)

**Example**: "Show me the order as it was on 2025-01-15"

```
Order (created 2025-01-10)
  ├── Customer (name changed on 2025-01-20)
  ├── Product (price changed on 2025-01-18)
  └── ShippingAddress (customer moved on 2025-02-01)
```

**Challenges**:
- Point-in-time consistency across services
- Event sourcing vs. snapshots
- Storage overhead

**Edge Cases**:
1. Service doesn't support temporal queries
2. Data was purged (GDPR)
3. Clock skew between services

---

### Scenario 7: Soft Deletes Across Services

**Example**: Customer soft-deleted, but Orders still reference them

```
CustomerService:
  - Customer (IsDeleted = true)

OrderService:
  - Order (CustomerId = <deleted-customer-id>)
```

**Challenges**:
- Should deleted customers appear in order details?
- Cascade soft deletes?
- Restore scenarios

**Edge Cases**:
1. Customer deleted, then order is queried
2. Customer restored, historical orders?
3. GDPR "right to be forgotten" (hard delete)

---

### Scenario 8: Conditional Nested Loading

**Example**: Load Product details only if Order.Status = "Pending"

```
If Order.Status == "Pending":
  Load full Product details (name, price, stock)
Else:
  Load minimal Product details (name only)
```

**Challenges**:
- Dynamic query composition
- Conditional service calls
- Performance optimization

**Edge Cases**:
1. Status changes mid-query
2. Different users need different detail levels
3. Caching conditional responses

---

## 4. API Composition Patterns

### Pattern 1: API Gateway Aggregation (BFF)

**Use Case**: Frontend needs complete Order with nested entities

```csharp
// API Gateway / BFF
[HttpGet("orders/{id}/details")]
public async Task<OrderDetailsDto> GetOrderDetails(Guid id)
{
    // Step 1: Get Order from OrderService
    var order = await _orderServiceClient.GetOrderAsync(id);
    
    // Step 2: Get Customer from CustomerService (parallel)
    var customerTask = _customerServiceClient.GetCustomerAsync(order.CustomerId);
    
    // Step 3: Get Products for all OrderItems (parallel)
    var productIds = order.Items.Select(i => i.ProductId).ToList();
    var productsTask = _catalogServiceClient.GetProductsByIdsAsync(productIds);
    
    // Step 4: Get Payment from PaymentService (parallel)
    var paymentTask = _paymentServiceClient.GetPaymentAsync(order.PaymentId);
    
    // Wait for all parallel calls
    await Task.WhenAll(customerTask, productsTask, paymentTask);
    
    // Step 5: Compose response
    return new OrderDetailsDto
    {
        OrderId = order.Id,
        OrderDate = order.CreatedAt,
        Status = order.Status,
        Customer = new CustomerSummaryDto
        {
            Id = customerTask.Result.Id,
            Name = customerTask.Result.Name,
            Email = customerTask.Result.Email
        },
        Items = order.Items.Select(item =>
        {
            var product = productsTask.Result.FirstOrDefault(p => p.Id == item.ProductId);
            return new OrderItemDetailsDto
            {
                ProductId = item.ProductId,
                ProductName = product?.Name ?? item.ProductName, // Fallback to denormalized
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Category = product?.Category?.Name
            };
        }).ToList(),
        Payment = new PaymentSummaryDto
        {
            Amount = paymentTask.Result.Amount,
            Method = paymentTask.Result.Method,
            Status = paymentTask.Result.Status
        },
        TotalAmount = order.TotalAmount
    };
}
```

**Pros**:
- Single API call for frontend
- Parallel service calls
- Centralized composition logic

**Cons**:
- BFF becomes complex
- Tight coupling to frontend needs
- Difficult to cache

---

### Pattern 2: GraphQL Federation

**Use Case**: Flexible querying with schema stitching

```graphql
# OrderService schema
type Order {
  id: ID!
  customerId: ID!
  customer: Customer @provides(fields: "id")
  items: [OrderItem!]!
  totalAmount: Float!
}

type OrderItem {
  productId: ID!
  product: Product @provides(fields: "id")
  quantity: Int!
  unitPrice: Float!
}

# CustomerService schema
type Customer {
  id: ID!
  name: String!
  email: String!
  orders: [Order!]! @provides(fields: "customerId")
}

# CatalogService schema
type Product {
  id: ID!
  name: String!
  price: Float!
  category: Category
}

# Client query (federated)
query GetOrderDetails($orderId: ID!) {
  order(id: $orderId) {
    id
    orderDate
    customer {
      name
      email
    }
    items {
      product {
        name
        category {
          name
        }
      }
      quantity
      unitPrice
    }
    totalAmount
  }
}
```

**Pros**:
- Flexible client queries
- Schema stitching handles composition
- Automatic batching/caching

**Cons**:
- Complex setup
- N+1 query problem
- Learning curve

---

### Pattern 3: Data Denormalization

**Use Case**: Avoid cross-service calls for read-heavy scenarios

```csharp
// OrderService - Denormalized Order
public class Order
{
    public Guid Id { get; set; }
    
    // Denormalized customer data
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }      // Snapshot
    public string CustomerEmail { get; set; }     // Snapshot
    
    public List<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }       // Snapshot
    public decimal UnitPrice { get; set; }        // Snapshot at order time
    public int Quantity { get; set; }
}

// Event handler to keep denormalized data updated
public class CustomerUpdatedEventHandler : IConsumer<CustomerUpdatedEvent>
{
    public async Task Consume(ConsumeContext<CustomerUpdatedEvent> context)
    {
        var customerId = context.Message.CustomerId;
        var newName = context.Message.Name;
        
        // Update all orders for this customer
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        foreach (var order in orders)
        {
            order.CustomerName = newName;
        }
        await _orderRepository.SaveChangesAsync();
    }
}
```

**Pros**:
- No cross-service calls
- Fast reads
- Simple queries

**Cons**:
- Data duplication
- Eventual consistency
- Sync complexity

---

### Pattern 4: CQRS with Read Models

**Use Case**: Optimized read models for complex queries

```csharp
// Write Model (OrderService)
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
}

// Read Model (Separate database/service)
public class OrderDetailsReadModel
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public List<OrderItemDetail> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; }
    
    // Flattened, denormalized, optimized for reads
}

public class OrderItemDetail
{
    public string ProductName { get; set; }
    public string CategoryName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

// Projection builder (subscribes to events)
public class OrderDetailsProjection
{
    public async Task Handle(OrderCreatedEvent evt)
    {
        var customer = await _customerServiceClient.GetCustomerAsync(evt.CustomerId);
        var products = await _catalogServiceClient.GetProductsByIdsAsync(evt.ProductIds);
        
        var readModel = new OrderDetailsReadModel
        {
            OrderId = evt.OrderId,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            Items = evt.Items.Select(item =>
            {
                var product = products.First(p => p.Id == item.ProductId);
                return new OrderItemDetail
                {
                    ProductName = product.Name,
                    CategoryName = product.Category.Name,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                };
            }).ToList()
        };
        
        await _readModelRepository.SaveAsync(readModel);
    }
}

// Query (fast, no joins)
[HttpGet("orders/{id}/details")]
public async Task<OrderDetailsReadModel> GetOrderDetails(Guid id)
{
    return await _readModelRepository.GetByIdAsync(id);
}
```

**Pros**:
- Extremely fast reads
- No cross-service calls
- Optimized for specific queries

**Cons**:
- Eventual consistency
- Complexity
- Storage overhead

---

### Pattern 5: Lazy Loading with Caching

**Use Case**: Load nested data on-demand with caching

```csharp
public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    
    [JsonIgnore]
    private CustomerDto _customer;
    
    public CustomerDto Customer
    {
        get
        {
            if (_customer == null)
            {
                _customer = _cache.GetOrAdd($"customer:{CustomerId}", () =>
                    _customerServiceClient.GetCustomerAsync(CustomerId).Result
                );
            }
            return _customer;
        }
    }
    
    public List<OrderItemDto> Items { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    
    [JsonIgnore]
    private ProductDto _product;
    
    public ProductDto Product
    {
        get
        {
            if (_product == null)
            {
                _product = _cache.GetOrAdd($"product:{ProductId}", () =>
                    _catalogServiceClient.GetProductAsync(ProductId).Result
                );
            }
            return _product;
        }
    }
}
```

**Pros**:
- Load only what's needed
- Caching reduces calls
- Flexible

**Cons**:
- Lazy loading can cause N+1
- Cache invalidation complexity
- Blocking calls

---

## 5. Handling Edge Cases

### Edge Case 1: Service Unavailable

**Problem**: CatalogService is down, can't load Product details

**Solution 1: Graceful Degradation**
```csharp
try
{
    var product = await _catalogServiceClient.GetProductAsync(productId);
    return new OrderItemDto
    {
        ProductName = product.Name,
        ProductPrice = product.Price
    };
}
catch (HttpRequestException)
{
    // Fallback to denormalized data
    return new OrderItemDto
    {
        ProductName = orderItem.ProductNameSnapshot,
        ProductPrice = orderItem.UnitPriceSnapshot,
        IsStale = true  // Indicate data might be outdated
    };
}
```

**Solution 2: Circuit Breaker**
```csharp
var product = await _catalogServiceClient
    .WithCircuitBreaker(
        exceptionsAllowed: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    )
    .GetProductAsync(productId);
```

---

### Edge Case 2: Deleted Referenced Entity

**Problem**: Product deleted, but Order still references it

**Solution: Soft Delete + Tombstone**
```csharp
// CatalogService
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// API returns deleted products with limited data
[HttpGet("products/{id}")]
public async Task<ProductDto> GetProduct(Guid id)
{
    var product = await _repository.GetByIdAsync(id, includeDeleted: true);
    
    if (product == null)
        throw new NotFoundException();
    
    if (product.IsDeleted)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = "[Deleted Product]",
            IsDeleted = true
        };
    }
    
    return _mapper.Map<ProductDto>(product);
}
```

---

### Edge Case 3: Circular Reference Serialization

**Problem**: Customer → Orders → Customer (infinite loop)

**Solution: DTO Projection with Depth Limit**
```csharp
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<OrderSummaryDto> RecentOrders { get; set; }  // Summary only
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    // No Customer property to avoid circular reference
}

public class OrderDetailsDto
{
    public Guid Id { get; set; }
    public CustomerSummaryDto Customer { get; set; }  // Summary only
    public List<OrderItemDto> Items { get; set; }
}

public class CustomerSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // No Orders property
}
```

---

### Edge Case 4: Eventual Consistency Window

**Problem**: Customer name updated, but Order still shows old name

**Solution: Versioning + Timestamps**
```csharp
public class OrderDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public DateTime CustomerNameAsOf { get; set; }  // Timestamp
    
    public bool IsCustomerNameStale()
    {
        return DateTime.UtcNow - CustomerNameAsOf > TimeSpan.FromMinutes(5);
    }
}

// UI can show warning
if (order.IsCustomerNameStale())
{
    // Show: "Customer information may be outdated. Refresh?"
}
```

---

## 6. Platform Generation Strategy

### 6.1 Automatic Pattern Selection

```csharp
public class CompositionPatternSelector
{
    public CompositionPattern SelectPattern(
        ServiceBoundary sourceService,
        ServiceBoundary targetService,
        EntityRelation relation)
    {
        // Rule 1: Same aggregate → Include in same service
        if (relation.IsPartOfAggregate)
            return CompositionPattern.SameService;
        
        // Rule 2: Read-heavy → Denormalize
        if (relation.ReadWriteRatio > 10)
            return CompositionPattern.Denormalize;
        
        // Rule 3: Complex queries → CQRS
        if (relation.QueryComplexity > 7)
            return CompositionPattern.CQRS;
        
        // Rule 4: Real-time data → API Composition
        if (relation.RequiresRealTimeData)
            return CompositionPattern.APIComposition;
        
        // Default: Lazy loading with cache
        return CompositionPattern.LazyLoadWithCache;
    }
}
```

### 6.2 Generated Code Examples

**For Denormalization:**
```csharp
// Auto-generated event handler
public class CustomerUpdatedEventHandler : IConsumer<CustomerUpdatedEvent>
{
    private readonly IOrderRepository _orderRepository;
    
    public async Task Consume(ConsumeContext<CustomerUpdatedEvent> context)
    {
        var orders = await _orderRepository
            .GetByCustomerIdAsync(context.Message.CustomerId);
        
        foreach (var order in orders)
        {
            order.CustomerName = context.Message.Name;
            order.CustomerEmail = context.Message.Email;
        }
        
        await _orderRepository.SaveChangesAsync();
    }
}
```

**For API Composition:**
```csharp
// Auto-generated BFF endpoint
[HttpGet("orders/{id}/details")]
public async Task<OrderDetailsDto> GetOrderDetails(Guid id)
{
    var order = await _orderService.GetOrderAsync(id);
    
    var tasks = new[]
    {
        _customerService.GetCustomerAsync(order.CustomerId),
        _catalogService.GetProductsByIdsAsync(order.Items.Select(i => i.ProductId)),
        _paymentService.GetPaymentAsync(order.PaymentId)
    };
    
    await Task.WhenAll(tasks);
    
    return ComposeOrderDetails(order, tasks);
}
```

---

## 7. Decision Matrix

| Scenario | Pattern | Pros | Cons | Use When |
|----------|---------|------|------|----------|
| **Simple 1-level nesting** | API Composition | Simple, real-time | Latency | < 3 services |
| **Deep nesting (3+ levels)** | CQRS Read Model | Fast reads | Complexity | Complex queries |
| **High read/write ratio** | Denormalization | No cross-calls | Sync overhead | Read-heavy |
| **Real-time requirements** | API Composition | Fresh data | Latency | Stock prices |
| **Historical queries** | Event Sourcing | Point-in-time | Storage | Audit trails |
| **Circular references** | DTO Projection | Avoids loops | Manual mapping | Customer ↔ Orders |
| **Soft deletes** | Tombstone Pattern | Preserves refs | Complexity | Compliance |
| **Polymorphism** | Type Discriminator | Flexible | Coupling | Payment types |

---

## 8. Recommendations

### For Platform Generation:

1. **Analyze Relationship Depth**: Warn if nesting > 3 levels
2. **Detect Circular References**: Auto-break with summary DTOs
3. **Suggest Denormalization**: For read-heavy entities (read/write > 10:1)
4. **Generate CQRS**: For complex aggregations
5. **Add Circuit Breakers**: For all cross-service calls
6. **Create Fallback DTOs**: With denormalized snapshots
7. **Implement Caching**: Redis for frequently accessed data
8. **Generate Health Checks**: Monitor service dependencies

### For Developers:

1. **Prefer Denormalization**: For historical/immutable data
2. **Use API Composition**: For real-time data
3. **Implement CQRS**: For complex read scenarios
4. **Add Graceful Degradation**: Always have fallbacks
5. **Monitor Performance**: Track cross-service call latency
6. **Document Patterns**: Make decisions explicit

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Status**: Ready for Review
