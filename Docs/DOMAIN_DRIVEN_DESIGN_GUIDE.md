# Domain-Driven Design (DDD) - Complete Beginner's Guide

## Table of Contents

1. [Introduction](#1-introduction)
2. [Core Concepts](#2-core-concepts)
3. [Strategic Design](#3-strategic-design)
4. [Tactical Design](#4-tactical-design)
5. [Practical Examples](#5-practical-examples)
6. [DDD for Microservices](#6-ddd-for-microservices)
7. [Common Pitfalls](#7-common-pitfalls)
8. [Implementation Checklist](#8-implementation-checklist)

---

## 1. Introduction

### 1.1 What is Domain-Driven Design?

**Domain-Driven Design (DDD)** is an approach to software development that focuses on:
- Understanding the **business domain** deeply
- Creating a **shared language** between developers and domain experts
- Modeling software to reflect **real-world business concepts**

**Created by**: Eric Evans (2003)  
**Goal**: Build software that solves real business problems effectively

---

### 1.2 Why DDD Matters for Your Platform

Your low-code platform generates applications for various domains (e-commerce, CRM, inventory, etc.). DDD helps:

✅ **Identify service boundaries** for microservices  
✅ **Model entities correctly** (Customer, Order, Product)  
✅ **Understand relationships** (aggregates, value objects)  
✅ **Avoid common mistakes** (anemic domain models, god objects)  

---

### 1.3 DDD in 60 Seconds

**Traditional Approach**:
```
Developer: "We need a Customer table with ID, Name, Email."
Business: "But customers have different types, loyalty tiers, credit limits..."
Developer: "We'll add those columns later."
Result: Messy database, complex logic scattered everywhere
```

**DDD Approach**:
```
Developer: "Tell me about customers in your business."
Business: "We have retail customers and corporate customers. 
          Retail customers earn loyalty points. 
          Corporate customers have credit terms."
Developer: "So we need different customer types with specific behaviors."
Result: Clean model that reflects business reality
```

---

## 2. Core Concepts

### 2.1 Ubiquitous Language

**Definition**: A shared vocabulary between developers and domain experts.

**Example - E-commerce Domain**:

| Business Term | Technical Term | Ubiquitous Language |
|---------------|----------------|---------------------|
| "Shopping Cart" | `List<Item>` | **ShoppingCart** |
| "Checkout" | `ProcessOrder()` | **PlaceOrder** |
| "Out of Stock" | `quantity == 0` | **ProductUnavailable** |

**Why It Matters**:
- Reduces miscommunication
- Code reads like business requirements
- Easier to maintain

**Bad Example** (No Ubiquitous Language):
```csharp
public class Data1
{
    public int Id { get; set; }
    public string Field1 { get; set; }
    public decimal Value { get; set; }
    
    public void Process()
    {
        // What does this do?
    }
}
```

**Good Example** (Ubiquitous Language):
```csharp
public class Order
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    
    public void PlaceOrder()
    {
        // Clear what this does
    }
}
```

---

### 2.2 Bounded Context

**Definition**: A boundary within which a particular domain model is valid.

**Analogy**: The word "Account" means different things in different contexts:
- **Banking Context**: Savings account, checking account
- **Social Media Context**: User profile
- **Accounting Context**: Ledger account

**Example - E-commerce Platform**:

```
┌─────────────────────────────────────────────────────────────┐
│                    E-commerce Platform                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ Catalog Context  │  │ Order Context    │               │
│  ├──────────────────┤  ├──────────────────┤               │
│  │ - Product        │  │ - Order          │               │
│  │ - Category       │  │ - OrderItem      │               │
│  │ - Inventory      │  │ - Customer       │               │
│  │ - Price          │  │ - ShippingAddr   │               │
│  └──────────────────┘  └──────────────────┘               │
│                                                             │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ Payment Context  │  │ Shipping Context │               │
│  ├──────────────────┤  ├──────────────────┤               │
│  │ - Payment        │  │ - Shipment       │               │
│  │ - Transaction    │  │ - Carrier        │               │
│  │ - Invoice        │  │ - TrackingInfo   │               │
│  └──────────────────┘  └──────────────────┘               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Key Point**: "Product" in Catalog Context is different from "Product" in Order Context:

```csharp
// Catalog Context - Product
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal CurrentPrice { get; set; }
    public int StockQuantity { get; set; }
    public Category Category { get; set; }
    
    public void UpdatePrice(decimal newPrice) { }
    public void AdjustInventory(int quantity) { }
}

// Order Context - Product (snapshot)
public class OrderedProduct
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal PriceAtOrderTime { get; set; }
    // No inventory, no category - not needed here
}
```

**Why Different?**
- Catalog needs current price and inventory
- Order needs historical price (at time of purchase)
- Each context has different concerns

---

## 3. Strategic Design

### 3.1 Context Mapping

**Definition**: How bounded contexts relate to each other.

**Relationship Types**:

#### 3.1.1 Shared Kernel
Two contexts share a common model.

**Example**:
```csharp
// Shared between Order and Payment contexts
public class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}
```

#### 3.1.2 Customer-Supplier
One context (supplier) provides data to another (customer).

**Example**:
```
Catalog Context (Supplier) → Order Context (Customer)
Catalog provides product information to Order
```

#### 3.1.3 Conformist
Customer context conforms to supplier's model.

**Example**:
```
Payment Context conforms to external Payment Gateway API
```

#### 3.1.4 Anti-Corruption Layer (ACL)
Translate between contexts to prevent corruption.

**Example**:
```csharp
// External Payment Gateway returns different model
public class PaymentGatewayAdapter
{
    private readonly IExternalPaymentGateway _gateway;
    
    public Payment ProcessPayment(Order order)
    {
        // Translate our Order to gateway's format
        var gatewayRequest = new GatewayPaymentRequest
        {
            amount = order.TotalAmount.Amount,
            currency = order.TotalAmount.Currency,
            merchant_id = "12345"
        };
        
        var gatewayResponse = _gateway.Charge(gatewayRequest);
        
        // Translate gateway response to our Payment model
        return new Payment
        {
            Id = Guid.NewGuid(),
            Amount = new Money 
            { 
                Amount = gatewayResponse.charged_amount,
                Currency = gatewayResponse.currency_code
            },
            Status = MapStatus(gatewayResponse.status),
            TransactionId = gatewayResponse.transaction_id
        };
    }
}
```

---

## 4. Tactical Design

### 4.1 Entities

**Definition**: Objects with unique identity that persists over time.

**Characteristics**:
- Has a unique ID
- Identity matters more than attributes
- Can change over time

**Example**:

```csharp
public class Customer
{
    public Guid CustomerId { get; private set; }  // Identity
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public Address ShippingAddress { get; private set; }
    public CustomerStatus Status { get; private set; }
    
    // Constructor
    public Customer(Guid id, string name, Email email)
    {
        CustomerId = id;
        Name = name;
        Email = email;
        Status = CustomerStatus.Active;
    }
    
    // Behavior
    public void UpdateShippingAddress(Address newAddress)
    {
        if (newAddress == null)
            throw new ArgumentNullException(nameof(newAddress));
            
        ShippingAddress = newAddress;
    }
    
    public void Deactivate()
    {
        Status = CustomerStatus.Inactive;
    }
}
```

**Why Entity?**
- Customer with ID "123" is the same customer even if name changes
- Identity (CustomerId) is what matters

**Real-World Analogy**: You are an entity. Even if you change your name, address, or appearance, you're still the same person (same identity).

---

### 4.2 Value Objects

**Definition**: Objects defined by their attributes, not identity.

**Characteristics**:
- No unique ID
- Immutable (cannot change)
- Equality based on values
- Can be replaced

**Example**:

```csharp
public class Address
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }
    
    public Address(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required");
            
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }
    
    // Equality based on values
    public override bool Equals(object obj)
    {
        if (obj is not Address other) return false;
        
        return Street == other.Street &&
               City == other.City &&
               State == other.State &&
               ZipCode == other.ZipCode &&
               Country == other.Country;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Street, City, State, ZipCode, Country);
    }
}

public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
            
        Amount = amount;
        Currency = currency;
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
}
```

**Why Value Object?**
- Two addresses with same values are identical
- No need for ID
- Can create new instance instead of modifying

**Real-World Analogy**: A $20 bill. You don't care which specific $20 bill you have, just that it's worth $20.

---

### 4.3 Aggregates

**Definition**: A cluster of entities and value objects treated as a single unit.

**Characteristics**:
- Has one **Aggregate Root** (entry point)
- Enforces consistency boundaries
- External objects can only reference the root

**Example - Order Aggregate**:

```csharp
// Aggregate Root
public class Order
{
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    
    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; }
    
    // Constructor
    public Order(Guid customerId, Address shippingAddress)
    {
        OrderId = Guid.NewGuid();
        OrderNumber = GenerateOrderNumber();
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Draft;
        TotalAmount = new Money(0, "USD");
    }
    
    // Add item (only through aggregate root)
    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify placed order");
            
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var newItem = new OrderItem(OrderId, productId, productName, unitPrice, quantity);
            _items.Add(newItem);
        }
        
        RecalculateTotal();
    }
    
    // Remove item
    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify placed order");
            
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }
    
    // Place order (state transition)
    public void PlaceOrder()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order already placed");
            
        if (!_items.Any())
            throw new InvalidOperationException("Cannot place empty order");
            
        Status = OrderStatus.Placed;
        // Publish OrderPlacedEvent
    }
    
    private void RecalculateTotal()
    {
        var total = _items.Sum(i => i.Subtotal.Amount);
        TotalAmount = new Money(total, "USD");
    }
    
    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}

