using Pi.Common.Models;
using Pi.Identity.Abstractions;
using Pi.Identity.Models;

namespace Pi.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly IUserStore _store;

    public IdentityService(IUserStore store)
    {
        _store = store;
    }

    public async Task<Result<UserAccount>> GetUserByMatricolaAsync(string matricola, CancellationToken ct = default)
    {
        var u = await _store.GetByMatricolaAsync(matricola, ct);
        if (u is null || !u.IsActive)
            return Result<UserAccount>.Failure($"Utente con matricola '{matricola}' non trovato o disabilitato.");

        return Result<UserAccount>.Success(u);
    }

    public async Task<Result<UserAccount>> GetUserByFiscalCodeAsync(string cf, CancellationToken ct = default)
    {
        var u = await _store.GetByFiscalCodeAsync(cf, ct);
        if (u is null || !u.IsActive)
            return Result<UserAccount>.Failure($"Utente con CF '{cf}' non trovato o disabilitato.");

        return Result<UserAccount>.Success(u);
    }

    public async Task<Result<IReadOnlyList<UserAccount>>> GetUsersAsync(string? role = null, string? department = null, CancellationToken ct = default)
    {
        var list = await _store.GetUsersAsync(role, department, ct);
        return Result<IReadOnlyList<UserAccount>>.Success(list);
    }

    public async Task<Result<UserAccount>> UpsertUserAsync(UserAccount user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Matricola))
            return Result<UserAccount>.Failure("Matricola obbligatoria.");
        if (string.IsNullOrWhiteSpace(user.CodiceFiscale))
            return Result<UserAccount>.Failure("Codice fiscale obbligatorio.");

        await _store.SaveUserAsync(user, ct);
        return Result<UserAccount>.Success(user);
    }
}

public class ActorResolver : IActorResolver
{
    private readonly IUserStore _store;

    public ActorResolver(IUserStore store)
    {
        _store = store;
    }

    public async Task<Result<string>> ResolveStepActorAsync(string stepRequiredRole, string applicantMatricola, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stepRequiredRole))
            return Result<string>.Failure("Ruolo richiesto per il passo non specificato.");

        // Se il ruolo richiesto è Dirigente / Superiore, cerchiamo il Manager del richiedente
        if (string.Equals(stepRequiredRole, "Dirigente", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stepRequiredRole, "Responsabile", StringComparison.OrdinalIgnoreCase))
        {
            var applicant = await _store.GetByMatricolaAsync(applicantMatricola, ct);
            if (applicant is not null && !string.IsNullOrWhiteSpace(applicant.ManagerMatricola))
            {
                return Result<string>.Success(applicant.ManagerMatricola);
            }
        }

        // Altrimenti cerchiamo il primo utente attivo con quel ruolo
        var usersWithRole = await _store.GetUsersAsync(role: stepRequiredRole, ct: ct);
        var actor = usersWithRole.FirstOrDefault(u => u.IsActive);
        if (actor is not null)
        {
            return Result<string>.Success(actor.Matricola);
        }

        return Result<string>.Failure($"Nessun attore trovato per il ruolo '{stepRequiredRole}'.");
    }
}
