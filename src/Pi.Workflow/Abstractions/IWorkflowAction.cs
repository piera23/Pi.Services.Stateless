using Pi.Common.Models;

namespace Pi.Workflow.Abstractions;

public interface IWorkflowAction
{
    string Name { get; }
    Task<Result> ExecuteAsync(string instanceId, CancellationToken ct = default);
}
