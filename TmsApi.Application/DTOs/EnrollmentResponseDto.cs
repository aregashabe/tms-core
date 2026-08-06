using TmsApi.Domain.Entities;

namespace TmsApi.Application.DTOs;

public record EnrollmentResponseDto(
    int Id,
    int StudentId,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    int CourseId,
    EnrollmentStatus Status,
    DateTime EnrolledAt
);