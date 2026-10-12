using AWVA.Domain;
using Microsoft.EntityFrameworkCore;

namespace AWVA.Data;

public class AwvaDbContext : DbContext
{
    public AwvaDbContext(DbContextOptions<AwvaDbContext> options) : base(options) { }

    public DbSet<Target> Targets => Set<Target>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<ToolCall> ToolCalls => Set<ToolCall>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<GroundTruthItem> GroundTruthItems => Set<GroundTruthItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.Host).HasMaxLength(255).IsRequired();
            e.Property(x => x.ResolvedIp).HasMaxLength(64);
            e.HasIndex(x => x.Host);
        });

        modelBuilder.Entity<Scan>(e =>
        {
            e.HasOne(x => x.Target).WithMany(t => t.Scans).HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToolCall>(e =>
        {
            e.Property(x => x.ExternalCallId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ToolName).HasMaxLength(128).IsRequired();
            e.Property(x => x.ArgumentsJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.RawOutput).HasColumnType("nvarchar(max)");
            e.Property(x => x.NormalizedOutput).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.Scan).WithMany(s => s.ToolCalls).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ScanId, x.ExternalCallId }).IsUnique();
        });

        modelBuilder.Entity<Finding>(e =>
        {
            e.Property(x => x.ExternalId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.Cwe).HasMaxLength(32).IsRequired();
            e.Property(x => x.OwaspCategory).HasMaxLength(128).IsRequired();
            e.Property(x => x.WstgId).HasMaxLength(32);
            e.Property(x => x.Evidence).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.AffectedUrlsJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Remediation).HasColumnType("nvarchar(max)").IsRequired();
            e.HasOne(x => x.Scan).WithMany(s => s.Findings).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SourceToolCall).WithMany(t => t.Findings).HasForeignKey(x => x.SourceToolCallId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(256).IsRequired();
            e.Property(x => x.DetailsJson).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.Scan).WithMany(s => s.AuditLogEntries).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroundTruthItem>(e =>
        {
            e.Property(x => x.ExternalId).HasMaxLength(64).IsRequired();
            e.Property(x => x.OwaspCategory).HasMaxLength(128).IsRequired();
            e.Property(x => x.Cwe).HasMaxLength(32).IsRequired();
            e.Property(x => x.WstgId).HasMaxLength(32);
            e.Property(x => x.Description).HasColumnType("nvarchar(max)").IsRequired();
            e.HasOne(x => x.Target).WithMany(t => t.GroundTruthItems).HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
