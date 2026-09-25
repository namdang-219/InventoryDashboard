namespace IID.Infrastructure.Persistence.Configurations;

public sealed class VehicleActionConfiguration : IEntityTypeConfiguration<VehicleAction>
{
    public void Configure(EntityTypeBuilder<VehicleAction> b)
    {
        b.ToTable("VehicleAction");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActionType).HasConversion<int>().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.LoggedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.LoggedAt).IsRequired();
        b.HasQueryFilter(e => e.DeletedAt == null);
        b.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VehicleId, x.LoggedAt }).HasFilter("[DeletedAt] IS NULL").HasDatabaseName("IX_VehicleAction_VehicleId_LoggedAt");
    }
}
