using TmsApi.Application.DTOs;


namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<CourseDetailDto> GetCourseAsync(
        string code,
        CancellationToken ct);

    Task<List<CourseDetailDto>> GetAllCoursesAsync(
        CancellationToken ct);

    Task InvalidateCourseCacheAsync(
        CancellationToken ct);
}