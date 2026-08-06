using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/courses")]
[Tags("Courses V1")]
public class CoursesController : ControllerBase
{
    private readonly TmsDbContext _context;

    public CoursesController(TmsDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("List courses V1")]
    public async Task<IActionResult> GetCourses(
        CancellationToken ct)
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);

        return Ok(courses);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get course by id V1")]
    public async Task<IActionResult> GetCourse(
        int id,
        CancellationToken ct)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .FirstOrDefaultAsync(ct);

        if (course is null)
        {
            return NotFound();
        }

        return Ok(course);
    }
}