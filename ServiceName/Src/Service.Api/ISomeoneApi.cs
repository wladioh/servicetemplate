using System;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;

namespace Service.Api
{
    public interface ISomeoneApi
    {
        [Get("/anyvalue/{id}")]
        Task<ApiResponse<SomeoneApiValue>> Get(int id);
    }

    public class SomeoneApiValue
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
