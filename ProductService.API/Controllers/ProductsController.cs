using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;

namespace ProductService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly ILogger<ProductsController> _logger;
    public ProductsController(IProductService service, ILogger<ProductsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        //var id = await _service.CreateProductAsync(dto);

        _logger.LogInformation(
           "Create Product request started. Name={Name}, Price={Price}, Stock={Stock}",
           dto.Name, dto.Price, dto.Stock);

        try
        {
            var productId = await _service.CreateProductAsync(dto);

            _logger.LogInformation(
                "Product created successfully. ProductId={ProductId}",
                productId);
            return Ok(productId);
            //return CreatedAtAction(nameof(GetById), new { id = productId }, new
            //{
            //    Id = productId,
            //    Message = "Product created successfully"
            //});
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while creating product. Name={Name}",
                dto.Name);

            throw; // handled by global exception middleware
        }

       // return Ok(id);
    }

    [HttpGet("{id}/availability/{qty}")]
    public async Task<IActionResult> CheckAvailability(int id, int qty)
    {
        var available = await _service.IsProductAvailableAsync(id, qty);
        return Ok(available);
    }

}
