using System;
using Service.Api;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.Settings;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var settings = new FluentMockServerSettings
            {
                AllowPartialMapping = true,
                StartAdminInterface = true,
                Port = 5000
            };
            var _server = StandAloneApp.Start(settings);
            _server.Given(Request.Create().WithPath("/api/values/1")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new MockValue())
                    .WithHeader("Content-Type","application/json"));
            Console.WriteLine("Press any key to stop the server");
            Console.ReadKey();
            _server.Stop();
        }
    }
}
