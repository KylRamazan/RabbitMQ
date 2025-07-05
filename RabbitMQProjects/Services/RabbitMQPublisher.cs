using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace RabbitMQProjects.Services
{
    public class RabbitMQPublisher
    {
        private readonly RabbitMQClientService _clientService;

        public RabbitMQPublisher(RabbitMQClientService clientService)
        {
            _clientService = clientService;
        }

        public async void Publish(ProductImageCreatedEvent productImageCreatedEvent)
        {
            var channel = await _clientService.Connect();
            string bodyString = JsonSerializer.Serialize(productImageCreatedEvent);
            byte[] bodyByte = Encoding.UTF8.GetBytes(bodyString);
            var properties = new BasicProperties
            {
                ContentType = "text/plain",
                DeliveryMode = DeliveryModes.Persistent,
                Priority = 0
            };

            await channel.BasicPublishAsync(exchange: RabbitMQClientService.ExchangeName,
                routingKey: RabbitMQClientService.RoutingWatermark,
                true,
                basicProperties: properties,
                body: bodyByte);
        }
    }
}
