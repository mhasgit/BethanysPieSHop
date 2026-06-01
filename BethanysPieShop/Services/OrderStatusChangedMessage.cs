namespace BethanysPieShop.Services
{
    public class OrderStatusChangedMessage
    {
        public int OrderId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}
