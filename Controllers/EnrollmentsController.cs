using Microsoft.AspNetCore.Mvc;
using Tms.Api.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
public class EnrollmentsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(
        ICourseService courseService,
        IEnrollmentService enrollmentService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
    }

    // GET: api/courses/1/enrollments
    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(
        typeof(IReadOnlyList<EnrollmentResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrolments for a course")]
    public async Task<IActionResult> GetEnrollments(
        int courseId,
        CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(courseId, ct);

        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"Course with id {courseId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var enrollments =
            await _enrollmentService.GetByCourseIdAsync(courseId, ct);

        return Ok(enrollments);
    }

    // GET: api/courses/1/enrollments/5
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    [ProducesResponseType(
        typeof(EnrollmentResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one enrolment for a course")]
    public async Task<IActionResult> GetEnrollment(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment =
            await _enrollmentService.GetByIdAsync(courseId, id, ct);

        if (enrollment is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Detail = $"Enrollment with id {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(enrollment);
    }

    // POST: api/courses/1/enrollments
    [HttpPost]
    [ProducesResponseType(
        typeof(EnrollmentResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [EndpointSummary("Enrol a student in a course")]
    [EndpointDescription(
        "Returns 404 if the course does not exist, " +
        "409 if the course has reached MaxCapacity.")]
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken ct)
    {
        // Check whether course exists
        var course =
            await _courseService.GetByIdAsync(courseId, ct);

        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"Course with id {courseId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Check course capacity
        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail =
                    $"Course '{course.Title}' has reached its " +
                    $"maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Create enrollment
        var enrollment =
            await _enrollmentService.CreateAsync(
                courseId,
                request,
                ct);

        // Return 201 Created
        return CreatedAtAction(
            nameof(GetEnrollment),
            new
            {
                courseId = courseId,
                id = enrollment.Id
            },
            enrollment);
    }
}