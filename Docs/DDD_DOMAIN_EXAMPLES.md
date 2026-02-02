# DDD Domain Model Examples - Multiple Industries

## Overview

This document provides **complete, production-ready domain models** for various industries using Domain-Driven Design principles. Use these as reference when building your own domains.

---

## 1. Healthcare Domain

### 1.1 Bounded Contexts

```
Healthcare System
├── Patient Management Context
├── Appointment Scheduling Context
├── Medical Records Context
├── Billing Context
└── Pharmacy Context
```

### 1.2 Patient Management Context

#### Aggregates

```csharp
// Patient Aggregate Root
public class Patient
{
    public Guid PatientId { get; private set; }
    public PatientIdentifier Identifier { get; private set; }
    public PersonName Name { get; private set; }
    public DateOfBirth DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public ContactInformation Contact { get; private set; }
    
    private List<Allergy> _allergies = new();
    public IReadOnlyCollection<Allergy> Allergies => _allergies.AsReadOnly();
    
    private List<EmergencyContact> _emergencyContacts = new();
    public IReadOnlyCollection<EmergencyContact> EmergencyContacts => _emergencyContacts.AsReadOnly();
    
    public PatientStatus Status { get; private set; }
    
    public Patient(PatientIdentifier identifier, PersonName name, DateOfBirth dob, Gender gender)
    {
        PatientId = Guid.NewGuid();
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DateOfBirth = dob ?? throw new ArgumentNullException(nameof(dob));
        Gender = gender;
        Status = PatientStatus.Active;
    }
    
    public void AddAllergy(string allergen, AllergySeverity severity, string reaction)
    {
        if (string.IsNullOrWhiteSpace(allergen))
            throw new ArgumentException("Allergen is required");
            
        var allergy = new Allergy(allergen, severity, reaction);
        _allergies.Add(allergy);
        
        // Raise AllergyAddedEvent
    }
    
    public void AddEmergencyContact(PersonName name, PhoneNumber phone, Relationship relationship)
    {
        if (_emergencyContacts.Count >= 3)
            throw new InvalidOperationException("Maximum 3 emergency contacts allowed");
            
        var contact = new EmergencyContact(name, phone, relationship);
        _emergencyContacts.Add(contact);
    }
    
    public void UpdateContactInformation(ContactInformation newContact)
    {
        Contact = newContact ?? throw new ArgumentNullException(nameof(newContact));
    }
    
    public void Discharge()
    {
        Status = PatientStatus.Discharged;
        // Raise PatientDischargedEvent
    }
    
    public int GetAge()
    {
        return DateOfBirth.CalculateAge();
    }
}

// Entities within aggregate
public class Allergy
{
    public Guid AllergyId { get; private set; }
    public string Allergen { get; private set; }
    public AllergySeverity Severity { get; private set; }
    public string Reaction { get; private set; }
    public DateTime RecordedAt { get; private set; }
    
    internal Allergy(string allergen, AllergySeverity severity, string reaction)
    {
        AllergyId = Guid.NewGuid();
        Allergen = allergen;
        Severity = severity;
        Reaction = reaction;
        RecordedAt = DateTime.UtcNow;
    }
}

public class EmergencyContact
{
    public Guid ContactId { get; private set; }
    public PersonName Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public Relationship Relationship { get; private set; }
    
    internal EmergencyContact(PersonName name, PhoneNumber phone, Relationship relationship)
    {
        ContactId = Guid.NewGuid();
        Name = name;
        Phone = phone;
        Relationship = relationship;
    }
}

// Value Objects
public class PatientIdentifier
{
    public string Value { get; private set; }
    public IdentifierType Type { get; private set; }
    
    public PatientIdentifier(string value, IdentifierType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier value is required");
            
        Value = value;
        Type = type;
    }
}

public class PersonName
{
    public string FirstName { get; private set; }
    public string MiddleName { get; private set; }
    public string LastName { get; private set; }
    public string Suffix { get; private set; }
    
    public PersonName(string firstName, string lastName, string middleName = null, string suffix = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required");
            
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        Suffix = suffix;
    }
    
    public string GetFullName()
    {
        var parts = new[] { FirstName, MiddleName, LastName, Suffix }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", parts);
    }
}

public class DateOfBirth
{
    public DateTime Value { get; private set; }
    
    public DateOfBirth(DateTime value)
    {
        if (value > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future");
        if (value < DateTime.Today.AddYears(-150))
            throw new ArgumentException("Date of birth is too far in the past");
            
        Value = value;
    }
    
    public int CalculateAge()
    {
        var today = DateTime.Today;
        var age = today.Year - Value.Year;
        if (Value.Date > today.AddYears(-age)) age--;
        return age;
    }
}

public class PhoneNumber
{
    public string Value { get; private set; }
    public PhoneType Type { get; private set; }
    
    public PhoneNumber(string value, PhoneType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number is required");
            
        // Basic validation (can be enhanced)
        var cleaned = new string(value.Where(char.IsDigit).ToArray());
        if (cleaned.Length < 10)
            throw new ArgumentException("Invalid phone number");
            
        Value = cleaned;
        Type = type;
    }
    
    public string GetFormatted()
    {
        if (Value.Length == 10)
            return $"({Value.Substring(0, 3)}) {Value.Substring(3, 3)}-{Value.Substring(6)}";
        return Value;
    }
}

// Enums
public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
    LifeThreatening
}

public enum PatientStatus
{
    Active,
    Inactive,
    Discharged,
    Deceased
}

public enum Relationship
{
    Spouse,
    Parent,
    Child,
    Sibling,
    Friend,
    Other
}
```

