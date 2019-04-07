namespace Service.Infra.Network.Options
{
    public class RetryOptions
    {
        public int MaxRetries { get; set; } = 2;
        public int MaxDelay { get; set; } = 200;
    }
}
