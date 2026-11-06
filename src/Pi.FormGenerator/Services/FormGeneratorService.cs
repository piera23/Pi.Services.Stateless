using System.Text.Json;
using System.Text.RegularExpressions;
using Pi.Common.Models;
using Pi.FormGenerator.Abstractions;
using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Services;

public class FormGeneratorService : IFormGeneratorService
{
    private readonly IFormStore _store;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^[\+]?[(]?[0-9]{2,4}[)]?[-\s\.]?[0-9]{3,8}[-\s\.]?[0-9]{3,8}$", RegexOptions.Compiled);
    private static readonly Regex HexColorRegex = new(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
    private static readonly Regex FiscalCodeRegex = new(@"^[A-Za-z0-9]{11,16}$", RegexOptions.Compiled);
    private static readonly Regex IbanRegex = new(@"^[A-Za-z]{2}[0-9]{2}[A-Za-z0-9]{11,30}$", RegexOptions.Compiled);
    private static readonly Regex OtpRegex = new(@"^\d{4,8}$", RegexOptions.Compiled);

    public FormGeneratorService(IFormStore store)
    {
        _store = store;
    }

    public async Task<Result<FormDefinition>> GetFormDefinitionAsync(string formCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(formCode))
            return Result<FormDefinition>.Failure("Il codice del modulo non può essere vuoto.");

        var def = await _store.GetDefinitionAsync(formCode, ct);
        if (def is null)
            return Result<FormDefinition>.Failure($"Nessun modulo trovato con codice '{formCode}'.");

        return Result<FormDefinition>.Success(def);
    }

    public async Task<Result<IReadOnlyList<FormDefinition>>> GetAllFormDefinitionsAsync(CancellationToken ct = default)
    {
        var list = await _store.GetAllDefinitionsAsync(ct);
        return Result<IReadOnlyList<FormDefinition>>.Success(list);
    }

    public async Task<Result<FormDefinition>> CreateOrUpdateFormDefinitionAsync(FormDefinition definition, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(definition.FormCode))
            return Result<FormDefinition>.Failure("Il codice del modulo (FormCode) è obbligatorio.");

        if (string.IsNullOrWhiteSpace(definition.Title))
            return Result<FormDefinition>.Failure("Il titolo del modulo è obbligatorio.");

