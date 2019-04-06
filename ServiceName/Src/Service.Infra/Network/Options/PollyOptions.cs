namespace Service.Infra.Network.Options
{
    public class PollyOptions
    {
        public static string Section = "Polly";
        public int Timeout { get; set; } = 5000;
        public RetryOptions Retry { get; set; } = new RetryOptions();
        public CircuitBreakOptions CircuitBreak { get; set; } = new CircuitBreakOptions();
    }
}