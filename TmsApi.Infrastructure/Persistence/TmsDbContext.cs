using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext : IdentityDbContext<TmsUser>
{
    public TmsDbContext(DbContextOptions<TmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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