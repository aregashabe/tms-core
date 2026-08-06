using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Api.ExceptionHandlers;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title, detail, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                (IDictionary<string, string[]>?)validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.ErrorMessage)
                            .ToArray()
                    )
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                $"An unexpected error occurred. Trace ID: {httpContext.TraceIdentifier}",
                null
            )
        };


        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }


        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };


        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }


        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";


        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            ct);


        return true;
    }
}