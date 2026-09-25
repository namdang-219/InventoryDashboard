using IID.Domain.Notifications;
namespace IID.Infrastructure.Persistence.Configurations;

public sealed class UserActivityReadStatusConfiguration : IEntityTypeConfiguration<UserActivityReadStatus>
{
    public void Configure(EntityTypeBuilder<UserActivityReadStatus> b)
    {
        b.ToTable("UserActivityReadStatus");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ActivityId).HasMaxLength(200).IsRequired();
        b.Property(x => x.IsRead).IsRequired();
        b.Property(x => x.ReadAtUtc).IsRequired();

        b.HasIndex(x => new { x.UserId, x.ActivityId })
            .IsUnique()
            .HasDatabaseName("IX_UserActivityReadStatus_UserId_ActivityId");
    }
}
