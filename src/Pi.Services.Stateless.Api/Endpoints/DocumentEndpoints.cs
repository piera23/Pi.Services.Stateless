using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Pi.Documents.Abstractions;
using Pi.Documents.Models;
using Pi.Documents.Services;

namespace Pi.Services.Stateless.Api.Endpoints;

public record CreateDocumentRequest(
    string DocumentTypeCode,
    string AuthorUser,
    string Subject,
    string? Notes = null,
    string? ReferencedDocId = null);

public record AddDocumentMessageRequest(string SenderUser, string Text);
public record UpdateDocumentStateRequest(bool IsCompleted, bool IsRejected, string? RejectionReason = null);

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        // -------------------------------------------------------------
        // Tipi di Documento
        // -------------------------------------------------------------
        group.MapGet("/types", async (IDocumentStore store, CancellationToken ct) =>
        {
            var list = await store.GetAllTypeCodesAsync(ct);
            return Results.Ok(list);
        })
        .WithName("GetAllDocumentTypes");

        group.MapPost("/types", async (DocumentTypeCode typeCode, IDocumentStore store, CancellationToken ct) =>
        {
            await store.SaveTypeCodeAsync(typeCode, ct);
            return Results.Created($"/api/documents/types/{typeCode.Code}", typeCode);
        })
        .WithName("SaveDocumentType");

        // -------------------------------------------------------------
        // Istanze Documentali
        // -------------------------------------------------------------
        group.MapPost("/", async (CreateDocumentRequest req, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.CreateDocumentAsync(req.DocumentTypeCode, req.AuthorUser, req.Subject, req.Notes, req.ReferencedDocId, ct);
            return res.IsSuccess ? Results.Created($"/api/documents/{res.Value?.DocumentId}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("CreateDocument");

        group.MapGet("/{id}", async (string id, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.GetDocumentAsync(id, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetDocumentById");

        group.MapGet("/author/{authorUser}", async (string authorUser, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.GetDocumentsByAuthorAsync(authorUser, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("GetDocumentsByAuthor");

        group.MapPut("/{id}/state", async (string id, UpdateDocumentStateRequest req, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.UpdateDocumentStateAsync(id, req.IsCompleted, req.IsRejected, req.RejectionReason, ct);
            return res.IsSuccess ? Results.NoContent() : Results.BadRequest(res.Error);
        })
        .WithName("UpdateDocumentState");

        // -------------------------------------------------------------
        // Allegati (Upload & Download Streaming)
        // -------------------------------------------------------------
        group.MapPost("/{id}/attachments", async (string id, IFormFile file, [FromForm] string uploadedBy, IDocumentService svc, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("Nessun file fornito.");

            using var stream = file.OpenReadStream();
            var res = await svc.UploadAttachmentAsync(id, file.FileName, file.ContentType, stream, uploadedBy, ct);
            return res.IsSuccess ? Results.Created($"/api/documents/attachments/{res.Value?.AttachmentId}", res.Value) : Results.BadRequest(res.Error);
        })
        .DisableAntiforgery()
        .WithName("UploadAttachment");

        group.MapGet("/{id}/attachments", async (string id, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.GetDocumentAttachmentsAsync(id, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetDocumentAttachments");

        group.MapGet("/attachments/{attachmentId}/download", async (string attachmentId, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.DownloadAttachmentAsync(attachmentId, ct);
            if (res.IsFailure || res.Value.Content is null)
                return Results.NotFound(res.Error);

            var (meta, stream) = res.Value;
            return Results.File(stream, meta.ContentType, meta.FileName);
        })
        .WithName("DownloadAttachment");

        // -------------------------------------------------------------
        // Messaggi e Note Istruttorie (DocumentiMessaggi)
        // -------------------------------------------------------------
        group.MapPost("/{id}/messages", async (string id, AddDocumentMessageRequest req, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.AddMessageAsync(id, req.SenderUser, req.Text, ct);
            return res.IsSuccess ? Results.Created($"/api/documents/{id}/messages/{res.Value?.MessageId}", res.Value) : Results.BadRequest(res.Error);
        })
        .WithName("AddDocumentMessage");

        group.MapGet("/{id}/messages", async (string id, IDocumentService svc, CancellationToken ct) =>
        {
            var res = await svc.GetMessagesAsync(id, ct);
            return res.IsSuccess ? Results.Ok(res.Value) : Results.NotFound(res.Error);
        })
        .WithName("GetDocumentMessages");

        return app;
    }
}
