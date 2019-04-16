using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Service.Infra.Database.Mongo
{
    public static class MongoExtensions
    {
        private static void GetMongoOptions(this IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var configuration = sp.GetService<IConfiguration>();
            services.AddOptions();
            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.DatabaseSection));
            services.AddSingleton(it => it.GetService<IOptions<DatabaseOptions>>().Value);
        }
        private static void RegisterAllDocumentMap(IServiceProvider it)
        {
            var baseType = typeof(BsonClassMap);
            var assembly = Assembly.GetEntryAssembly();
            var types = assembly.GetTypes()
                .Where(type =>
                    !type.GetTypeInfo().IsAbstract
                    && !type.GetTypeInfo().IsInterface
                    && baseType.IsAssignableFrom(type)
                ).ToList();
            types.ForEach(y =>
            {
                var mapper = ActivatorUtilities.CreateInstance<BsonClassMap>(it, y);
                BsonClassMap.RegisterClassMap(mapper);
            });
        }
        public static IServiceCollection AddMongo(this IServiceCollection services)
        {
            GetMongoOptions(services);
            services.AddSingleton(provider =>
            {
                RegisterAllDocumentMap(provider);
                var options = provider.GetService<DatabaseOptions>();
                var settings = MongoClientSettings.FromUrl(
                    new MongoUrl(options.ConnectionString));
                settings.MaxConnectionPoolSize = 100;
                settings.WaitQueueSize = 5000;
                settings.MaxConnectionIdleTime = TimeSpan.FromSeconds(30);
                settings.WaitQueueTimeout = TimeSpan.FromSeconds(10);
                if (options.SslEnabled)
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };
                var mongoClient = new MongoClient(settings);
                return new MongoClient(settings);
            });
            services.AddTransient(provider =>
            {
                var options = provider.GetService<DatabaseOptions>();
                var client = provider.GetService<MongoClient>();
                return client.GetDatabase(options.Database);
            });
            return services;
        }

        private static string GetPropertyName<TD>(Expression<Func<TD, object>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;

            memberExpression = lambda.Body is UnaryExpression unaryExpression ?
                (MemberExpression)unaryExpression.Operand : (MemberExpression)lambda.Body;

            return ((PropertyInfo)memberExpression.Member).Name;
        }

        private static async Task CreateShards(IMongoDatabase database, string collectionName, string shardkeyName)
        {
            var partition = new BsonDocument
            {
                {"shardCollection", collectionName},
                {"key", new BsonDocument {{shardkeyName, "hashed"}}}
            };
            var command = new BsonDocumentCommand<BsonDocument>(partition);
            await database.RunCommandAsync(command);
        }

        public static async Task CreateShard<T>(this IMongoDatabase database,
            ILogger<T> logger, Expression<Func<T, object>> property)
        {
            var collection = database.GetCollection<T>(typeof(T).Name);
            try
            {
                await CreateShards(database, collection.CollectionNamespace.FullName, GetPropertyName(property))
                    .ConfigureAwait(false);
            }
            catch (MongoCommandException)
            {
                logger.LogWarning("Collection {0} already contains shards or the database don't have support to shards",
                    collection.CollectionNamespace.CollectionName);
            }
        }

        public static async Task CreateIndex<T>(this IMongoDatabase database,
            Func<IndexKeysDefinitionBuilder<T>, IndexKeysDefinition<T>> buildIndex)
        {
            var notificationLogBuilder = Builders<T>.IndexKeys;
            var collection = database.GetCollection<T>(typeof(T).Name);
            var indexModel = new CreateIndexModel<T>(buildIndex?.Invoke(notificationLogBuilder));
            await collection.Indexes.CreateOneAsync(indexModel).ConfigureAwait(false);
        }
    }
}
