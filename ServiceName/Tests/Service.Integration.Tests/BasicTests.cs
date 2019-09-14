using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver;
using Rebus.Bus;
using Rebus.Routing.TypeBased;
using Service.Api;
using Service.Api.Handlers;
using Service.Api.Integrations;
using Service.Domain;
using Service.Integration.Tests.Extensions;
using WireMock.RequestBuilders;
using Xunit;

namespace Service.Integration.Tests
{
    public class BasicTests
        : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;
        private readonly IBus _bus;

        public BasicTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _factory.ConfigureRoutingMessages(t =>
            {
                t.TypeBased()
                    .Map<TestMessage>("ServiceName")
                    .Map<OtherMessage>("ServiceName")
                    .Map<OtherMessagePublish>("ServiceName")
                    .MapFallback("TestErrors");
            });
            _bus = _factory.Bus;
        }


        [Theory]
        [InlineData("api/values/1")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentTypeAsync(string url)
        {
            // Arrange
            //_factory.WireMockServer.Given(Request.Create().WithPath("/anyvalue/1")
            //        .UsingGet())
            //    .RespondWith(WireMock.ResponseBuilders.Response.Create()
            //        .WithBodyAsJson(new SomeoneApiValue()));

            //// Act
            //var response = await _client.GetAsync(url);

            //response.EnsureSuccessStatusCode(); // Status Code 200-299
            //response.StatusCode.Should().Be(HttpStatusCode.OK);
            //var message = await response.Get<SomeoneApiValue>();
            //message.Should().NotBeNull();
        }
        [Fact]
        public async Task Post_CheckDb()
        {
            // Arrange
            var name = "name to be saved.";
            // Act
            var response = await _client.PostAsJsonAsync("api/values", name);

            response.EnsureSuccessStatusCode(); // Status Code 200-299
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var collection = _factory.MongoDb.GetCollection<Value>(typeof(Value).Name);
            var savedMessage = collection.FindSync(it => it.Name == name).FirstOrDefault();
            savedMessage.Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessage_WaitToReply()
        {
            var message = new TestMessage
            {
                Name = "Any Message"
            };
            await _bus.Send(message);
            // Assert
            var messageReplied = await _factory.MessageReceiver.WaitForMessage<TestMessage>();
            message.Name.Should().Be(messageReplied.Name);
        }

        [Fact]
        public async Task SendOtherMessage_WaitForMessageInSpecificQueue()
        {
            _factory.MessageReceiver.ListenerQueue("OtherService");
            var message = new OtherMessage
            {
                Name = "Any Message"
            };
            await _bus.Send(message);
            // Assert
            var messageReplied = await _factory.MessageReceiver.WaitForMessage<OtherMessage>();
            message.Name.Should().Be(messageReplied.Name);
        }

        [Fact]
        public async Task SendOtherMessage_WaitForMessagePublished()
        {
            await _bus.Subscribe<OtherMessagePublish>();
            var message = new OtherMessagePublish
            {
                Name = "Any Message"
            };
            await _bus.Send(message);
            // Assert
            var messageReplied = await _factory.MessageReceiver.WaitForMessage<OtherMessagePublish>();
            message.Name.Should().Be(messageReplied.Name);
        }
    }
}
