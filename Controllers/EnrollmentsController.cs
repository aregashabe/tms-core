using Microsoft.AspNetCore.Mvc;
using Tms.Api.Dtos;
using TmsApi.Services;
namespace TmsApi.Services;
[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(ICourseService courseService,IEnrollmentService enrollmentService) : ControllerBase
{
[HttpGet("{id:int}", Name = nameof(GetEnrollment))]
public async Task<IActionResult> GetEnrollment(int courseId, int id,CancellationToken ct)
{
var enrollment = await enrollmentService.GetByIdAsync(courseId,id, ct);
return enrollment is not null ? Ok(enrollment) : NotFound();
}
[HttpPost]
public async Task<IActionResult> EnrollStudent(
    int courseId,
    EnrollStudentRequest request,
    CancellationToken ct)
{
    // TODO 3.1: Check if course exists
    var course = await courseService.GetByIdAsync(courseId, ct);

    if (course is null)
        return NotFound();

    // TODO 3.2: Check capacity
    if (course.EnrollmentCount >= course.MaxCapacity)
    {
        return Conflict(new ProblemDetails
        {
            Title = "Course is full",
            Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
            Status = StatusCodes.Status409Conflict
        });
    }

    // TODO 3.3: Create enrollment
    var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);

    // TODO 3.4: Return 201 Created with route to GetEnrollment
    return CreatedAtAction(
        nameof(GetEnrollment),
        new { courseId, id = enrollment.Id },
        enrollment
    );
}
}