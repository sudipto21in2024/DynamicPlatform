namespace Platform.Engine.Workflows.Activities;

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Elsa activity for generating report output in various formats
/// </summary>
[Activity("Data Operations", "Generate Report Output", Description = "Generates report in Excel, PDF, CSV, or JSON format")]
public class GenerateReportOutputActivity : CodeActivity
{
    [Input(Description = "Data to generate report from")]
    public Input<List<object>> Data { get; set; } = default!;
    
    [Input(Description = "Output format (Excel, PDF, CSV, JSON)", DefaultValue = "Excel")]
    public Input<string> OutputFormat { get; set; } = new("Excel");
    
    [Input(Description = "Report title")]
    public Input<string?> Title { get; set; } = default!;
    
    [Input(Description = "Include headers", DefaultValue = true)]
    public Input<bool> IncludeHeaders { get; set; } = new(true);
    
    [Output]
    public Output<Stream> OutputFile { get; set; } = default!;
    
    [Output]
    public Output<string> FileName { get; set; } = default!;
    
    [Output]
    public Output<long> FileSizeBytes { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var generators = context.GetRequiredService<IEnumerable<IOutputGenerator>>();
        
        var outputFormat = OutputFormat.Get(context);
        var generator = generators.FirstOrDefault(g => 
            g.Format.Equals(outputFormat, StringComparison.OrdinalIgnoreCase));
        
        if (generator == null)
        {
            throw new InvalidOperationException(
                $"No generator found for format: {outputFormat}. " +
                $"Available formats: {string.Join(", ", generators.Select(g => g.Format))}"
            );
        }
        
        var options = new OutputOptions
        {
            Format = outputFormat,
            Title = Title.Get(context),
            IncludeHeaders = IncludeHeaders.Get(context)
        };
        
        var data = Data.Get(context) ?? new List<object>();
        var outputFile = await generator.GenerateAsync(data, options, context.CancellationToken);
        
        // Get file size
        var fileSizeBytes = outputFile.Length;
        
        // Generate filename
        var extension = outputFormat.ToLower() switch
        {
            "excel" => "xlsx",
            "pdf" => "pdf",
            "csv" => "csv",
            "json" => "json",
            _ => "bin"
        };
        
        var title = Title.Get(context);
        var sanitizedTitle = title != null 
            ? string.Join("_", title.Split(Path.GetInvalidFileNameChars()))
            : "report";
        
        var fileName = $"{sanitizedTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";
        
        // Reset stream position for next activity (important!)
        outputFile.Position = 0;
        
        OutputFile.Set(context, outputFile);
        FileName.Set(context, fileName);
        FileSizeBytes.Set(context, fileSizeBytes);
    }
}
