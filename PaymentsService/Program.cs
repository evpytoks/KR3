using PaymentsService.Data;
using Microsoft.EntityFrameworkCore;
using Objects;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDb")));
builder.Services.AddScoped<PaymentsService.Services.AccountsService>();

var messageBusSettings = builder.Configuration.GetSection("MessageBus").Get<MessageBusSettings>()
    ?? new MessageBusSettings();
builder.Services.AddSingleton(messageBusSettings);

builder.Services.AddHostedService<PaymentsService.Services.TransactionalOutboxService>();
builder.Services.AddHostedService<PaymentsService.Services.PaymentsService>();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

