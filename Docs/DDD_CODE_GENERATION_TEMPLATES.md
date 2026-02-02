# DDD Code Generation Templates

## Overview

This document provides **Scriban templates** for generating DDD-compliant code from your platform's metadata. These templates ensure generated code follows Domain-Driven Design principles.

---

## 1. Entity Template

### 1.1 Template File: `Entity.scriban`

```scriban
{{~ ## Entity Template - Generates DDD Entity with behavior ~}}
using System;
using System.Collections.Generic;
using System.Linq;

namespace {{ namespace }}.Domain.Entities
{
    /// <summary>
    /// {{ entity.description }}
    /// </summary>
    public class {{ entity.name }}
    {
        #region Properties

        {{~ for property in entity.properties ~}}
        {{~ if property.is_identity ~}}
        /// <summary>
        /// Unique identifier for {{ entity.name }}
        /// </summary>
        public {{ property.type }} {{ property.name }} { get; private set; }
        {{~ else if property.is_collection ~}}
        
        private List<{{ property.element_type }}> _{{ property.name | string.downcase }} = new();
        /// <summary>
        /// {{ property.description }}
        /// </summary>
        public IReadOnlyCollection<{{ property.element_type }}> {{ property.name }} => _{{ property.name | string.downcase }}.AsReadOnly();
        {{~ else ~}}
        
        /// <summary>
        /// {{ property.description }}
        /// </summary>
        public {{ property.type }} {{ property.name }} { get; private set; }
        {{~ end ~}}
        {{~ end ~}}

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new {{ entity.name }}
        /// </summary>
        public {{ entity.name }}({{~ for param in entity.constructor_parameters ~}}{{ param.type }} {{ param.name | string.downcase }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{~ for param in entity.constructor_parameters ~}}
            {{~ if param.is_required ~}}
            {{ param.name }} = {{ param.name | string.downcase }} ?? throw new ArgumentNullException(nameof({{ param.name | string.downcase }}));
            {{~ else ~}}
            {{ param.name }} = {{ param.name | string.downcase }};
            {{~ end ~}}
            {{~ end ~}}
            
            {{~ if entity.has_identity ~}}
            {{ entity.identity_property }} = Guid.NewGuid();
            {{~ end ~}}
            
            {{~ for init in entity.initializations ~}}
            {{ init.property }} = {{ init.value }};
            {{~ end ~}}
        }

        #endregion

        #region Business Methods

        {{~ for method in entity.methods ~}}
        /// <summary>
        /// {{ method.description }}
        /// </summary>
        public {{ method.return_type }} {{ method.name }}({{~ for param in method.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{~ for validation in method.validations ~}}
            if ({{ validation.condition }})
                throw new {{ validation.exception_type }}("{{ validation.message }}");
            {{~ end ~}}
            
            {{~ if method.body ~}}
            {{ method.body }}
            {{~ end ~}}
            
            {{~ if method.raises_event ~}}
            // Raise {{ method.event_name }}
            RaiseDomainEvent(new {{ method.event_name }}
            {
                {{~ for prop in method.event_properties ~}}
                {{ prop.name }} = {{ prop.value }},
                {{~ end ~}}
            });
            {{~ end ~}}
            
            {{~ if method.return_type != "void" ~}}
            return {{ method.return_value }};
            {{~ end ~}}
        }
        
        {{~ end ~}}

        #endregion

        #region Domain Events

        private List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        #endregion
    }
}
```

### 1.2 Metadata Example

