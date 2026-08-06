using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentHandler(
    IEnrollmentService enrollmentService,
    ICourseService courseService,
    ICachedCourseService cachedCourseService)
    : IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
        EnrollStudentCommand command,
        CancellationToken ct)
    {
        // Find course by code
        var course = await courseService.GetByCodeAsync(
            command.CourseCode,
            ct);


        // Course does not exist
        if (course is null)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseNotFound(
                    command.CourseCode));
        }



        // Check maximum capacity
        if (course.Enrollments.Count >= course.MaxCapacity)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseFull(
                    course.Title,
                    course.MaxCapacity));
        }



        // Check duplicate enrollment
        var exists = await enrollmentService.ExistsAsync(
            command.StudentId,
            command.CourseCode,
            ct);



        if (exists)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.AlreadyEnrolled(
                    command.StudentId,
                    command.CourseCode));
        }



        // Create enrollment entity
        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };



        // Save enrollment
        await enrollmentService.AddAsync(
            enrollment,
            ct);



        // STEP 6:
        // Invalidate cached course data because
        // EnrollmentCount has changed
        await cachedCourseService.InvalidateCourseCacheAsync(ct);



        // Return success result
        return Result<EnrollmentCreated, EnrollmentError>.Success(
            new EnrollmentCreated(
                enrollment.Id,
                enrollment.StudentId,
                course.Code));
    }
}