// Entity within aggregate (not aggregate root)
public class OrderItem
{
    public Guid OrderItemId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money Subtotal { get; private set; }
    
    internal OrderItem(Guid orderId, Guid productId, string productName, Money unitPrice, int quantity)
    {
        OrderItemId = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Subtotal = new Money(unitPrice.Amount * quantity, unitPrice.Currency);
    }
    
    internal void IncreaseQuantity(int additionalQuantity)
    {
        Quantity += additionalQuantity;
        Subtotal = new Money(UnitPrice.Amount * Quantity, UnitPrice.Currency);
    }
}
```

**Key Rules**:
1. ✅ External code can only access `Order` (aggregate root)
2. ✅ Cannot directly create or modify `OrderItem` from outside
3. ✅ All changes go through `Order` methods
4. ✅ `Order` ensures consistency (e.g., can't modify placed order)

**Why Aggregate?**
- Ensures business rules are enforced
- Prevents invalid states
- Clear transaction boundary

**Visual Representation**:

```
┌─────────────────────────────────────────┐
│         Order (Aggregate Root)          │
│  ┌───────────────────────────────────┐  │
│  │ - OrderId                         │  │
│  │ - OrderNumber                     │  │
│  │ - CustomerId                      │  │
│  │ - Status                          │  │
│  │ - TotalAmount                     │  │
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │      OrderItem (Entity)           │  │
│  │  - ProductId                      │  │
│  │  - Quantity                       │  │
│  │  - UnitPrice                      │  │
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │   ShippingAddress (Value Object)  │  │
│  │  - Street, City, State, Zip       │  │
│  └───────────────────────────────────┘  │
│                                         │
└─────────────────────────────────────────┘
     ↑
     │ Only access through root
     │
