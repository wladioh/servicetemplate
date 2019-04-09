namespace Service.Infra.Database
{
    public class DatabaseOptions
    {
        public static readonly string DatabaseSection = "Database";
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public bool SslEnabled { get; set; } = false;
    }
}
