using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Services;

public interface ISchemaGeneratorService
{
    DatabaseSchemaResult GenerateSchema(FormDefinition form, DatabaseDialect dialect);
    string GenerateInsertQuery(FormDefinition form, Dictionary<string, object?> values, DatabaseDialect dialect);
}
