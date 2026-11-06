using System.Text;
using Pi.Common.Models;

namespace Pi.Integrations.Reporting;

public enum ReportFormat
{
    Pdf,
    Excel,
    Csv,
    Json
}

public class ReportRequest
{
    public required string ReportName { get; init; }
    public ReportFormat Format { get; init; } = ReportFormat.Pdf;
    public Dictionary<string, string> Parameters { get; init; } = new();
    public object? DataSource { get; init; }
}

public class ReportResult
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}

public interface IReportGenerator
{
    Task<Result<ReportResult>> GenerateReportAsync(ReportRequest request, CancellationToken ct = default);
}

public class DocumentReportGenerator : IReportGenerator
{
    public Task<Result<ReportResult>> GenerateReportAsync(ReportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReportName))
            return Task.FromResult(Result<ReportResult>.Failure("Nome del report obbligatorio."));

        var sb = new StringBuilder();
        sb.AppendLine($"REPORT GENERATO: {request.ReportName}");
        sb.AppendLine($"Data Elaborazione: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("PARAMETRI:");
        foreach (var p in request.Parameters)
        {
            sb.AppendLine($"  {p.Key}: {p.Value}");
        }
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("STATO: Esecuzione stateless completata con successo.");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        string mimeType = request.Format switch
        {
            ReportFormat.Pdf => "application/pdf",
            ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Csv => "text/csv",
            ReportFormat.Json => "application/json",
            _ => "application/octet-stream"
        };

        var ext = request.Format switch
        {
            ReportFormat.Pdf => "pdf",
            ReportFormat.Excel => "xlsx",
            ReportFormat.Csv => "csv",
            ReportFormat.Json => "json",
            _ => "bin"
        };

        var res = new ReportResult
        {
            Content = bytes,
            ContentType = mimeType,
            FileName = $"{request.ReportName}_{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}"
        };

        return Task.FromResult(Result<ReportResult>.Success(res));
    }
}
