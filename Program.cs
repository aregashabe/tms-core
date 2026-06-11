using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddLogging();
builder.Services.AddControllers();
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
// app.MapScalarApiReference();
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
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();

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