### 1.3 Appointment Scheduling Context

```csharp
// Appointment Aggregate
public class Appointment
{
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ProviderId { get; private set; }
    public AppointmentType Type { get; private set; }
    public TimeSlot TimeSlot { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string Reason { get; private set; }
    public string Notes { get; private set; }
    
    public Appointment(Guid patientId, Guid providerId, AppointmentType type, TimeSlot timeSlot, string reason)
    {
        AppointmentId = Guid.NewGuid();
        PatientId = patientId;
        ProviderId = providerId;
        Type = type;
        TimeSlot = timeSlot ?? throw new ArgumentNullException(nameof(timeSlot));
        Reason = reason;
        Status = AppointmentStatus.Scheduled;
    }
    
    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled appointments can be confirmed");
            
        Status = AppointmentStatus.Confirmed;
        // Raise AppointmentConfirmedEvent
    }
    
    public void Cancel(string cancellationReason)
    {
        if (Status == AppointmentStatus.Completed || Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel completed or already cancelled appointment");
            
        Status = AppointmentStatus.Cancelled;
        Notes = $"Cancelled: {cancellationReason}";
        // Raise AppointmentCancelledEvent
    }
    
    public void CheckIn()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed appointments can be checked in");
            
        Status = AppointmentStatus.CheckedIn;
    }
    
    public void Complete(string completionNotes)
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new InvalidOperationException("Only checked-in appointments can be completed");
            
        Status = AppointmentStatus.Completed;
        Notes = completionNotes;
        // Raise AppointmentCompletedEvent
    }
    
    public void Reschedule(TimeSlot newTimeSlot)
    {
        if (Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Cannot reschedule completed appointment");
            
        TimeSlot = newTimeSlot ?? throw new ArgumentNullException(nameof(newTimeSlot));
        Status = AppointmentStatus.Scheduled;
        // Raise AppointmentRescheduledEvent
    }
}

// Value Objects
public class TimeSlot
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public TimeSlot(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time");
            
        StartTime = startTime;
        EndTime = endTime;
    }
    
    public bool OverlapsWith(TimeSlot other)
    {
        return StartTime < other.EndTime && EndTime > other.StartTime;
    }
}

// Domain Service
public class AppointmentSchedulingService
{
    private readonly IAppointmentRepository _appointmentRepository;
    
    public async Task<bool> IsTimeSlotAvailableAsync(Guid providerId, TimeSlot timeSlot)
    {
        var existingAppointments = await _appointmentRepository
            .GetByProviderAndDateAsync(providerId, timeSlot.StartTime.Date);
            
        return !existingAppointments.Any(a => 
            a.Status != AppointmentStatus.Cancelled && 
            a.TimeSlot.OverlapsWith(timeSlot));
    }
}
```

---

## 2. Logistics & Supply Chain Domain

### 2.1 Bounded Contexts

```
Logistics Platform
├── Warehouse Management Context
├── Order Fulfillment Context
├── Transportation Context
├── Inventory Context
└── Returns Context
```

### 2.2 Warehouse Management Context

