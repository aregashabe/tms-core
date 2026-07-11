using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
namespace TmsApi.Data;

public class TmsDbContext : DbContext
{
    public TmsDbContext(DbContextOptions<TmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);

  modelBuilder.Entity<Student>()
        .HasQueryFilter(s => !s.IsDeleted);
        base.OnModelCreating(modelBuilder);
    }
    public override int SaveChanges()
{
    UpdateAudit();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    UpdateAudit();
    return base.SaveChangesAsync(cancellationToken);
}

private void UpdateAudit()
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
        {
            var lastUpdated = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == "LastUpdated");

            if (lastUpdated != null)
            {
                lastUpdated.CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
}