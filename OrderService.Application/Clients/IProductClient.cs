namespace OrderService.Application.Clients;

public interface IProductClient
{
    Task<bool> IsProductAvailableAsync(int productId, int quantity);
}
