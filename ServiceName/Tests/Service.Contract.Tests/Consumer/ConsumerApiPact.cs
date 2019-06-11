using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PactNet;
using PactNet.Mocks.MockHttpService;

namespace Service.Contract.Tests.Consumer
{
    public abstract class ConsumerApiPact : IDisposable
    {
        private readonly string _apiName;
        private const string ConsumerName = "ServiceName";
        public IPactBuilder PactBuilder { get; }
        public IMockProviderService MockProviderService { get; }

        public int MockServerPort => 9222;
        public string MockProviderServiceBaseUri => $"http://localhost:{MockServerPort}";
        private const string BrokerEndPoint = "http://localhost:9292/"; /// GetEnvironmentVariable 
        private const string Version = "1.0.0"; /// GetEnvironmentVariable Commit number
        private const string PactsFolder = @"..\..\..\pacts";
        protected ConsumerApiPact(string apiName)
        {
            _apiName = apiName;
            // PactBuilder = new PactBuilder(new PactConfig { SpecificationVersion = "2.0.0" }); //Configures the Specification Version
            //or
            PactBuilder = new PactBuilder(new PactConfig
            {
                SpecificationVersion = "2.0.0",
                PactDir = PactsFolder,
                LogDir = @"c:\temp\logs"
            }); //Configures the PactDir and/or LogDir.

            PactBuilder
                .ServiceConsumer(ConsumerName)
                .HasPactWith(apiName);

            //MockProviderService = PactBuilder.MockService(MockServerPort); //Configure the http mock server
            //or
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            MockProviderService = PactBuilder.MockService(MockServerPort,
                
                new JsonSerializerSettings
                {
                    ContractResolver = contractResolver
                }); //By passing true as the second param, you can enabled SSL. A self signed SSL cert will be provisioned by default.
            //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, true, sslCert: sslCert, sslKey: sslKey); //By passing true as the second param and an sslCert and sslKey, you can enabled SSL with a custom certificate. See "Using a Custom SSL Certificate" for more details.
            //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, new JsonSerializerSettings()); //You can also change the default Json serialization settings using this overload    
            ////or
            //MockProviderService = PactBuilder.MockService(MockServerPort, host: IPAddress.Any); //By passing host as IPAddress.Any, the mock provider service will bind and listen on all ip addresses

        }

        private string PactFileName()
        {
            return
                $"{ConsumerName.Replace(' ', '_').ToLowerInvariant()}-" +
                $"{_apiName.Replace(' ', '_').ToLowerInvariant()}.json";
        }
        public void Dispose()
        {
            PactBuilder.Build();
            MockProviderService.Stop();
            var pactPublisher = new PactPublisher(BrokerEndPoint);
            pactPublisher.PublishToBroker(
                $@"{PactsFolder}\{PactFileName()}",
                Version);        }
    }

    public class ConsumerPokemonApiPact : ConsumerApiPact
    {
        public ConsumerPokemonApiPact() : base("Pokemon API") { }
    }

    public class ConsumerSelfSampleApiPact : ConsumerApiPact
    {
        public ConsumerSelfSampleApiPact() : base("Self sample API") { }
    }
}
