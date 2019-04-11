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
    public class ClassMongoRepository : MongoRepositoryBase<Value>, IClassRepository
    {
        public ClassMongoRepository(IMongoDatabase dataBase) : base(dataBase)
        { }

        public async Task Add(Value item)
        {
            await Collection.InsertOneAsync(item);
        }

        public async Task<Value> GetAll()
        {
            return await Collection.Find(class1 => true).FirstOrDefaultAsync();
        }
    }

    public static class RepositoryExtensions
    {
        public static IServiceCollection AddMongoRepositories(this IServiceCollection serviceCollection)
        {
            serviceCollection.Scan(scan => scan
                .FromAssemblyOf<ClassMongoRepository>()
                .AddClasses(classes => classes.AssignableTo(typeof(MongoRepositoryBase<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            return serviceCollection;
        }
    }
}
