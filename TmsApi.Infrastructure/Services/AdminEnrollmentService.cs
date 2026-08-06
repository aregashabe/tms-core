using TmsApi.Domain.Entities;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class AdminEnrollmentService : IAdminEnrollmentService
{
    private readonly TmsDbContext _context;

    public AdminEnrollmentService(TmsDbContext context)
    {
        _context = context;
    }

public async Task<IReadOnlyList<EnrollmentResponseDto>> GetAllAsync(
    CancellationToken ct)
{
    return await _context.Enrollments
        .Include(e => e.Student)
        .Include(e => e.Course)
        .Select(e => new EnrollmentResponseDto(
            e.Id,
            e.StudentId,
            e.Student.Name,
            e.Course.Code,
            e.Course.Title,
            e.CourseId,
            e.Status,
            e.EnrolledAt
        ))
        .ToListAsync(ct);
}

    
public async Task<bool> ApproveAsync(
    int enrollmentId,
    CancellationToken ct)
{
    var enrollment = await _context.Enrollments
        .FirstOrDefaultAsync(
            e => e.Id == enrollmentId,
            ct);

    if (enrollment is null)
        return false;

    enrollment.Status = EnrollmentStatus.Approved;

    await _context.SaveChangesAsync(ct);

    return true;
}
    public async Task<bool> DeleteAsync(
        int enrollmentId,
        CancellationToken ct)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(
                e => e.Id == enrollmentId,
                ct);

        if (enrollment is null)
            return false;

        _context.Enrollments.Remove(enrollment);

        await _context.SaveChangesAsync(ct);

        return true;
    }
}