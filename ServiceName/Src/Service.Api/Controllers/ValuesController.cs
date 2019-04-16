using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Rebus.Bus;
using Service.Api.Resources;
using Service.Domain;

namespace Service.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _i18N;
        private readonly ISomeoneApi _someone;
        private readonly IBus _bus;
        private readonly IClassRepository _repository;
        private readonly IDistributedCache _cache;

        public ValuesController(IStringLocalizer<SharedResource> i18N, ISomeoneApi someone,
            IBus bus, IClassRepository repository, IDistributedCache cache)
        {
            _i18N = i18N;
            _someone = someone;
            _bus = bus;
            _repository = repository;
            _cache = cache;
        }
        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SomeoneApiValue>>> Get()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            var x = 0;
            var sucess = 0;
            var error = 0;
            var results = new List<SomeoneApiValue>();
            while (x < 100)
            {
                var re = await _someone.Get(random.Next(1, 500));
                if (re.IsSuccessStatusCode)
                {
                    results.Add(re.Content);
                    sucess++;
                }
                else
                    error++;
                x++;
            }
            Console.WriteLine($"Result = {sucess}");
            Console.WriteLine($"Errors = {error}");
            return results;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var re = await _someone.Get(id);
            var x = await _cache.GetOrSetAsync("All",
                async () => await _repository.GetAll(), 
                new DistributedCacheEntryOptions());
            if (re.IsSuccessStatusCode)
                return Ok(re.Content);
            return BadRequest(re.Error.Content);
        }

        // POST api/values
        [HttpPost]
        public async Task Post([FromBody] string value)
        {
            await _repository.Add(new Value
            {
                Name = value
            });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    public static class DistributedCaching
    {
        public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            await distributedCache.SetAsync(key, value.ToByteArray(), options, token);
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default) where T : class
        {
            var result = await distributedCache.GetAsync(key, token);
            return result.FromByteArray<T>();
        }

        public static async Task<T> GetOrSetAsync<T>(this IDistributedCache distributedCache,
            string key, Func<T> factory, DistributedCacheEntryOptions options, CancellationToken token = default) where T : class
        {
            var result = await distributedCache.GetAsync(key, token);
            var resultTyped = result.FromByteArray<T>();
            if (resultTyped is null)
            {
                resultTyped = factory();
                if (resultTyped is null)
                    return null;
                await distributedCache.SetAsync(key, resultTyped, options, token);
            }

            return resultTyped;
        }
    }

    public static class Serialization
    {
        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        public static T FromByteArray<T>(this byte[] byteArray) where T : class
        {
            if (byteArray == null)
            {
                return default;
            }
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(byteArray))
            {
                return binaryFormatter.Deserialize(memoryStream) as T;
            }
        }

    }
}
