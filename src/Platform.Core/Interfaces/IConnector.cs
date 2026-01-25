using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Core.Interfaces;

public interface IConnector
{
    string Name { get; }
    Task<object?> ExecuteAsync(IDictionary<string, object?> inputs);
}

public interface IConnector<TIn, TOut> : IConnector
{
    Task<TOut> ExecuteAsync(TIn input);
}
