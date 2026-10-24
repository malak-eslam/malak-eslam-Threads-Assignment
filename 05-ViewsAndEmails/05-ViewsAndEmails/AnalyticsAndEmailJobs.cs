namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly Dictionary<string, int> Views = new();
    public readonly Queue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _lock = new();
    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        lock (_lock)
        {
            if (!Views.ContainsKey(product))
                Views.Add(product, 0);

            Views[product] = Views[product] + 1;
        }
    }

    public void PlaceOrder(int orderId)
    {
        lock (_lock)
        {
            EmailJobs.Enqueue($"Email: order {orderId}");
        }
    }

    public void EmailWorker()
    {
        while (true)
        {
        
                string? job = null;
                lock (_lock)
                {
                    if (EmailJobs.Count > 0)
                        job = EmailJobs.Dequeue();
                }
                    if (job is not null)
                    {
                        lock (_sentSync)
                            SentEmails.Add(job);
                        continue;
                    }

                lock (_lock)
                {
                    if (ProducersDone && EmailJobs.Count == 0)
                        return;
                }
                Thread.Sleep(1);
            
        }
    }
}
