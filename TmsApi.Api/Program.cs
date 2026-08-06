using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.RateLimiting;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Filters;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using TmsApi.Middleware;


var builder = WebApplication.CreateBuilder(args);


// ======================================
// RATE LIMITING
// ======================================

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



// ======================================
// MEDIATR
// ======================================

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentHandler).Assembly);
});



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

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngular",
        policy =>
        {
            policy
            .WithOrigins(
                "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});



var app = builder.Build();

app.MapHealthChecks("/health/live").DisableRateLimiting();
app.MapHealthChecks("/health/ready").DisableRateLimiting();

// ======================================
// MIDDLEWARE
// ======================================

app.UseCors("AllowAngular");


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

    context.Database.Migrate();

    await DataSeeder.SeedAsync(context);
}



app.Run();