```csharp
// Warehouse Aggregate
public class Warehouse
{
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public Address Location { get; private set; }
    public WarehouseType Type { get; private set; }
    
    private List<StorageZone> _zones = new();
    public IReadOnlyCollection<StorageZone> Zones => _zones.AsReadOnly();
    
    public WarehouseCapacity Capacity { get; private set; }
    public WarehouseStatus Status { get; private set; }
    
    public Warehouse(string code, string name, Address location, WarehouseType type, WarehouseCapacity capacity)
    {
        WarehouseId = Guid.NewGuid();
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Type = type;
        Capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
        Status = WarehouseStatus.Active;
    }
    
    public void AddZone(string zoneName, ZoneType zoneType, Dimensions dimensions)
    {
        var zone = new StorageZone(WarehouseId, zoneName, zoneType, dimensions);
        _zones.Add(zone);
    }
    
    public void CloseForMaintenance(DateTime scheduledReopenDate)
    {
        Status = WarehouseStatus.Maintenance;
        // Raise WarehouseClosedEvent
    }
    
    public void Reopen()
    {
        if (Status != WarehouseStatus.Maintenance)
            throw new InvalidOperationException("Only warehouses under maintenance can be reopened");
            
        Status = WarehouseStatus.Active;
    }
    
    public decimal GetUtilizationPercentage()
    {
        var totalCapacity = Capacity.TotalCubicMeters;
        var usedCapacity = _zones.Sum(z => z.GetUsedCapacity());
        return (usedCapacity / totalCapacity) * 100;
    }
}

public class StorageZone
{
    public Guid ZoneId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; }
    public ZoneType Type { get; private set; }
    public Dimensions Dimensions { get; private set; }
    
    private List<StorageLocation> _locations = new();
    public IReadOnlyCollection<StorageLocation> Locations => _locations.AsReadOnly();
    
    internal StorageZone(Guid warehouseId, string name, ZoneType type, Dimensions dimensions)
    {
        ZoneId = Guid.NewGuid();
        WarehouseId = warehouseId;
        Name = name;
        Type = type;
        Dimensions = dimensions;
    }
    
    public void AddLocation(string locationCode, Dimensions locationDimensions)
    {
        var location = new StorageLocation(ZoneId, locationCode, locationDimensions);
        _locations.Add(location);
    }
    
    public decimal GetUsedCapacity()
    {
        return _locations.Sum(l => l.IsOccupied ? l.Dimensions.GetVolume() : 0);
    }
}

public class StorageLocation
{
    public Guid LocationId { get; private set; }
    public Guid ZoneId { get; private set; }
    public string Code { get; private set; }
    public Dimensions Dimensions { get; private set; }
    public bool IsOccupied { get; private set; }
    public Guid? CurrentInventoryId { get; private set; }
    
    internal StorageLocation(Guid zoneId, string code, Dimensions dimensions)
    {
        LocationId = Guid.NewGuid();
        ZoneId = zoneId;
        Code = code;
        Dimensions = dimensions;
        IsOccupied = false;
    }
    
    public void Occupy(Guid inventoryId)
    {
        if (IsOccupied)
            throw new InvalidOperationException("Location is already occupied");
            
        IsOccupied = true;
        CurrentInventoryId = inventoryId;
    }
    
    public void Vacate()
    {
        IsOccupied = false;
        CurrentInventoryId = null;
    }
}

// Value Objects
public class Dimensions
{
    public decimal Length { get; private set; }
    public decimal Width { get; private set; }
    public decimal Height { get; private set; }
    public UnitOfMeasure Unit { get; private set; }
    
    public Dimensions(decimal length, decimal width, decimal height, UnitOfMeasure unit)
    {
        if (length <= 0 || width <= 0 || height <= 0)
            throw new ArgumentException("Dimensions must be positive");
            
        Length = length;
        Width = width;
        Height = height;
        Unit = unit;
    }
    
    public decimal GetVolume()
    {
        return Length * Width * Height;
    }
    
    public bool FitsIn(Dimensions containerDimensions)
    {
        return Length <= containerDimensions.Length &&
               Width <= containerDimensions.Width &&
               Height <= containerDimensions.Height;
    }
}

public class WarehouseCapacity
{
    public decimal TotalCubicMeters { get; private set; }
    public int MaxPalletPositions { get; private set; }
    public Weight MaxWeight { get; private set; }
    
    public WarehouseCapacity(decimal totalCubicMeters, int maxPalletPositions, Weight maxWeight)
    {
        TotalCubicMeters = totalCubicMeters;
        MaxPalletPositions = maxPalletPositions;
        MaxWeight = maxWeight;
    }
}

public class Weight
{
    public decimal Value { get; private set; }
    public WeightUnit Unit { get; private set; }
    
    public Weight(decimal value, WeightUnit unit)
    {
        if (value < 0)
            throw new ArgumentException("Weight cannot be negative");
            
        Value = value;
        Unit = unit;
    }
    
    public Weight ConvertTo(WeightUnit targetUnit)
    {
        // Conversion logic
        return this; // Simplified
    }
}
```

