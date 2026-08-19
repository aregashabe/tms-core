using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;

namespace TmsApi.Api.Controllers.V2;


[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/enrollments")]
[Tags("Enrollments V2")]
public class EnrollmentsController(IMediator mediator,IHubContext<TmsHub, ITmsHubClient> hubContext)
    : ControllerBase
{


    [HttpPost]
    [ProducesResponseType(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollStudentCommand command,
        CancellationToken ct)
    {

        var result =
            await mediator.Send(command, ct);



        return result.Match<IActionResult>(

            onSuccess: created =>
            {
                return CreatedAtAction(
                    nameof(GetSchedule),
                    new
                    {
                        studentId =
                            created.StudentId
                    },
                    created);
            },


            onFailure: error =>
            {
                var status =
                    error.Code switch
                    {
                        "course_not_found" =>
                            StatusCodes.Status404NotFound,


                        "course_full" or
                        "already_enrolled" =>
                            StatusCodes.Status409Conflict,


                        _ =>
                            StatusCodes.Status400BadRequest
                    };


                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message);
            });
    }




    [HttpGet("{studentId:int}/schedule")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        int studentId,
        CancellationToken ct)
    {

        var schedule =
            await mediator.Send(
                new GetStudentScheduleQuery(studentId),
                ct);


        return Ok(schedule);
    }
    [HttpPost("{id}/approve")]
public async Task<IActionResult> Approve(string id, CancellationToken ct)
{
// Your existing approval logic ...
// After the database commit succeeds, broadcast to all connected Angular clients
await hubContext.Clients.All
.ReceiveEnrollmentStatusUpdated(id, "Approved");
return NoContent();
}
}