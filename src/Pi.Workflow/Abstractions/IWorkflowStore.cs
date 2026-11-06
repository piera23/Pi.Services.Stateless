using Pi.Workflow.Models;

namespace Pi.Workflow.Abstractions;

public interface IWorkflowStore
{
    Task<WorkflowDefinition?> GetDefinitionAsync(string workflowCode, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default);
    Task SaveDefinitionAsync(WorkflowDefinition definition, CancellationToken ct = default);
    Task<WorkflowInstance?> GetInstanceAsync(string instanceId, CancellationToken ct = default);
    Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken ct = default);
}
