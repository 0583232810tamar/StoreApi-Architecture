using MongoDB.Bson;

namespace ProductCatalogService.Common;

public static class MongoId
{
    public static bool IsValid(string id) => ObjectId.TryParse(id, out _);
}
