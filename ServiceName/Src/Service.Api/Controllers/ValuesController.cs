using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Polenter.Serialization;
using Rebus.Bus;
using Service.Api.Integrations;
using Service.Api.Resources;
using Service.Domain;

namespace Service.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _i18N;
        private readonly IPokemonApi _pokemon;
        private readonly IBus _bus;
        private readonly IValueRepository _repository;
        private readonly IDistributedCache _cache;

        public ValuesController(IStringLocalizer<SharedResource> i18N, IPokemonApi pokemon,
            IBus bus, IValueRepository repository, IDistributedCache cache)
        {
            _i18N = i18N;
            _pokemon = pokemon;
            _bus = bus;
            _repository = repository;
            _cache = cache;
        }
        // GET api/values
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Value>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var values = await _repository.GetAllAsync().ConfigureAwait(false);
            return Ok(values);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var re = await _pokemon.Get(id);
            if (re.IsSuccessStatusCode)
                return Ok(re.Content);
            return BadRequest(re.Error.Content);
        }
        // GET api/values/5
        [HttpGet("cache/{id:guid}")]
        [ProducesResponseType(typeof(Value), (int)HttpStatusCode.OK), ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCache(Guid id)
        {
            var value = await _cache.GetOrSetAsync($"value{id}",
                async () => await _repository.GetById(id),
                new DistributedCacheEntryOptions());
            if (value is null)
                return NotFound(id);
            return Ok(value);
        }
        [HttpGet("db/{id:guid}")]
        [ProducesResponseType(typeof(Value), (int)HttpStatusCode.OK), ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetDb(Guid id)
        {
            var value = await _repository.GetById(id).ConfigureAwait(false);
            if (value is null)
                return NotFound(id);
            return Ok(value);
        }
        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string value)
        {
            var x = new Value
            {
                Name = value
            };
            await _repository.Add(x);
            return Ok(new { x.Id });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id:guid}")]
        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id).ConfigureAwait(false);
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
            string key, Func<Task<T>> factory, DistributedCacheEntryOptions options, CancellationToken token = default) where T : class
        {
            var result = await distributedCache.GetAsync(key, token);
            var resultTyped = result.FromByteArray<T>();
            if (resultTyped is null)
            {
                resultTyped = await factory?.Invoke();
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
            var settings = new SharpSerializerBinarySettings(BinarySerializationMode.Burst)
            {
                IncludeAssemblyVersionInTypeName = true,
                IncludeCultureInTypeName = true,
                IncludePublicKeyTokenInTypeName = true
            };
            var serializer = new SharpSerializer(settings);
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(obj, memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static T FromByteArray<T>(this byte[] byteArray) where T : class
        {
            if (byteArray == null)
            {
                return default;
            }
            var settings = new SharpSerializerBinarySettings(BinarySerializationMode.Burst)
            {
                IncludeAssemblyVersionInTypeName = true,
                IncludeCultureInTypeName = true,
                IncludePublicKeyTokenInTypeName = true
            };
            var serializer = new SharpSerializer(settings);
            using (var memoryStream = new MemoryStream(byteArray))
            {
                return serializer.Deserialize(memoryStream) as T;
            }
        }

    }
}
