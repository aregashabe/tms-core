using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface IAdminEnrollmentService
{
    // Get all enrollments for admin dashboard
    Task<IReadOnlyList<EnrollmentResponseDto>> GetAllAsync(
        CancellationToken ct);

    // Approve an enrollment
    Task<bool> ApproveAsync(
        int enrollmentId,
        CancellationToken ct);

    // Delete an enrollment
    Task<bool> DeleteAsync(
        int enrollmentId,
        CancellationToken ct);
}