# API Composition Patterns - Implementation Guide

## Overview

This document provides **complete, production-ready implementation code** for all API composition patterns discussed in the Nested Entity Composition Patterns document.

---

## Pattern 1: API Gateway Aggregation (BFF)

### Complete Backend Implementation

```csharp
// ApiGateway/Controllers/OrderAggregationController.cs

[ApiController]
[Route("api/bff/orders")]
public class OrderAggregationController : ControllerBase
{
    private readonly IOrderServiceClient _orderService;
    private readonly ICustomerServiceClient _customerService;
    private readonly ICatalogServiceClient _catalogService;
    private readonly IMemoryCache _cache;

    [HttpGet("{orderId}/details")]
    public async Task<ActionResult<OrderDetailsDto>> GetOrderDetails(Guid orderId)
    {
        // Step 1: Get Order
        var order = await _orderService.GetOrderAsync(orderId);
        if (order == null) return NotFound();

        // Step 2: Parallel fetch
        var customerTask = FetchCustomerWithFallback(order.CustomerId);
        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var productsTask = FetchProductsWithFallback(productIds);

        await Task.WhenAll(customerTask, productsTask);

        // Step 3: Compose response
        return Ok(new OrderDetailsDto
        {
            OrderId = order.Id,
            Customer = MapCustomer(await customerTask),
            Items = MapItems(order.Items, await productsTask)
        });
    }
}
```

**Document Version**: 1.0  
**Last Updated**: 2026-02-02
