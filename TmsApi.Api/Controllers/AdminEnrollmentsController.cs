using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/enrollments")]
[Tags("Admin Enrollments")]
public class AdminEnrollmentsController : ControllerBase
{
    private readonly IAdminEnrollmentService _adminEnrollmentService;

    public AdminEnrollmentsController(
        IAdminEnrollmentService adminEnrollmentService)
    {
        _adminEnrollmentService = adminEnrollmentService;
    }

    // GET: api/admin/enrollments
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponseDto>>> GetAll(
        CancellationToken ct)
    {
        var enrollments = await _adminEnrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }

    // POST: api/enrollments/9/approve
[HttpPost("{id:int}/approve")]
public async Task<IActionResult> ApproveEnrollment(
    int id,
    CancellationToken ct)
{
    var result = await _adminEnrollmentService.ApproveAsync(
        id,
        ct);

    if (!result)
    {
        return NotFound(new ProblemDetails
        {
            Title = "Enrollment not found",
            Detail = $"Enrollment with id {id} was not found.",
            Status = StatusCodes.Status404NotFound
        });
    }

    return Ok();
}
}