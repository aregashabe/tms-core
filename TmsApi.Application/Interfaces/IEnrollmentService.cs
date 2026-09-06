 using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
namespace TmsApi.Application.Interfaces;
public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct);

    Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct);
        Task<List<EnrollmentResponseDto>> GetByCourseAsync(
       int courseId,
       CancellationToken ct);
       Task<IEnumerable<EnrollmentResponseDto>> GetByCourseIdAsync(
    int courseId,
    CancellationToken ct);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct);
    Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct);


    Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct);
     

    Task<bool> DeleteAsync(
        int courseId,
        int enrollmentId,
        CancellationToken ct);
}