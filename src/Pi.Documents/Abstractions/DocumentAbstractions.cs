using Pi.Documents.Models;

namespace Pi.Documents.Abstractions;

public interface IDocumentStore
{
    Task<DocumentTypeCode?> GetTypeCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentTypeCode>> GetAllTypeCodesAsync(CancellationToken ct = default);
    Task SaveTypeCodeAsync(DocumentTypeCode typeCode, CancellationToken ct = default);

    Task<DocumentInstance?> GetDocumentAsync(string documentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentInstance>> GetDocumentsByAuthorAsync(string authorUser, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentInstance>> SearchDocumentsAsync(string? typeCode, bool? isCompleted, CancellationToken ct = default);
    Task SaveDocumentAsync(DocumentInstance document, CancellationToken ct = default);

    Task<DocumentAttachment?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentAttachment>> GetAttachmentsByDocumentAsync(string documentId, CancellationToken ct = default);
    Task SaveAttachmentMetadataAsync(DocumentAttachment attachment, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentMessage>> GetMessagesAsync(string documentId, CancellationToken ct = default);
    Task SaveMessageAsync(DocumentMessage message, CancellationToken ct = default);
}

public interface IFileStore
{
    Task SaveFileAsync(string fileKey, Stream content, CancellationToken ct = default);
    Task<Stream?> GetFileAsync(string fileKey, CancellationToken ct = default);
    Task DeleteFileAsync(string fileKey, CancellationToken ct = default);
}
