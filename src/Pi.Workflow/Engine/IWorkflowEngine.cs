using Pi.Common.Models;
using Pi.Workflow.Models;

namespace Pi.Workflow.Engine;

public interface IWorkflowEngine
{
    // Funzionalità Pure Stateless: computa la transizione senza toccare alcun database
    Result<WorkflowState> EvaluateTransition(
        WorkflowDefinition definition,
        WorkflowState currentState,
        string action,
        string actor,
        string? notes = null);

    // Funzionalità con storage pluggabile (In-Memory di default)
    Task<Result<WorkflowDefinition>> CreateOrUpdateDefinitionAsync(WorkflowDefinition definition, CancellationToken ct = default);
    Task<Result<WorkflowDefinition>> GetDefinitionAsync(string workflowCode, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkflowDefinition>>> GetAllDefinitionsAsync(CancellationToken ct = default);
    Task<Result<WorkflowInstance>> StartWorkflowAsync(string workflowCode, string createdBy, CancellationToken ct = default);
    Task<Result<WorkflowInstance>> ExecuteStepAsync(string instanceId, string action, string actor, string? notes = null, CancellationToken ct = default);
    Task<Result<WorkflowInstance>> GetInstanceAsync(string instanceId, CancellationToken ct = default);
}
