namespace Pi.FormGenerator.Models;

public enum DatabaseDialect
{
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite
}

public class DatabaseSchemaResult
{
    public required string TableName { get; init; }
    public required DatabaseDialect Dialect { get; init; }
    public required string CreateTableScript { get; init; }
    public required string SampleInsertScript { get; init; }
}
