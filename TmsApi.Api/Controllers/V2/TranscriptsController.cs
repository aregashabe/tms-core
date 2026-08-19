
using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v2/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController(
    Channel<TranscriptRequest> channel,
    ITranscriptStatusStore statusStore) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        [FromBody] TranscriptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // Check whether this request was already submitted
        // with the same idempotency key.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing =
                await statusStore.GetReportIdForIdempotencyKeyAsync(
                    idempotencyKey,
                    ct);

            if (existing is not null)
            {
                var existingStatus =
                    await statusStore.GetAsync(
                        existing,
                        ct);

                return Accepted(
                    Url.Action(
                        nameof(GetStatus),
                        new { id = existing }),
                    existingStatus);
            }
        }

        // Create a new report ID.
        var reportId =
            Guid.NewGuid().ToString("N")[..12];

        // Create the initial transcript status.
        var status =
            await statusStore.CreateAsync(
                reportId,
                request.StudentId,
                ct);

        // Save the idempotency key if one was supplied.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await statusStore.LinkIdempotencyKeyAsync(
                idempotencyKey,
                reportId,
                ct);
        }

        // Put the transcript request into the background queue.
        await channel.Writer.WriteAsync(
            request.WithReportId(reportId),
            ct);

        // Tell the client to check the status again later.
        Response.Headers.RetryAfter = "5";

        return Accepted(
            Url.Action(
                nameof(GetStatus),
                new { id = reportId }),
            status);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(
        string id,
        CancellationToken ct)
    {
        var status =
            await statusStore.GetAsync(
                id,
                ct);

        if (status is null)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Transcript not found",
                    Detail =
                        $"No transcript request with id '{id}'.",
                    Status =
                        StatusCodes.Status404NotFound
                });
        }

        return Ok(status);
    }
}
