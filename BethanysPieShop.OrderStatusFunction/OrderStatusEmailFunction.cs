using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BethanysPieShop.OrderStatusFunction
{
    public class OrderStatusEmailFunction
    {
        private readonly ILogger<OrderStatusEmailFunction> _logger;

        public OrderStatusEmailFunction(ILogger<OrderStatusEmailFunction> logger)
        {
            _logger = logger;
        }

        [Function("OrderStatusChanged")]
        public async Task Run(
            [QueueTrigger("order-status-changed", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            _logger.LogInformation("OrderStatusChanged triggered. Message: {Message}", queueMessage);

            OrderStatusChangedMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<OrderStatusChangedMessage>(queueMessage);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize queue message.");
                throw;
            }

            if (message == null || string.IsNullOrEmpty(message.Email))
            {
                _logger.LogWarning("Invalid message: missing Email or deserialization returned null.");
                return;
            }

            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("SendGridApiKey is not configured.");
                throw new InvalidOperationException("SendGridApiKey is not configured.");
            }

            var fromEmail = Environment.GetEnvironmentVariable("SendGridFromEmail") ?? "noreply@example.com";
            var fromName = Environment.GetEnvironmentVariable("SendGridFromName") ?? "Bethany's Pie Shop";

            var subject = $"Order #{message.OrderId} status update";
            var plainText = $"Hello {message.CustomerName},\r\n\r\nYour order #{message.OrderId} status has been updated to: {message.NewStatus}.\r\n\r\nThank you for your order!";
            var htmlContent = $"<p>Hello {message.CustomerName},</p><p>Your order <strong>#{message.OrderId}</strong> status has been updated to: <strong>{message.NewStatus}</strong>.</p><p>Thank you for your order!</p>";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(message.Email, message.CustomerName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Order status email sent to {Email} for order {OrderId}.", message.Email, message.OrderId);
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid failed. Status: {Status}, Body: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"SendGrid failed: {response.StatusCode} - {body}");
            }
        }
    }
}
