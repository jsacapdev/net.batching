namespace NetBatching.Worker;

public interface IMessageProcessor
{
    void Initialize();

    void Enqueue(string message);

    void Enqueue(MessageBatch<string> eventDataBatch);
}
