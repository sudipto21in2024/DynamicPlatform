namespace Platform.Engine.Workflows.Activities;

using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Elsa activity for generating report output in various formats
/// </summary>
[Activity(
    Category = "Data Operations",
    DisplayName = "Generate Report Output",
    Description = "Generates report in Excel, PDF, CSV, or JSON format"
)]
public class GenerateReportOutputActivity : Activity
{
    private readonly IEnumerable<IOutputGenerator> _generators;
    
    [ActivityInput(Hint = "Data to generate report from")]
    public List<object> Data { get; set; } = new();
    
    [ActivityInput(Hint = "Output format (Excel, PDF, CSV, JSON)", DefaultValue = "Excel")]
    public string OutputFormat { get; set; } = "Excel";
    
    [ActivityInput(Hint = "Report title")]
    public string? Title { get; set; }
    
    [ActivityInput(Hint = "Include headers", DefaultValue = true)]
    public bool IncludeHeaders { get; set; } = true;
    
    [ActivityOutput]
    public Stream OutputFile { get; set; } = null!;
    
    [ActivityOutput]
    public string FileName { get; set; } = string.Empty;
    
    [ActivityOutput]
    public long FileSizeBytes { get; set; }
    
    public GenerateReportOutputActivity(IEnumerable<IOutputGenerator> generators)
    {
        _generators = generators;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        var generator = _generators.FirstOrDefault(g => 
            g.Format.Equals(OutputFormat, StringComparison.OrdinalIgnoreCase));
        
        if (generator == null)
        {
            throw new InvalidOperationException(
                $"No generator found for format: {OutputFormat}. " +
                $"Available formats: {string.Join(", ", _generators.Select(g => g.Format))}"
            );
        }
        
        var options = new OutputOptions
        {
            Format = OutputFormat,
            Title = Title,
            IncludeHeaders = IncludeHeaders
        };
        
        OutputFile = await generator.GenerateAsync(Data, options, context.CancellationToken);
        
        // Get file size
        FileSizeBytes = OutputFile.Length;
        
        // Generate filename
        var extension = OutputFormat.ToLower() switch
        {
            "excel" => "xlsx",
            "pdf" => "pdf",
            "csv" => "csv",
            "json" => "json",
            _ => "bin"
        };
        
        var sanitizedTitle = Title != null 
            ? string.Join("_", Title.Split(Path.GetInvalidFileNameChars()))
            : "report";
        
        FileName = $"{sanitizedTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";
        
        // Reset stream position for next activity
        OutputFile.Position = 0;
        
        return Done();
    }
}
