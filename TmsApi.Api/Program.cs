using Asp.Versioning;
using FluentValidation;
using TmsApi.Infrastructure.Services;
using MediatR;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Threading.Channels;

using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.RateLimiting;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Filters;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using TmsApi.Middleware;

using TmsApi.Api.Hubs;
using TmsApi.Api.Notifications;
using TmsApi.Application.Notifications;
using Microsoft.AspNetCore.Antiforgery;
using TmsApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tms.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options =>
{
options.HeaderName = "X-XSRF-TOKEN";
});

builder.Services.AddAuthentication(options =>
{
options.DefaultAuthenticateScheme =
JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme =
JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
options.TokenValidationParameters = new TokenValidationParameters
{
ValidateIssuer = true,
ValidateAudience = true,
ValidateLifetime = true,
ValidateIssuerSigningKey = true,
ValidIssuer = builder.Configuration["Jwt:Issuer"],
ValidAudience = builder.Configuration["Jwt:Audience"],
IssuerSigningKey = new SymmetricSecurityKey(
Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
};
});
builder.Services.AddAuthorizationBuilder()
.AddPolicy("CanEditCourse", policy =>
policy.Requirements.Add(new CourseInstructorRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, CourseInstructorHandler>();
// ======================================
// RATE LIMITING
// ======================================
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("search", opt =>
{
opt.TokenLimit = 10;
opt.TokensPerPeriod = 5;
opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
opt.QueueLimit = 2;
});
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var (partitionKey, tier) =
                ApiKeyResolver.Resolve(httpContext);


            return tier switch
            {

                ApiKeyTier.Paid =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"paid:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 200,
                            TokensPerPeriod = 100,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),


                ApiKeyTier.Free =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"free:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 30,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),


                _ =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"anon:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,
                            TokensPerPeriod = 5,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        })

            };
        });


    options.AddConcurrencyLimiter(
        "transcripts",
        limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.QueueLimit = 20;
            limiter.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
        });



    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;



    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";


        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var ts))
        {
            retryAfter =
                ((int)ts.TotalSeconds).ToString();
        }


        context.HttpContext.Response.Headers.RetryAfter =
            retryAfter;


        context.HttpContext.Response.ContentType =
            "application/problem+json";


        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail =
                    $"Too many requests. Retry after {retryAfter} seconds.",
                Status =
                    StatusCodes.Status429TooManyRequests,
                Type =
                    "https://tms.local/errors/rate_limit_exceeded"
            },
            ct);
    };
});

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
new BoundedChannelOptions(100)
{
FullMode = BoundedChannelFullMode.Wait
}));

// ======================================
// CONTROLLERS
// ======================================

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<AuditLogFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });



// ======================================
// EXCEPTION HANDLING
// ======================================

builder.Services.AddProblemDetails();

builder.Services
    .AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddLogging();



// ======================================
// DATABASE
// ======================================

builder.Services.AddDbContext<TmsDbContext>(
    options =>
    {
        options.UseNpgsql(
            builder.Configuration
            .GetConnectionString("TmsDatabase"));
    });

// ======================================
// HEALTH CHECKS
// ======================================

builder.Services.AddHealthChecks();

// ======================================
// APPLICATION SERVICES
// ======================================

builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddScoped<EnrollmentWorker>();

builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

builder.Services.AddScoped<IAdminEnrollmentService, AdminEnrollmentService>();
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();
builder.Services.AddSingleton<ITranscriptNotificationService, SignalRTranscriptNotificationService>();
builder.Services.AddScoped<TokenService>();
// ======================================
// MEDIATR
// ======================================

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentHandler).Assembly);
});

builder.Services.AddIdentityCore<TmsUser>(options =>
{
// Enterprise Password Policy
options.Password.RequiredLength = 12;
options.Password.RequireUppercase = true;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
// Brute-Force Lockout Protection
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();


// ======================================
// VALIDATION
// ======================================

builder.Services.AddValidatorsFromAssembly(
    typeof(EnrollStudentValidator).Assembly);



// ======================================
// PIPELINE BEHAVIORS
// ======================================

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(LoggingBehavior<,>));


builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));



// ======================================
// HYBRID CACHE
// ======================================

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions =
        new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration =
                TimeSpan.FromMinutes(2)
        };
});



