namespace BuildBlock.Consul
{
    public class ServiceConfigurationOptions
    {
        public static string SectioName = "ServiceConfiguration";
        public string ConnectionString { get; set; }
        public bool Optional { get; set; } = true;
        public string KeyName { get; set; }
    }
}
