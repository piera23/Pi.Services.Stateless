using Pi.Common.Models;
using Pi.Workflow.Abstractions;
using Pi.Workflow.Models;

namespace Pi.Workflow.Engine;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowStore _store;
    private readonly IEnumerable<IWorkflowAction> _automaticActions;

    public WorkflowEngine(IWorkflowStore store, IEnumerable<IWorkflowAction> automaticActions)
    {
        _store = store;
        _automaticActions = automaticActions;
    }

    public Result<WorkflowState> EvaluateTransition(
        WorkflowDefinition definition,
        WorkflowState currentState,
        string action,
        string actor,
        string? notes = null)
    {
        if (definition.Steps.Count == 0)
            return Result<WorkflowState>.Failure("La definizione di workflow non contiene alcun passo.");

        if (currentState.Status != WorkflowStatus.InCorso)
            return Result<WorkflowState>.Failure($"Impossibile avanzare un workflow con stato '{currentState.Status}'.");

        var currentStep = definition.Steps.FirstOrDefault(s => s.StepIndex == currentState.CurrentStepIndex);
        if (currentStep is null)
            return Result<WorkflowState>.Failure($"Passo {currentState.CurrentStepIndex} non trovato nella definizione.");

        // Controllo se l'azione è ammessa
        var actionMatch = currentStep.AllowedActions.FirstOrDefault(a => string.Equals(a, action, StringComparison.OrdinalIgnoreCase));
        if (actionMatch is null)
            return Result<WorkflowState>.Failure($"L'azione '{action}' non è consentita per il passo '{currentStep.Title}'.");

        var newState = new WorkflowState
        {
            CurrentStepIndex = currentState.CurrentStepIndex,
            Status = currentState.Status,
            History = new List<StepExecutionLog>(currentState.History)
        };

        var log = new StepExecutionLog
        {
            StepIndex = currentStep.StepIndex,
            Action = actionMatch,
            Actor = actor,
            Notes = notes,
            ExecutedAt = DateTime.UtcNow,
            IsSuccessful = true
        };
        newState.History.Add(log);

        // Valutazione transizione
        if (string.Equals(actionMatch, "Rifiuta", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionMatch, "Respingi", StringComparison.OrdinalIgnoreCase))
        {
            newState.Status = WorkflowStatus.Rifiutato;
        }
        else if (string.Equals(actionMatch, "RiportaIndietro", StringComparison.OrdinalIgnoreCase))
        {
            if (newState.CurrentStepIndex > 0)
                newState.CurrentStepIndex--;
        }
        else if (string.Equals(actionMatch, "RiportaPrimoPasso", StringComparison.OrdinalIgnoreCase))
        {
            newState.CurrentStepIndex = 0;
        }
        else // "Approva", "Avanza", "Visto", ecc.
        {
            var nextIndex = currentState.CurrentStepIndex + 1;
            if (nextIndex >= definition.Steps.Count)
            {
                // Flusso completato con successo
                newState.Status = WorkflowStatus.Completato;
            }
            else
            {
                newState.CurrentStepIndex = nextIndex;
            }
        }

        return Result<WorkflowState>.Success(newState);
    }

    public async Task<Result<WorkflowDefinition>> CreateOrUpdateDefinitionAsync(WorkflowDefinition definition, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(definition.WorkflowCode))
            return Result<WorkflowDefinition>.Failure("WorkflowCode obbligatorio.");

        if (string.IsNullOrWhiteSpace(definition.Title))
            return Result<WorkflowDefinition>.Failure("Titolo workflow obbligatorio.");

        await _store.SaveDefinitionAsync(definition, ct);
        return Result<WorkflowDefinition>.Success(definition);
    }

    public async Task<Result<WorkflowDefinition>> GetDefinitionAsync(string workflowCode, CancellationToken ct = default)
    {
        var def = await _store.GetDefinitionAsync(workflowCode, ct);
        if (def is null)
            return Result<WorkflowDefinition>.Failure($"Workflow '{workflowCode}' non trovato.");

        return Result<WorkflowDefinition>.Success(def);
    }

    public async Task<Result<IReadOnlyList<WorkflowDefinition>>> GetAllDefinitionsAsync(CancellationToken ct = default)
    {
        var list = await _store.GetAllDefinitionsAsync(ct);
        return Result<IReadOnlyList<WorkflowDefinition>>.Success(list);
    }

    public async Task<Result<WorkflowInstance>> StartWorkflowAsync(string workflowCode, string createdBy, CancellationToken ct = default)
    {
        var defResult = await GetDefinitionAsync(workflowCode, ct);
        if (defResult.IsFailure || defResult.Value is null)
            return Result<WorkflowInstance>.Failure(defResult.Error ?? "Definizione non trovata.");

        var instance = new WorkflowInstance
        {
            WorkflowCode = defResult.Value.WorkflowCode,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            State = new WorkflowState
            {
                CurrentStepIndex = 0,
                Status = WorkflowStatus.InCorso,
                History = new List<StepExecutionLog>
                {
                    new()
                    {
                        StepIndex = 0,
                        Action = "Avvio",
                        Actor = createdBy,
                        Notes = "Avvio procedimento",
                        ExecutedAt = DateTime.UtcNow
                    }
                }
            }
        };

        await _store.SaveInstanceAsync(instance, ct);
        return Result<WorkflowInstance>.Success(instance);
    }

    public async Task<Result<WorkflowInstance>> ExecuteStepAsync(
        string instanceId,
        string action,
        string actor,
        string? notes = null,
        CancellationToken ct = default)
    {
        var instance = await _store.GetInstanceAsync(instanceId, ct);
        if (instance is null)
            return Result<WorkflowInstance>.Failure($"Istanza di workflow '{instanceId}' non trovata.");

        var def = await _store.GetDefinitionAsync(instance.WorkflowCode, ct);
        if (def is null)
            return Result<WorkflowInstance>.Failure($"Definizione di workflow '{instance.WorkflowCode}' non trovata.");

        var currentStep = def.Steps.FirstOrDefault(s => s.StepIndex == instance.State.CurrentStepIndex);

        var transitionResult = EvaluateTransition(def, instance.State, action, actor, notes);
        if (transitionResult.IsFailure || transitionResult.Value is null)
            return Result<WorkflowInstance>.Failure(transitionResult.Error ?? "Transizione non valida.");

        instance.State = transitionResult.Value;

        if (instance.State.Status == WorkflowStatus.Completato || instance.State.Status == WorkflowStatus.Rifiutato)
        {
            instance.CompletedAt = DateTime.UtcNow;
        }

        // Se il passo eseguito aveva un'azione automatica configurata ed è stato approvato, eseguiamola
        if (currentStep?.AutomaticActionName is not null && instance.State.Status != WorkflowStatus.Rifiutato)
        {
            var autoAction = _automaticActions.FirstOrDefault(a => string.Equals(a.Name, currentStep.AutomaticActionName, StringComparison.OrdinalIgnoreCase));
            if (autoAction is not null)
            {
                await autoAction.ExecuteAsync(instanceId, ct);
            }
        }

        await _store.SaveInstanceAsync(instance, ct);
        return Result<WorkflowInstance>.Success(instance);
    }

    public async Task<Result<WorkflowInstance>> GetInstanceAsync(string instanceId, CancellationToken ct = default)
    {
        var instance = await _store.GetInstanceAsync(instanceId, ct);
        if (instance is null)
            return Result<WorkflowInstance>.Failure($"Istanza '{instanceId}' non trovata.");

        return Result<WorkflowInstance>.Success(instance);
    }
}
