using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.Connectivity;
using Platform.Engine.Models.DataExecution;

namespace Platform.Engine.Services.DataExecution;

public class ConnectorDataProvider : IDataProvider
{
    private readonly IConnectivityHub _connectivityHub;

    public ConnectorDataProvider(IConnectivityHub connectivityHub)
    {
        _connectivityHub = connectivityHub;
    }

    public string ProviderType => "Connector";

    public async Task<DataResult> ExecuteAsync(
        DataOperationMetadata metadata,
        Dictionary<string, object> parameters,
        Platform.Engine.Models.DataExecution.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (string.IsNullOrEmpty(metadata.RootEntity))
        {
            return new DataResult { Success = false, ErrorMessage = "Connector Name (RootEntity) is required." };
        }

        Guid projectId = Guid.Empty;
        if (context.AdditionalContext?.TryGetValue("ProjectId", out var pid) == true && pid is Guid guid)
        {
            projectId = guid;
        }

        var request = new ConnectorExecutionRequest
        {
            ConnectorName = metadata.RootEntity!,
            Inputs = parameters.ToDictionary(k => k.Key, v => (object?)v.Value)
        };

        var result = await _connectivityHub.ExecuteConnectorAsync(projectId, request);

        return new DataResult
        {
            Success = result.Success,
            Data = result.Data,
            ErrorMessage = result.ErrorMessage,
            ExecutionTimeSeconds = sw.Elapsed.TotalSeconds,
            RowCount = result.Success && result.Data is System.Collections.IEnumerable en ? en.Cast<object>().Count() : (result.Data != null ? 1 : 0)
        };
    }

    public Task<long> EstimateRowCountAsync(DataOperationMetadata metadata, Dictionary<string, object> parameters, Platform.Engine.Models.DataExecution.ExecutionContext context)
    {
        return Task.FromResult(1L);
    }

    public Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.RootEntity))
        {
            return Task.FromResult(new ValidationResult { IsValid = false, Errors = new List<ValidationError> { new() { Message = "RootEntity must specify the Connector Name." } } });
        }
        return Task.FromResult(new ValidationResult { IsValid = true });
    }
}
