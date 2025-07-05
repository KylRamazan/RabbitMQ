using RabbitMQ.Client;

namespace RabbitMQExcelCreate.Services
{
    public class RabbitMQClientService:IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IChannel _channel;
        public static string ExchangeName = "ExcelDirectExchange";
        public static string RoutingExcel = "ExcelRouteFile";
        public static string QueueName = "QueueExcelFile";
        private readonly ILogger<RabbitMQClientService> _logger;

        public RabbitMQClientService(ConnectionFactory connectionFactory, ILogger<RabbitMQClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IChannel> Connect()
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            
            if (_channel is { IsOpen:true })
            {
                return _channel;
            }

            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(ExchangeName, "direct", true, false);
            await _channel.QueueDeclareAsync(QueueName, true, false, false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingExcel);

            _logger.LogInformation("RabbitMQ ile bağlantı kuruldu...");
            
            return _channel;
        }

        public void Dispose()
        {
            _channel.CloseAsync();
            _channel.Dispose();
            _connection.Dispose();

            _logger.LogInformation("RabbitMQ ile bağlantı koptu!");
        }
    }
}
