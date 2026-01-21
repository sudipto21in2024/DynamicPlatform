# Handling Many-to-Many Relationships

This document details the architectural strategy for handling Many-to-Many (M:N) relationships (e.g., Students enrolling in Courses, Users having Roles) in the Low-Code Platform.

## 1. The Design Choice: Explicit vs. Implicit

While EF Core supports "skip navigation" (hidden join tables), a Low-Code platform **MUST** use **Explicit Join Entities**.

**Why?**
1.  **Extensibility**: Users almost always want to add data to the link eventually (e.g., "Permissions" on a Role, or "EnrollmentDate" on a Course).
2.  **Control**: Easier to generate UI for a tangible entity than a hidden concept.

## 2. Visual Builder Experience

1.  User drags `Student` and `Course` entities onto the canvas.
2.  User selects the "Association" tool and draws a line between them.
3.  User selects relation type: **Many-to-Many**.
4.  **System Prompt**: "A junction entity will be created. Name it: [StudentCourse]".
5.  The system automatically generates a third entity on the canvas visible to the user.

## 3. Metadata Representation

The metadata records 3 entities, but marks the middle one as a `JoinEntity`.

```json
{
  "entities": [
    { "name": "Student", "id": "e_student" },
    { "name": "Course", "id": "e_course" },
    { 
      "name": "StudentCourse", 
      "id": "e_join",
      "isJoinEntity": true,
      "properties": [
        { "name": "EnrolledDate", "type": "DateTime" } // User adds custom field
      ]
    }
  ],
  "relationships": [
    {
      "from": "Student", "to": "StudentCourse", "type": "OneToMany"
    },
    {
      "from": "Course", "to": "StudentCourse", "type": "OneToMany"
    }
  ]
}
```

## 4. Code Generation Strategy (Backend)

The engine generates standard One-to-Many patterns from both sides pointing to the center.

### 4.1. Entity Generation
**File: `Domain/Entities/Student.Generated.cs`**
```csharp
public partial class Student {
    public Guid Id { get; set; }
    public string Name { get; set; }
    // Navigation to the join table
    public virtual ICollection<StudentCourse> StudentCourses { get; set; } 
}
```

**File: `Domain/Entities/StudentCourse.Generated.cs`**
```csharp
public partial class StudentCourse {
    [Key]
    public Guid Id { get; set; } // Surrogate Key is safer for low-code than Composite
    
    public Guid StudentId { get; set; }
    public virtual Student Student { get; set; }

    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; }

    // Custom Payload (if added by user)
    public DateTime EnrolledDate { get; set; } 
}
```

### 4.2. EF Core Configuration (DbContext)
We need to ensure unique pairing constraints.

```csharp
modelBuilder.Entity<StudentCourse>()
    .HasIndex(sc => new { sc.StudentId, sc.CourseId })
    .IsUnique();
    
// Optional: Cascade Delete behavior
modelBuilder.Entity<StudentCourse>()
    .HasOne(sc => sc.Student)
    .WithMany(s => s.StudentCourses)
    .OnDelete(DeleteBehavior.Cascade);
```

## 5. UI Generation Strategy

Handling M:N in UI is complex. We offer two "Patterns" in the Page Builder.

### Pattern A: The "Dual List" (Manage Links)
Good for small lists (e.g., Roles).
*   **Component**: `TransferList` (Left side: Available, Right side: Selected).
*   **Action**: Clicking "Save" deletes all internal `StudentCourse` records for that Student and inserts the new set.

### Pattern B: The "Sub-Grid" (Master-Detail)
Good for heavy data (e.g., Order Items, Course Enrollments).
1.  Open `Student` Detail Page.
2.  Tab: "Courses".
3.  Shows a Grid of `StudentCourse` records.
4.  Button: **"Add Course"**.
    *   Opens Modal -> Select `Course`.
    *   (Optional) Fill `EnrollmentDate`.
    *   Save -> Creates 1 `StudentCourse` record.

## 6. Business Logic Example (API)

How does a developer add a rule: *"Student cannot enroll in more than 5 courses"*?

**File: `Domain/Entities/StudentCourse.Custom.cs`** (User Code)

```csharp
public partial class StudentCourse : IValidatableObject 
{
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        // Must inject a service or check DB (simpler example here)
        var limit = 5;
        // Logic to check existing count...
        if (ExceedsLimit) yield return new ValidationResult("Too many courses");
    }
}
```
