namespace Platform.Engine.Services.OutputGenerators;

using System.Text;
using System.Text.Json;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Generates JSON output from data
/// </summary>
public class JsonOutputGenerator : IOutputGenerator
{
    public string Format => "JSON";
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        
        return Task.FromResult<Stream>(stream);
    }
}
