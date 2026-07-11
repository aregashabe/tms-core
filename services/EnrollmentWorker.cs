using Microsoft.Extensions.DependencyInjection;
using TmsApi.Services;
using Tms.Api.Dtos;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessBatch()
    {
        using var scope = _scopeFactory.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        var request = new EnrollStudentRequest
        {
            StudentId = 1
        };

        var result = await svc.CreateAsync(
            courseId: 1,
            request: request,
            ct: CancellationToken.None
        );

        Console.WriteLine($"Processed Enrollment Id: {result.Id}");
    }
}