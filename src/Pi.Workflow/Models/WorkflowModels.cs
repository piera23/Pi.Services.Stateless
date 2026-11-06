namespace Pi.Workflow.Models;

public enum WorkflowStatus
{
    InCorso,
    Completato,
    Rifiutato,
    Annullato
}

public class StepDefinition
{
    public int StepIndex { get; init; }
    public required string Title { get; init; }
    public string? RequiredRole { get; init; }
    public bool IsBlocking { get; init; } = true;
    public IReadOnlyList<string> AllowedActions { get; init; } = new[] { "Approva", "Rifiuta", "RiportaIndietro" };
    public string? AutomaticActionName { get; init; }
}

public class WorkflowDefinition
{
    public required string WorkflowCode { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; } = 1;
    public IReadOnlyList<StepDefinition> Steps { get; init; } = Array.Empty<StepDefinition>();
}

public class StepExecutionLog
{
    public int StepIndex { get; init; }
    public required string Action { get; init; }
    public required string Actor { get; init; }
    public string? Notes { get; init; }
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public bool IsSuccessful { get; init; } = true;
}

public class WorkflowState
{
    public int CurrentStepIndex { get; set; } = 0;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.InCorso;
    public List<StepExecutionLog> History { get; init; } = new();
}

public class WorkflowInstance
{
    public string InstanceId { get; init; } = Guid.NewGuid().ToString("N");
    public required string WorkflowCode { get; init; }
    public required string CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public WorkflowState State { get; set; } = new();
}
