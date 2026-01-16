using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace OrderService.Application.Clients;

public class ProductClient : IProductClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
    {
        var token = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(token))
        {
            _client.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);
        }

        return await _client.GetFromJsonAsync<bool>(
            $"/api/products/{productId}/availability/{quantity}");
    }
}
