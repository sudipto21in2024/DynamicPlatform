namespace Platform.Engine.Services.OutputGenerators;

using System.Text;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Generates CSV output from data
/// </summary>
public class CsvOutputGenerator : IOutputGenerator
{
    public string Format => "CSV";
    
    public Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options,
        CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, new UTF8Encoding(true)); // UTF-8 with BOM
        
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            writer.Flush();
            stream.Position = 0;
            return Task.FromResult<Stream>(stream);
        }
        
        // Get properties from first item
        var firstItem = dataList.First();
        var properties = firstItem.GetType().GetProperties();
        
        // Write headers
        if (options.IncludeHeaders)
        {
            var headers = properties.Select(p => EscapeCsvValue(p.Name));
            writer.WriteLine(string.Join(",", headers));
        }
        
        // Write data rows
        foreach (var item in dataList)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvValue(value?.ToString() ?? string.Empty);
            });
            writer.WriteLine(string.Join(",", values));
        }
        
        writer.Flush();
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
    
    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}
