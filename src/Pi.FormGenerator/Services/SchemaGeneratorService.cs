using System.Text;
using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Services;

public class SchemaGeneratorService : ISchemaGeneratorService
{
    public DatabaseSchemaResult GenerateSchema(FormDefinition form, DatabaseDialect dialect)
    {
        var tableName = SanitizeIdentifier(form.FormCode.ToLowerInvariant());
        var sb = new StringBuilder();

        sb.AppendLine($"-- ================================================================");
        sb.AppendLine($"-- Tabella generata da Pi.FormGenerator Low-Code Engine per: {form.Title}");
        sb.AppendLine($"-- Dialetto Target: {dialect}");
        sb.AppendLine($"-- Data Generazione: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- ================================================================");
        sb.AppendLine();

        sb.AppendLine($"CREATE TABLE {FormatIdentifier(tableName, dialect)} (");

        // Primary key e colonne audit standard
        switch (dialect)
        {
            case DatabaseDialect.PostgreSql:
                sb.AppendLine("    id VARCHAR(64) PRIMARY KEY,");
                sb.AppendLine("    submitted_by VARCHAR(128) NOT NULL,");
                sb.AppendLine("    submitted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,");
                break;
            case DatabaseDialect.SqlServer:
                sb.AppendLine("    [id] NVARCHAR(64) PRIMARY KEY,");
                sb.AppendLine("    [submitted_by] NVARCHAR(128) NOT NULL,");
                sb.AppendLine("    [submitted_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),");
                break;
            case DatabaseDialect.MySql:
                sb.AppendLine("    `id` VARCHAR(64) PRIMARY KEY,");
                sb.AppendLine("    `submitted_by` VARCHAR(128) NOT NULL,");
                sb.AppendLine("    `submitted_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,");
                break;
            case DatabaseDialect.Sqlite:
                sb.AppendLine("    \"id\" TEXT PRIMARY KEY,");
                sb.AppendLine("    \"submitted_by\" TEXT NOT NULL,");
                sb.AppendLine("    \"submitted_at\" TEXT NOT NULL,");
                break;
        }

        // Filtra campi non memorizzabili (solo layout visivo o verifica interattiva client)
        var persistentFields = form.AllFields
            .Where(f => f.Type != FieldType.SectionHeader && 
                        f.Type != FieldType.InfoAlert && 
                        f.Type != FieldType.Divider && 
                        f.Type != FieldType.ProgressStep && 
                        f.Type != FieldType.Captcha)
            .ToList();

        for (int i = 0; i < persistentFields.Count; i++)
        {
            var field = persistentFields[i];
            var colName = SanitizeIdentifier(field.Key);
            var colType = MapDataType(field.Type, field.Validation, dialect);
            var nullable = field.Validation.IsRequired ? "NOT NULL" : "NULL";
            var isLast = (i == persistentFields.Count - 1);

            var colDef = $"    {FormatIdentifier(colName, dialect)} {colType} {nullable}{(isLast ? "" : ",")}";
            sb.AppendLine(colDef);
        }

        sb.AppendLine(");");

        // Esempio query insert
        var sampleValues = persistentFields.ToDictionary(f => f.Key, f => (object?)GetSampleValue(f));
        var sampleInsert = GenerateInsertQuery(form, sampleValues, dialect);

        return new DatabaseSchemaResult
        {
            TableName = tableName,
            Dialect = dialect,
            CreateTableScript = sb.ToString(),
            SampleInsertScript = sampleInsert
        };
    }

    public string GenerateInsertQuery(FormDefinition form, Dictionary<string, object?> values, DatabaseDialect dialect)
    {
        var tableName = SanitizeIdentifier(form.FormCode.ToLowerInvariant());
        var cols = new List<string> { FormatIdentifier("id", dialect), FormatIdentifier("submitted_by", dialect), FormatIdentifier("submitted_at", dialect) };
        var vals = new List<string> { "'sub_demo_001'", "'user_123'", dialect == DatabaseDialect.SqlServer ? "SYSUTCDATETIME()" : "CURRENT_TIMESTAMP" };

        var persistentFields = form.AllFields
            .Where(f => f.Type != FieldType.SectionHeader && 
                        f.Type != FieldType.InfoAlert && 
                        f.Type != FieldType.Divider && 
                        f.Type != FieldType.ProgressStep && 
                        f.Type != FieldType.Captcha);

        foreach (var field in persistentFields)
        {
            cols.Add(FormatIdentifier(SanitizeIdentifier(field.Key), dialect));
            values.TryGetValue(field.Key, out var rawVal);
            vals.Add(FormatSqlValue(rawVal, field.Type, dialect));
        }

        return $"INSERT INTO {FormatIdentifier(tableName, dialect)} ({string.Join(", ", cols)})\nVALUES ({string.Join(", ", vals)});";
    }

