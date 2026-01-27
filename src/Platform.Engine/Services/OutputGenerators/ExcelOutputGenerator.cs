namespace Platform.Engine.Services.OutputGenerators;

using ClosedXML.Excel;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Generates Excel output from data using ClosedXML
/// </summary>
public class ExcelOutputGenerator : IOutputGenerator
{
    public string Format => "Excel";
    
    public Task<Stream> GenerateAsync(
        IEnumerable<object> data,
        OutputOptions options,
        CancellationToken cancellationToken = default)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(options.Title ?? "Report");
        
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            var emptyStream = new MemoryStream();
            workbook.SaveAs(emptyStream);
            emptyStream.Position = 0;
            return Task.FromResult<Stream>(emptyStream);
        }
        
        // Get properties from first item
        var firstItem = dataList.First();
        var properties = firstItem.GetType().GetProperties();
        
        // Write headers
        if (options.IncludeHeaders)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = properties[i].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
        }
        
        // Write data rows
        int rowIndex = options.IncludeHeaders ? 2 : 1;
        foreach (var item in dataList)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item);
                var cell = worksheet.Cell(rowIndex, i + 1);
                
                // Handle different data types
                if (value == null)
                {
                    cell.Value = string.Empty;
                }
                else if (value is DateTime dateTime)
                {
                    cell.Value = dateTime;
                    cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                }
                else if (value is decimal || value is double || value is float)
                {
                    cell.Value = Convert.ToDouble(value);
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (value is int || value is long)
                {
                    cell.Value = Convert.ToInt64(value);
                    cell.Style.NumberFormat.Format = "#,##0";
                }
                else if (value is bool boolValue)
                {
                    cell.Value = boolValue ? "Yes" : "No";
                }
                else
                {
                    cell.Value = value.ToString();
                }
            }
            rowIndex++;
        }
        
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        
        // Add borders to all cells
        var dataRange = worksheet.Range(1, 1, rowIndex - 1, properties.Length);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        
        // Freeze header row
        if (options.IncludeHeaders)
        {
            worksheet.SheetView.FreezeRows(1);
        }
        
        // Save to stream
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        
        return Task.FromResult<Stream>(stream);
    }
}
