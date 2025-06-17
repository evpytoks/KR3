using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using Objects;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using PaymentsService.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace PaymentsService.Services;

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
            Password = MessageBusSettings_.Password,
            RequestedConnectionTimeout = MessageBusSettings_.ConnectionTimeout,
            SocketReadTimeout = MessageBusSettings_.ReadTimeout,
            SocketWriteTimeout = MessageBusSettings_.WriteTimeout,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }


    private Task ProcessMessageAsync(OutboxMessage message, PaymentsDbContext context, CancellationToken cancellationToken)
    {
        var response = JsonSerializer.Deserialize<PaymentResponse>(message.Data);
        try
        {
            if (Model_ != null && Model_.IsOpen)
            {
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                Model_.BasicPublish(
                    exchange: "",
                    routingKey: MessageBusSettings_.ResponseQueue,
                    basicProperties: null,
                    body: bytes
                );
            }
        }
        catch (Exception exception)
        {
            throw new Exception($"Error: {exception}");
        }
        return Task.CompletedTask;
    }


    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider_.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var messages = await context.OutboxMessages
                .Where(e => e.ProcessedUtc == null)
                .OrderBy(e => e.CreatedUtc)
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
                message.ProcessedUtc = DateTime.UtcNow;
                message.Error = string.Empty;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Connection_ == null || Model_ == null || !Connection_.IsOpen || !Model_.IsOpen)
                {
                    try
                    {
                        if (Model_ != null)
                        {
                            Model_.Dispose();
                            Model_ = null;
                        }
                        if (Connection_ != null)
                        {
                            Connection_.Dispose();
                            Connection_ = null;
                        }

                        Connection_ = ConnectionFactory_.CreateConnection();
                        Model_ = Connection_.CreateModel();
                        Model_.QueueDeclare(
                            queue: MessageBusSettings_.ResponseQueue,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null
                        );

                    }
                    catch (Exception)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                }
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                throw new Exception($"Error: {exception}.");
            }

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

