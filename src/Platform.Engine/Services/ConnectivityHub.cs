using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;
using Platform.Engine.Models;
using Platform.Engine.Models.Connectivity;

namespace Platform.Engine.Services;

public class ConnectivityHub : IConnectivityHub
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IArtifactRepository _artifactRepository;

    public ConnectivityHub(IServiceProvider serviceProvider, IArtifactRepository artifactRepository)
    {
        _serviceProvider = serviceProvider;
        _artifactRepository = artifactRepository;
    }

    public async Task<ConnectorExecutionResult> ExecuteConnectorAsync(Guid projectId, ConnectorExecutionRequest request)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. Try to find a specialized named connector in DI
            var allConnectors = _serviceProvider.GetServices<IConnector>();
            var connector = allConnectors.FirstOrDefault(c => c.Name == request.ConnectorName);

            if (connector != null)
            {
                var result = await connector.ExecuteAsync(request.Inputs);
                return new ConnectorExecutionResult 
                { 
                    Success = true, 
                    Data = result, 
                    ExecutionTimeMs = sw.Elapsed.TotalMilliseconds 
                };
            }

            // 2. Fallback: Check if it is a "Dynamic Connector" artifact
            var artifacts = await _artifactRepository.GetByProjectIdAsync(projectId);
            var artifact = artifacts.FirstOrDefault(a => a.Name == request.ConnectorName && (a.Type == ArtifactType.Connector || a.Type == ArtifactType.Integration));

            if (artifact != null)
            {
                // Here we would typically invoke the "Dynamic Execution Engine" 
                // for the business logic stored in the artifact.
                // For this implementation, we simulate the execution of the logic.
                
                var metadata = JsonSerializer.Deserialize<ConnectorMetadata>(artifact.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (metadata != null)
                {
                    // Logic simulation (assuming the system uses an evaluator like Roslyn or DynamicExpresso)
                    // In a real system, this would be: return await _dynamicEvaluator.ExecuteAsync(metadata.BusinessLogic, request.Inputs);
                    
                    return new ConnectorExecutionResult 
                    { 
                        Success = true, 
                        Data = new { Message = $"Dynamic Execution of {artifact.Name} simulated.", InputsReceived = request.Inputs },
                        ExecutionTimeMs = sw.Elapsed.TotalMilliseconds 
                    };
                }
            }

            return new ConnectorExecutionResult 
            { 
                Success = false, 
                ErrorMessage = $"Connector '{request.ConnectorName}' not found in DI or Artifacts.",
                ExecutionTimeMs = sw.Elapsed.TotalMilliseconds 
            };
        }
        catch (Exception ex)
        {
            return new ConnectorExecutionResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message, 
                ExecutionTimeMs = sw.Elapsed.TotalMilliseconds 
            };
        }
    }

    public async Task<IEnumerable<ConnectorMetadata>> GetAvailableConnectorsAsync(Guid projectId)
    {
        var artifacts = await _artifactRepository.GetByProjectIdAsync(projectId);
        var connectorArtifacts = artifacts.Where(a => a.Type == ArtifactType.Connector || a.Type == ArtifactType.Integration);

        var results = new List<ConnectorMetadata>();
        foreach (var a in connectorArtifacts)
        {
            try 
            {
                var meta = JsonSerializer.Deserialize<ConnectorMetadata>(a.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (meta != null) results.Add(meta);
            }
            catch { /* Ignore malformed metadata */ }
        }

        return results;
    }
}