// ======================================
// API VERSIONING
// ======================================

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion =
            new ApiVersion(1, 0);

        options.AssumeDefaultVersionWhenUnspecified =
            true;

        options.ReportApiVersions =
            true;

        options.ApiVersionReader =
            new UrlSegmentApiVersionReader();

    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";

        options.SubstituteApiVersionInUrl = true;
    });



// ======================================
// SWAGGER
// ======================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new()
        {
            Title = "TMS API V1",
            Version = "v1"
        });


    options.SwaggerDoc(
        "v2",
        new()
        {
            Title = "TMS API V2",
            Version = "v2"
        });
});



// ======================================
// CORS
// ======================================

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(
//         "AllowAngular",
//         policy =>
//         {
//             policy
//             .WithOrigins(
//                 "http://localhost:4200")
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//         });
// });
var allowedOrigins = builder.Configuration
.GetSection("AllowedOrigins").Get<string[]>()
?? ["http://localhost:4200"];
// Register the CORS policy in the Dependency Injection container
builder.Services.AddCors(options =>
{
options.AddPolicy("TmsClient", policy =>
{
policy.WithOrigins(allowedOrigins)
.AllowAnyHeader()
.AllowAnyMethod()
.AllowCredentials() // Vital for HttpOnly auth cookies in Session 2
.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});
});

builder.Services.AddRateLimiter(options =>
{
options.AddFixedWindowLimiter("AuthLimiter", opt =>
{
opt.PermitLimit = 5;
opt.Window = TimeSpan.FromMinutes(1);
opt.QueueLimit = 0;
});
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
if (context.User.Identity?.IsAuthenticated == true || context.
Request.Cookies.ContainsKey("tms_auth"))
{
var antiforgery = context.RequestServices
.GetRequiredService<IAntiforgery>();
var tokens = antiforgery.GetAndStoreTokens(context);
context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
new CookieOptions
{
HttpOnly = false, // MUST be false so Angular JavaScript can read it!
Secure = !builder.Environment.IsDevelopment(),
SameSite = SameSiteMode.Strict
});
}
await next(context);
});
// After app.Build()
// app.MapHub<TmsHub>("/hubs/tms");
app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");

app.MapHealthChecks("/health/live").DisableRateLimiting();
app.MapHealthChecks("/health/ready").DisableRateLimiting();

// ======================================
// MIDDLEWARE
// ======================================

// app.UseCors("AllowAngular");
app.UseCors("TmsClient");

app.UseExceptionHandler();


app.UseStatusCodePages();


app.UseRouting();


app.UseRateLimiter();


app.UseMiddleware<RequestLoggingMiddleware>();


app.UseMiddleware<V1DeprecationMiddleware>();


app.UseHttpsRedirection();



// ======================================
// SWAGGER UI
// ======================================

app.UseSwagger();


app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "TMS API V1");


    options.SwaggerEndpoint(
        "/swagger/v2/swagger.json",
        "TMS API V2");


    options.RoutePrefix = "swagger";
});


app.Use(async (context, next) =>
{
context.Response.Headers.Append("X-Content-Type-Options",
"nosniff");
context.Response.Headers.Append("X-Frame-Options", "DENY");
context.Response.Headers.Append("Referrer-Policy", "strict-origin when-cross-origin");
context.Response.Headers.Append("Content-Security-Policy","default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
await next();
});

// ======================================
// ROUTES
// ======================================

app.MapControllers();



app.MapGet(
    "/api/error",
    () =>
    {
        throw new Exception("Test error");
    });



// ======================================
// MIGRATION + SEED
// ======================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
        .GetRequiredService<TmsDbContext>();

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var migrationScope = app.Services.CreateScope())
    {
        var migrationContext = migrationScope.ServiceProvider
            .GetRequiredService<TmsDbContext>();

        migrationContext.Database.Migrate();
    }
}

    await DataSeeder.SeedAsync(context);
}
var service = new CryptoDemoService();
string hash1 = service.HashUserPassword("Password123!");
string hash2 = service.HashUserPassword("Password123!");
// hash1 and hash2 are completely different strings because of unique random salts!
Console.WriteLine($"Hash 1: {hash1}");
Console.WriteLine($"Hash 2: {hash2}");
// Both verify to true against the same plain text:
bool match1 = service.VerifyUserPassword("Password123!", hash1);// true
bool match2 = service.VerifyUserPassword("Password123!", hash2);// true


app.Run();
public partial class Program { }