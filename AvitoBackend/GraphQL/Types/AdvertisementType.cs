using AvitoBackend.Models;
using AvitoBackend.Models.Core;
using HotChocolate.Types;

namespace AvitoBackend.GraphQL.Types;

public class AdvertisementType : ObjectType<Advertisement>
{
    protected override void Configure(IObjectTypeDescriptor<Advertisement> descriptor)
    {
        descriptor.Field(a => a.Id);
        descriptor.Field(a => a.Title);
        descriptor.Field(a => a.Description);
        descriptor.Field(a => a.Price);
        descriptor.Field(a => a.Category);
        descriptor.Field(a => a.UserId);
        descriptor.Field(a => a.ImageUrls);
    }
}