namespace Pi.Documents.Models;

public class DocumentTypeCode
{
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? WorkflowCode { get; init; }
    public string? FormCode { get; init; }
    public bool IsProtocolloRequired { get; init; }
    public bool IsActive { get; init; } = true;
}

public class DocumentInstance
{
    public string DocumentId { get; init; } = Guid.NewGuid().ToString("N");
    public required string DocumentTypeCode { get; init; }
    public required string AuthorUser { get; init; }
    public required string Subject { get; init; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReferencedDocumentId { get; set; } // Per rettifiche / sostituzioni (IdDocRif)
    public string? WorkflowInstanceId { get; set; }
    public string? FormSubmissionId { get; set; }
    public string? ProtocolNumber { get; set; }
    public DateTime? ProtocolDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRejected { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class DocumentAttachment
{
    public string AttachmentId { get; init; } = Guid.NewGuid().ToString("N");
    public required string DocumentId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string? Sha256Hash { get; init; }
    public required string UploadedBy { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

public class DocumentMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
    public required string DocumentId { get; init; }
    public required string SenderUser { get; init; }
    public required string MessageText { get; init; }
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}
