using AvitoBackend.Models;
using AvitoBackend.Models.Core;
using HotChocolate.Types;

namespace AvitoBackend.GraphQL.Types;

public class UserType : ObjectType<AppUser>
{
    protected override void Configure(IObjectTypeDescriptor<AppUser> descriptor)
    {
        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.Email);
        descriptor.Field(u => u.UserName);
    }
}