using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
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
        public readonly MongoDbRunner Runner = MongoDbRunner.Start();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                dict.Add("Database:ConnectionString", Runner.ConnectionString);
                config.AddInMemoryCollection(dict);
            }).ConfigureServices((build, collection) =>
            {
                collection.AddSingleton(InMemoryBus);
            });
        }
    }
    public interface IMessageWaiter
    {
        bool CheckMessage(object message);
        void Done(object message);
    }
    public class MessageWaiter<T> : IMessageWaiter
    {
        private readonly int _timeout;
        public Func<T, bool> Specification { get; }
        private readonly TaskCompletionSource<T> _taskCompletionSource =
            new TaskCompletionSource<T>();
        public MessageWaiter(Func<T, bool> specification, int timeout = 5000)
        {
            _timeout = timeout;
            Specification = specification;
        }


        public void Done(object message)
        {
            _taskCompletionSource.TrySetResult((T)message);
        }

        public void Cancel()
        {
            _taskCompletionSource.TrySetCanceled();
        }

        public Task<T> ToTask()
        {
            var ct = new CancellationTokenSource(_timeout);
            ct.Token.Register(() => _taskCompletionSource.TrySetCanceled(), false);
            return _taskCompletionSource.Task;
        }

        public bool CheckMessage(object message)
        {
            if (message is T msg)
                return Specification.Invoke(msg);
            return false;
        }
    }
    public class ReplyMessages : IHandleMessages<object>
    {
        private readonly List<object> _replyMessages;
        private readonly List<IMessageWaiter> _waiters = new List<IMessageWaiter>();

        public ReplyMessages()
        {
            _replyMessages = new List<object>();
        }

        public Task Handle(object message)
        {
            var waiters = _waiters.Where(it => it.CheckMessage(message)).ToList();
            if (waiters.Any())
                waiters.ForEach(it => it.Done(message));
            else
                _replyMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task<T> WaitForMessage<T>(int timeout = 5000)
        {
            var waiter = new MessageWaiter<T>(m => true, timeout);
            _waiters.Add(waiter);
            var message = _replyMessages.FirstOrDefault();
            if (message != null)
                waiter.Done(message);
            return waiter.ToTask();
        }

        public Task<T> WaitForMessage<T>(Func<T, bool> specification, int timeout = 5000)
        {
            var waiter = new MessageWaiter<T>(specification, timeout);
            _waiters.Add(waiter);
            var message = _replyMessages.OfType<T>()
                .FirstOrDefault(specification);
            if (message != null)
                waiter.Done(message);
            return waiter.ToTask();
        }
    }
    public class BasicTests
        : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly FluentMockServer _server;
        private readonly IBus _bus;
        private ReplyMessages _messageReciver;

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
            _messageReciver = new ReplyMessages();
            x.Register((bus, context) => _messageReciver);
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
                    l.ColoredConsole();
                }).Options(it => it.SimpleRetryStrategy())
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
            //_messageReciver.ListenerQueue("umafila");
            // Act
            var response = await client.GetAsync(url);
            await _bus.Send(new TestMessage
            {
                Name = "asdasd"
            });
            // Assert
            var message = await _messageReciver
                .WaitForMessage<TestMessage>();
            message.Name.Should().NotBeNullOrWhiteSpace();
            //_messageReciver.GetMessage<TestMessage>(it=> it.Id);
            //  var failedMessage = await _factory.InMemoryBus.WaitForNextMessageFrom("error");
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Be("Please try again later");
            //response.EnsureSuccessStatusCode(); // Status Code 200-299
            //Assert.Equal("text/html; charset=utf-8",
            //response.Content.Headers.ContentType.ToString());
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

