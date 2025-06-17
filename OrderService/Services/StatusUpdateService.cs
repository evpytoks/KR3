using System;
using Microsoft.AspNetCore.Connections;
using System.Threading.Channels;
using Objects;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Objects.Enums;

namespace OrderService.Services;

public class StatusUpdateService : BackgroundService
{
    private readonly IServiceProvider ServiceProvider_;
    private readonly MessageBusSettings MessageBusSettings_;
    private readonly ConnectionFactory ConnectionFactory_;
    private IConnection Connection_;
    private IModel Model_;


    public StatusUpdateService(IServiceProvider provider, MessageBusSettings settings)
	{
        ServiceProvider_ = provider;
        MessageBusSettings_ = settings;
        ConnectionFactory_ = new ConnectionFactory
        {
            HostName = MessageBusSettings_.Host,
            Port = MessageBusSettings_.Port,
            UserName = MessageBusSettings_.UserName,
            Password = MessageBusSettings_.Password
        };
    }


    private async Task ProcessPaymentAsync(PaymentResponse response)
    {
        using var scope = ServiceProvider_.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        var status = response.Success ? StatusEnum.Finished : StatusEnum.Cancelled;

        await orderService.UpdateStatusAsync(response.OrderId, status);
    }


    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Connection_ = ConnectionFactory_.CreateConnection();
        Model_ = Connection_.CreateModel();
        Model_.QueueDeclare(
            queue: MessageBusSettings_.ResponseQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        return base.StartAsync(cancellationToken);
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(Model_);
        consumer.Received += async (model, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(message);
                if (paymentResponse != null)
                {
                    await ProcessPaymentAsync(paymentResponse);
                    Model_.BasicAck(eventArgs.DeliveryTag, false);
                }
            }
            catch (Exception)
            {

                Model_.BasicNack(eventArgs.DeliveryTag, false, true);
            }
        };

        Model_.BasicQos(0, 1, false);
        Model_.BasicConsume(
            queue: MessageBusSettings_.ResponseQueue,
            autoAck: false,
            consumer: consumer
        );
        return Task.CompletedTask;
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {

        Model_?.Close();
        Connection_?.Close();
        return base.StopAsync(cancellationToken);
    }
}

