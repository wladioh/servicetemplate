using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Service.Domain;

namespace Service.Infra.Repositories
{
    public abstract class MongoRepositoryBase<T>
    {
        private IMongoCollection<T> _collection;
        private readonly IMongoDatabase _dataBase;

        protected IMongoCollection<T> Collection => _collection ?? (_collection = _dataBase.GetCollection<T>(typeof(T).Name));

        protected MongoRepositoryBase(IMongoDatabase dataBase)
        {
            _dataBase = dataBase;
        }
    }
    public class ValueMongoRepository : MongoRepositoryBase<Value>, IValueRepository
    {
        public ValueMongoRepository(IMongoDatabase dataBase) : base(dataBase)
        { }

        public async Task Add(Value item)
        {
            await Collection.InsertOneAsync(item);
        }

        public async Task DeleteAsync(Guid id)
        {
            await Collection.DeleteOneAsync(it=> it.Id == id);
        }

        public async Task<IEnumerable<Value>> Find(Expression<Func<Value, bool>> filter)
        {
            return await Collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<Value>> GetAllAsync()
        {
            return await Find(value => true);
        }

        public async Task<Value> GetById(Guid id)
        {
            return await Find(value => value.Id == id)
                .FirstOrDefaultAsync();
        }
    }

    public static class RepositoryExtensions
    {
        public static IServiceCollection AddMongoRepositories(this IServiceCollection serviceCollection)
        {
            serviceCollection.Scan(scan => scan
                .FromAssemblyOf<ValueMongoRepository>()
                .AddClasses(classes => classes.AssignableTo(typeof(MongoRepositoryBase<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            return serviceCollection;
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).FirstOrDefault();
        }
        public static async Task<T> FirstAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).First();
        }
        public static async Task<T> LastAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).Last();
        }

        public static async Task<T> LastOrDefaultAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).LastOrDefault();
        }
    }
}
