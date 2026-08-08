namespace OrderService.Application.Services
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

}
