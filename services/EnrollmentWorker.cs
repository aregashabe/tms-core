using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // TODO2: create scope
        using var scope = _scopeFactory.CreateScope();

        // TODO3: resolve scoped service
        var svc = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // TODO4: use service
        var result = svc.EnrollAsync("S-001", "CS-101").Result;

        Console.WriteLine($"Processed: {result.Id}");
    }
}