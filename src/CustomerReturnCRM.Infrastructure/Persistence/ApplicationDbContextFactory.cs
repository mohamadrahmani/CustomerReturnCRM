using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomerReturnCRM.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CustomerReturnCRM;Trusted_Connection=True;TrustServerCertificate=True;", sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
