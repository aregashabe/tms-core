using TmsApi.Entities;
using Tms.Api.Dtos;
using TmsApi.Data;
using Microsoft.EntityFrameworkCore;
namespace TmsApi.Services;
public class CourseService: ICourseService
{
    private readonly ILogger<CourseService> _logger;
    private readonly TmsDbContext _context;

    public CourseService(ILogger<CourseService> logger, TmsDbContext context){
        _logger=logger;
        _context = context;
    }
//..............................m6 lab 1........................................................................

public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>_context.Courses
.AsNoTracking()
.Where(c => c.Id == id)
.Select(c => new CourseResponseDto(
c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
.FirstOrDefaultAsync(ct);

//.................................m6 lab1
public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
{
var course = new Course
{
Code = request.Code,
Title = request.Title,
MaxCapacity = request.MaxCapacity
};
_context.Courses.Add(course);
await _context.SaveChangesAsync(ct);
_logger.LogInformation("Created course {CourseId} ({Code})", course.
Id, course.Code);
return (await GetByIdAsync(course.Id, ct))!;
}
public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
_context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request,CancellationToken ct)
{
    IQueryable<Course> query = _context.Courses.AsNoTracking();


    // TODO 2 Search
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
            EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }


    // TODO 3 Count before paging
    var totalCount = await query.CountAsync(ct);


    // TODO 4 Sorting
    query = request.OrderBy?.ToLower() switch
    {
        "code" =>
            request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),

        "maxcapacity" =>
            request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),

        _ =>
            request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };


    // TODO 5 Paging + DTO
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count
        ))
        .ToListAsync(ct);


    // TODO 6 Response
    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}