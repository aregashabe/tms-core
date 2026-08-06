using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
[Tags("Courses V2")]
public class CoursesController : ControllerBase
{
    private readonly TmsDbContext _context;

    public CoursesController(TmsDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("List courses V2")]
    [EndpointDescription(
        "Returns paginated courses with metadata and HATEOAS links.")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Validate pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);


        var baseQuery = _context.Courses
            .AsNoTracking();


        var totalCount = await baseQuery
            .CountAsync(ct);


        var rows = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);


        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);


        var hasNext = page < totalPages;

        var hasPrevious = page > 1;


        return Ok(new
        {
            data = rows,

            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },

            links = new
            {
                self =
                    $"/api/v2/courses?page={page}&pageSize={pageSize}",


                next = hasNext
                    ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}"
                    : null,


                prev = hasPrevious
                    ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}"
                    : null,


                enroll =
                    "/api/v2/enrollments"
            }
        });
    }
}