External Code
```

---

### 4.4 Domain Services

**Definition**: Operations that don't naturally belong to an entity or value object.

**When to Use**:
- Operation involves multiple aggregates
- Operation is stateless
- Operation represents a business process

**Example**:

```csharp
public interface IPricingService
{
    Money CalculateOrderTotal(Order order, Customer customer);
}

public class PricingService : IPricingService
{
    public Money CalculateOrderTotal(Order order, Customer customer)
    {
        var subtotal = order.Items.Sum(i => i.Subtotal.Amount);
        
        // Apply customer-specific discount
        var discount = customer.LoyaltyTier switch
        {
            LoyaltyTier.Gold => 0.10m,
            LoyaltyTier.Silver => 0.05m,
            _ => 0m
        };
        
        var discountAmount = subtotal * discount;
        var total = subtotal - discountAmount;
        
        return new Money(total, "USD");
    }
}
```

**Why Domain Service?**
- Pricing logic involves both Order and Customer
- Doesn't belong to either entity
- Represents business operation

---

### 4.5 Repositories

**Definition**: Abstraction for accessing aggregates from storage.

**Characteristics**:
- One repository per aggregate root
- Hides persistence details
- Returns fully-formed aggregates

**Example**:

```csharp
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(Guid orderId);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid orderId);
}

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order> GetByIdAsync(Guid orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)  // Load entire aggregate
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }
    
    public async Task<List<Order>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }
    
    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Guid orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}
