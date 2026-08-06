using Microsoft.AspNetCore.Mvc;
namespace TmsApi.Services;
[ApiController]
[Route("api/studentregister")]
public class StudentRegisterController : ControllerBase
{
    private readonly IStudentService _service;

    public StudentRegisterController(IStudentService service)
    {
        _service = service;
    }

    //GET all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }
    [HttpGet("paged")]
    
public async Task<IActionResult> GetPaged(  int pageNumber = 1,CancellationToken ct = default)
{
    var result = await _service.GetPagedStudentsAsync(pageNumber, ct);
    return Ok(result);
}
[HttpGet("active-high-gpa-count")]
public async Task<IActionResult> GetCount()
{
    var count = await _service.GetActiveHighGpaCountAsync();
    return Ok(count);
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
    public async Task<IActionResult> Create([FromBody] CreateRegisterRequest request)
    {
        var record = await _service.RegisterAsync(
            request.StudentId,
            request.Name,
            request.Age,
            request.GPA
        );

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }
[HttpGet("enrollment-report")]

public async Task<IActionResult> GetEnrollmentReport(CancellationToken cancellationToken)
{
    var report = await _service.GetStudentEnrollmentReport(cancellationToken);
    return Ok(report);
}


    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}