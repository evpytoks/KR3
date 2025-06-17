using Microsoft.EntityFrameworkCore;
using OrderService.Services;
using OrderService.Data;
using Objects;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb")));
builder.Services.AddScoped<OrderService.Services.OrderService>();

var messageBusSettings = builder.Configuration.GetSection("MessageBus").Get<MessageBusSettings>()
    ?? new MessageBusSettings();
builder.Services.AddSingleton(messageBusSettings);

builder.Services.AddHostedService<TransactionalOutboxService>();
builder.Services.AddHostedService<StatusUpdateService>();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