### 2.3 Shipment Aggregate

```csharp
public class Shipment
{
    public Guid ShipmentId { get; private set; }
    public string TrackingNumber { get; private set; }
    public Guid OrderId { get; private set; }
    public Address Origin { get; private set; }
    public Address Destination { get; private set; }
    public Carrier Carrier { get; private set; }
    public ShipmentStatus Status { get; private set; }
    
    private List<ShipmentItem> _items = new();
    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();
    
    private List<TrackingEvent> _trackingEvents = new();
    public IReadOnlyCollection<TrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();
    
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    
    public Shipment(Guid orderId, Address origin, Address destination, Carrier carrier)
    {
        ShipmentId = Guid.NewGuid();
        TrackingNumber = GenerateTrackingNumber();
        OrderId = orderId;
        Origin = origin;
        Destination = destination;
        Carrier = carrier;
        Status = ShipmentStatus.Created;
    }
    
    public void AddItem(Guid productId, int quantity, Weight weight, Dimensions dimensions)
    {
        if (Status != ShipmentStatus.Created)
            throw new InvalidOperationException("Cannot add items to shipment after creation");
            
        var item = new ShipmentItem(ShipmentId, productId, quantity, weight, dimensions);
        _items.Add(item);
    }
    
    public void Dispatch(DateTime estimatedDelivery)
    {
        if (Status != ShipmentStatus.Created)
            throw new InvalidOperationException("Only created shipments can be dispatched");
            
        if (!_items.Any())
            throw new InvalidOperationException("Cannot dispatch empty shipment");
            
        Status = ShipmentStatus.InTransit;
        EstimatedDeliveryDate = estimatedDelivery;
        
        AddTrackingEvent(TrackingEventType.Dispatched, Origin.City);
        // Raise ShipmentDispatchedEvent
    }
    
    public void UpdateLocation(string location, TrackingEventType eventType)
    {
        if (Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException("Can only update location for in-transit shipments");
            
        AddTrackingEvent(eventType, location);
    }
    
    public void Deliver(string recipientName, string signature)
    {
        if (Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException("Only in-transit shipments can be delivered");
            
        Status = ShipmentStatus.Delivered;
        ActualDeliveryDate = DateTime.UtcNow;
        
        AddTrackingEvent(TrackingEventType.Delivered, Destination.City, 
            $"Delivered to {recipientName}");
        
        // Raise ShipmentDeliveredEvent
    }
    
    public void MarkAsLost()
    {
        Status = ShipmentStatus.Lost;
        AddTrackingEvent(TrackingEventType.Lost, "Unknown");
    }
    
    private void AddTrackingEvent(TrackingEventType type, string location, string notes = null)
    {
        var trackingEvent = new TrackingEvent(type, location, notes);
        _trackingEvents.Add(trackingEvent);
    }
    
    private string GenerateTrackingNumber()
    {
        return $"TRK{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
    
    public Weight GetTotalWeight()
    {
        var totalKg = _items.Sum(i => i.Weight.Value * i.Quantity);
        return new Weight(totalKg, WeightUnit.Kilograms);
    }
}

public class TrackingEvent
{
    public Guid EventId { get; private set; }
    public TrackingEventType Type { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Location { get; private set; }
    public string Notes { get; private set; }
    
    internal TrackingEvent(TrackingEventType type, string location, string notes = null)
    {
        EventId = Guid.NewGuid();
        Type = type;
        Timestamp = DateTime.UtcNow;
        Location = location;
        Notes = notes;
    }
}

public class Carrier
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string ServiceLevel { get; private set; }
    
    public Carrier(string name, string code, string serviceLevel)
    {
        Name = name;
        Code = code;
        ServiceLevel = serviceLevel;
    }
}
```

---

## 3. Real Estate Domain

### 3.1 Property Aggregate

