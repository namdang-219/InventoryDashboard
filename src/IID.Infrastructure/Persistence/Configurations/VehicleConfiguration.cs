namespace IID.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("Vehicle");
        b.HasKey(x => x.Id);

        b.Property(x => x.Vin)
            .HasConversion(v => v.Value, (string s) => Vin.Parse(s))
            .HasMaxLength(17)
            .IsRequired();

        b.Property(x => x.StockNumber)
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.Make).HasMaxLength(50).IsRequired();
        b.Property(x => x.Model).HasMaxLength(50).IsRequired();
        b.Property(x => x.Year).IsRequired();
        b.Property(x => x.Color).HasMaxLength(30).IsRequired();
        b.Property(x => x.Mileage).IsRequired();

        b.Property(x => x.FuelType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        b.OwnsOne(x => x.PurchasePrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("PurchasePriceAmount").HasColumnType("decimal(18,4)");
            m.Property(p => p.Currency).HasColumnName("PurchasePriceCurrency").HasMaxLength(3);
        });

        b.OwnsOne(x => x.AskingPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("AskingPriceAmount").HasColumnType("decimal(18,4)");
            m.Property(p => p.Currency).HasColumnName("AskingPriceCurrency").HasMaxLength(3);
        });

        b.OwnsOne(x => x.SoldPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("SoldPriceAmount").HasColumnType("decimal(18,4)");
            m.Property(p => p.Currency).HasColumnName("SoldPriceCurrency").HasMaxLength(3);
        });

        b.Property(x => x.SoldAt);

        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.DateAddedToInventory).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.CreatedByUserId).HasMaxLength(450);
        b.Property(x => x.UpdatedByUserId).HasMaxLength(450);
        b.Property(x => x.DeletedAt);
        b.Property(x => x.RowVersion)
            .IsRowVersion()
            .HasColumnName("RowVersion")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // EF Core 10 named query filter
        b.HasQueryFilter(e => e.DeletedAt == null);
        b.HasIndex(x => x.Vin).IsUnique().HasFilter("[DeletedAt] IS NULL").HasDatabaseName("UX_Vehicle_Vin_Active");
        b.HasIndex(x => x.StockNumber).IsUnique().HasFilter("[DeletedAt] IS NULL").HasDatabaseName("UX_Vehicle_StockNumber_Active");
        b.HasIndex(x => new { x.Status, x.DateAddedToInventory }).HasDatabaseName("IX_Vehicle_Status_DateAdded");
        b.HasIndex(x => x.FuelType).HasDatabaseName("IX_Vehicle_FuelType");
        b.HasIndex(x => x.DealershipId).HasDatabaseName("IX_Vehicle_DealershipId");

        // Domain events and computed properties are not persisted
        b.Ignore(x => x.DomainEvents);
        b.Ignore(x => x.GrossProfit);
        b.Ignore(x => x.GrossMarginPercent);
    }
}
