using Microsoft.Extensions.Azure;

namespace NetBatching.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        builder.Services.AddSingleton<IMessageReceiver, MessageReceiver>();
        builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();
        builder.Services.AddSingleton<IMessageSender, EventHubMessageSender>();

        builder.Services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddEventHubProducerClient(builder.Configuration["EventHub:ConnectionString"],
                                                    builder.Configuration["EventHub:Name"]);
        });

        var host = builder.Build();
        host.Run();
    }
}