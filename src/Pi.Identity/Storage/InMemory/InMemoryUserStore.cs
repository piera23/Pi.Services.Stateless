using System.Collections.Concurrent;
using Pi.Identity.Abstractions;
using Pi.Identity.Models;

namespace Pi.Identity.Storage.InMemory;

public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, UserAccount> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore()
    {
        SeedDemoUsers();
    }

    private void SeedDemoUsers()
    {
        var admin = new UserAccount
        {
            Matricola = "1001",
            CodiceFiscale = "RSSMRA80A01A000A",
            FirstName = "Mario",
            LastName = "Rossi",
            Email = "mario.rossi@platform.local",
            DepartmentCode = "DEP_IT",
            DepartmentName = "Information Technology",
            Roles = new List<string> { "Amministratore", "Dipendente" }
        };

        var dirigente = new UserAccount
        {
            Matricola = "1002",
            CodiceFiscale = "BNCGPP75B02B111B",
            FirstName = "Giuseppe",
            LastName = "Bianchi",
            Email = "giuseppe.bianchi@platform.local",
            DepartmentCode = "DEP_MGMT",
            DepartmentName = "Direzione Generale",
            Roles = new List<string> { "Dirigente" }
        };

        var dipendente = new UserAccount
        {
            Matricola = "1003",
            CodiceFiscale = "VRDLGI90C03C222C",
            FirstName = "Luigi",
            LastName = "Verdi",
            Email = "luigi.verdi@platform.local",
            DepartmentCode = "DEP_MGMT",
            DepartmentName = "Direzione Generale",
            ManagerMatricola = "1002",
            Roles = new List<string> { "Dipendente" }
        };

        _users[admin.Matricola] = admin;
        _users[dirigente.Matricola] = dirigente;
        _users[dipendente.Matricola] = dipendente;
    }

    public Task<UserAccount?> GetByMatricolaAsync(string matricola, CancellationToken ct = default)
    {
        _users.TryGetValue(matricola, out var user);
        return Task.FromResult(user);
    }

    public Task<UserAccount?> GetByFiscalCodeAsync(string fiscalCode, CancellationToken ct = default)
    {
        var user = _users.Values.FirstOrDefault(u => string.Equals(u.CodiceFiscale, fiscalCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<UserAccount>> GetUsersAsync(string? role = null, string? department = null, CancellationToken ct = default)
    {
        var query = _users.Values.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(u => string.Equals(u.DepartmentCode, department, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<UserAccount> list = query.ToList();
        return Task.FromResult(list);
    }

    public Task SaveUserAsync(UserAccount user, CancellationToken ct = default)
    {
        _users[user.Matricola] = user;
        return Task.CompletedTask;
    }
}
