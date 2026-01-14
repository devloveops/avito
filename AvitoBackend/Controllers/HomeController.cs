using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AvitoBackend.Controllers;

[ApiController]
public class HomeController : ControllerBase
{
    private readonly EndpointDataSource _endpointDataSource;

    public HomeController(EndpointDataSource endpointDataSource)
    {
        _endpointDataSource = endpointDataSource;
    }

    /// <summary>
    /// Список всех доступных эндпоинтов
    /// </summary>
    [HttpGet("/")]
    public IActionResult GetEndpoints()
    {
        var endpoints = _endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new
            {
                Pattern = e.RoutePattern.RawText,
                Methods = string.Join(", ", e.Metadata.OfType<IHttpMethodMetadata>()
                    .SelectMany(m => m.HttpMethods))
            })
            .OrderBy(x => x.Pattern)
            .ToList();

        return Ok(endpoints);
    }

    /// <summary>
    /// Проверка работоспособности сервиса
    /// </summary>
    [HttpGet("/health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0"
        });
    }
}