```

**Key Points**:
- Repository works with `Order` (aggregate root), not `OrderItem`
- Always loads complete aggregate (including items)
- Hides EF Core details from domain

---

### 4.6 Domain Events

**Definition**: Something that happened in the domain that domain experts care about.

**Example**:

```csharp
public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
}

public class Order
{
    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void PlaceOrder()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order already placed");
            
        Status = OrderStatus.Placed;
        
        // Raise domain event
        _domainEvents.Add(new OrderPlacedEvent
        {
            OrderId = OrderId,
            CustomerId = CustomerId,
            TotalAmount = TotalAmount.Amount,
            PlacedAt = DateTime.UtcNow
        });
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// Event handler
public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;
    
    public async Task Handle(OrderPlacedEvent evt, CancellationToken cancellationToken)
    {
        // Send confirmation email
        await _emailService.SendOrderConfirmationAsync(evt.CustomerId, evt.OrderId);
        
        // Reserve inventory
        await _inventoryService.ReserveInventoryAsync(evt.OrderId);
    }
}
```

---

## 5. Practical Examples

### 5.1 E-commerce Domain Model

Let's build a complete e-commerce domain model using DDD principles.

#### 5.1.1 Identify Bounded Contexts

```
E-commerce Platform
├── Catalog Context (Product management)
├── Order Context (Order processing)
├── Customer Context (Customer management)
├── Payment Context (Payment processing)
└── Shipping Context (Fulfillment)
```

#### 5.1.2 Define Aggregates

**Catalog Context**:

```csharp
// Product Aggregate
public class Product
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public ProductCategory Category { get; private set; }
    public int StockQuantity { get; private set; }
    
    private List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    
    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new ArgumentException("Price must be positive");
            
        Price = newPrice;
        // Raise PriceChangedEvent
    }
    
    public void AdjustInventory(int quantity, string reason)
    {
        var newQuantity = StockQuantity + quantity;
        
        if (newQuantity < 0)
            throw new InvalidOperationException("Insufficient inventory");
            
        StockQuantity = newQuantity;
        // Raise InventoryAdjustedEvent
    }
    
    public bool IsAvailable()
    {
        return StockQuantity > 0;
    }
}

public class ProductImage
{
    public Guid ImageId { get; private set; }
    public string Url { get; private set; }
    public string AltText { get; private set; }
    public int DisplayOrder { get; private set; }
}
```

**Customer Context**:

```csharp
// Customer Aggregate
public class Customer
{
    public Guid CustomerId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public LoyaltyTier LoyaltyTier { get; private set; }
    public int LoyaltyPoints { get; private set; }
    
    private List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    
    public void AddAddress(Address address)
    {
        if (_addresses.Count >= 5)
            throw new InvalidOperationException("Maximum 5 addresses allowed");
            
        _addresses.Add(address);
    }
    
