using System.Collections.Concurrent;
using Pi.Documents.Abstractions;
using Pi.Documents.Models;

namespace Pi.Documents.Storage.InMemory;

public class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<string, DocumentTypeCode> _typeCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DocumentInstance> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DocumentAttachment> _attachments = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<DocumentMessage> _messages = new();

    public Task<DocumentTypeCode?> GetTypeCodeAsync(string code, CancellationToken ct = default)
    {
        _typeCodes.TryGetValue(code, out var typeCode);
        return Task.FromResult(typeCode);
    }

    public Task<IReadOnlyList<DocumentTypeCode>> GetAllTypeCodesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<DocumentTypeCode> list = _typeCodes.Values.ToList();
        return Task.FromResult(list);
    }

    public Task SaveTypeCodeAsync(DocumentTypeCode typeCode, CancellationToken ct = default)
    {
        _typeCodes[typeCode.Code] = typeCode;
        return Task.CompletedTask;
    }

    public Task<DocumentInstance?> GetDocumentAsync(string documentId, CancellationToken ct = default)
    {
        _documents.TryGetValue(documentId, out var doc);
        return Task.FromResult(doc);
    }

    public Task<IReadOnlyList<DocumentInstance>> GetDocumentsByAuthorAsync(string authorUser, CancellationToken ct = default)
    {
        IReadOnlyList<DocumentInstance> list = _documents.Values
            .Where(d => string.Equals(d.AuthorUser, authorUser, StringComparison.OrdinalIgnoreCase) && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<DocumentInstance>> SearchDocumentsAsync(string? typeCode, bool? isCompleted, CancellationToken ct = default)
    {
        var query = _documents.Values.Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(typeCode))
            query = query.Where(d => string.Equals(d.DocumentTypeCode, typeCode, StringComparison.OrdinalIgnoreCase));

        if (isCompleted.HasValue)
            query = query.Where(d => d.IsCompleted == isCompleted.Value);

        IReadOnlyList<DocumentInstance> list = query.OrderByDescending(d => d.CreatedAt).ToList();
        return Task.FromResult(list);
    }

    public Task SaveDocumentAsync(DocumentInstance document, CancellationToken ct = default)
    {
        _documents[document.DocumentId] = document;
        return Task.CompletedTask;
    }

    public Task<DocumentAttachment?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default)
    {
        _attachments.TryGetValue(attachmentId, out var att);
        return Task.FromResult(att);
    }

    public Task<IReadOnlyList<DocumentAttachment>> GetAttachmentsByDocumentAsync(string documentId, CancellationToken ct = default)
    {
        IReadOnlyList<DocumentAttachment> list = _attachments.Values
            .Where(a => string.Equals(a.DocumentId, documentId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.UploadedAt)
            .ToList();
        return Task.FromResult(list);
    }

    public Task SaveAttachmentMetadataAsync(DocumentAttachment attachment, CancellationToken ct = default)
    {
        _attachments[attachment.AttachmentId] = attachment;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentMessage>> GetMessagesAsync(string documentId, CancellationToken ct = default)
    {
        IReadOnlyList<DocumentMessage> list = _messages
            .Where(m => string.Equals(m.DocumentId, documentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.SentAt)
            .ToList();
        return Task.FromResult(list);
    }

    public Task SaveMessageAsync(DocumentMessage message, CancellationToken ct = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}

public class InMemoryFileStore : IFileStore
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

    public async Task SaveFileAsync(string fileKey, Stream content, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        _files[fileKey] = ms.ToArray();
    }

    public Task<Stream?> GetFileAsync(string fileKey, CancellationToken ct = default)
    {
        if (_files.TryGetValue(fileKey, out var bytes))
        {
            Stream s = new MemoryStream(bytes);
            return Task.FromResult<Stream?>(s);
        }
        return Task.FromResult<Stream?>(null);
    }

    public Task DeleteFileAsync(string fileKey, CancellationToken ct = default)
    {
        _files.TryRemove(fileKey, out _);
        return Task.CompletedTask;
    }
}
