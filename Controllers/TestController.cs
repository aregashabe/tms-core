using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TmsApi.Data;
namespace TmsApi.Controllers;
[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
[HttpGet("deferred")]
public IActionResult TestDeferred()
{
Console.WriteLine("\n>>> STEP 1: Building the query object (nodatabase contact)...");
var query = context.Students.Where(s => s.GPA >= 3.0m);
Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
var orderedQuery = query.OrderBy(s => s.Name);
Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
var results = orderedQuery.ToList(); // Execution is triggeredhere
Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
return Ok(results);
}

// Non-translatable helper method
private static bool IsHonorRoll(decimal gpa)
{
return gpa >= 3.5m;
}
[HttpGet("translation-fail")]
public IActionResult TestTranslationFail()
{
Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
try
{
var students = context.Students
.Where(s => IsHonorRoll(s.GPA)) // EF Core does not know how to map this method to SQL
.ToList();
return Ok(students);
}
catch (Exception ex)
{
Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
return BadRequest(new { Message = ex.Message });
}
}}