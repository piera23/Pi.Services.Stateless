using System.Security.Cryptography;
using Pi.Common.Models;
using Pi.Documents.Abstractions;
using Pi.Documents.Models;

namespace Pi.Documents.Services;

public interface IDocumentService
{
    Task<Result<DocumentInstance>> CreateDocumentAsync(
        string typeCode,
        string authorUser,
        string subject,
        string? notes = null,
        string? referencedDocId = null,
        CancellationToken ct = default);

    Task<Result<DocumentInstance>> GetDocumentAsync(string documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentInstance>>> GetDocumentsByAuthorAsync(string authorUser, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentInstance>>> SearchDocumentsAsync(string? typeCode, bool? isCompleted, CancellationToken ct = default);
    Task<Result> UpdateDocumentStateAsync(string documentId, bool isCompleted, bool isRejected, string? rejectionReason = null, CancellationToken ct = default);
    Task<Result> SetProtocolDataAsync(string documentId, string protocolNumber, DateTime protocolDate, CancellationToken ct = default);

    Task<Result<DocumentAttachment>> UploadAttachmentAsync(
        string documentId,
        string fileName,
        string contentType,
        Stream contentStream,
        string uploadedBy,
        CancellationToken ct = default);

    Task<Result<(DocumentAttachment Metadata, Stream Content)>> DownloadAttachmentAsync(string attachmentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentAttachment>>> GetDocumentAttachmentsAsync(string documentId, CancellationToken ct = default);

    Task<Result<DocumentMessage>> AddMessageAsync(string documentId, string senderUser, string text, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentMessage>>> GetMessagesAsync(string documentId, CancellationToken ct = default);
}

public class DocumentService : IDocumentService
{
    private readonly IDocumentStore _documentStore;
    private readonly IFileStore _fileStore;

    public DocumentService(IDocumentStore documentStore, IFileStore fileStore)
    {
        _documentStore = documentStore;
        _fileStore = fileStore;
    }

    public async Task<Result<DocumentInstance>> CreateDocumentAsync(
        string typeCode,
        string authorUser,
        string subject,
        string? notes = null,
        string? referencedDocId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
            return Result<DocumentInstance>.Failure("Codice tipo documento obbligatorio.");
        if (string.IsNullOrWhiteSpace(authorUser))
            return Result<DocumentInstance>.Failure("Autore documento obbligatorio.");
        if (string.IsNullOrWhiteSpace(subject))
            return Result<DocumentInstance>.Failure("Oggetto documento obbligatorio.");

        var doc = new DocumentInstance
        {
            DocumentTypeCode = typeCode,
            AuthorUser = authorUser,
            Subject = subject,
            Notes = notes,
            ReferencedDocumentId = referencedDocId,
            CreatedAt = DateTime.UtcNow
        };

        await _documentStore.SaveDocumentAsync(doc, ct);
        return Result<DocumentInstance>.Success(doc);
    }

    public async Task<Result<DocumentInstance>> GetDocumentAsync(string documentId, CancellationToken ct = default)
    {
        var doc = await _documentStore.GetDocumentAsync(documentId, ct);
        if (doc is null || doc.IsDeleted)
            return Result<DocumentInstance>.Failure($"Documento '{documentId}' non trovato.");

        return Result<DocumentInstance>.Success(doc);
    }

    public async Task<Result<IReadOnlyList<DocumentInstance>>> GetDocumentsByAuthorAsync(string authorUser, CancellationToken ct = default)
    {
        var list = await _documentStore.GetDocumentsByAuthorAsync(authorUser, ct);
        return Result<IReadOnlyList<DocumentInstance>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<DocumentInstance>>> SearchDocumentsAsync(string? typeCode, bool? isCompleted, CancellationToken ct = default)
    {
        var list = await _documentStore.SearchDocumentsAsync(typeCode, isCompleted, ct);
        return Result<IReadOnlyList<DocumentInstance>>.Success(list);
    }

    public async Task<Result> UpdateDocumentStateAsync(string documentId, bool isCompleted, bool isRejected, string? rejectionReason = null, CancellationToken ct = default)
    {
        var doc = await _documentStore.GetDocumentAsync(documentId, ct);
        if (doc is null)
            return Result.Failure($"Documento '{documentId}' non trovato.");

        doc.IsCompleted = isCompleted;
        doc.IsRejected = isRejected;
        doc.RejectionReason = rejectionReason;
        if (isCompleted || isRejected)
            doc.CompletedAt = DateTime.UtcNow;

        await _documentStore.SaveDocumentAsync(doc, ct);
        return Result.Success();
    }

    public async Task<Result> SetProtocolDataAsync(string documentId, string protocolNumber, DateTime protocolDate, CancellationToken ct = default)
    {
        var doc = await _documentStore.GetDocumentAsync(documentId, ct);
        if (doc is null)
            return Result.Failure($"Documento '{documentId}' non trovato.");

        doc.ProtocolNumber = protocolNumber;
        doc.ProtocolDate = protocolDate;
        await _documentStore.SaveDocumentAsync(doc, ct);
        return Result.Success();
    }

    public async Task<Result<DocumentAttachment>> UploadAttachmentAsync(
        string documentId,
        string fileName,
        string contentType,
        Stream contentStream,
        string uploadedBy,
        CancellationToken ct = default)
    {
        var doc = await _documentStore.GetDocumentAsync(documentId, ct);
        if (doc is null)
            return Result<DocumentAttachment>.Failure($"Documento '{documentId}' non trovato.");

        using var ms = new MemoryStream();
        await contentStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        string hash;
        using (var sha = SHA256.Create())
        {
            hash = Convert.ToHexString(sha.ComputeHash(bytes));
        }

        var attachment = new DocumentAttachment
        {
            DocumentId = documentId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = bytes.Length,
            Sha256Hash = hash,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        // Salva file e metadati
        ms.Position = 0;
        await _fileStore.SaveFileAsync(attachment.AttachmentId, ms, ct);
        await _documentStore.SaveAttachmentMetadataAsync(attachment, ct);

        return Result<DocumentAttachment>.Success(attachment);
    }

    public async Task<Result<(DocumentAttachment Metadata, Stream Content)>> DownloadAttachmentAsync(string attachmentId, CancellationToken ct = default)
    {
        var meta = await _documentStore.GetAttachmentAsync(attachmentId, ct);
        if (meta is null)
            return Result<(DocumentAttachment, Stream)>.Failure("Allegato non trovato.");

        var stream = await _fileStore.GetFileAsync(attachmentId, ct);
        if (stream is null)
            return Result<(DocumentAttachment, Stream)>.Failure("Contenuto del file non trovato.");

        return Result<(DocumentAttachment, Stream)>.Success((meta, stream));
    }

    public async Task<Result<IReadOnlyList<DocumentAttachment>>> GetDocumentAttachmentsAsync(string documentId, CancellationToken ct = default)
    {
        var list = await _documentStore.GetAttachmentsByDocumentAsync(documentId, ct);
        return Result<IReadOnlyList<DocumentAttachment>>.Success(list);
    }

    public async Task<Result<DocumentMessage>> AddMessageAsync(string documentId, string senderUser, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<DocumentMessage>.Failure("Il testo del messaggio non può essere vuoto.");

        var msg = new DocumentMessage
        {
            DocumentId = documentId,
            SenderUser = senderUser,
            MessageText = text,
            SentAt = DateTime.UtcNow
        };

        await _documentStore.SaveMessageAsync(msg, ct);
        return Result<DocumentMessage>.Success(msg);
    }

    public async Task<Result<IReadOnlyList<DocumentMessage>>> GetMessagesAsync(string documentId, CancellationToken ct = default)
    {
        var list = await _documentStore.GetMessagesAsync(documentId, ct);
        return Result<IReadOnlyList<DocumentMessage>>.Success(list);
    }
}
