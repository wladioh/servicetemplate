using System;
using System.Collections.Generic;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Refit;
using Service.Api.Integrations;
using Service.Domain;
using Xunit;

namespace Service.Contract.Tests.Consumer
{
    public class ConsumerSelfSampleApiTest: IClassFixture<ConsumerSelfSampleApiPact>
    {
        private readonly IMockProviderService _mockProviderService;
        private readonly string _mockProviderServiceBaseUri;

        public ConsumerSelfSampleApiTest(ConsumerSelfSampleApiPact data)
        {
            _mockProviderService = data.MockProviderService;
            _mockProviderService.ClearInteractions(); //NOTE: Clears any previously registered interactions before the test is run
            _mockProviderServiceBaseUri = data.MockProviderServiceBaseUri;
        }

        [Fact]

        public async void GetSomething_WhenTheTesterSomethingExists_ReturnsTheSomething()
        {
            //Arrange
            _mockProviderService
                .Given("self api sample")
                .UponReceiving("A GET request to /api/values resources")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/api/values"
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object>
                    {
                        { "Content-Type", "application/json; charset=utf-8"}
                    },
                    Body = new List<Value>
                    {
                        new Value
                        {
                            Id = Guid.Parse("38dcc45c-2dc7-4f57-9755-a756503c77fc"),
                            Name = "Same name" 
                        }
                    }
                });

            //NOTE: WillRespondWith call must come last as it will register the interaction
            var selfApi = RestService.For<ISelfApi>(_mockProviderServiceBaseUri);

            //Act
            await selfApi.Get();

            //Assert
            _mockProviderService.VerifyInteractions(); //NOTE: Verifies that interactions registered on the mock provider are called at least once
        }
    }
}
