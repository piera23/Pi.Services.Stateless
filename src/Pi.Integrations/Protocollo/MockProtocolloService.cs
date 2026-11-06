using Pi.Common.Models;

namespace Pi.Integrations.Protocollo;

public class MockProtocolloService : IProtocolloService
{
    private static int _counter = 1000;

    public Task<Result<ProtocolloEsito>> ProtocollaAsync(ProtocolloRichiesta richiesta, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Oggetto))
            return Task.FromResult(Result<ProtocolloEsito>.Failure("Oggetto della richiesta di protocollo obbligatorio."));

        var seq = Interlocked.Increment(ref _counter);
        var anno = DateTime.UtcNow.Year;
        var numProt = $"PROT-{anno}-{seq:D7}";
        var segnatura = $"ORG-ID/{anno}/{seq:D7}";

        var esito = new ProtocolloEsito
        {
            NumeroProtocollo = numProt,
            Segnatura = segnatura,
            DataProtocollo = DateTime.UtcNow,
            Note = $"Protocollato con successo tramite protocollo gateway agnostico per '{richiesta.CognomeAutore} {richiesta.NomeAutore}'."
        };

        return Task.FromResult(Result<ProtocolloEsito>.Success(esito));
    }
}
