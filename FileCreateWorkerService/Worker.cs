using System.Data;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using FileCreateWorkerService.Models;
using FileCreateWorkerService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMQClientService _rabbitMqClientService;
        private readonly IServiceProvider _serviceProvider;
        private IChannel _channel;

        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitMqClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMqClientService = rabbitMqClientService;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var channelTask = _rabbitMqClientService.Connect();
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

        private async Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                await Task.Delay(5000);

                var createExcelMessage = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));

                using var ms = new MemoryStream();
                var wb = new XLWorkbook();
                var ds = new DataSet();
                ds.Tables.Add(GetTable("products"));
                wb.Worksheets.Add(ds);
                wb.SaveAs(ms);

                MultipartFormDataContent multipartFormDataContent = new();
                multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()), "file", Guid.NewGuid() + ".xlsx");

                string baseUrl = "https://localhost:7101/api/files";
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsync($"{baseUrl}?fileId={createExcelMessage?.FileId}", multipartFormDataContent);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"File(Id:{createExcelMessage?.FileId}) was created by successful");
                        await _channel.BasicAckAsync(@event.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogError($"Failed to create file with FileId:{createExcelMessage?.FileId}. StatusCode: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private DataTable GetTable(string tableName)
        {
            List<Product> products;

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdventureWorks2019Context>();
                products = context.Products.ToList();
            }

            DataTable table = new DataTable {TableName = tableName};
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ProductNumber", typeof(string));
            table.Columns.Add("Color", typeof(string));

            products.ForEach(x =>
            {
                table.Rows.Add(x.ProductId, x.Name, x.ProductNumber, x.Color);
            });

            return table;
        }
    }
}
