using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pi.Documents.Services;
using Pi.Integrations.Protocollo;
using Pi.Integrations.Reporting;

namespace Pi.Services.Stateless.Api.Endpoints;

public record ProtocollaDocumentoRequest(
    string DocumentId,
    string DocumentTypeCode,
    string Oggetto,
    string CodiceFiscaleAutore,
    string CognomeAutore,
    string NomeAutore,
    string? CodiceStruttura = null);

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integrations").WithTags("Integrations (Protocollo & Reporting)");

        // -------------------------------------------------------------
        // Protocollazione (SOAP Paleo o Mock Stateless)
        // -------------------------------------------------------------
        group.MapPost("/protocollo/registra", async (
            ProtocollaDocumentoRequest req,
            IProtocolloService protocolloSvc,
            IDocumentService docSvc,
            CancellationToken ct) =>
        {
            var richiesta = new ProtocolloRichiesta
            {
                DocumentId = req.DocumentId,
                DocumentTypeCode = req.DocumentTypeCode,
                Oggetto = req.Oggetto,
                CodiceFiscaleAutore = req.CodiceFiscaleAutore,
                CognomeAutore = req.CognomeAutore,
                NomeAutore = req.NomeAutore,
                CodiceStruttura = req.CodiceStruttura
            };

            var res = await protocolloSvc.ProtocollaAsync(richiesta, ct);
            if (res.IsFailure || res.Value is null)
                return Results.BadRequest(res.Error);

            // Aggiorna anche il documento con i dati del protocollo assegnato
            if (!string.IsNullOrWhiteSpace(req.DocumentId))
            {
                await docSvc.SetProtocolDataAsync(req.DocumentId, res.Value.NumeroProtocollo, res.Value.DataProtocollo, ct);
            }

            return Results.Ok(res.Value);
        })
        .WithName("RegistraProtocollo")
        .WithSummary("Registra il protocollo documentale tramite il provider configurato (Paleo o Mock Stateless).");

        // -------------------------------------------------------------
        // Reportistica (Generazione PDF / Excel / CSV / JSON)
        // -------------------------------------------------------------
        group.MapPost("/reports/render", async (ReportRequest req, IReportGenerator reportGen, CancellationToken ct) =>
        {
            var res = await reportGen.GenerateReportAsync(req, ct);
            if (res.IsFailure || res.Value is null)
                return Results.BadRequest(res.Error);

            return Results.File(res.Value.Content, res.Value.ContentType, res.Value.FileName);
        })
        .WithName("RenderReport")
        .WithSummary("Genera un output documentale o reportistica formattata a partire dai parametri forniti.");

        return app;
    }
}
