using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Mongo2Go;
using MongoDB.Driver;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;
using Service.Api;
using Service.Infra.ConfigurationService;
using Service.Integration.Tests.RebusHelpers;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Service.Contract.Tests
{
    public class StartupMock
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

        public StartupMock()
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
        public void ConfigureRoutingMessages(Action<StandardConfigurer<Rebus.Routing.IRouter>> router)
        {
            _router = router;
        }

        public IWebHostBuilder CreateWebHostBuilder()
        {
            var x= WebHost.CreateDefaultBuilder()
                .ConfigureKestrel(opt => opt.AddServerHeader = false)
                .UseSetting("ServiceConfiguration:ConnectionString", "http://localhost:5001")
                .UseUrls("https://localhost:5090;http://localhost:5091;https://hostname:5092")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    Dict.Add("Database:ConnectionString", Runner.ConnectionString);
                    config.AddInMemoryCollection(Dict);
                }).ConfigureServices((build, collection) =>
                {
                    collection.AddSingleton(InMemoryBus);
                })
                .UseEnvironment("IntegrationTest")
                .ConfigureMetrics()
                .UseMetrics()
                .UseStartup<Startup>();
            SetContentRoot<Startup>(x);
            return x;
        }
        private WebApplicationFactoryContentRootAttribute[] GetContentRootMetadataAttributes<TEntryPoint>(
            string tEntryPointAssemblyFullName,
            string tEntryPointAssemblyName)
        {
            var testAssembly = GetTestAssemblies<TEntryPoint>();
            var metadataAttributes = testAssembly
                .SelectMany(a => CustomAttributeExtensions.GetCustomAttributes<WebApplicationFactoryContentRootAttribute>((Assembly) a))
                .Where(a => string.Equals(a.Key, tEntryPointAssemblyFullName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.Key, tEntryPointAssemblyName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Priority)
                .ToArray();

            return metadataAttributes;
        }
        protected virtual IEnumerable<Assembly> GetTestAssemblies<TEntryPoint>()
        {
            try
            {
                // The default dependency context will be populated in .net core applications.
                var context = DependencyContext.Default;
                if (context == null || context.CompileLibraries.Count == 0)
                {
                    // The app domain friendly name will be populated in full framework.
                    return new[] { Assembly.Load(AppDomain.CurrentDomain.FriendlyName) };
                }

                var runtimeProjectLibraries = context.RuntimeLibraries
                    .ToDictionary(r => r.Name, r => r, StringComparer.Ordinal);

                // Find the list of projects
                var projects = context.CompileLibraries.Where(l => l.Type == "project");

                var entryPointAssemblyName = typeof(TEntryPoint).Assembly.GetName().Name;

                // Find the list of projects referencing TEntryPoint.
                var candidates = context.CompileLibraries
                    .Where(library => library.Dependencies.Any(d => string.Equals(d.Name, entryPointAssemblyName, StringComparison.Ordinal)));

                var testAssemblies = new List<Assembly>();
                foreach (var candidate in candidates)
                {
                    if (runtimeProjectLibraries.TryGetValue(candidate.Name, out var runtimeLibrary))
                    {
                        var runtimeAssemblies = runtimeLibrary.GetDefaultAssemblyNames(context);
                        testAssemblies.AddRange(runtimeAssemblies.Select(Assembly.Load));
                    }
                }

                return testAssemblies;
            }
            catch (Exception)
            {
            }

            return Array.Empty<Assembly>();
        }

        private void SetContentRoot<TEntryPoint>(IWebHostBuilder builder)
        {
            if (SetContentRootFromSetting<TEntryPoint>(builder))
            {
                return;
            }

            var metadataAttributes = GetContentRootMetadataAttributes<TEntryPoint>(
                typeof(TEntryPoint).Assembly.FullName,
                typeof(TEntryPoint).Assembly.GetName().Name);

            string contentRoot = null;
            for (var i = 0; i < metadataAttributes.Length; i++)
            {
                var contentRootAttribute = metadataAttributes[i];
                var contentRootCandidate = Path.Combine(
                    AppContext.BaseDirectory,
                    contentRootAttribute.ContentRootPath);

                var contentRootMarker = Path.Combine(
                    contentRootCandidate,
                    Path.GetFileName(contentRootAttribute.ContentRootTest));

                if (File.Exists(contentRootMarker))
                {
                    contentRoot = contentRootCandidate;
                    break;
                }
            }

            if (contentRoot != null)
            {
                builder.UseContentRoot(contentRoot);
            }
            else
            {
                builder.UseSolutionRelativeContentRoot(typeof(TEntryPoint).Assembly.GetName().Name);
            }
        }
        private static bool SetContentRootFromSetting<TEntryPoint>(IWebHostBuilder builder)
        {
            // Attempt to look for TEST_CONTENTROOT_APPNAME in settings. This should result in looking for
            // ASPNETCORE_TEST_CONTENTROOT_APPNAME environment variable.
            var assemblyName = typeof(TEntryPoint).Assembly.GetName().Name;
            var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_");
            var settingName = $"TEST_CONTENTROOT_{settingSuffix}";

            var settingValue = builder.GetSetting(settingName);
            if (settingValue == null)
            {
                return false;
            }

            builder.UseContentRoot(settingValue);
            return true;
        }
    }
}
