using System;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;

namespace Service.Api
{
    public interface IMockApi
    {
        [Get("/api/values/{id}")]
        Task<ApiResponse<MockValue>> Get(int id);
    }

    public class MockValue
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
