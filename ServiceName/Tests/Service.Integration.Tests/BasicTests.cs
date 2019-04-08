using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Service.Api;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace Service.Integration.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

        }
    }
    public class BasicTests
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private FluentMockServer _server;


        public BasicTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            var settings = new FluentMockServerSettings
            {
                AllowPartialMapping = false,
                StartAdminInterface = true,
                Port = 5000
            };
            _server = StandAloneApp.Start(settings);
        }


        [Theory]
        [InlineData("api/values/2")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentTypeAsync(string url)
        {
            // Arrange
            _server.Given(Request.Create().WithPath("/api/values/1")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithBodyAsJson(new MockValue()));
            var client = _factory.CreateClient();

            //await Task.Delay(10000);
            // Act
            var response = await client.GetAsync(url);


            // Assert
            //;// response.
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Be("Please try again later");
            //response.EnsureSuccessStatusCode(); // Status Code 200-299
            //Assert.Equal("text/html; charset=utf-8",
            //    response.Content.Headers.ContentType.ToString());
        }
    }

    public static class HttpResponseMessageExtensions
    {
        public static async Task<TResult> Get<TResult>(this HttpResponseMessage message)
        {
            var x = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResult>(x);
        }
    }
}

