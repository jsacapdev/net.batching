using System.Collections.Concurrent;

namespace NetBatching;

public class ProducerConsumer
{
    private static int BATCH_SIZE = 100;

    private readonly BlockingCollection<string> _staging = [];

    private readonly BlockingCollection<string> _readyToBeBatched = new BlockingCollection<string>(BATCH_SIZE);

    List<MessageBatch<string>> _batches = [];

    public ProducerConsumer()
    {
        Task.Factory.StartNew(CreateMessages, TaskCreationOptions.LongRunning);

        Task.Factory.StartNew(CreateBatch, TaskCreationOptions.LongRunning);

        Task.Factory.StartNew(ReportOnBatchProgress, TaskCreationOptions.LongRunning);
    }

    public void CreateMessages()
    {
        while (true)
        {
            var msg = DateTimeOffset.UtcNow.ToString();

            // create a constant stream of messages. do not let up...
            // but don't block (i.e. do a relatively long operation)
            // instead put everything we get onto an internal queue
            // and hand-off that long processing to something else 
            // using a staging queue
            _staging.Add(msg);
        }
    }

    public void PlaySomething()
    {
        // take everything off the staging queue as soon as it arrives
        foreach (var item in _staging.GetConsumingEnumerable())
        {
            // and simulate something that will take a little while to complete 
            Thread.Sleep(50);

            // _readyToBeBatched.Add(item);
        }
    }

    public void CreateBatch()
    {
        while (true)
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
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Batch Loop Done!");
            }
            finally
            {
                // if we have items in our batch create a new batch
                // ready for consumption
                if (_batches.Count > 0)
                {
                    _batches.Add(new MessageBatch<string>(readyToBeBatched));
                }
            }

            Console.WriteLine($"Collection size now at {_readyToBeBatched.Count}");
        }
    }

    public void ReportOnBatchProgress()
    {
        while (true)
        {
            Thread.Sleep(20000);

            Console.WriteLine();

            foreach (var item in _batches)
            {
                Console.WriteLine($"Batch Id {item.Id} -> size {item.Items.Count}");
            }

            Console.WriteLine();
        }
    }

}
