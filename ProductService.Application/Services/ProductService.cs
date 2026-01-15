using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _db;

    public ProductService(ProductDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.Stock
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return product.Id;
    }

    public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
    {
        var product = await _db.Products.FindAsync(productId);
        return product != null && product.Stock >= quantity;
    }
}
