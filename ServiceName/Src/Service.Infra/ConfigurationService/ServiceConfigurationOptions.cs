namespace Service.Infra.ConfigurationService
{
    public class ServiceConfigurationOptions
    {
        public static string SectionName = "ServiceConfiguration";
        public string ConnectionString { get; set; }
        public bool Optional { get; set; } = true;
        public string KeyName { get; set; }
    }
}