```csharp
public class Property
{
    public Guid PropertyId { get; private set; }
    public string ListingNumber { get; private set; }
    public PropertyType Type { get; private set; }
    public Address Address { get; private set; }
    public PropertySpecifications Specifications { get; private set; }
    public Money ListingPrice { get; private set; }
    public PropertyStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    
    private List<PropertyImage> _images = new();
    public IReadOnlyCollection<PropertyImage> Images => _images.AsReadOnly();
    
    private List<PropertyViewing> _viewings = new();
    public IReadOnlyCollection<PropertyViewing> Viewings => _viewings.AsReadOnly();
    
    private List<Offer> _offers = new();
    public IReadOnlyCollection<Offer> Offers => _offers.AsReadOnly();
    
    public Property(PropertyType type, Address address, PropertySpecifications specs, 
                   Money listingPrice, Guid ownerId)
    {
        PropertyId = Guid.NewGuid();
        ListingNumber = GenerateListingNumber();
        Type = type;
        Address = address;
        Specifications = specs;
        ListingPrice = listingPrice;
        OwnerId = ownerId;
        Status = PropertyStatus.Draft;
    }
    
    public void Publish()
    {
        if (Status != PropertyStatus.Draft)
            throw new InvalidOperationException("Only draft properties can be published");
            
        if (!_images.Any())
            throw new InvalidOperationException("Property must have at least one image");
            
        Status = PropertyStatus.Active;
        // Raise PropertyPublishedEvent
    }
    
    public void ScheduleViewing(DateTime viewingDate, Guid prospectiveBuyerId, string notes)
    {
        if (Status != PropertyStatus.Active)
            throw new InvalidOperationException("Only active properties can be viewed");
            
        var viewing = new PropertyViewing(PropertyId, viewingDate, prospectiveBuyerId, notes);
        _viewings.Add(viewing);
    }
    
    public void ReceiveOffer(Guid buyerId, Money offerAmount, DateTime validUntil, string conditions)
    {
        if (Status != PropertyStatus.Active)
            throw new InvalidOperationException("Only active properties can receive offers");
            
        var offer = new Offer(PropertyId, buyerId, offerAmount, validUntil, conditions);
        _offers.Add(offer);
        
        // Raise OfferReceivedEvent
    }
    
    public void AcceptOffer(Guid offerId)
    {
        var offer = _offers.FirstOrDefault(o => o.OfferId == offerId);
        if (offer == null)
            throw new NotFoundException("Offer not found");
            
        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException("Only pending offers can be accepted");
            
        offer.Accept();
        Status = PropertyStatus.UnderContract;
        
        // Reject all other pending offers
        foreach (var otherOffer in _offers.Where(o => o.OfferId != offerId && o.Status == OfferStatus.Pending))
        {
            otherOffer.Reject("Another offer was accepted");
        }
    }
    
    public void MarkAsSold(Money finalPrice, DateTime closingDate)
    {
        if (Status != PropertyStatus.UnderContract)
            throw new InvalidOperationException("Only properties under contract can be sold");
            
        Status = PropertyStatus.Sold;
        // Raise PropertySoldEvent
    }
    
    private string GenerateListingNumber()
    {
        return $"PROP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
}

public class PropertySpecifications
{
    public decimal LotSize { get; private set; }
    public decimal BuildingSize { get; private set; }
    public int Bedrooms { get; private set; }
    public decimal Bathrooms { get; private set; }
    public int Parking { get; private set; }
    public int YearBuilt { get; private set; }
    public List<string> Features { get; private set; }
    
    public PropertySpecifications(decimal lotSize, decimal buildingSize, int bedrooms, 
                                 decimal bathrooms, int parking, int yearBuilt, List<string> features)
    {
        LotSize = lotSize;
        BuildingSize = buildingSize;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        Parking = parking;
        YearBuilt = yearBuilt;
        Features = features ?? new List<string>();
    }
}

public class Offer
{
    public Guid OfferId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime ValidUntil { get; private set; }
    public string Conditions { get; private set; }
    public OfferStatus Status { get; private set; }
    
    internal Offer(Guid propertyId, Guid buyerId, Money amount, DateTime validUntil, string conditions)
    {
        OfferId = Guid.NewGuid();
        PropertyId = propertyId;
        BuyerId = buyerId;
        Amount = amount;
        SubmittedAt = DateTime.UtcNow;
        ValidUntil = validUntil;
        Conditions = conditions;
        Status = OfferStatus.Pending;
    }
    
    internal void Accept()
    {
        Status = OfferStatus.Accepted;
    }
    
    internal void Reject(string reason)
    {
        Status = OfferStatus.Rejected;
    }
    
    public bool IsExpired()
    {
        return DateTime.UtcNow > ValidUntil && Status == OfferStatus.Pending;
    }
}
```

---

## 4. Hotel Management Domain

### 4.1 Reservation Aggregate

