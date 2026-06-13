using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddLogging();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// Options binding + validation
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

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