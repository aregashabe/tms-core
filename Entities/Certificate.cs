using System;
namespace TmsApi.Entities;
public class Certificate
{
// surrogate primary key
public int Id { get; set; }

public required string SerialNumber { get; set; } // naturalkey — human-readable (uniqueness configured in Session 2)
public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
// Foreign keys + navigation to the student and course
public int StudentId { get; set; }
public int CourseId { get; set; }
public Student Student { get; set; } = null!;
public Course Course { get; set; } = null!;
}