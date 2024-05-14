using Newtonsoft.Json;

namespace NetBatching.Worker;

public class MessageReceiver : IMessageReceiver
{
    private readonly ILogger<MessageReceiver> _logger;

    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly IMessageProcessor _processor;

    public MessageReceiver(ILogger<MessageReceiver> logger,
                           IMessageProcessor processor,
                           IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;

        _processor = processor;

        _hostApplicationLifetime = hostApplicationLifetime;

        _hostApplicationLifetime.ApplicationStopped.Register(() => StopReceiving());
    }

    public void Initialize()
    {
        Task.Factory.StartNew(GetMessageFromTopic, TaskCreationOptions.LongRunning);

        _processor.Initialize();
    }

    public void GetMessageFromTopic()
    {
        while (!_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            var msg = JsonConvert.SerializeObject(new { Id = Guid.NewGuid().ToString(), Date = DateTimeOffset.UtcNow });

            // create a constant stream of messages. do not let up...
            // but don't block (i.e. do a relatively long operation)
            // instead put everything we get onto an internal queue
            // and hand-off that long processing to something else 
            // using a staging queue
            _processor.Enqueue(msg);
        }

        _logger.LogInformation("Finished MessageReceiver::GetMessageFromTopic()");
    }

    private void StopReceiving()
    {
        _logger.LogInformation($"MessageReceiver stopped receiving at: {DateTimeOffset.Now}");
    }
}
