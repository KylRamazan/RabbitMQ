using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared;

namespace RabbitMQExcelCreate.Services
{
    public class RabbitMQPublisher
    {
        private readonly RabbitMQClientService _clientService;

        public RabbitMQPublisher(RabbitMQClientService clientService)
        {
            _clientService = clientService;
        }

        public async void Publish(CreateExcelMessage createExcelMessage)
        {
            var channel = await _clientService.Connect();
            string bodyString = JsonSerializer.Serialize(createExcelMessage);
            byte[] bodyByte = Encoding.UTF8.GetBytes(bodyString);
            var properties = new BasicProperties
            {
                ContentType = "text/plain",
                DeliveryMode = DeliveryModes.Persistent,
                Priority = 0
            };

            await channel.BasicPublishAsync(exchange: RabbitMQClientService.ExchangeName,
                routingKey: RabbitMQClientService.RoutingExcel,
                true,
                basicProperties: properties,
                body: bodyByte);
        }
    }
}
