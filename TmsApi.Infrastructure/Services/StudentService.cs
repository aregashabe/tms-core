using TmsApi.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

public interface IStudentService
{
    Task<Student> RegisterAsync(string registrationNumber, string name, int age, decimal? gpa);
    Task<Student?> GetByIdAsync(string id);
    Task<IReadOnlyList<Student>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<int> GetActiveHighGpaCountAsync();
    Task<List<Student>> GetPagedStudentsAsync(int pageNumber, CancellationToken cancellationToken);
    Task<List<object>> GetStudentEnrollmentReport(CancellationToken cancellationToken);
}


public class StudentService : IStudentService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<StudentService> _logger;

    public StudentService(ILogger<StudentService> logger, TmsDbContext context)
    {
        _logger = logger;
        _context = context;
    }
 public async Task<List<Student>> GetPagedStudentsAsync(
    int pageNumber,
    CancellationToken cancellationToken)
{
    const int pageSize = 20;

    if (pageNumber < 1)
        pageNumber = 1;   // 🔥 prevents negative OFFSET

    return await _context.Students
        .OrderBy(s => s.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);
}
    // =========================
    // REGISTER STUDENT
    // =========================
    public async Task<Student> RegisterAsync(string registrationNumber, string name, int age, decimal? gpa)
    {
        var existing = await _context.Students
            .FirstOrDefaultAsync(s => s.RegistrationNumber == registrationNumber);

        if (existing != null)
        {
            _logger.LogWarning("Duplicate student {RegistrationNumber}", registrationNumber);
            return existing;
        }

        var student = new Student
        {
            RegistrationNumber = registrationNumber,
            Name = name,
            GPA = gpa ?? 0m,
            IsActive = true
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered student {RegistrationNumber}", registrationNumber);

        return student;
    }

    // =========================
    // GET BY ID
    // =========================
    public async Task<Student?> GetByIdAsync(string id)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Id.ToString() == id);
    }

    // =========================
    // GET ALL
    // =========================
    public async Task<IReadOnlyList<Student>> GetAllAsync()
    {
        return await _context.Students.ToListAsync();
    }

    // =========================
    // DELETE
    // =========================
    public async Task<bool> DeleteAsync(string id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id.ToString() == id);

        if (student == null)
        {
            _logger.LogWarning("Student not found {Id}", id);
            return false;
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted student {Id}", id);

        return true;
    }

    // =========================
    // HIGH GPA COUNT
    // =========================
    public async Task<int> GetActiveHighGpaCountAsync()
    {
        return await _context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
    }



    public async Task<List<object>> GetStudentEnrollmentReport(CancellationToken cancellationToken)
{
    var report = await _context.Students
        .AsNoTracking()
        .Select(s => new
        {
            s.Name,
            EnrollmentCount = s.Enrollments.Count
        })
        .ToListAsync(cancellationToken);

    foreach (var r in report)
    {
        Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");
    }

    return report.Cast<object>().ToList();
}
}

