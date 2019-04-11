namespace Service.Infra.Network.Options
{
    public class BulkheadOptions
    {
        public int MaxQueuingActions { get; set; } = 100;
        public int MaxParallelization { get; set; } = 100;
    }
}
