using System.Collections.Concurrent;

namespace NetBatching.Worker;

public class MessageProcessor : IMessageProcessor
{
    private readonly ILogger<MessageProcessor> _logger;

    private readonly IMessageSender _sender;

    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly BlockingCollection<string> _staging = [];

    private readonly BlockingCollection<string> _readyToBeBatched = [];

    private readonly BlockingCollection<MessageBatch<string>> _events = [];

    private readonly int _stagingProcessingThreadCount = 1;

    private readonly int _batchCreationThreadCount = 1;

    private readonly int _batchProcessingThreadCount = 1;

    public MessageProcessor(ILogger<MessageProcessor> logger,
                            IMessageSender sender,
                            IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;

        _sender = sender;

        _hostApplicationLifetime = hostApplicationLifetime;

        _hostApplicationLifetime.ApplicationStopped.Register(() => StopProcessing());
    }

    public void Initialize()
    {
        for (int i = 0; i < _stagingProcessingThreadCount; i++)
        {
            Task.Factory.StartNew(ProcessStagingMessages, TaskCreationOptions.LongRunning);
        }

        for (int i = 0; i < _batchCreationThreadCount; i++)
        {
            Task.Factory.StartNew(CreateMessageBatch, TaskCreationOptions.LongRunning);
        }

        for (int i = 0; i < _batchProcessingThreadCount; i++)
        {
            Task.Factory.StartNew(ProcessBatchesAsync, TaskCreationOptions.LongRunning);
        }

        _logger.LogInformation($"Initialized MessageProcessor at: {DateTimeOffset.Now}");
    }

    public void Enqueue(string message)
    {
        _staging.Add(message);
    }

    private void ProcessStagingMessages()
    {
        foreach (var item in _staging.GetConsumingEnumerable())
        {
            // and simulate something that will take a little while to complete 
            Thread.Sleep(50);

            _readyToBeBatched.Add(item);
        }
    }

    public void CreateMessageBatch()
    {
        while (!_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            List<string> readyToBeBatched = [];

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(6500);

            try
            {
                // add items that are ready to be batched into a batch.
                // this call will block until an item becomes available.
                // stop the batching after an internal. this is because 
                // receiving messages is not linear. we may have hours 
                // between messages being received, and so take the latest
                // messages and batch them
                foreach (var readyItem in _readyToBeBatched.GetConsumingEnumerable(cts.Token))
                {
                    readyToBeBatched.Add(readyItem);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // if we have items in our batch create a new batch
                // ready for consumption
                if (readyToBeBatched.Count > 0)
                {
                    Enqueue(new MessageBatch<string>(readyToBeBatched));
                }
            }
        }

        _logger.LogInformation("Finished MessageProcessor::CreateMessageBatch()");
    }

    public void Enqueue(MessageBatch<string> eventDataBatch)
    {
        _events.Add(eventDataBatch);
    }

    private async Task ProcessBatchesAsync()
    {
        foreach (var item in _events.GetConsumingEnumerable())
        {
            await ProcessItemAsync(item);
        }
    }

    private async Task ProcessItemAsync(MessageBatch<string> eventBatch)
    {
        await _sender.SendBatchAsync(eventBatch);
    }

    private void StopProcessing()
    {
        _events.CompleteAdding();

        _logger.LogInformation($"MessageProcessor stopped processing at: {DateTimeOffset.Now}");
    }
}
