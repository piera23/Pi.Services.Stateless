using System.Collections.Concurrent;
using Pi.Workflow.Abstractions;
using Pi.Workflow.Models;

namespace Pi.Workflow.Storage.InMemory;

public class InMemoryWorkflowStore : IWorkflowStore
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new(StringComparer.OrdinalIgnoreCase);

    public Task<WorkflowDefinition?> GetDefinitionAsync(string workflowCode, CancellationToken ct = default)
    {
        _definitions.TryGetValue(workflowCode, out var def);
        return Task.FromResult(def);
    }

    public Task<IReadOnlyList<WorkflowDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<WorkflowDefinition> list = _definitions.Values.ToList();
        return Task.FromResult(list);
    }

    public Task SaveDefinitionAsync(WorkflowDefinition definition, CancellationToken ct = default)
    {
        _definitions[definition.WorkflowCode] = definition;
        return Task.CompletedTask;
    }

    public Task<WorkflowInstance?> GetInstanceAsync(string instanceId, CancellationToken ct = default)
    {
        _instances.TryGetValue(instanceId, out var instance);
        return Task.FromResult(instance);
    }

    public Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        _instances[instance.InstanceId] = instance;
        return Task.CompletedTask;
    }
}
