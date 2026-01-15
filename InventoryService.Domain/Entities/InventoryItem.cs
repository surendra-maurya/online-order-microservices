namespace InventoryService.Domain.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int AvailableStock { get; set; }
}
