using IID.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IID.Api.Data;

public sealed class IidDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IidDbContext>
{
    public IidDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IidDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=IID;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;Encrypt=False;");
        return new IidDbContext(optionsBuilder.Options, null);
    }
}
