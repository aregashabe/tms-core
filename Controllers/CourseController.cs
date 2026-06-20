using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/courseregister")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _service;

    public CourseController(ICourseService service)
    {
        _service = service;
    }

    // GET all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    // GET by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _service.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    // POST register student
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CourseRequest request)
    {
        var record = await _service.RegisterAsync(
            request.Title,
            request.Capacity
        );

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
    [HttpGet("enrollment-stats")]
public async Task<IActionResult> GetCourseEnrollmentStats()
{
    var stats = await _service.GetCourseEnrollmentStatsAsync();
    return Ok(stats);
}

}