    private static string MapDataType(FieldType type, FieldValidation val, DatabaseDialect dialect) => (type, dialect) switch
    {
        // Numeri Interi
        (FieldType.Number or FieldType.Slider or FieldType.Rating, DatabaseDialect.PostgreSql) => "BIGINT",
        (FieldType.Number or FieldType.Slider or FieldType.Rating, DatabaseDialect.SqlServer) => "BIGINT",
        (FieldType.Number or FieldType.Slider or FieldType.Rating, DatabaseDialect.MySql) => "BIGINT",
        (FieldType.Number or FieldType.Slider or FieldType.Rating, DatabaseDialect.Sqlite) => "INTEGER",

        // Metriche & Score (Likert / NPS)
        (FieldType.LikertScale or FieldType.Nps, DatabaseDialect.PostgreSql) => "SMALLINT",
        (FieldType.LikertScale or FieldType.Nps, DatabaseDialect.SqlServer) => "TINYINT",
        (FieldType.LikertScale or FieldType.Nps, DatabaseDialect.MySql) => "TINYINT",
        (FieldType.LikertScale or FieldType.Nps, DatabaseDialect.Sqlite) => "INTEGER",

        // Decimali / Valuta / Percentuale
        (FieldType.Decimal or FieldType.Percentage or FieldType.Currency, DatabaseDialect.PostgreSql) => "NUMERIC(18,2)",
        (FieldType.Decimal or FieldType.Percentage or FieldType.Currency, DatabaseDialect.SqlServer) => "DECIMAL(18,2)",
        (FieldType.Decimal or FieldType.Percentage or FieldType.Currency, DatabaseDialect.MySql) => "DECIMAL(18,2)",
        (FieldType.Decimal or FieldType.Percentage or FieldType.Currency, DatabaseDialect.Sqlite) => "REAL",

        // Date & DateTime
        (FieldType.Date, DatabaseDialect.PostgreSql) => "DATE",
        (FieldType.Date, DatabaseDialect.SqlServer) => "DATE",
        (FieldType.Date, DatabaseDialect.MySql) => "DATE",
        (FieldType.Date, DatabaseDialect.Sqlite) => "TEXT",

        (FieldType.Time, DatabaseDialect.PostgreSql) => "TIME",
        (FieldType.Time, DatabaseDialect.SqlServer) => "TIME(7)",
        (FieldType.Time, DatabaseDialect.MySql) => "TIME",
        (FieldType.Time, DatabaseDialect.Sqlite) => "TEXT",

        (FieldType.DateTime, DatabaseDialect.PostgreSql) => "TIMESTAMPTZ",
        (FieldType.DateTime, DatabaseDialect.SqlServer) => "DATETIME2",
        (FieldType.DateTime, DatabaseDialect.MySql) => "DATETIME",
        (FieldType.DateTime, DatabaseDialect.Sqlite) => "TEXT",

        // Booleani & Switch
        (FieldType.Checkbox or FieldType.Switch, DatabaseDialect.PostgreSql) => "BOOLEAN",
        (FieldType.Checkbox or FieldType.Switch, DatabaseDialect.SqlServer) => "BIT",
        (FieldType.Checkbox or FieldType.Switch, DatabaseDialect.MySql) => "BOOLEAN",
        (FieldType.Checkbox or FieldType.Switch, DatabaseDialect.Sqlite) => "INTEGER",

        // Array / JSON / Liste di Scelte / Griglie / Sotto-Form / Strutture Complesse
        (FieldType.MultiSelect or FieldType.CheckboxGroup or FieldType.TagInput or FieldType.DateRange or FieldType.CascadingSelect or FieldType.DataGrid or FieldType.RepeatableSection or FieldType.JsonEditor or FieldType.Location, DatabaseDialect.PostgreSql) => "JSONB",
        (FieldType.MultiSelect or FieldType.CheckboxGroup or FieldType.TagInput or FieldType.DateRange or FieldType.CascadingSelect or FieldType.DataGrid or FieldType.RepeatableSection or FieldType.JsonEditor or FieldType.Location, DatabaseDialect.SqlServer) => "NVARCHAR(MAX)",
        (FieldType.MultiSelect or FieldType.CheckboxGroup or FieldType.TagInput or FieldType.DateRange or FieldType.CascadingSelect or FieldType.DataGrid or FieldType.RepeatableSection or FieldType.JsonEditor or FieldType.Location, DatabaseDialect.MySql) => "JSON",
        (FieldType.MultiSelect or FieldType.CheckboxGroup or FieldType.TagInput or FieldType.DateRange or FieldType.CascadingSelect or FieldType.DataGrid or FieldType.RepeatableSection or FieldType.JsonEditor or FieldType.Location, DatabaseDialect.Sqlite) => "TEXT",

        // Testo Lungo / RichText / Firma Grafica / Markdown / Audio
        (FieldType.TextArea or FieldType.RichText or FieldType.Signature or FieldType.Markdown or FieldType.AudioRecorder, DatabaseDialect.PostgreSql) => "TEXT",
        (FieldType.TextArea or FieldType.RichText or FieldType.Signature or FieldType.Markdown or FieldType.AudioRecorder, DatabaseDialect.SqlServer) => "NVARCHAR(MAX)",
        (FieldType.TextArea or FieldType.RichText or FieldType.Signature or FieldType.Markdown or FieldType.AudioRecorder, DatabaseDialect.MySql) => "TEXT",
        (FieldType.TextArea or FieldType.RichText or FieldType.Signature or FieldType.Markdown or FieldType.AudioRecorder, DatabaseDialect.Sqlite) => "TEXT",

        // Codice Fiscale & IBAN
        (FieldType.FiscalCode, DatabaseDialect.PostgreSql) => "VARCHAR(16)",
        (FieldType.FiscalCode, DatabaseDialect.SqlServer) => "NVARCHAR(16)",
        (FieldType.FiscalCode, DatabaseDialect.MySql) => "VARCHAR(16)",
        (FieldType.FiscalCode, DatabaseDialect.Sqlite) => "TEXT",

        (FieldType.IBAN, DatabaseDialect.PostgreSql) => "VARCHAR(34)",
        (FieldType.IBAN, DatabaseDialect.SqlServer) => "NVARCHAR(34)",
        (FieldType.IBAN, DatabaseDialect.MySql) => "VARCHAR(34)",
        (FieldType.IBAN, DatabaseDialect.Sqlite) => "TEXT",

        (FieldType.Otp, DatabaseDialect.PostgreSql) => "VARCHAR(8)",
        (FieldType.Otp, DatabaseDialect.SqlServer) => "NVARCHAR(8)",
        (FieldType.Otp, DatabaseDialect.MySql) => "VARCHAR(8)",
        (FieldType.Otp, DatabaseDialect.Sqlite) => "TEXT",

        (FieldType.BarcodeQrCode, DatabaseDialect.PostgreSql) => "VARCHAR(128)",
        (FieldType.BarcodeQrCode, DatabaseDialect.SqlServer) => "NVARCHAR(128)",
        (FieldType.BarcodeQrCode, DatabaseDialect.MySql) => "VARCHAR(128)",
        (FieldType.BarcodeQrCode, DatabaseDialect.Sqlite) => "TEXT",

        // Colori
        (FieldType.ColorPicker, DatabaseDialect.PostgreSql) => "VARCHAR(16)",
        (FieldType.ColorPicker, DatabaseDialect.SqlServer) => "NVARCHAR(16)",
        (FieldType.ColorPicker, DatabaseDialect.MySql) => "VARCHAR(16)",
        (FieldType.ColorPicker, DatabaseDialect.Sqlite) => "TEXT",

        // Email, Url, Phone
        (FieldType.Email or FieldType.Url, DatabaseDialect.PostgreSql) => "VARCHAR(320)",
        (FieldType.Email or FieldType.Url, DatabaseDialect.SqlServer) => "NVARCHAR(320)",
        (FieldType.Email or FieldType.Url, DatabaseDialect.MySql) => "VARCHAR(320)",
        (FieldType.Email or FieldType.Url, DatabaseDialect.Sqlite) => "TEXT",

        _ => dialect switch
        {
            DatabaseDialect.PostgreSql => $"VARCHAR({val.MaxLength ?? 255})",
            DatabaseDialect.SqlServer => $"NVARCHAR({val.MaxLength ?? 255})",
            DatabaseDialect.MySql => $"VARCHAR({val.MaxLength ?? 255})",
            DatabaseDialect.Sqlite => "TEXT",
            _ => "VARCHAR(255)"
        }
    };

