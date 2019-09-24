using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using MongoDB.Driver;
using Newtonsoft.Json;
using Service.Domain;

namespace Service.Contract.Tests.Middleware
{
    public class ProviderStateService
    {
        private const string ConsumerName = "ServiceName";
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IDictionary<string, Action> _providerStates;

        public ProviderStateService(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
            _providerStates = new Dictionary<string, Action>
            {
                {
                    "There is no data",
                    RemoveAllData
                },
                {
                    "self api sample",
                    AddData
                }
            };
        }

        private void RemoveAllData()
        {

        }

        private void AddData()
        {
            var collection1 = _mongoDatabase.GetCollection<Value>(typeof(Value).Name);
            collection1.InsertOne(new Value
            {
                Id = Guid.Parse("38dcc45c-2dc7-4f57-9755-a756503c77fc"),
                Name = "Same name"
            });
        }


        public async Task Provide(ProviderState providerState)
        {
            //using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            //var jsonRequestBody = await reader.ReadToEndAsync();

            //var providerState = JsonConvert.DeserializeObject<ProviderState>(jsonRequestBody);

            //A null or empty provider state key must be handled
            if (providerState != null && !String.IsNullOrEmpty(providerState.State) &&
                providerState.Consumer == ConsumerName)
            {
                _providerStates[providerState.State]?.Invoke();
            }
        }
    }
}
