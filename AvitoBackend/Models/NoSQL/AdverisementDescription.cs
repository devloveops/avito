using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AvitoBackend.Models.NoSQL;

public class AdvertisementDescription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; 
    
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;
    
    [BsonElement("features")]
    public Dictionary<string, string> Features { get; set; } = [];
}