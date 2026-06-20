using TmsApi.Data;
using Microsoft.EntityFrameworkCore;
public interface IStudentService
{
    Task<StudentRecord> RegisterAsync(string studentId, string name, int age, decimal? gpa);
    Task<StudentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<int> GetActiveHighGpaCountAsync();
}


public class StudentService : IStudentService
{
    private readonly Dictionary<string, StudentRecord> _store = new();
     private readonly TmsDbContext _context;
    private readonly ILogger<StudentService> _logger;

    public StudentService(ILogger<StudentService> logger,TmsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public Task<StudentRecord> RegisterAsync(string studentId, string name, int age, decimal? gpa)
    {
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId == studentId);

        if (existing is not null)
        {
            _logger.LogWarning("Duplicate student {StudentId}", studentId);
            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];

        var record = new StudentRecord(
            id,
            studentId,
            name,
            age,
            gpa ?? 0m,
            DateTime.UtcNow
        );

        _store[id] = record;

        _logger.LogInformation(
            "Registered student {StudentId} - {Name}",
            studentId, name
        );
        _logger.LogInformation("Store count after register: {Count}", _store.Count);

        return Task.FromResult(record);
    }

    public Task<StudentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<StudentRecord>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<StudentRecord>)_store.Values.ToList());
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
            _logger.LogInformation("Deleted student {Id}", id);
        else
            _logger.LogWarning("Student not found {Id}", id);
        _logger.LogInformation("Store count during get: {Count}", _store.Count);

        return Task.FromResult(removed);
    }
    // public async Task<int> GetActiveHighGpaStudentsCount()
    // {
    //     return await _context.Students
    //         .Where(s => s.IsActive && s.GPA >= 3.0m)
    //         .CountAsync();
    // }


    public async Task<int> GetActiveHighGpaCountAsync()
{
    return await _context.Students
        .Where(s => s.IsActive && s.GPA >= 3.0m)
        .CountAsync();
}


}

