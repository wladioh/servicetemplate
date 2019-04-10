using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Service.Integration.Tests.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<TResult> Get<TResult>(this HttpResponseMessage message)
        {
            var body = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResult>(body);
        }
    }
}