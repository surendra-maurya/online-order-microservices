using System.Net.Http.Json;

namespace OrderService.Application.Clients;

public class ProductClient : IProductClient
{
    private readonly HttpClient _client;

    public ProductClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
    {
        return await _client.GetFromJsonAsync<bool>(
            $"/api/products/{productId}/availability/{quantity}");
    }
}
