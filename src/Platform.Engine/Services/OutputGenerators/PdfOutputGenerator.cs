namespace Platform.Engine.Services.OutputGenerators;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Generates PDF output from data using QuestPDF
/// </summary>
public class PdfOutputGenerator : IOutputGenerator
{
    public string Format => "PDF";
    
    static PdfOutputGenerator()
    {
        // Set QuestPDF license (Community license for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options,
        CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        var properties = dataList.Any() 
            ? dataList.First().GetType().GetProperties() 
            : Array.Empty<System.Reflection.PropertyInfo>();
        
        var stream = new MemoryStream();
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                // Header
                page.Header()
                    .AlignCenter()
                    .Text(options.Title ?? "Report")
                    .SemiBold()
                    .FontSize(16)
                    .FontColor(Colors.Blue.Darken2);
                
                // Content
                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        column.Spacing(5);
                        
                        if (!dataList.Any())
                        {
                            column.Item().Text("No data available").Italic();
                            return;
                        }
                        
                        // Create table
                        column.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var prop in properties)
                                {
                                    columns.RelativeColumn();
                                }
                            });
                            
                            // Header row
                            if (options.IncludeHeaders)
                            {
                                table.Header(header =>
                                {
                                    foreach (var prop in properties)
                                    {
                                        header.Cell()
                                            .Background(Colors.Grey.Lighten2)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Darken1)
                                            .Padding(5)
                                            .Text(prop.Name)
                                            .SemiBold()
                                            .FontSize(9);
                                    }
                                });
                            }
                            
                            // Data rows
                            foreach (var item in dataList)
                            {
                                foreach (var prop in properties)
                                {
                                    var value = prop.GetValue(item);
                                    var displayValue = FormatValue(value);
                                    
                                    table.Cell()
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Padding(5)
                                        .Text(displayValue)
                                        .FontSize(8);
                                }
                            }
                        });
                    });
                
                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).SemiBold();
                        text.Span(" | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    })
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        })
        .GeneratePdf(stream);
        
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
    
    private string FormatValue(object? value)
    {
        if (value == null)
            return string.Empty;
        
        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? string.Empty
        };
    }
}
