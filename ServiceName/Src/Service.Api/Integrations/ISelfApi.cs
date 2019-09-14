using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using Service.Domain;

namespace Service.Api.Integrations
{
    public interface ISelfApi
    {
        [Get("/api/values")]
        Task<ApiResponse<IList<Value>>> Get();
    }
}
