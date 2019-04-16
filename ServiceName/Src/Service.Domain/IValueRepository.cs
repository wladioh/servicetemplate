using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Service.Domain
{
    public interface IValueRepository
    {
        Task<Value> GetById(Guid id);
        Task<IEnumerable<Value>> Find(Expression<Func<Value, bool>> filter);
        Task<IEnumerable<Value>> GetAllAsync();
        Task Add(Value item);
        Task DeleteAsync(Guid item);
    }
}
