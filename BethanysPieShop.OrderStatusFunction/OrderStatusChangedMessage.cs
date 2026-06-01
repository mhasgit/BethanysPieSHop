using System.Text.Json.Serialization;

namespace BethanysPieShop.OrderStatusFunction
{
    public class OrderStatusChangedMessage
    {
        [JsonPropertyName("OrderId")]
        public int OrderId { get; set; }

        [JsonPropertyName("Email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("CustomerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("NewStatus")]
        public string NewStatus { get; set; } = string.Empty;
    }
}
