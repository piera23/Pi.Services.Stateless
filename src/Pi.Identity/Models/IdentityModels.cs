namespace Pi.Identity.Models;

public class UserAccount
{
    public required string Matricola { get; init; }
    public required string CodiceFiscale { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Email { get; init; }
    public string? DepartmentCode { get; init; }
    public string? DepartmentName { get; init; }
    public string? ManagerMatricola { get; init; }
    public List<string> Roles { get; init; } = new();
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}

public class RoleDefinition
{
    public required string RoleCode { get; init; }
    public required string Description { get; init; }
}
