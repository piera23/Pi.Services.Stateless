using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pi.Workflow.Engine;
using Pi.Workflow.Models;

namespace Pi.Services.Stateless.Api.Endpoints;

public record EvaluateTransitionRequest(
    WorkflowDefinition Definition,
    WorkflowState CurrentState,
    string Action,
    string Actor,
    string? Notes = null);

public record StartWorkflowRequest(string WorkflowCode, string CreatedBy);
public record ExecuteStepRequest(string Action, string Actor, string? Notes = null);

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workflow").WithTags("Workflow");

        // -------------------------------------------------------------
        // STATELSS PURO: Calcolo matematico transizione senza DB o Store
        // -------------------------------------------------------------
        group.MapPost("/evaluate-transition", (EvaluateTransitionRequest req, IWorkflowEngine engine) =>
        {
            var res = engine.EvaluateTransition(req.Definition, req.CurrentState, req.Action, req.Actor, req.Notes);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("EvaluateTransitionStateless")
        .WithSummary("Valutazione transizione pura stateless: calcola il NextState e l'audit log senza scrivere su DB o memoria.");

        // -------------------------------------------------------------
        // Definizioni di Workflow
        // -------------------------------------------------------------
        group.MapGet("/definitions", async (IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.GetAllDefinitionsAsync(ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("GetAllWorkflowDefinitions");

        group.MapGet("/definitions/{workflowCode}", async (string workflowCode, IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.GetDefinitionAsync(workflowCode, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetWorkflowDefinition");

        group.MapPost("/definitions", async (WorkflowDefinition definition, IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.CreateOrUpdateDefinitionAsync(definition, ct);
            return res.IsSuccess ? Results.Created($"/api/workflow/definitions/{res.Value?.WorkflowCode}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("SaveWorkflowDefinition");

        // -------------------------------------------------------------
        // Istanze di Workflow (con store pluggabile - in-memory di default)
        // -------------------------------------------------------------
        group.MapPost("/instances/start", async (StartWorkflowRequest req, IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.StartWorkflowAsync(req.WorkflowCode, req.CreatedBy, ct);
            return res.IsSuccess ? Results.Created($"/api/workflow/instances/{res.Value?.InstanceId}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("StartWorkflowInstance");

        group.MapPost("/instances/{instanceId}/execute-step", async (string instanceId, ExecuteStepRequest req, IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.ExecuteStepAsync(instanceId, req.Action, req.Actor, req.Notes, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("ExecuteWorkflowStep");

        group.MapGet("/instances/{instanceId}", async (string instanceId, IWorkflowEngine engine, CancellationToken ct) =>
        {
            var res = await engine.GetInstanceAsync(instanceId, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetWorkflowInstance");

        return app;
    }
}
