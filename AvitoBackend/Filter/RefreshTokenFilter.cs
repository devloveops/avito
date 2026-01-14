using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class RefreshTokenFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isRefreshEndpoint = context.ApiDescription.RelativePath == "api/auth/refresh" 
                               && context.ApiDescription.HttpMethod == "POST";
        
        if (isRefreshEndpoint)
        {
            operation.Security = null;
            
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "RefreshToken" }
                        },
                        Array.Empty<string>()
                    }
                }
            };
        }
    }
}