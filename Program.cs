using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services;
using Scalar.AspNetCore;
using Tms.Api.Filters;
using Asp.Versioning;
using TmsApi.Middleware;
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
builder.Services.AddOpenApi("v1", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v2";
});
builder.Services.AddApiVersioning(options =>
{
options.DefaultApiVersion = new ApiVersion(1, 0);
options.AssumeDefaultVersionWhenUnspecified = true;
options.ReportApiVersions = true;
options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
options.GroupNameFormat = "'v'VVV";
options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

app.UseRouting();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<V1DeprecationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
   app.MapScalarApiReference(options =>
{
options.WithTitle("TMS API Reference")
.WithTheme(ScalarTheme.DeepSpace)
.WithDefaultHttpClient(ScalarTarget.CSharp,
ScalarClient.HttpClient);
// Tell Scalar to pull both documents into its sidebar dropdown
options
.AddDocument("v1", "API Version 1.0")
.AddDocument("v2", "API Version 2.0");
});
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