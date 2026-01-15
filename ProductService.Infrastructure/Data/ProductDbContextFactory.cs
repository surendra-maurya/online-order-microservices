using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductService.Infrastructure.Data;

public class ProductDbContextFactory
    : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=sqlserver;Database=ProductDB;User Id=sa;Password=Surendra@123;TrustServerCertificate=True");

        return new ProductDbContext(optionsBuilder.Options);
    }
}
