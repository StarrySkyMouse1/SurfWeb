using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurfWeb.Configurations;
using SurfWeb.Repositories.Entities;

namespace SurfWeb.Repositories.Persistence;

public sealed class ShavitDbContext : DbContext
{
    private readonly byte _defaultStyleId;

    public ShavitDbContext(DbContextOptions<ShavitDbContext> options, IOptions<SurfWebOptions> surfOptions)
        : base(options)
    {
        _defaultStyleId = surfOptions.Value.DefaultStyleId;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<MapTier> MapTiers => Set<MapTier>();
    public DbSet<PlayerTime> PlayerTimes => Set<PlayerTime>();
    public DbSet<StageTime> StageTimes => Set<StageTime>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Auth);
            e.Property(x => x.Name).HasMaxLength(32);
        });

        modelBuilder.Entity<MapTier>(e =>
        {
            e.ToTable("maptiers");
            e.HasKey(x => x.Map);
            e.Property(x => x.Map).HasMaxLength(255);
        });

        modelBuilder.Entity<PlayerTime>(e =>
        {
            e.ToTable("playertimes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Map).HasMaxLength(255);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.Auth).HasPrincipalKey(u => u.Auth);
            e.HasQueryFilter(pt => pt.Style == _defaultStyleId);
        });

        modelBuilder.Entity<StageTime>(e =>
        {
            e.ToTable("stagetimes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Map).HasMaxLength(255);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.Auth).HasPrincipalKey(u => u.Auth);
            e.HasQueryFilter(st => st.Style == _defaultStyleId);
        });
    }

    public override int SaveChanges() =>
        throw new InvalidOperationException("Shavit database is read-only.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Shavit database is read-only.");
}
