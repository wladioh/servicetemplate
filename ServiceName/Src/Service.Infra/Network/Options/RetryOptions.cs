namespace Service.Infra.Network.Options
{
    public class RetryOptions
    {
        public int MaxRetries { get; set; } = 5;
        public int MaxDelay { get; set; } = 200;
    }
}
