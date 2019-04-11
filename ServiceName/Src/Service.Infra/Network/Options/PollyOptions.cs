namespace Service.Infra.Network.Options
{
    public class PollyOptions
    {
        public static string Section = "Resiliencie";
        public int Timeout { get; set; } = 5000;
        public RetryOptions Retry { get; set; } = new RetryOptions();
        public CircuitBreakOptions CircuitBreak { get; set; } = new CircuitBreakOptions();
        public CacheOptions Cache { get; set; } = new CacheOptions();
        public BulkheadOptions Bulkhead { get; set; } = new BulkheadOptions();
    }
}
