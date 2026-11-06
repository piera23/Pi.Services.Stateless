using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pi.Identity.Abstractions;
using Pi.Identity.Models;

namespace Pi.Services.Stateless.Api.Endpoints;

public record ResolveActorRequest(string StepRequiredRole, string ApplicantMatricola);

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity").WithTags("Identity & Profiling");

        group.MapGet("/users", async (string? role, string? department, IIdentityService svc, CancellationToken ct) =>
        {
            var res = await svc.GetUsersAsync(role, department, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("GetUsers");

        group.MapGet("/users/{matricola}", async (string matricola, IIdentityService svc, CancellationToken ct) =>
        {
            var res = await svc.GetUserByMatricolaAsync(matricola, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetUserByMatricola");

        group.MapPost("/users", async (UserAccount user, IIdentityService svc, CancellationToken ct) =>
        {
            var res = await svc.UpsertUserAsync(user, ct);
            return res.IsSuccess ? Results.Created($"/api/identity/users/{res.Value?.Matricola}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("UpsertUser");

        // -------------------------------------------------------------
        // Profilatura Dinamica Passi: Risoluzione Attore (es. Dirigente)
        // -------------------------------------------------------------
        group.MapPost("/resolve-actor", async (ResolveActorRequest req, IActorResolver resolver, CancellationToken ct) =>
        {
            var res = await resolver.ResolveStepActorAsync(req.StepRequiredRole, req.ApplicantMatricola, ct);
            return res.IsSuccess 
                ? Results.Ok(new { actorMatricola = res.Value }) 
                : Results.NotFound(new { error = res.Error });
        })
        .WithName("ResolveStepActor")
        .WithSummary("Risolve dinamicamente la matricola dell'attore preposto al passo in base al ruolo o alla gerarchia (es. Dirigente dell'autore).");

        return app;
    }
}