    public void EarnLoyaltyPoints(int points)
    {
        LoyaltyPoints += points;
        UpdateLoyaltyTier();
    }
    
    private void UpdateLoyaltyTier()
    {
        LoyaltyTier = LoyaltyPoints switch
        {
            >= 10000 => LoyaltyTier.Gold,
            >= 5000 => LoyaltyTier.Silver,
            >= 1000 => LoyaltyTier.Bronze,
            _ => LoyaltyTier.Standard
        };
    }
}

public class Email
{
    public string Value { get; private set; }
    
    public Email(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid email format");
            
        Value = value;
    }
    
    private bool IsValid(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}
```

---

### 5.2 Banking Domain Model

```csharp
// Account Aggregate
public class BankAccount
{
    public Guid AccountId { get; private set; }
    public string AccountNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    
    private List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();
    
    public void Deposit(Money amount, string description)
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");
            
        if (amount.Amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");
            
        Balance = Balance.Add(amount);
        
        var transaction = new Transaction(
            AccountId,
            TransactionType.Deposit,
            amount,
            description
        );
        
        _transactions.Add(transaction);
        
        // Raise DepositedEvent
    }
    
    public void Withdraw(Money amount, string description)
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");
            
        if (amount.Amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive");
            
        if (Balance.Amount < amount.Amount)
            throw new InvalidOperationException("Insufficient funds");
            
        Balance = new Money(Balance.Amount - amount.Amount, Balance.Currency);
        
        var transaction = new Transaction(
            AccountId,
            TransactionType.Withdrawal,
            amount,
            description
        );
        
        _transactions.Add(transaction);
        
        // Raise WithdrawnEvent
    }
    
    public void Close()
    {
        if (Balance.Amount != 0)
            throw new InvalidOperationException("Cannot close account with non-zero balance");
            
        Status = AccountStatus.Closed;
    }
}

public class Transaction
{
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    internal Transaction(Guid accountId, TransactionType type, Money amount, string description)
    {
        TransactionId = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
}

// Domain Service for transfers
public class TransferService
{
    private readonly IBankAccountRepository _accountRepository;
    
    public async Task TransferAsync(Guid fromAccountId, Guid toAccountId, Money amount)
    {
        var fromAccount = await _accountRepository.GetByIdAsync(fromAccountId);
        var toAccount = await _accountRepository.GetByIdAsync(toAccountId);
        
        if (fromAccount == null || toAccount == null)
            throw new NotFoundException("Account not found");
            
        // Withdraw from source
        fromAccount.Withdraw(amount, $"Transfer to {toAccount.AccountNumber}");
        
        // Deposit to destination
        toAccount.Deposit(amount, $"Transfer from {fromAccount.AccountNumber}");
        
        await _accountRepository.UpdateAsync(fromAccount);
        await _accountRepository.UpdateAsync(toAccount);
    }
}
```

---

## 6. DDD for Microservices

### 6.1 Bounded Context = Microservice

**Rule of Thumb**: Each bounded context becomes a microservice.

**Example**:

```
E-commerce Platform
├── Catalog Service (Catalog Context)
├── Order Service (Order Context)
├── Customer Service (Customer Context)
├── Payment Service (Payment Context)
└── Shipping Service (Shipping Context)
```

### 6.2 Aggregate = Transaction Boundary

**Rule**: One transaction = One aggregate

**Example**:

```csharp
// ✅ GOOD: Single aggregate, single transaction
public async Task PlaceOrderAsync(Guid orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.PlaceOrder();
    await _orderRepository.UpdateAsync(order);
}

// ❌ BAD: Multiple aggregates, single transaction (tight coupling)
public async Task PlaceOrderAsync(Guid orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
    var product = await _productRepository.GetByIdAsync(order.Items.First().ProductId);
    
    order.PlaceOrder();
    customer.EarnLoyaltyPoints(100);
    product.AdjustInventory(-1, "Order placed");
    
    // All in one transaction - tight coupling!
}
```

**Solution**: Use **Saga** or **Domain Events** for cross-aggregate operations.

---

## 7. Common Pitfalls

### 7.1 Anemic Domain Model

**Problem**: Entities with only getters/setters, no behavior.

**❌ Bad Example**:

```csharp
public class Order
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
}

// Business logic in service
public class OrderService
{
    public void PlaceOrder(Order order)
    {
        if (order.TotalAmount <= 0)
            throw new Exception("Invalid amount");
            
        order.Status = "Placed";
    }
}
```

**✅ Good Example**:

```csharp
public class Order
{
    public Guid OrderId { get; private set; }
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public void PlaceOrder()
    {
        if (TotalAmount.Amount <= 0)
            throw new InvalidOperationException("Invalid amount");
            
        Status = OrderStatus.Placed;
    }
}
```

---

### 7.2 God Objects

**Problem**: One entity does everything.

**❌ Bad Example**:

```csharp
public class Order
{
    // Order data
    public Guid OrderId { get; set; }
    
