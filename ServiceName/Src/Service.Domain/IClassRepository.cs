using System.Threading.Tasks;

namespace Service.Domain
{
    public interface IClassRepository
    {
        Task<Class1> GetAll();
        Task Add(Class1 item);
    }
}
