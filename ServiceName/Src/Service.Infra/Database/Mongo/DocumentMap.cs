using MongoDB.Bson.Serialization;

namespace Service.Infra.Database.Mongo
{
    public abstract class DocumentMap<T> : BsonClassMap<T>
    {
        protected DocumentMap()
        {
            AutoMap();
            Map();
            SetIgnoreExtraElements(true);            
            SetIgnoreExtraElementsIsInherited(true);
        }

        protected abstract void Map();
    }
}