    private static string FormatIdentifier(string name, DatabaseDialect dialect) => dialect switch
    {
        DatabaseDialect.SqlServer => $"[{name}]",
        DatabaseDialect.MySql => $"`{name}`",
        DatabaseDialect.PostgreSql => $"\"{name}\"",
        DatabaseDialect.Sqlite => $"\"{name}\"",
        _ => name
    };

    private static string SanitizeIdentifier(string input)
    {
        var clean = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "campo" : clean.ToLowerInvariant();
    }

    private static string FormatSqlValue(object? val, FieldType type, DatabaseDialect dialect)
    {
        if (val is null) return "NULL";
        var str = val.ToString()?.Replace("'", "''");

        if (type == FieldType.Number || type == FieldType.Decimal || type == FieldType.Slider || type == FieldType.Rating || type == FieldType.Percentage || type == FieldType.Currency || type == FieldType.Nps || type == FieldType.LikertScale)
            return string.IsNullOrWhiteSpace(str) ? "0" : str;

        if (type == FieldType.Checkbox || type == FieldType.Switch)
            return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) ? (dialect == DatabaseDialect.PostgreSql || dialect == DatabaseDialect.MySql ? "TRUE" : "1") : (dialect == DatabaseDialect.PostgreSql || dialect == DatabaseDialect.MySql ? "FALSE" : "0");

