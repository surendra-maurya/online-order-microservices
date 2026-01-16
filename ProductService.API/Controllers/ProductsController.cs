using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;

namespace ProductService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var id = await _service.CreateProductAsync(dto);
        return Ok(id);
    }

    [HttpGet("{id}/availability/{qty}")]
    public async Task<IActionResult> CheckAvailability(int id, int qty)
    {
        var available = await _service.IsProductAvailableAsync(id, qty);
        return Ok(available);
    }
}
