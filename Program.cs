using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services;
using Scalar.AspNetCore;
using Tms.Api.Filters;
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<EnrollmentWorker>();
builder.Services.AddControllers(options =>
{
options.Filters.Add<AuditLogFilter>();
});
builder.Services.AddLogging();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// DbContext
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging());

var app = builder.Build();

app.UseRouting();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.MapGet("/api/error", () =>
{
    throw new Exception("Test error");
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();
}
if (app.Environment.IsDevelopment())
{
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
await DataSeeder.SeedAsync(context);
}

app.Run();