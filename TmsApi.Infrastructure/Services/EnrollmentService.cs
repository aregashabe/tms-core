using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        TmsDbContext context,
        ILogger<EnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

   public Task<EnrollmentResponseDto?> GetByIdAsync(
    int courseId,
    int id,
    CancellationToken ct) =>
    _context.Enrollments
        .AsNoTracking()
        .Where(e => e.Id == id && e.CourseId == courseId)
        .Select(e => new EnrollmentResponseDto(
            e.Id,
            e.StudentId,
            e.Student.Name,
            e.Course.Code,
            e.Course.Title,
            e.CourseId,
            e.Status,
            e.EnrolledAt))
        .FirstOrDefaultAsync(ct);


    public async Task<EnrollmentResponseDto> CreateAsync(
    int courseId,
    EnrollStudentRequest request,
    CancellationToken ct)
{
    var enrollment = new Enrollment
    {
        CourseId = courseId,
        StudentId = request.StudentId,
        Status = EnrollmentStatus.Pending,
        EnrolledAt = DateTime.UtcNow
    };

    _context.Enrollments.Add(enrollment);

    await _context.SaveChangesAsync(ct);

    _logger.LogInformation(
        "Enrollment created: Id={Id}, CourseId={CourseId}, StudentId={StudentId}",
        enrollment.Id,
        courseId,
        request.StudentId
    );

    var result = await GetByIdAsync(
        courseId,
        enrollment.Id,
        ct);

    return result!;
}


    public async Task<IEnumerable<EnrollmentResponseDto>> GetByCourseIdAsync(
        int courseId,
        CancellationToken ct)
    {
        return await _context.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
    e.Id,
    e.StudentId,
    e.Student.Name,
    e.Course.Code,
    e.Course.Title,
    e.CourseId,
    e.Status,
    e.EnrolledAt))
            .ToListAsync(ct);
    }


    public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct)
    {
        return await _context.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
    e.Id,
    e.StudentId,
    e.Student.Name,
    e.Course.Code,
    e.Course.Title,
    e.CourseId,
    e.Status,
    e.EnrolledAt))
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new EnrollmentResponseDto(
    e.Id,
    e.StudentId,
    e.Student.Name,
    e.Course.Code,
    e.Course.Title,
    e.CourseId,
    e.Status,
    e.EnrolledAt))
            .ToListAsync(ct);
    }


    public async Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct)
    {
        return await _context.Enrollments
            .AnyAsync(
                e =>
                    e.StudentId == studentId &&
                    e.Course.Code == courseCode,
                ct);
    }


    public async Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct)
    {
        await _context.Enrollments.AddAsync(
            enrollment,
            ct);

        await _context.SaveChangesAsync(ct);
    }
   

public async Task<bool> DeleteAsync(
    int courseId,
    int enrollmentId,
    CancellationToken ct)
{
    var enrollment = await _context.Enrollments
        .FirstOrDefaultAsync(e =>
            e.CourseId == courseId &&
            e.Id == enrollmentId,
            ct);

    if (enrollment == null)
        return false;

    _context.Enrollments.Remove(enrollment);

    await _context.SaveChangesAsync(ct);

    return true;
}
}