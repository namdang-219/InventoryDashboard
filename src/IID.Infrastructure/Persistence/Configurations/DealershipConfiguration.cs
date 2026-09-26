using IID.Domain.Dealerships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IID.Infrastructure.Persistence.Configurations;

public sealed class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> b)
    {
        b.ToTable("Dealerships");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.State).HasMaxLength(32).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(50).IsRequired();

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Dealership_Code");

        b.HasMany<Vehicle>()
            .WithOne(v => v.Dealership)
            .HasForeignKey(v => v.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Ignore(x => x.DomainEvents);
    }
}
