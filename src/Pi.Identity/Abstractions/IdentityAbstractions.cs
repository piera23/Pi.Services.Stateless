using Pi.Common.Models;
using Pi.Identity.Models;

namespace Pi.Identity.Abstractions;

public interface IUserStore
{
    Task<UserAccount?> GetByMatricolaAsync(string matricola, CancellationToken ct = default);
    Task<UserAccount?> GetByFiscalCodeAsync(string fiscalCode, CancellationToken ct = default);
    Task<IReadOnlyList<UserAccount>> GetUsersAsync(string? role = null, string? department = null, CancellationToken ct = default);
    Task SaveUserAsync(UserAccount user, CancellationToken ct = default);
}

public interface IActorResolver
{
    /// <summary>
    /// Risolve dinamicamente la matricola dell'attore abilitato ad eseguire un passo di workflow,
    /// ad esempio individuando il superiore gerarchico dell'autore se il ruolo richiesto è 'Dirigente'.
    /// </summary>
    Task<Result<string>> ResolveStepActorAsync(string stepRequiredRole, string applicantMatricola, CancellationToken ct = default);
}

public interface IIdentityService
{
    Task<Result<UserAccount>> GetUserByMatricolaAsync(string matricola, CancellationToken ct = default);
    Task<Result<UserAccount>> GetUserByFiscalCodeAsync(string cf, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserAccount>>> GetUsersAsync(string? role = null, string? department = null, CancellationToken ct = default);
    Task<Result<UserAccount>> UpsertUserAsync(UserAccount user, CancellationToken ct = default);
}
