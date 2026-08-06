namespace TmsApi.Middleware;

public class V1DeprecationMiddleware(RequestDelegate next)
{
    private static readonly DateTimeOffset SunsetDate =
        new(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);


    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1"))
            {
                context.Response.Headers["Deprecation"] = "true";

                context.Response.Headers["Sunset"] =
                    SunsetDate.ToString("R");


                var v2Path =
                    context.Request.Path.Value?[7..];


                context.Response.Headers["Link"] =
                    $"<{context.Request.Scheme}://{context.Request.Host}/api/v2{v2Path}>; rel=\"successor-version\"";
            }

            return Task.CompletedTask;
        });


        await next(context);
    }
}