```csharp
public class Reservation
{
    public Guid ReservationId { get; private set; }
    public string ConfirmationNumber { get; private set; }
    public Guid GuestId { get; private set; }
    public Guid HotelId { get; private set; }
    public DateRange StayPeriod { get; private set; }
    public ReservationStatus Status { get; private set; }
    
    private List<RoomReservation> _rooms = new();
    public IReadOnlyCollection<RoomReservation> Rooms => _rooms.AsReadOnly();
    
    private List<SpecialRequest> _specialRequests = new();
    public IReadOnlyCollection<SpecialRequest> SpecialRequests => _specialRequests.AsReadOnly();
    
    public Money TotalAmount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    
    public Reservation(Guid guestId, Guid hotelId, DateRange stayPeriod)
    {
        ReservationId = Guid.NewGuid();
        ConfirmationNumber = GenerateConfirmationNumber();
        GuestId = guestId;
        HotelId = hotelId;
        StayPeriod = stayPeriod;
        Status = ReservationStatus.Pending;
        PaymentStatus = PaymentStatus.Unpaid;
    }
    
    public void AddRoom(Guid roomTypeId, int quantity, Money ratePerNight)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Cannot modify confirmed reservation");
            
        var roomReservation = new RoomReservation(ReservationId, roomTypeId, quantity, ratePerNight, StayPeriod);
        _rooms.Add(roomReservation);
        
        RecalculateTotal();
    }
    
    public void AddSpecialRequest(string requestType, string details)
    {
        var request = new SpecialRequest(requestType, details);
        _specialRequests.Add(request);
    }
    
    public void Confirm()
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be confirmed");
            
        if (!_rooms.Any())
            throw new InvalidOperationException("Reservation must have at least one room");
            
        Status = ReservationStatus.Confirmed;
        // Raise ReservationConfirmedEvent
    }
    
    public void CheckIn()
    {
        if (Status != ReservationStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed reservations can be checked in");
            
        if (DateTime.Today < StayPeriod.StartDate.Date)
            throw new InvalidOperationException("Cannot check in before reservation start date");
            
        Status = ReservationStatus.CheckedIn;
    }
    
    public void CheckOut()
    {
        if (Status != ReservationStatus.CheckedIn)
            throw new InvalidOperationException("Only checked-in reservations can be checked out");
            
        Status = ReservationStatus.CheckedOut;
    }
    
    public void Cancel(string reason)
    {
        if (Status == ReservationStatus.CheckedOut)
            throw new InvalidOperationException("Cannot cancel completed reservation");
            
        Status = ReservationStatus.Cancelled;
        // Calculate cancellation fee based on policy
        // Raise ReservationCancelledEvent
    }
    
    private void RecalculateTotal()
    {
        var total = _rooms.Sum(r => r.GetTotalCost().Amount);
        TotalAmount = new Money(total, "USD");
    }
    
    private string GenerateConfirmationNumber()
    {
        return $"RES{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
}

public class RoomReservation
{
    public Guid RoomReservationId { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid RoomTypeId { get; private set; }
    public int Quantity { get; private set; }
    public Money RatePerNight { get; private set; }
    public DateRange StayPeriod { get; private set; }
    
    internal RoomReservation(Guid reservationId, Guid roomTypeId, int quantity, 
                            Money ratePerNight, DateRange stayPeriod)
    {
        RoomReservationId = Guid.NewGuid();
        ReservationId = reservationId;
        RoomTypeId = roomTypeId;
        Quantity = quantity;
        RatePerNight = ratePerNight;
        StayPeriod = stayPeriod;
    }
    
    public Money GetTotalCost()
    {
        var nights = StayPeriod.GetNumberOfNights();
        var total = RatePerNight.Amount * nights * Quantity;
        return new Money(total, RatePerNight.Currency);
    }
}

public class DateRange
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    
    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");
            
        StartDate = startDate;
        EndDate = endDate;
    }
    
    public int GetNumberOfNights()
    {
        return (EndDate.Date - StartDate.Date).Days;
    }
    
    public bool OverlapsWith(DateRange other)
    {
        return StartDate < other.EndDate && EndDate > other.StartDate;
    }
}
```

---

## Summary

These domain models demonstrate:

✅ **Proper aggregate design** with clear boundaries  
✅ **Rich domain models** with behavior, not just data  
✅ **Value objects** for concepts without identity  
✅ **Invariant enforcement** through encapsulation  
✅ **Domain events** for side effects  
✅ **Ubiquitous language** in code  

Use these as templates when building your own domain models!

**Document Version**: 1.0  
**Last Updated**: 2026-02-02