```json
{
  "entity": {
    "name": "Order",
    "namespace": "ECommerce",
    "description": "Represents a customer order",
    "has_identity": true,
    "identity_property": "OrderId",
    "properties": [
      {
        "name": "OrderId",
        "type": "Guid",
        "is_identity": true,
        "description": "Unique identifier"
      },
      {
        "name": "OrderNumber",
        "type": "string",
        "description": "Human-readable order number"
      },
      {
        "name": "CustomerId",
        "type": "Guid",
        "description": "Reference to customer"
      },
      {
        "name": "Items",
        "type": "List<OrderItem>",
        "element_type": "OrderItem",
        "is_collection": true,
        "description": "Order line items"
      },
      {
        "name": "Status",
        "type": "OrderStatus",
        "description": "Current order status"
      },
      {
        "name": "TotalAmount",
        "type": "Money",
        "description": "Total order amount"
      }
    ],
    "constructor_parameters": [
      {
        "name": "CustomerId",
        "type": "Guid",
        "is_required": true
      },
      {
        "name": "ShippingAddress",
        "type": "Address",
        "is_required": true
      }
    ],
    "initializations": [
      {
        "property": "Status",
        "value": "OrderStatus.Draft"
      },
      {
        "property": "TotalAmount",
        "value": "new Money(0, \"USD\")"
      }
    ],
    "methods": [
      {
        "name": "AddItem",
        "return_type": "void",
        "description": "Adds an item to the order",
        "parameters": [
          {
            "name": "productId",
            "type": "Guid"
          },
          {
            "name": "quantity",
            "type": "int"
          },
          {
            "name": "unitPrice",
            "type": "Money"
          }
        ],
        "validations": [
          {
            "condition": "Status != OrderStatus.Draft",
            "exception_type": "InvalidOperationException",
            "message": "Cannot modify placed order"
          },
          {
            "condition": "quantity <= 0",
            "exception_type": "ArgumentException",
            "message": "Quantity must be positive"
          }
        ],
        "body": "var item = new OrderItem(OrderId, productId, quantity, unitPrice);\n_items.Add(item);\nRecalculateTotal();"
      },
      {
        "name": "PlaceOrder",
        "return_type": "void",
        "description": "Places the order",
        "parameters": [],
        "validations": [
          {
            "condition": "Status != OrderStatus.Draft",
            "exception_type": "InvalidOperationException",
            "message": "Order already placed"
          },
          {
            "condition": "!_items.Any()",
            "exception_type": "InvalidOperationException",
            "message": "Cannot place empty order"
          }
        ],
        "body": "Status = OrderStatus.Placed;",
        "raises_event": true,
        "event_name": "OrderPlacedEvent",
        "event_properties": [
          {
            "name": "OrderId",
            "value": "OrderId"
          },
          {
            "name": "CustomerId",
            "value": "CustomerId"
          },
          {
            "name": "TotalAmount",
            "value": "TotalAmount.Amount"
          }
        ]
      }
    ]
  }
}
```

---

## 2. Value Object Template

### 2.1 Template File: `ValueObject.scriban`

```scriban
{{~ ## Value Object Template - Immutable, equality by values ~}}
using System;

namespace {{ namespace }}.Domain.ValueObjects
{
    /// <summary>
    /// {{ value_object.description }}
    /// </summary>
    public class {{ value_object.name }}
    {
        #region Properties

        {{~ for property in value_object.properties ~}}
        /// <summary>
        /// {{ property.description }}
        /// </summary>
        public {{ property.type }} {{ property.name }} { get; private set; }
        
        {{~ end ~}}
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new {{ value_object.name }}
        /// </summary>
        public {{ value_object.name }}({{~ for param in value_object.properties ~}}{{ param.type }} {{ param.name | string.downcase }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{~ for validation in value_object.validations ~}}
            if ({{ validation.condition }})
                throw new {{ validation.exception_type }}("{{ validation.message }}");
            {{~ end ~}}
            
            {{~ for property in value_object.properties ~}}
            {{ property.name }} = {{ property.name | string.downcase }};
            {{~ end ~}}
        }

        #endregion

        #region Methods

        {{~ for method in value_object.methods ~}}
        /// <summary>
        /// {{ method.description }}
        /// </summary>
        public {{ method.return_type }} {{ method.name }}({{~ for param in method.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{ method.body }}
        }
        
        {{~ end ~}}
        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is not {{ value_object.name }} other) return false;
            
            return {{~ for property in value_object.properties ~}}{{ property.name }} == other.{{ property.name }}{{ if !for.last }} &&
                   {{ end }}{{~ end ~}};
        }

        public override int GetHashCode()
        {
            return HashCode.Combine({{~ for property in value_object.properties ~}}{{ property.name }}{{ if !for.last }}, {{ end }}{{~ end ~}});
        }

        public static bool operator ==({{ value_object.name }} left, {{ value_object.name }} right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=({{ value_object.name }} left, {{ value_object.name }} right)
        {
            return !(left == right);
        }

        #endregion
    }
}
```

### 2.2 Metadata Example

```json
{
  "value_object": {
    "name": "Money",
    "namespace": "ECommerce",
    "description": "Represents a monetary amount with currency",
    "properties": [
      {
        "name": "Amount",
        "type": "decimal",
        "description": "The monetary amount"
      },
      {
        "name": "Currency",
        "type": "string",
        "description": "Currency code (e.g., USD, EUR)"
      }
    ],
    "validations": [
      {
        "condition": "amount < 0",
        "exception_type": "ArgumentException",
        "message": "Amount cannot be negative"
      },
      {
        "condition": "string.IsNullOrWhiteSpace(currency)",
        "exception_type": "ArgumentException",
        "message": "Currency is required"
      }
    ],
    "methods": [
      {
        "name": "Add",
        "return_type": "Money",
        "description": "Adds two money amounts",
        "parameters": [
          {
            "name": "other",
            "type": "Money"
          }
        ],
        "body": "if (Currency != other.Currency)\n    throw new InvalidOperationException(\"Cannot add different currencies\");\n\nreturn new Money(Amount + other.Amount, Currency);"
      }
    ]
  }
}
```

