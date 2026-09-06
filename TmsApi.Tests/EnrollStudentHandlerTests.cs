using NSubstitute;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Domain.Entities;

namespace TmsApi.Tests;

public class EnrollStudentHandlerTests
{
    [Fact]
    public async Task Handle_WhenAlreadyEnrolled_ReturnsDuplicateError()
    {
        // Arrange
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();
        var cachedCourseService = Substitute.For<ICachedCourseService>();

        enrollmentService
            .ExistsAsync(
                99,
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 30,
            Enrollments = new List<Enrollment>()
        };

        courseService
            .GetByCodeAsync(
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        var handler = new EnrollStudentHandler(
            enrollmentService,
            courseService,
            cachedCourseService);

        var command = new EnrollStudentCommand(
            StudentId: 99,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);

        Assert.Equal(
            "already_enrolled",
            result.Error.Code);

        Assert.Equal(
            TmsApi.Application.Enrollments.Commands.EnrollmentError
                .AlreadyEnrolled(
                    99,
                    "CS-401"),
            result.Error);

        // The duplicate branch must not add an enrollment.
        await enrollmentService
            .DidNotReceive()
            .AddAsync(
                Arg.Any<Enrollment>(),
                Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Handle_WhenCourseFull_ReturnsCapacityError()
    {
        // Arrange
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();
        var cachedCourseService = Substitute.For<ICachedCourseService>();

        // Course is at maximum capacity.
        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 35,

            Enrollments = Enumerable
                .Range(1, 35)
                .Select(i => new Enrollment
                {
                    Id = i,
                    CourseId = 1,
                    Status = EnrollmentStatus.Pending
                })
                .ToList()
        };

        courseService
            .GetByCodeAsync(
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        var handler = new EnrollStudentHandler(
            enrollmentService,
            courseService,
            cachedCourseService);

        var command = new EnrollStudentCommand(
            StudentId: 100,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);

        Assert.Equal(
            "course_full",
            result.Error.Code);

        Assert.Equal(
            TmsApi.Application.Enrollments.Commands.EnrollmentError
                .CourseFull(
                    "Advanced Web Dev",
                    35),
            result.Error);

        // The full-course branch must not add an enrollment.
        await enrollmentService
            .DidNotReceive()
            .AddAsync(
                Arg.Any<Enrollment>(),
                Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Handle_SuccessfulPath_AddsEnrollmentOnce()
    {
        // Arrange
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();
        var cachedCourseService = Substitute.For<ICachedCourseService>();

        // Course has 20 enrollments and capacity is 35.
        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 35,

            Enrollments = Enumerable
                .Range(1, 20)
                .Select(i => new Enrollment
                {
                    Id = i,
                    CourseId = 1,
                    Status = EnrollmentStatus.Pending
                })
                .ToList()
        };

        courseService
            .GetByCodeAsync(
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        // Student is not already enrolled.
        enrollmentService
            .ExistsAsync(
                100,
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new EnrollStudentHandler(
            enrollmentService,
            courseService,
            cachedCourseService);

        var command = new EnrollStudentCommand(
            StudentId: 100,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        Assert.Equal(
            100,
            result.Value.StudentId);

        Assert.Equal(
            "CS-401",
            result.Value.CourseCode);

        // Successful branch must add exactly one enrollment.
        await enrollmentService
            .Received(1)
            .AddAsync(
                Arg.Is<Enrollment>(
                    e =>
                        e.StudentId == 100 &&
                        e.CourseId == 1),
                Arg.Any<CancellationToken>());
    }
}
