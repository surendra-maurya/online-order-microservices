using InventoryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Data
{
    public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=sqlserver;Database=InventoryDB;User Id=sa;Password=Surendra@123;TrustServerCertificate=True");

            return new InventoryDbContext(optionsBuilder.Options);
        }
    }
}
