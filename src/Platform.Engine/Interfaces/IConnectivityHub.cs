using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Engine.Models;
using Platform.Engine.Models.Connectivity;

namespace Platform.Engine.Interfaces;

public interface IConnectivityHub
{
    Task<ConnectorExecutionResult> ExecuteConnectorAsync(Guid projectId, ConnectorExecutionRequest request);
    Task<IEnumerable<ConnectorMetadata>> GetAvailableConnectorsAsync(Guid projectId);
}
