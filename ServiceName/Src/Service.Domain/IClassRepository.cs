using System.Threading.Tasks;

namespace Service.Domain
{
    public interface IClassRepository
    {
        Task<Value> GetAll();
        Task Add(Value item);
    }
}
