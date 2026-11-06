namespace Pi.FormGenerator.Models;

public enum FieldType
{
    // Testo Standard
    Text,
    TextArea,
    Email,
    Password,
    Phone,
    Url,
    RichText,
    Markdown,

    // Dati Personali & Finanziari
    FiscalCode,
    IBAN,
    Currency,
    Location,

    // Numeri, Metriche & Rating
    Number,
    Decimal,
    Percentage,
    Slider,
    Rating,
    LikertScale,
    Nps,

    // Data e Ora
    Date,
    Time,
    DateTime,
    DateRange,

    // Scelta & Selezione
    Select,
    MultiSelect,
    CascadingSelect,
    Radio,
    Checkbox,
    CheckboxGroup,
    Switch,
    TagInput,

    // Avanzati, Codici & Media
    ColorPicker,
    File,
    ImageUpload,
    Signature,
    AudioRecorder,
    Otp,
    BarcodeQrCode,
    JsonEditor,

    // Dati Complessi & Sub-Form
    DataGrid,
    RepeatableSection,

    // Layout, Display & Sicurezza
    SectionHeader,
    InfoAlert,
    Divider,
    ProgressStep,
    Captcha,
    Hidden
}

public record FieldOption(string Value, string Label);

public record FieldValidation(
    bool IsRequired = false,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? RegexPattern = null,
    string? CustomErrorMessage = null,
    string? AllowedExtensions = null,
    long? MaxFileSizeBytes = null);

public class FieldDefinition
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
    public string? Placeholder { get; init; }
    public FieldType Type { get; init; } = FieldType.Text;
    public int Order { get; init; }
    public FieldValidation Validation { get; init; } = new();
    public IReadOnlyList<FieldOption> Options { get; init; } = Array.Empty<FieldOption>();
    public object? DefaultValue { get; init; }

    // Proprietà UI avanzate
    public string? Prefix { get; init; }
    public string? Suffix { get; init; }
    public string? HelpText { get; init; }
    public int? Step { get; init; }
    public int? MaxRatingStars { get; init; } = 5;
    public bool IsReadOnly { get; init; } = false;
}

public class FormSection
{
    public required string SectionId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
    public IReadOnlyList<FieldDefinition> Fields { get; init; } = Array.Empty<FieldDefinition>();
}

public class FormDefinition
{
    public required string FormCode { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<FormSection> Sections { get; init; } = Array.Empty<FormSection>();

    public IEnumerable<FieldDefinition> AllFields => Sections.SelectMany(s => s.Fields);
}

public class FormSubmission
{
    public string SubmissionId { get; init; } = Guid.NewGuid().ToString("N");
    public required string FormCode { get; init; }
    public int FormVersion { get; init; }
    public Dictionary<string, object?> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public required string SubmittedBy { get; init; }
    public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;
}
