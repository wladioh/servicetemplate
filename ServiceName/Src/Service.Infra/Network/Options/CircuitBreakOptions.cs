namespace Service.Infra.Network.Options
{
    public class CircuitBreakOptions
    {
        public double FailureThreshold { get; set; } = 0.1;
        public int SamplingDuration { get; set; } = 3;
        public int MinimumThroughput { get; set; } = 10;
        public int DurationOfBreak { get; set; } = 5;
    }
}