        return $"'{str}'";
    }

    private static object GetSampleValue(FieldDefinition f) => f.Type switch
    {
        FieldType.Number => 42,
        FieldType.Decimal => 1500.50m,
        FieldType.Currency => 2450.00m,
        FieldType.Percentage => 75.0m,
        FieldType.Slider => 80,
        FieldType.Rating => 5,
        FieldType.Nps => 9,
        FieldType.LikertScale => 5,
        FieldType.FiscalCode => "RSSMRA85M01H501Z",
        FieldType.IBAN => "IT60X0542811101000000123456",
        FieldType.Otp => "492817",
        FieldType.BarcodeQrCode => "QR-PI-STATELSS-2026",
        FieldType.Date => DateTime.UtcNow.ToString("yyyy-MM-dd"),
        FieldType.Time => "14:30:00",
        FieldType.DateTime => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        FieldType.Checkbox or FieldType.Switch => true,
        FieldType.Email => "utente@platform.local",
        FieldType.Phone => "+39 071 1234567",
        FieldType.Url => "https://pi-platform.local",
        FieldType.ColorPicker => "#38bdf8",
        FieldType.Select => f.Options.FirstOrDefault()?.Value ?? "opzione_1",
        FieldType.MultiSelect or FieldType.CheckboxGroup or FieldType.TagInput => "[\"tag1\", \"tag2\"]",
        FieldType.CascadingSelect => "{\"settore\":\"IT\", \"sub\":\"Cloud Architecture\"}",
        FieldType.Location => "{\"via\":\"Corso Garibaldi 12\", \"citta\":\"Ancona\", \"cap\":\"60121\", \"lat\":43.6158, \"lng\":13.5189}",
        FieldType.JsonEditor => "{\"environment\":\"production\", \"debug\":false, \"timeout\":30}",
        FieldType.DataGrid => "[{\"prodotto\":\"Licenza Server\", \"quantita\":2, \"prezzo\":1200}]",
        FieldType.RepeatableSection => "[{\"nome\":\"Marco Rossi\", \"relazione\":\"Coniuge\", \"eta\":38}]",
        FieldType.Markdown => "# Specifiche del Progetto\n- Architettura 100% Stateless\n- Zero DB coupling",
        FieldType.AudioRecorder => "data:audio/mp3;base64,//uQx...",
        FieldType.Signature => "data:image/png;base64,iVBORw0KGgo...",
        _ => $"Valore di prova per {f.Label}"
    };
}
