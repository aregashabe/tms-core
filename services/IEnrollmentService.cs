using Tms.Api.Dtos;
namespace TmsApi.Services;

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
}