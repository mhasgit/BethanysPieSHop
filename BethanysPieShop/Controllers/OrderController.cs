using BethanysPieShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace BethanysPieShop.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IShoppingCart _shoppingCart;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, IShoppingCart shoppingCart, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _shoppingCart = shoppingCart;
            _logger = logger;
        }

        public IActionResult Checkout() //Get
        {
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            var items = _shoppingCart.GetShoppingCartItems();
            _shoppingCart.ShoppingCartItems = items;

            if (_shoppingCart.ShoppingCartItems.Count == 0)
            {
                _logger.LogWarning("Checkout attempted with empty cart.");
                ModelState.AddModelError("", "Your cart is empty, add some pies first");
            }

            if (ModelState.IsValid)
            {
                _orderRepository.CreateOrder(order);
                _logger.LogInformation("Order {OrderId} created.", order.OrderId);
                _shoppingCart.ClearCart();
                return RedirectToAction("CheckoutComplete");
            }
            return View(order);
        }

        public IActionResult CheckoutComplete()
        {
            ViewBag.CheckoutCompleteMessage = "Thanks for your order. You'll soon enjoy our delicious pies";
            return View();
        }
    }
}
