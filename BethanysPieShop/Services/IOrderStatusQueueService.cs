namespace BethanysPieShop.Services
{
    public interface IOrderStatusQueueService
    {
        Task EnqueueOrderStatusChangedAsync(OrderStatusChangedMessage message, CancellationToken cancellationToken = default);
    }
}