---

## 3. Repository Template

### 3.1 Template File: `Repository.scriban`

```scriban
{{~ ## Repository Template - Data access for aggregate roots ~}}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace {{ namespace }}.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for {{ aggregate.name }} aggregate
    /// </summary>
    public interface I{{ aggregate.name }}Repository
    {
        Task<{{ aggregate.name }}> GetByIdAsync({{ aggregate.id_type }} id);
        {{~ for query in aggregate.queries ~}}
        Task<{{ query.return_type }}> {{ query.name }}({{~ for param in query.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}});
        {{~ end ~}}
        Task AddAsync({{ aggregate.name }} {{ aggregate.name | string.downcase }});
        Task UpdateAsync({{ aggregate.name }} {{ aggregate.name | string.downcase }});
        Task DeleteAsync({{ aggregate.id_type }} id);
    }

    public class {{ aggregate.name }}Repository : I{{ aggregate.name }}Repository
    {
        private readonly {{ db_context_name }} _context;

        public {{ aggregate.name }}Repository({{ db_context_name }} context)
        {
            _context = context;
        }

        public async Task<{{ aggregate.name }}> GetByIdAsync({{ aggregate.id_type }} id)
        {
            return await _context.{{ aggregate.plural_name }}
                {{~ for include in aggregate.includes ~}}
                .Include(x => x.{{ include }})
                {{~ end ~}}
                .FirstOrDefaultAsync(x => x.{{ aggregate.id_property }} == id);
        }

        {{~ for query in aggregate.queries ~}}
        public async Task<{{ query.return_type }}> {{ query.name }}({{~ for param in query.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{ query.implementation }}
        }
        
        {{~ end ~}}

        public async Task AddAsync({{ aggregate.name }} {{ aggregate.name | string.downcase }})
        {
            await _context.{{ aggregate.plural_name }}.AddAsync({{ aggregate.name | string.downcase }});
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync({{ aggregate.name }} {{ aggregate.name | string.downcase }})
        {
            _context.{{ aggregate.plural_name }}.Update({{ aggregate.name | string.downcase }});
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync({{ aggregate.id_type }} id)
        {
            var {{ aggregate.name | string.downcase }} = await GetByIdAsync(id);
            if ({{ aggregate.name | string.downcase }} != null)
            {
                _context.{{ aggregate.plural_name }}.Remove({{ aggregate.name | string.downcase }});
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

---

## 4. Domain Service Template

### 4.1 Template File: `DomainService.scriban`

```scriban
{{~ ## Domain Service Template - Cross-aggregate operations ~}}
using System;
using System.Threading.Tasks;

namespace {{ namespace }}.Domain.Services
{
    /// <summary>
    /// {{ service.description }}
    /// </summary>
    public interface I{{ service.name }}
    {
        {{~ for method in service.methods ~}}
        Task<{{ method.return_type }}> {{ method.name }}({{~ for param in method.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}});
        {{~ end ~}}
    }

    public class {{ service.name }} : I{{ service.name }}
    {
        {{~ for dependency in service.dependencies ~}}
        private readonly {{ dependency.type }} {{ dependency.name }};
        {{~ end ~}}

        public {{ service.name }}({{~ for dependency in service.dependencies ~}}{{ dependency.type }} {{ dependency.name }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{~ for dependency in service.dependencies ~}}
            {{ dependency.name }} = {{ dependency.name }};
            {{~ end ~}}
        }

        {{~ for method in service.methods ~}}
        public async Task<{{ method.return_type }}> {{ method.name }}({{~ for param in method.parameters ~}}{{ param.type }} {{ param.name }}{{ if !for.last }}, {{ end }}{{~ end ~}})
        {
            {{ method.implementation }}
        }
        
        {{~ end ~}}
    }
}
```

---

## 5. Domain Event Template

### 5.1 Template File: `DomainEvent.scriban`

```scriban
{{~ ## Domain Event Template - Something that happened ~}}
using System;

namespace {{ namespace }}.Domain.Events
{
    /// <summary>
    /// {{ event.description }}
    /// </summary>
    public record {{ event.name }} : IDomainEvent
    {
        {{~ for property in event.properties ~}}
        /// <summary>
        /// {{ property.description }}
        /// </summary>
        public {{ property.type }} {{ property.name }} { get; init; }
        
        {{~ end ~}}
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
```

---

## 6. Complete Generation Example

### 6.1 Input Metadata (JSON)

```json
{
  "domain": "ECommerce",
  "bounded_context": "Order",
  "aggregates": [
    {
      "name": "Order",
      "is_aggregate_root": true,
      "entities": [
        {
          "name": "Order",
          "properties": [
            { "name": "OrderId", "type": "Guid", "is_identity": true },
            { "name": "CustomerId", "type": "Guid" },
            { "name": "Status", "type": "OrderStatus" },
            { "name": "TotalAmount", "type": "Money" }
          ],
          "methods": [
            {
              "name": "PlaceOrder",
              "validations": ["Status == Draft", "Items.Any()"],
              "raises_event": "OrderPlacedEvent"
            }
          ]
        },
        {
          "name": "OrderItem",
          "properties": [
            { "name": "ProductId", "type": "Guid" },
            { "name": "Quantity", "type": "int" },
            { "name": "UnitPrice", "type": "Money" }
          ]
        }
      ],
      "value_objects": [
        {
          "name": "Money",
          "properties": [
            { "name": "Amount", "type": "decimal" },
            { "name": "Currency", "type": "string" }
          ]
        }
      ],
      "domain_events": [
        {
          "name": "OrderPlacedEvent",
          "properties": [
            { "name": "OrderId", "type": "Guid" },
            { "name": "CustomerId", "type": "Guid" },
            { "name": "TotalAmount", "type": "decimal" }
          ]
        }
      ]
    }
  ]
}
```

### 6.2 Generated Code Structure

```
/ECommerce.Order
├── /Domain
│   ├── /Entities
│   │   ├── Order.cs (Generated from Entity template)
│   │   └── OrderItem.cs
│   ├── /ValueObjects
│   │   └── Money.cs (Generated from ValueObject template)
│   ├── /Events
│   │   └── OrderPlacedEvent.cs (Generated from DomainEvent template)
│   └── /Services
│       └── PricingService.cs (Generated from DomainService template)
├── /Infrastructure
│   └── /Repositories
│       └── OrderRepository.cs (Generated from Repository template)
└── /Application
    └── /Commands
        └── PlaceOrderCommand.cs
```

---

## 7. Generator Service

### 7.1 C# Code Generator

```csharp
public class DddCodeGenerator
{
    private readonly ITemplateEngine _templateEngine;
    
    public async Task<GeneratedCode> GenerateAggregateAsync(AggregateMetadata metadata)
    {
        var generatedFiles = new List<GeneratedFile>();
        
        // Generate entities
        foreach (var entity in metadata.Entities)
        {
            var code = await _templateEngine.RenderAsync("Entity.scriban", new { entity });
            generatedFiles.Add(new GeneratedFile
            {
                Path = $"Domain/Entities/{entity.Name}.cs",
                Content = code
            });
        }
        
        // Generate value objects
        foreach (var vo in metadata.ValueObjects)
        {
            var code = await _templateEngine.RenderAsync("ValueObject.scriban", new { value_object = vo });
            generatedFiles.Add(new GeneratedFile
            {
                Path = $"Domain/ValueObjects/{vo.Name}.cs",
                Content = code
            });
        }
        
        // Generate repository
        var repoCode = await _templateEngine.RenderAsync("Repository.scriban", new { aggregate = metadata });
        generatedFiles.Add(new GeneratedFile
        {
            Path = $"Infrastructure/Repositories/{metadata.Name}Repository.cs",
            Content = repoCode
        });
        
        // Generate domain events
        foreach (var evt in metadata.DomainEvents)
        {
            var code = await _templateEngine.RenderAsync("DomainEvent.scriban", new { @event = evt });
            generatedFiles.Add(new GeneratedFile
            {
                Path = $"Domain/Events/{evt.Name}.cs",
                Content = code
            });
        }
        
        return new GeneratedCode { Files = generatedFiles };
    }
}
```

---

## 8. Usage in Platform

### 8.1 Integration with Entity Designer

```typescript
// platform-studio/src/app/services/code-generation.service.ts

export class CodeGenerationService {
  generateDddCode(entities: Entity[]): Promise<GeneratedCode> {
    // Convert Entity Designer metadata to DDD metadata
    const dddMetadata = this.convertToDddMetadata(entities);
    
    // Call backend generator
    return this.http.post<GeneratedCode>('/api/codegen/ddd', dddMetadata).toPromise();
  }
  
  private convertToDddMetadata(entities: Entity[]): DddMetadata {
    return {
      aggregates: entities
        .filter(e => e.isAggregateRoot)
        .map(e => ({
          name: e.name,
          entities: this.getAggregateEntities(e, entities),
          valueObjects: this.extractValueObjects(e),
          domainEvents: this.extractDomainEvents(e)
        }))
    };
  }
}
```

---

## Summary

These templates ensure:

✅ **Entities have behavior**, not just data  
✅ **Value objects are immutable**  
✅ **Aggregates enforce invariants**  
✅ **Repositories work with aggregate roots**  
✅ **Domain events capture important occurrences**  
✅ **Domain services handle cross-aggregate logic**  

**Document Version**: 1.0  
**Last Updated**: 2026-02-02
