using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Polly.Caching;
using Polly.Caching.Serialization.Json;

namespace Service.Infra.Network
{
    public class HttpResponseCacheSerializer : ICacheItemSerializer<HttpResponseMessage, string>
    {
        private readonly JsonSerializer<HttpResponseCache> _jsonSerializer;

        public HttpResponseCacheSerializer()
        {
            var serializerSettings = new JsonSerializerSettings();
            _jsonSerializer = new JsonSerializer<HttpResponseCache>(serializerSettings);
        }

        public HttpResponseMessage Deserialize(string objectToDeserialize)
        {
            var cache = _jsonSerializer.Deserialize(objectToDeserialize);
            var responseMessage = new HttpResponseMessage
            {
                Content = new StringContent(cache.Content),
                StatusCode = cache.StatusCode,
                ReasonPhrase = cache.ReasonPhrase
            };
            foreach (var httpContentHeader in cache.Headers)
                responseMessage.Headers.TryAddWithoutValidation(httpContentHeader.Key, httpContentHeader.Value);
            return responseMessage;
        }
        public string Serialize(HttpResponseMessage objectToSerialize)
        {
            var cache = new HttpResponseCache
            {
                Content = objectToSerialize.Content.ReadAsStringAsync().Result,
                Headers = objectToSerialize.Content.Headers.ToArray(),
                ReasonPhrase = objectToSerialize.ReasonPhrase,
                StatusCode = objectToSerialize.StatusCode
            };
            return _jsonSerializer.Serialize(cache);
        }

        public class HttpResponseCache
        {
            public string Content { get; set; }
            public string ReasonPhrase { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public KeyValuePair<string, IEnumerable<string>>[] Headers { get; set; }
        }
    }
}
