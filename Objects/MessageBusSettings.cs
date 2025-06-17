using System;
namespace Objects;

public class MessageBusSettings
{
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string RequestQueue { get; set; } = "requests";
    public string ResponseQueue { get; set; } = "responses";
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
