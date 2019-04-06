namespace Service.Infra.Network.Options
{
    public class CircuitBreakOptions
    {
        public double FailureThreshold { get; set; } = 0.5;
        public int SamplingDuration { get; set; } = 10;
        public int MinimumThroughput { get; set; } = 8;
        public int DurationOfBreak { get; set; } = 30;
    }
}
