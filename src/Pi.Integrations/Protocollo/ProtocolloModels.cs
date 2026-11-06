using Pi.Common.Models;

namespace Pi.Integrations.Protocollo;

public record ProtocolloAllegato(string FileName, byte[] Content);

public class ProtocolloRichiesta
{
    public required string DocumentId { get; init; }
    public required string DocumentTypeCode { get; init; }
    public required string Oggetto { get; init; }
    public required string CodiceFiscaleAutore { get; init; }
    public required string CognomeAutore { get; init; }
    public required string NomeAutore { get; init; }
    public string? CodiceStruttura { get; init; }
    public byte[]? DocumentoPrincipalePdf { get; init; }
    public IReadOnlyList<ProtocolloAllegato> Allegati { get; init; } = Array.Empty<ProtocolloAllegato>();
}

public class ProtocolloEsito
{
    public required string NumeroProtocollo { get; init; }
    public required string Segnatura { get; init; }
    public DateTime DataProtocollo { get; init; } = DateTime.UtcNow;
    public string? Note { get; init; }
}

public interface IProtocolloService
{
    Task<Result<ProtocolloEsito>> ProtocollaAsync(ProtocolloRichiesta richiesta, CancellationToken ct = default);
}
