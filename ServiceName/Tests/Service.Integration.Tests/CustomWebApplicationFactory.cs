using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using Rebus.Transport.InMem;
using Service.Integration.Tests.RebusHelpers;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Service.Integration.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        private static readonly Dictionary<string, string> dict =
            new Dictionary<string, string>
            {
                {"Endpoints:Mock", "http://localhost:5001"},
                {"MessageBus:Transport", "Memory" }
            };
        public readonly InMemNetwork InMemoryBus = new InMemNetwork();
        public readonly MongoDbRunner Runner = MongoDbRunner.Start();
        public FluentMockServer WireMockServer { get; }
        public IMongoDatabase MongoDB { get; }
        public MessageHelper MessageReciver { get; }

        public CustomWebApplicationFactory()
        {
            var settings = new FluentMockServerSettings
            {
                AllowPartialMapping = false,
                StartAdminInterface = true,
                Port = 5001
            };
            WireMockServer = StandAloneApp.Start(settings);
            MongoDB = new MongoClient(Runner.ConnectionString).GetDatabase("ServiceName");

            MessageReciver = new MessageHelper(InMemoryBus);
            //mock consul request
            WireMockServer.Given(Request.Create().WithPath("/v1/kv/ServiceName")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create().WithNotFound());
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseSetting("ServiceConfiguration:ConnectionString", "http://localhost:5001")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    dict.Add("Database:ConnectionString", Runner.ConnectionString);
                    config.AddInMemoryCollection(dict);
                }).ConfigureServices((build, collection) =>
                {
                    collection.AddSingleton(InMemoryBus);
                })
                .UseEnvironment("IntegrationTest");
        }
    }
}
