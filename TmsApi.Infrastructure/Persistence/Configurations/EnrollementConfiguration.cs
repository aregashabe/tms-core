using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        builder.Property(e => e.Grade);

        // FK → Student (int, NO HasMaxLength)
        builder.Property(e => e.StudentId)
            .IsRequired();

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → Course
        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            // .OnDelete(DeleteBehavior.Cascade);
            .OnDelete(DeleteBehavior.Restrict);
    }
}