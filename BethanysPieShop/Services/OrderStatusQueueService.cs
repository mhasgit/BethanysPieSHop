using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace BethanysPieShop.Services
{
    public class OrderStatusQueueService : IOrderStatusQueueService
    {
        private const string QueueName = "order-status-changed";
        private readonly QueueClient _queueClient;
        private readonly ILogger<OrderStatusQueueService> _logger;

        public OrderStatusQueueService(IConfiguration configuration, ILogger<OrderStatusQueueService> logger)
        {
            _logger = logger;
            var connectionString = configuration["OrderStatusQueueConnection"]
                ?? configuration["ConnectionStrings:OrderStatusQueueConnection"];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("OrderStatusQueueConnection is not configured.");
                throw new InvalidOperationException("OrderStatusQueueConnection is not configured.");
            }
            _queueClient = new QueueClient(connectionString, QueueName);
        }

        public async Task EnqueueOrderStatusChangedAsync(OrderStatusChangedMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                var json = JsonSerializer.Serialize(message);
                await _queueClient.SendMessageAsync(json, cancellationToken: cancellationToken);
                _logger.LogInformation("Order status change enqueued for order {OrderId}.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue order status change for order {OrderId}.", message.OrderId);
                throw;
            }
        }
    }
}
