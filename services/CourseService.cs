using TmsApi.Data;
using Microsoft.EntityFrameworkCore;
public interface ICourseService{
     Task<CourseRecord> RegisterAsync(string title,int capacity);
    Task<CourseRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<CourseRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<List<object>> GetCourseEnrollmentStatsAsync();
}
public class CourseService: ICourseService
{
     private readonly Dictionary<string, CourseRecord> _store = new();
    private readonly ILogger<CourseService> _logger;
    private readonly TmsDbContext _context;

    public CourseService(ILogger<CourseService> logger, TmsDbContext context){
        _logger=logger;
    }
      public Task<CourseRecord> RegisterAsync(string title,  int capacity)
    {
        var existing = _store.Values
            .FirstOrDefault(e => e.Title == title && e.Capacity==capacity);

        if (existing is not null)
        {
            _logger.LogWarning("Duplicate course {Title}", title);
            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];

        var record = new CourseRecord(
            id,
           title,
           capacity,
            DateTime.UtcNow
        );

        _store[id] = record;

        _logger.LogInformation(
            "Registered course {Title} - {Capacity}",
            title, capacity
        );
        return Task.FromResult(record);
    }
    public Task<CourseRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<CourseRecord>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<CourseRecord>)_store.Values.ToList());
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
            _logger.LogInformation("Deleted student {Id}", id);
        else
            _logger.LogWarning("Student not found {Id}", id);

        return Task.FromResult(removed);
    }

    public async Task<List<object>> GetCourseEnrollmentStatsAsync()
{
    var list = await _context.Courses
        .Select(c => new
        {
            c.Title,
            EnrollmentCount = _context.Enrollments.Count(e => e.CourseId == c.Id)
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .ToListAsync();

    return Task.FromResult(list.Cast<object>().ToList());
}

}