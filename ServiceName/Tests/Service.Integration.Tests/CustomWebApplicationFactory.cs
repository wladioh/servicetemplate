using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;
using Service.Api;
using Service.Integration.Tests.RebusHelpers;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.Server;
using WireMock.Settings;
using System.Reflection;
using App.Metrics.AspNetCore;
using Microsoft.Extensions.DependencyModel;
using Service.Infra.ConfigurationService;
using OpenTracing;
using OpenTracing.Propagation;
using Moq;
using Jaeger;

namespace Service.Integration.Tests
{
    public class CustomWebApplicationFactory<TStartup>
         : WebApplicationFactory<TStartup> where TStartup : class
    {
        private static readonly Dictionary<string, string> Dict =
            new Dictionary<string, string>
            {
                {"Endpoints:Mock", "http://localhost:5001"},
                {"MessageBus:Transport", "Memory" }
            };
        public readonly InMemNetwork InMemoryBus = new InMemNetwork();
        public readonly MongoDbRunner Runner = MongoDbRunner.Start();
        private IBus _bus;
        public FluentMockServer WireMockServer { get; }
        public IMongoDatabase MongoDb { get; }
        public MessageHelper MessageReceiver { get; }
        private Action<StandardConfigurer<Rebus.Routing.IRouter>> _router;
        public IBus Bus => _bus ?? (_bus = BusHelper.Create(MessageReceiver, InMemoryBus, "TestQueue", _router));

        public CustomWebApplicationFactory()
        {
            var settings = new FluentMockServerSettings
            {
                AllowPartialMapping = false,
                StartAdminInterface = true,
                Port = 5001
            };
            WireMockServer = StandAloneApp.Start(settings);
            MongoDb = new MongoClient(Runner.ConnectionString).GetDatabase("ServiceName");

            MessageReceiver = new MessageHelper(InMemoryBus);
            //mock consul request
            WireMockServer.Given(Request.Create().WithPath("/v1/kv/ServiceName")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create().WithNotFound());
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseSetting("ServiceConfiguration:ConnectionString", "http://localhost:5001")
                .UseUrls("https://localhost:5090;http://localhost:5091;https://hostname:5092")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    Dict.Add("Database:ConnectionString", Runner.ConnectionString);
                    config.AddInMemoryCollection(Dict);
                }).ConfigureServices((build, collection) =>
                {
                    collection.AddSingleton(InMemoryBus);
                    collection.AddSingleton<ITracer>(new Tracer.Builder("IntegrationTest").Build());
                })
                .UseEnvironment("IntegrationTest");
        }

        public void ConfigureRoutingMessages(Action<StandardConfigurer<Rebus.Routing.IRouter>> router)
        {
            _router = router;
        }
    }

}