        await _store.SaveDefinitionAsync(definition, ct);
        return Result<FormDefinition>.Success(definition);
    }

    public async Task<Result> ValidateSubmissionFieldsAsync(string formCode, Dictionary<string, object?> values, CancellationToken ct = default)
    {
        var formResult = await GetFormDefinitionAsync(formCode, ct);
        if (formResult.IsFailure || formResult.Value is null)
            return Result.Failure(formResult.Error ?? "Modulo non trovato.");

        var form = formResult.Value;
        var errors = new List<ValidationError>();

        foreach (var field in form.AllFields)
        {
            // Salta elementi di solo layout visivo o controllo client
            if (field.Type == FieldType.SectionHeader || field.Type == FieldType.InfoAlert || field.Type == FieldType.Divider || field.Type == FieldType.ProgressStep || field.Type == FieldType.Captcha)
                continue;

            values.TryGetValue(field.Key, out var rawVal);
            var strVal = rawVal?.ToString()?.Trim();

            // Controllo Obbligatorietà
            if (field.Validation.IsRequired && (rawVal is null || string.IsNullOrWhiteSpace(strVal)))
            {
                errors.Add(new ValidationError(field.Key, field.Validation.CustomErrorMessage ?? $"Il campo '{field.Label}' è obbligatorio."));
                continue;
            }

            if (rawVal is null || string.IsNullOrWhiteSpace(strVal))
                continue;

            // Validazioni specifiche per componente
            switch (field.Type)
            {
                case FieldType.FiscalCode:
                    var cleanFc = strVal.Replace(" ", "").ToUpperInvariant();
                    if (!FiscalCodeRegex.IsMatch(cleanFc))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' non è un codice fiscale / identificativo fiscale valido."));
                    break;

                case FieldType.IBAN:
                    var cleanIban = strVal.Replace(" ", "").ToUpperInvariant();
                    if (!IbanRegex.IsMatch(cleanIban))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' non è un codice IBAN valido."));
                    break;

                case FieldType.Otp:
                    var cleanOtp = strVal.Replace(" ", "").Replace("-", "");
                    if (!OtpRegex.IsMatch(cleanOtp))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un codice OTP valido di 4-8 cifre."));
                    break;

                case FieldType.JsonEditor:
                    try
                    {
                        using var _ = JsonDocument.Parse(strVal);
                    }
                    catch (Exception)
                    {
                        errors.Add(new ValidationError(field.Key, $"Il contenuto di '{field.Label}' non è un JSON valido."));
                    }
                    break;

                case FieldType.Email:
                    if (!EmailRegex.IsMatch(strVal))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un indirizzo email valido."));
                    break;

                case FieldType.Url:
                    if (!Uri.TryCreate(strVal, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un URL valido (http:// o https://)."));
                    break;

                case FieldType.Phone:
                    if (!PhoneRegex.IsMatch(strVal))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un numero telefonico valido."));
                    break;

                case FieldType.ColorPicker:
                    if (!HexColorRegex.IsMatch(strVal))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un codice colore esadecimale valido (#RGB o #RRGGBB)."));
                    break;

                case FieldType.Number:
                case FieldType.Slider:
                case FieldType.Rating:
                    if (!long.TryParse(strVal, out var num))
                    {
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un numero intero valido."));
                    }
                    else
                    {
                        if (field.Validation.MinValue.HasValue && num < field.Validation.MinValue.Value)
                            errors.Add(new ValidationError(field.Key, $"Il valore di '{field.Label}' deve essere >= {field.Validation.MinValue.Value}."));
                        if (field.Validation.MaxValue.HasValue && num > field.Validation.MaxValue.Value)
                            errors.Add(new ValidationError(field.Key, $"Il valore di '{field.Label}' deve essere <= {field.Validation.MaxValue.Value}."));
                    }
                    break;

                case FieldType.Decimal:
                case FieldType.Percentage:
                case FieldType.Currency:
                    if (!decimal.TryParse(strVal.Replace("€", "").Replace("$", "").Replace("£", "").Trim(), out var dec))
                    {
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un valore numerico/monetario valido."));
                    }
                    else
                    {
                        if (field.Validation.MinValue.HasValue && dec < field.Validation.MinValue.Value)
                            errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere >= {field.Validation.MinValue.Value}."));
                        if (field.Validation.MaxValue.HasValue && dec > field.Validation.MaxValue.Value)
                            errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere <= {field.Validation.MaxValue.Value}."));
                    }
                    break;

                case FieldType.Nps:
                    if (!int.TryParse(strVal, out var npsVal) || npsVal < 0 || npsVal > 10)
                        errors.Add(new ValidationError(field.Key, $"Il punteggio NPS di '{field.Label}' deve essere compreso tra 0 e 10."));
                    break;

                case FieldType.LikertScale:
                    if (!int.TryParse(strVal, out var lkVal) || lkVal < 1 || lkVal > 7)
                        errors.Add(new ValidationError(field.Key, $"Il punteggio della scala '{field.Label}' deve essere compreso tra 1 e 7."));
                    break;

                case FieldType.Date:
                case FieldType.DateTime:
                    if (!DateTime.TryParse(strVal, out _))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere una data valida."));
                    break;

                case FieldType.Time:
                    if (!TimeSpan.TryParse(strVal, out _))
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve essere un orario valido (hh:mm)."));
                    break;

                case FieldType.Select:
                case FieldType.Radio:
                    if (field.Options.Count > 0 && !field.Options.Any(o => string.Equals(o.Value, strVal, StringComparison.OrdinalIgnoreCase)))
                        errors.Add(new ValidationError(field.Key, $"Il valore selezionato per '{field.Label}' non è valido tra le opzioni disponibili."));
                    break;

                case FieldType.MultiSelect:
                case FieldType.CheckboxGroup:
                    // Verifica elementi lista
                    var selectedValues = TryParseArray(rawVal);
                    if (selectedValues.Count > 0 && field.Options.Count > 0)
                    {
                        var invalidOptions = selectedValues.Where(v => !field.Options.Any(o => string.Equals(o.Value, v, StringComparison.OrdinalIgnoreCase))).ToList();
                        if (invalidOptions.Count > 0)
                            errors.Add(new ValidationError(field.Key, $"Valori non ammessi per '{field.Label}': {string.Join(", ", invalidOptions)}"));
                    }
                    break;

                case FieldType.Signature:
                    if (field.Validation.IsRequired && (strVal.Length < 20 || !strVal.Contains("data:image")))
                        errors.Add(new ValidationError(field.Key, $"La firma grafica per '{field.Label}' è obbligatoria."));
                    break;

                case FieldType.Text:
                case FieldType.TextArea:
                case FieldType.Password:
                case FieldType.RichText:
                case FieldType.Markdown:
                case FieldType.Location:
                    if (field.Validation.MinLength.HasValue && strVal.Length < field.Validation.MinLength.Value)
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' deve contenere almeno {field.Validation.MinLength.Value} caratteri."));
                    if (field.Validation.MaxLength.HasValue && strVal.Length > field.Validation.MaxLength.Value)
                        errors.Add(new ValidationError(field.Key, $"Il campo '{field.Label}' può contenere al massimo {field.Validation.MaxLength.Value} caratteri."));
                    if (!string.IsNullOrWhiteSpace(field.Validation.RegexPattern) && !Regex.IsMatch(strVal, field.Validation.RegexPattern))
                        errors.Add(new ValidationError(field.Key, field.Validation.CustomErrorMessage ?? $"Il formato di '{field.Label}' non è valido."));
                    break;
            }
        }

        return errors.Count > 0 ? Result.ValidationFailure(errors) : Result.Success();
    }

    private static List<string> TryParseArray(object rawVal)
    {
        if (rawVal is IEnumerable<string> strEnum)
            return strEnum.ToList();
        if (rawVal is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.ToString()).ToList();

        var s = rawVal.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(s)) return new List<string>();

        if (s.StartsWith("[") && s.EndsWith("]"))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(s);
                return list ?? new List<string>();
            }
            catch { }
        }

        return s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public async Task<Result<FormSubmission>> SubmitFormAsync(string formCode, string submittedBy, Dictionary<string, object?> values, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(submittedBy))
            return Result<FormSubmission>.Failure("L'utente compilatore (SubmittedBy) è obbligatorio.");

        var valResult = await ValidateSubmissionFieldsAsync(formCode, values, ct);
        if (valResult.IsFailure)
            return Result<FormSubmission>.ValidationFailure(valResult.Errors);

        var form = (await _store.GetDefinitionAsync(formCode, ct))!;

        var submission = new FormSubmission
        {
            FormCode = form.FormCode,
            FormVersion = form.Version,
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow,
            Values = values
        };

        await _store.SaveSubmissionAsync(submission, ct);
        return Result<FormSubmission>.Success(submission);
    }

    public async Task<Result<FormSubmission>> GetSubmissionAsync(string submissionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
            return Result<FormSubmission>.Failure("Id sottomissione non valido.");

        var sub = await _store.GetSubmissionAsync(submissionId, ct);
        if (sub is null)
            return Result<FormSubmission>.Failure($"Compilazione '{submissionId}' non trovata.");

        return Result<FormSubmission>.Success(sub);
    }
}
