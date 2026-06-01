using Microsoft.Extensions.Logging;

namespace BethanysPieShop.Models
{
    public class OrderRepository : IOrderRepository
    {
        private readonly BethanysPieShopDbContext _bethanysPieShopDbContext;
        private readonly IShoppingCart _shoppingCart;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(BethanysPieShopDbContext bethanysPieShopDbContext, IShoppingCart shoppingCart, ILogger<OrderRepository> logger)
        {
            _bethanysPieShopDbContext = bethanysPieShopDbContext;
            _shoppingCart = shoppingCart;
            _logger = logger;
        }

        public void CreateOrder(Order order)
        {
            order.OrderPlaced = DateTime.Now;
            order.Status = OrderStatus.Pending;

            List<ShoppingCartItem>? shoppingCartItems = _shoppingCart.ShoppingCartItems;
            order.OrderTotal = _shoppingCart.GetShoppingCartTotal();

            order.OrderDetails = new List<OrderDetail>();

            //adding the order with its details

            foreach (ShoppingCartItem? shoppingCartItem in shoppingCartItems)
            {
                var orderDetail = new OrderDetail
                {
                    Amount = shoppingCartItem.Amount,
                    PieId = shoppingCartItem.Pie.PieId,
                    Price = shoppingCartItem.Pie.Price
                };

                order.OrderDetails.Add(orderDetail);
            }

            _bethanysPieShopDbContext.Orders.Add(order);

            _bethanysPieShopDbContext.SaveChanges();
            _logger.LogInformation("Order {OrderId} created and saved.", order.OrderId);
        }

        public Order? GetOrderById(int orderId)
        {
            return _bethanysPieShopDbContext.Orders.Find(orderId);
        }

        public void UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var order = _bethanysPieShopDbContext.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = status;
                _bethanysPieShopDbContext.SaveChanges();
                _logger.LogInformation("Order {OrderId} status updated to {Status}.", orderId, status);
            }
        }
    }
}
