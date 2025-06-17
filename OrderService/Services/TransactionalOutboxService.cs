using System;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Objects;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using System.Text;

namespace OrderService.Services;

public class TransactionalOutboxService : BackgroundService
{
    private readonly IServiceProvider ServiceProvider_;
    private readonly MessageBusSettings MessageBusSettings_;
    private readonly ConnectionFactory ConnectionFactory_;
    private IConnection Connection_;
    private RabbitMQ.Client.IModel Model_;


    public TransactionalOutboxService(IServiceProvider provider, MessageBusSettings settings)
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


    private Task ProcessMessageAsync(OutboxMessage message, OrderDbContext context, CancellationToken cancellationToken)
    {
        var paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(message.Data);
        try
        {
            if (Connection_ == null || Model_ == null || !Connection_.IsOpen || !Model_.IsOpen)
            {
                try
                {
                    Connection_?.Close();
                    Model_?.Close();
                    Connection_ = ConnectionFactory_.CreateConnection();
                    Model_ = Connection_.CreateModel();
                    Model_.QueueDeclare(
                        queue: MessageBusSettings_.RequestQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );
                }
                catch (Exception)
                {
                    throw new Exception("Error: can't connect to RabbitMQ.");
                }
            }
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paymentRequest));
            Model_.BasicPublish(
                exchange: "",
                routingKey: MessageBusSettings_.RequestQueue,
                basicProperties: null,
                body: messageBytes);
        }
        catch (Exception)
        {
            throw new Exception("Error: can't send payment request to RabbitMQ.");
        }
        return Task.CompletedTask;
    }


    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider_.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedUtc == null)
            .OrderBy(m => m.CreatedUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, context, cancellationToken);
                message.Error = string.Empty;
                message.ProcessedUtc = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                message.Error = exception.Message;
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }


    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Connection_ = ConnectionFactory_.CreateConnection();
            Model_ = Connection_.CreateModel();
            Model_.QueueDeclare(
                queue: MessageBusSettings_.RequestQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
            Model_.QueueDeclare(
                queue: MessageBusSettings_.ResponseQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
             );
        }
        catch (Exception){}

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception) {}

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Model_?.Close();
        Connection_?.Close();
        await base.StopAsync(cancellationToken);
    }
}

