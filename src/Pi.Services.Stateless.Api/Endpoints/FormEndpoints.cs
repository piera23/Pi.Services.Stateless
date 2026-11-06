using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pi.FormGenerator.Models;
using Pi.FormGenerator.Services;

namespace Pi.Services.Stateless.Api.Endpoints;

public record ValidateSubmissionRequest(Dictionary<string, object?> Values);
public record SubmitFormRequest(string SubmittedBy, Dictionary<string, object?> Values);

public static class FormEndpoints
{
    public static IEndpointRouteBuilder MapFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forms").WithTags("FormGenerator (Low-Code Form Builder)");

        group.MapGet("/", async (IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.GetAllFormDefinitionsAsync(ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("GetAllForms")
        .WithSummary("Restituisce l'elenco di tutte le definizioni di form create.");

        group.MapGet("/{formCode}", async (string formCode, IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.GetFormDefinitionAsync(formCode, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetFormByCode")
        .WithSummary("Restituisce lo schema e i campi del form specificato.");

        group.MapPost("/", async (FormDefinition definition, IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.CreateOrUpdateFormDefinitionAsync(definition, ct);
            return res.IsSuccess ? Results.Created($"/api/forms/{res.Value?.FormCode}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("SaveFormDefinition")
        .WithSummary("Crea o aggiorna la definizione, il layout e i campi di un form Low-Code.");

        group.MapPost("/{formCode}/validate", async (string formCode, ValidateSubmissionRequest req, IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.ValidateSubmissionFieldsAsync(formCode, req.Values, ct);
            return res.IsSuccess 
                ? Results.Ok(new { isValid = true }) 
                : Results.UnprocessableEntity(new { isValid = false, errors = res.Errors });
        })
        .WithName("ValidateFormFields")
        .WithSummary("Validazione pura stateless dei campi inviati senza salvare alcunché.");

        group.MapPost("/{formCode}/submissions", async (string formCode, SubmitFormRequest req, IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.SubmitFormAsync(formCode, req.SubmittedBy, req.Values, ct);
            return res.IsSuccess 
                ? Results.Created($"/api/forms/{formCode}/submissions/{res.Value?.SubmissionId}", res.Value) 
                : Results.BadRequest(new { error = res.Error, errors = res.Errors });
        })
        .WithName("SubmitForm")
        .WithSummary("Valida e registra i valori compilati per un form.");

        group.MapGet("/{formCode}/submissions/{submissionId}", async (string formCode, string submissionId, IFormGeneratorService svc, CancellationToken ct) =>
        {
            var res = await svc.GetSubmissionAsync(submissionId, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetFormSubmission")
        .WithSummary("Recupera una specifica compilazione di un form.");

        // -------------------------------------------------------------------------------------------------------
        // FUNZIONE LOW-CODE: Generazione Schema e Query di Salvataggio per il DB scelto dall'utente
        // -------------------------------------------------------------------------------------------------------
        group.MapPost("/{formCode}/database-schema", async (
            string formCode,
            DatabaseDialect? dialect,
            IFormGeneratorService formSvc,
            ISchemaGeneratorService schemaSvc,
            CancellationToken ct) =>
        {
            var formRes = await formSvc.GetFormDefinitionAsync(formCode, ct);
            if (formRes.IsFailure || formRes.Value is null)
                return Results.NotFound(formRes.Error);

            var targetDialect = dialect ?? DatabaseDialect.PostgreSql;
            var schemaResult = schemaSvc.GenerateSchema(formRes.Value, targetDialect);
            return Results.Ok(schemaResult);
        })
        .WithName("GenerateDatabaseSchema")
        .WithSummary("Genera lo script DDL 'CREATE TABLE' per creare la tabella nel database a scelta dell'utente (PostgreSql, SqlServer, MySql, Sqlite).");

        group.MapPost("/{formCode}/export-insert", async (
            string formCode,
            DatabaseDialect? dialect,
            Dictionary<string, object?> values,
            IFormGeneratorService formSvc,
            ISchemaGeneratorService schemaSvc,
            CancellationToken ct) =>
        {
            var formRes = await formSvc.GetFormDefinitionAsync(formCode, ct);
            if (formRes.IsFailure || formRes.Value is null)
                return Results.NotFound(formRes.Error);

            var targetDialect = dialect ?? DatabaseDialect.PostgreSql;
            var query = schemaSvc.GenerateInsertQuery(formRes.Value, values, targetDialect);
            return Results.Ok(new { dialect = targetDialect.ToString(), query });
        })
        .WithName("GenerateInsertQuery")
        .WithSummary("Genera la query 'INSERT INTO' per persistere i dati compilati direttamente nel database configurato dall'utente.");

        return app;
    }
}
