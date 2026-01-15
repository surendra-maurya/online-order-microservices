using ProductService.Application.DTOs;

namespace ProductService.Application.Interfaces;

public interface IProductService
{
    Task<int> CreateProductAsync(CreateProductDto dto);
    Task<bool> IsProductAvailableAsync(int productId, int quantity);
}
