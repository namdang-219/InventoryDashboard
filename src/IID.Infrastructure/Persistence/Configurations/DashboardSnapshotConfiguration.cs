using IID.Domain.Dashboard;

namespace IID.Infrastructure.Persistence.Configurations;

public sealed class DashboardSnapshotConfiguration : IEntityTypeConfiguration<DashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardSnapshot> b)
    {
        b.ToTable("DashboardSnapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.ScopeKey)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.DealershipId);

        b.Property(x => x.SummaryJson).IsRequired();
        b.Property(x => x.QuickStatsJson).IsRequired();
        b.Property(x => x.ChartsJson).IsRequired();
        b.Property(x => x.ActionCenterJson).IsRequired();
        b.Property(x => x.AiInsightsJson).IsRequired();
        b.Property(x => x.LowInventoryAlertsJson).IsRequired();
        b.Property(x => x.RealtimeAlertsJson).IsRequired();
        b.Property(x => x.ComputedAtUtc).IsRequired();
        b.Property(x => x.Version).IsRequired();

        b.HasIndex(x => x.ScopeKey)
            .IsUnique()
            .HasDatabaseName("UX_DashboardSnapshots_ScopeKey");

        b.HasIndex(x => x.DealershipId)
            .HasDatabaseName("IX_DashboardSnapshots_DealershipId");
    }
}

