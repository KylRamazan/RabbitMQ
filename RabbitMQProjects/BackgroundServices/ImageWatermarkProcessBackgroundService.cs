using System.Drawing;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQProjects.Services;

namespace RabbitMQProjects.BackgroundServices
{
    public class ImageWatermarkProcessBackgroundService:BackgroundService
    {
        private readonly RabbitMQClientService _rabbitmqClientService;
        private readonly ILogger<ImageWatermarkProcessBackgroundService> _logger;
        private IChannel _channel;

        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitmqClientService, ILogger<ImageWatermarkProcessBackgroundService> logger)
        {
            _rabbitmqClientService = rabbitmqClientService;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var channelTask = _rabbitmqClientService.Connect();
            _channel = channelTask.Result;
            _channel.BasicQosAsync(0, 1, false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsumeAsync(RabbitMQClientService.QueueName, false, consumer);
            consumer.ReceivedAsync += Consumer_ReceivedAsync;

            return Task.CompletedTask;
        }

        private Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                var productImageCreatedEvent = JsonSerializer.Deserialize<ProductImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray()));

                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", productImageCreatedEvent.ImageName);
                string watermarkText = "RabbitMQ Dersleri";
                using var img = Image.FromFile(path);
                using var graphic = Graphics.FromImage(img);

                Font font = new Font(FontFamily.GenericMonospace, 32, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = graphic.MeasureString(watermarkText, font);
                Color color = Color.FromArgb(128, 255, 255, 255);
                SolidBrush brush = new SolidBrush(color);
                Point position = new Point(img.Width - ((int)textSize.Width + 30), img.Height - ((int)textSize.Height + 30));

                graphic.DrawString(watermarkText, font, brush, position);
                img.Save("wwwroot/images/watermarks/" + productImageCreatedEvent.ImageName);
                img.Dispose();
                graphic.Dispose();

                _channel.BasicAckAsync(@event.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
