using System.Text.Json;

public class GraphQLRequest
{
    public string? Query { get; set; }
    public string? OperationName { get; set; }
    public JsonElement? Variables { get; set; }
}
