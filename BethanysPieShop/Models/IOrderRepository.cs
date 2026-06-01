namespace BethanysPieShop.Models
{
    public interface IOrderRepository
    {
        void CreateOrder(Order order);
        Order? GetOrderById(int orderId);
        void UpdateOrderStatus(int orderId, OrderStatus status);
    }
}
