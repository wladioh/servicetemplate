namespace Service.Infra.MessageBus
{
    public class MessageBusOptions
    {
        public static string Section => "MessageBus";
        public string ConnectionString { get; set; }
        public string Queue { get; set; }
        public int Retry { get; set; } = 5;
        public string ErrorQueue { get; set; } = "Errors";
        public int MaxParallelism { get; set; } = 20;
        public int NumberOfWorkers { get; set; } = 2;
        public int Prefetch { get; set; } = 30;
        public bool UseAzureServiceBus { get; set; } = false;
    }
}
