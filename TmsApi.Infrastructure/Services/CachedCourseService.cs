using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{

    public async Task<CourseDetailDto> GetCourseAsync(
        string code,
        CancellationToken ct)
    {
        var key = CacheKeys.Course(code);

        var dbHit = false;


        var dto = await cache.GetOrCreateAsync(
            key,
            code,

            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    key);


                var course =
                    await service.GetByCodeAsync(
                        state,
                        token);


                if (course is null)
                {
                    throw new Exception(
                        $"Course {state} not found.");
                }


                return new CourseDetailDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Code = course.Code,
                    MaxCapacity = course.MaxCapacity,
                    EnrollmentCount = course.Enrollments.Count,
                    Links = []
                };
            },


            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),

                LocalCacheExpiration =
                    TimeSpan.FromMinutes(2)
            },


            tags:
            [
                CacheKeys.CoursesTag
            ],

            cancellationToken: ct
        );


        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }


        return dto;
    }





    public async Task<List<CourseDetailDto>> GetAllCoursesAsync(
        CancellationToken ct)
    {

        var key = CacheKeys.CoursesAll;

        var dbHit = false;



        var list = await cache.GetOrCreateAsync(
            key,

            "all",


            async (_, token) =>
            {
                dbHit = true;


                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    key);



                var courses =
                    await service.GetAllCoursesAsync(token);



                return courses
                    .Select(c =>
                        new CourseDetailDto
                        {
                            Id = c.Id,
                            Title = c.Title,
                            Code = c.Code,
                            MaxCapacity = c.MaxCapacity,
                            EnrollmentCount = c.Enrollments.Count,
                            Links = []
                        })
                    .ToList();

            },


            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),

                LocalCacheExpiration =
                    TimeSpan.FromMinutes(2)
            },


            tags:
            [
                CacheKeys.CoursesTag
            ],


            cancellationToken: ct
        );



        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }


        return list;
    }





    public async Task InvalidateCourseCacheAsync(
        CancellationToken ct)
    {

        logger.LogInformation(
            "Invalidating cache tag {Tag}",
            CacheKeys.CoursesTag);



        await cache.RemoveByTagAsync(
            CacheKeys.CoursesTag,
            ct);
    }
}