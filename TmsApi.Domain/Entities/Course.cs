namespace TmsApi.Domain.Entities;
public class Course
{
public int Id { get; set; }

// surrogate primary key — internal, used by foreign keys
public required string Code { get; set; } // natural key — human-readable (uniqueness configured in Session 2)
public required string Title { get; set; }
public int MaxCapacity { get; set; }
  public string? InstructorId { get; set; }
// Navigation property for many-to-many relationship
public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
