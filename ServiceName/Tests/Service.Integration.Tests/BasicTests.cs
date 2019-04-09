using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using Newtonsoft.Json;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
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
        private static readonly Dictionary<string, string> dict =
            new Dictionary<string, string>
            {
                {"Endpoints:Mock", "http://localhost:5001"},
                {"MessageBus:Transport", "Memory" }
            };
        public readonly InMemNetwork InMemoryBus = new InMemNetwork();
        public readonly MongoDbRunner _runner = MongoDbRunner.Start();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                dict.Add("Database:ConnectionString", _runner.ConnectionString);
                config.AddInMemoryCollection(dict);
            }).ConfigureServices((build, collection) =>
            {
                collection.AddSingleton(InMemoryBus);
            });
        }
    }
    public class BasicTests
        : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly FluentMockServer _server;
        private IBus _bus;

        public BasicTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            var settings = new FluentMockServerSettings
            {
                AllowPartialMapping = false,
                StartAdminInterface = true,
                Port = 5001
            };
            _server = StandAloneApp.Start(settings);
            var x = new BuiltinHandlerActivator();
            _bus = Configure.With(x)
                .Transport(t => t.UseInMemoryTransport(_factory.InMemoryBus, "TestQueue"))
                .Routing(t =>
                {
                    t.TypeBased()
                        .Map<TestMessage>("ServiceName")
                        .MapFallback("TestErrors");
                })
                .Logging(l =>
                {
                    l.ColoredConsole(LogLevel.Debug);
                })
                .Start();
        }


        [Theory]
        [InlineData("api/values/1")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentTypeAsync(string url)
        {
            // Arrange
            _server.Given(Request.Create().WithPath("/api/values/1")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithBodyAsJson(new MockValue()));
            var client = _factory.CreateClient();
            var result = Result<string>.New();
            result.WithValidation("Erro ao fazer algo");
            // Act
            var response = await client.GetAsync(url);
            await _bus.Send(new TestMessage
            {
                Name = "asdasd"
            });
            // Assert
            var failedMessage = await _factory.InMemoryBus.WaitForNextMessageFrom("error");
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Be("Please try again later");
            //response.EnsureSuccessStatusCode(); // Status Code 200-299
            //Assert.Equal("text/html; charset=utf-8",
            //response.Content.Headers.ContentType.ToString());
        }
    }
    public class Result<T>
    {
        public T Value { get; set; }
        public List<ValidationResult> TransactionErrors { get; set; } = new List<ValidationResult>();
        public bool Success => !TransactionErrors.Any();

        /// <summary>
        /// Returns an instance of the builder to start the fluent creation of the object.
        /// </summary>
        public static Result<T> New()
        {
            return new Result<T>();
        }

        public Result<T> WithValidation(string validation)
        {
            TransactionErrors.Add(new ValidationResult(validation));
            return this;
        }
        public Result<T> WithValidations(params string[] validation)
        {
            TransactionErrors.AddRange(validation.Select(v => new ValidationResult(v)));
            return this;
        }
        public Result<T> WithValue(T result)
        {
            Value = result;
            return this;
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

