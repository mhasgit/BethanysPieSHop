using BethanysPieShop.Models;
using BethanysPieShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace BethanysPieShop.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusQueueService _orderStatusQueueService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(IOrderRepository orderRepository, IOrderStatusQueueService orderStatusQueueService, ILogger<OrdersApiController> logger)
        {
            _orderRepository = orderRepository;
            _orderStatusQueueService = orderStatusQueueService;
            _logger = logger;
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrEmpty(request.Status))
                return BadRequest("Status is required.");

            if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var newStatus))
                return BadRequest($"Invalid status. Allowed: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");

            var order = _orderRepository.GetOrderById(id);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update.", id);
                return NotFound();
            }

            _orderRepository.UpdateOrderStatus(id, newStatus);

            try
            {
                await _orderStatusQueueService.EnqueueOrderStatusChangedAsync(
                    new OrderStatusChangedMessage
                    {
                        OrderId = order.OrderId,
                        Email = order.Email,
                        CustomerName = $"{order.FirstName} {order.LastName}".Trim(),
                        NewStatus = newStatus.ToString()
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue order status change for order {OrderId}.", order.OrderId);
                throw;
            }

            _logger.LogInformation("Order {OrderId} status updated to {NewStatus}.", order.OrderId, newStatus);
            return NoContent();
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string? Status { get; set; }
    }
}
