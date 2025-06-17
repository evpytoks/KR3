using System;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Objects;
using PaymentsService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Services;

public class PaymentsService : BackgroundService
{
    private readonly IServiceProvider ServiceProvider_;
    private readonly MessageBusSettings MessageBusSettings_;
    private readonly ConnectionFactory ConnectionFactory_;
    private IConnection? Connection_;
    private IModel? Model_;


    public PaymentsService(IServiceProvider provider, MessageBusSettings settings)
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


    private async Task<bool> IsProcessedAsync(Guid messageId)
    {
        using var scope = ServiceProvider_.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        return await context.InboxMessages.AnyAsync(e => e.MessageId == messageId && e.ProcessedUtc != null);
    }


    private async Task SaveToInboxAsync(PaymentRequest request)
    {
        using var scope = ServiceProvider_.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        if (await context.InboxMessages.AnyAsync(e => e.MessageId == request.MessageId))
        {
            return;
        }

        var message = new InboxMessage
        {
            MessageId = request.MessageId,
            Data = JsonSerializer.Serialize(request),
            CreatedUtc = DateTime.UtcNow
        };

        await context.InboxMessages.AddAsync(message);
        await context.SaveChangesAsync();
    }


    private async Task<bool> ProcessPaymentAsync(PaymentRequest request)
    {
        using var scope = ServiceProvider_.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var accountsService = scope.ServiceProvider.GetRequiredService<AccountsService>();
        using var payment = await context.Database.BeginTransactionAsync();

        try
        {
            var message = await context.InboxMessages.FirstOrDefaultAsync(m => m.MessageId == request.MessageId);
            if (message == null || message.ProcessedUtc != null)
            {
                return false;
            }

            bool isSuccess = await accountsService.PaymentAsync(request, context);
            var response = new PaymentResponse
            {
                OrderId = request.OrderId,
                Success = isSuccess,
                MessageId = Guid.NewGuid(),
                OriginalMessageId = request.MessageId,
                Timestamp = DateTime.UtcNow
            };
            var outboxMessage = new OutboxMessage
            {
                Data = JsonSerializer.Serialize(response),
                CreatedUtc = DateTime.UtcNow
            };

            await context.OutboxMessages.AddAsync(outboxMessage);
            message.ProcessedUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await payment.CommitAsync();
            return true;
        }
        catch (Exception exception)
        {
            await payment.RollbackAsync();
            throw new Exception($"Error: {exception}.");
        }
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
                if (Connection_ == null || Model_ == null || !Connection_.IsOpen)
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
                        queue: MessageBusSettings_.RequestQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    var consumer = new EventingBasicConsumer(Model_);

                    consumer.Received += async (model, eventArgs) =>
                    {
                        var body = eventArgs.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var request = JsonSerializer.Deserialize<PaymentRequest>(message);
                        try
                        {
                            if (request != null && await IsProcessedAsync(request.MessageId))
                            {
                                Model_.BasicAck(eventArgs.DeliveryTag, false);
                                return;
                            }

                            if (request != null)
                            {
                                await SaveToInboxAsync(request);
                                var result = await ProcessPaymentAsync(request);
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
                        queue: MessageBusSettings_.RequestQueue,
                        autoAck: false,
                        consumer: consumer
                    );

                    while (!stoppingToken.IsCancellationRequested && Connection_.IsOpen)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
        return;
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Model_?.Close();
        Connection_?.Close();
        return base.StopAsync(cancellationToken);
    }
}

