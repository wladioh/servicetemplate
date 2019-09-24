using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PactNet;
using PactNet.Infrastructure.Outputters;
using Rebus.Routing.TypeBased;
using Service.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Service.Contract.Tests
{
    public class BasicTests : IClassFixture<StartupMock>, IDisposable
    {
        private readonly string _pactServiceUri;
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _providerUri;
        private readonly PactVerifierConfig _config;
        private readonly IWebHost _webHost;        
        private readonly CancellationTokenSource _tokenSource;


        public BasicTests(StartupMock factory, ITestOutputHelper output)
        {
            _pactServiceUri = "http://localhost:9001";
            _providerUri = "https://localhost:5090";
            _outputHelper = output;
            factory.ConfigureRoutingMessages(t =>
            {
                t.TypeBased().MapFallback("TestErrors");
            });
            _config = new PactVerifierConfig
            {
                Outputters = new List<IOutput>
                {
                    new XUnitOutput(_outputHelper)
                },
                Verbose = false,
                ProviderVersion = "1.0", //git commit
                PublishVerificationResults = true
            };
            _tokenSource = new CancellationTokenSource();
            
            factory.CreateWebHostBuilder().Build()
                .RunAsync(_tokenSource.Token).GetAwaiter();
            
            _webHost = WebHost.CreateDefaultBuilder()
                .UseUrls(_pactServiceUri)
                .ConfigureServices((build, collection) =>
                {
                    collection.AddSingleton(factory.MongoDb);
                })
                .UseStartup<PactStartup>()
                .Build();

            _webHost.Start();
        }


        [Fact]
        public void VerifyPactsWithMySelf()
        {
            //Act / Assert
            IPactVerifier pactVerifier = new PactVerifier(_config);

            pactVerifier.ProviderState($"{_pactServiceUri}/provider/states")
                .ServiceProvider("Self sample API", _providerUri)
                .HonoursPactWith("ServiceName")
                .PactUri("http://localhost:9292/pacts/provider/Self%20sample%20API/consumer/ServiceName/latest")
                .Verify();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
            {
                _webHost.StopAsync().GetAwaiter().GetResult();
                _webHost.Dispose();
            }
            _disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            _tokenSource.Cancel();
        }
        #endregion
    }
}