    // Customer data (should be separate)
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    
    // Payment data (should be separate)
    public string CreditCardNumber { get; set; }
    public string CVV { get; set; }
    
    // Shipping data (should be separate)
    public string CarrierName { get; set; }
    public string TrackingNumber { get; set; }
    
    // Too many responsibilities!
}
```

**✅ Good Example**:

```csharp
public class Order
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }  // Reference only
    public Guid? PaymentId { get; private set; }  // Reference only
    public Guid? ShipmentId { get; private set; } // Reference only
}
```

---

### 7.3 Ignoring Invariants

**Problem**: Allowing invalid states.

**❌ Bad Example**:

```csharp
public class Order
{
    public List<OrderItem> Items { get; set; }
    
    // Anyone can add invalid items!
}

// External code
order.Items.Add(new OrderItem { Quantity = -5 }); // Invalid!
```

**✅ Good Example**:

```csharp
public class Order
{
    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    public void AddItem(Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
            
        _items.Add(new OrderItem(productId, quantity));
    }
}
```

---

## 8. Implementation Checklist

### 8.1 For Your Low-Code Platform

When generating code, ensure:

✅ **Entities have behavior**, not just data  
✅ **Value objects are immutable**  
✅ **Aggregates enforce invariants**  
✅ **One repository per aggregate root**  
✅ **Domain events for cross-aggregate operations**  
✅ **Bounded contexts map to microservices**  
✅ **Ubiquitous language in code**  

### 8.2 DDD Checklist

- [ ] Identified bounded contexts
- [ ] Defined ubiquitous language
- [ ] Identified aggregates and roots
- [ ] Separated entities from value objects
- [ ] Created repositories for aggregate roots
- [ ] Implemented domain services for cross-aggregate logic
- [ ] Used domain events for side effects
- [ ] Enforced invariants in aggregates
- [ ] Avoided anemic domain models
- [ ] Documented context map

---

## 9. Quick Reference

### Entity vs Value Object

| Aspect | Entity | Value Object |
|--------|--------|--------------|
| **Identity** | Has unique ID | No ID |
| **Equality** | By ID | By values |
| **Mutability** | Mutable | Immutable |
| **Example** | Customer, Order | Address, Money |

### Aggregate Rules

1. Reference other aggregates by ID only
2. One transaction = One aggregate
3. Enforce invariants within aggregate
4. Use domain events for cross-aggregate operations

### Repository Pattern

- One repository per aggregate root
- Return fully-formed aggregates
- Hide persistence details

---

## 10. Further Learning

**Books**:
- "Domain-Driven Design" by Eric Evans (The Blue Book)
- "Implementing Domain-Driven Design" by Vaughn Vernon (The Red Book)
- "Domain-Driven Design Distilled" by Vaughn Vernon (Quick intro)

**Online Resources**:
- https://martinfowler.com/tags/domain%20driven%20design.html
- https://www.domainlanguage.com/ddd/

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-02  
**Author**: Platform Architecture Team  
**Status**: Complete Beginner's Guide
