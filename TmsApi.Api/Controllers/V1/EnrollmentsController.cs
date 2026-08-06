using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
[Tags("Enrollments V1")]
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



    // GET api/v1/courses/{courseId}/enrollments
    [HttpGet(Name = "ListCourseEnrollmentsV1")]
    [ProducesResponseType(
        typeof(IReadOnlyList<EnrollmentResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollments(
        int courseId,
        CancellationToken ct)
    {
        var course =
            await _courseService.GetByIdAsync(courseId, ct);


        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail =
                    $"Course with id {courseId} was not found.",
                Status =
                    StatusCodes.Status404NotFound
            });
        }


        var enrollments =
            await _enrollmentService.GetByCourseIdAsync(
                courseId,
                ct);


        return Ok(enrollments);
    }




    // GET api/v1/courses/{courseId}/enrollments/{id}
    [HttpGet("{id:int}", Name = "GetEnrollmentV1")]
    [ProducesResponseType(
        typeof(EnrollmentResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollment(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment =
            await _enrollmentService.GetByIdAsync(
                courseId,
                id,
                ct);


        if (enrollment is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Detail =
                    $"Enrollment with id {id} was not found.",
                Status =
                    StatusCodes.Status404NotFound
            });
        }


        return Ok(enrollment);
    }




    // POST api/v1/courses/{courseId}/enrollments
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
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken ct)
    {

        var course =
            await _courseService.GetByIdAsync(
                courseId,
                ct);


        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail =
                    $"Course with id {courseId} was not found.",
                Status =
                    StatusCodes.Status404NotFound
            });
        }



        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail =
                    $"Course '{course.Title}' has reached maximum capacity.",
                Status =
                    StatusCodes.Status409Conflict
            });
        }



        var enrollment =
            await _enrollmentService.CreateAsync(
                courseId,
                request,
                ct);



        return CreatedAtAction(
            nameof(GetEnrollment),
            new
            {
                courseId,
                id = enrollment.Id
            },
            enrollment);
    }
}