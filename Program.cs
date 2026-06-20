using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddLogging();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// Options binding + validation
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();
//     builder.Services.AddDbContext<TmsDbContext>(options =>
// options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase")));
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information) // Log SQL to output window
.EnableSensitiveDataLogging()); // Show parameters in query logs (dev only)
// builder.Services.AddAuthentication("DefaultScheme")
//     .AddScheme<AuthenticationSchemeOptions, DummyAuthHandler>(
//         "DefaultScheme", options => { });
//         builder.Host.UseDefaultServiceProvider(options =>
// {
// options.ValidateScopes = true;
// options.ValidateOnBuild = true;
// });

// builder.Services.AddAuthorization();

var app = builder.Build();
app.UseRouting();
// app.MapGet("/test-di", (IEnrollmentService service) =>
// {
//     return Results.Ok(new
//     {
//         instance = service.GetHashCode()
//     });
// });

// app.UseAuthentication();
// app.UseAuthorization();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.MapGet("/api/error", () =>
{
throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

// app.MapGet("/api/assessments/results", () =>
// {
//     return Results.Ok(new
//     {
//         courseCode = "CS-101",
//         studentId = "S-001",
//         letterGrade = "A"
//     });
// })
// .RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

    // Apply migrations automatically
    context.Database.Migrate();

    // Seed only if database is empty
    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };

        context.Students.AddRange(students);
        context.SaveChanges();

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", Capacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", Capacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", Capacity = 40 }
        };

        context.Courses.AddRange(courses);
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };

        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}
app.Run();


// ============================
// ADD THIS BELOW Program.cs
// ============================
// public class DummyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
// {
//     public DummyAuthHandler(
//         IOptionsMonitor<AuthenticationSchemeOptions> options,
//         ILoggerFactory logger,
//         UrlEncoder encoder)
//         : base(options, logger, encoder)
//     { }

//     protected override Task<AuthenticateResult> HandleAuthenticateAsync()
//     {
//         return Task.FromResult(AuthenticateResult.Fail("No token"));
//     }
// }