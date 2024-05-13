namespace NetBatching.Worker;

public interface IMessageSender
{
    Task SendBatchAsync(MessageBatch<string> batch);
}
