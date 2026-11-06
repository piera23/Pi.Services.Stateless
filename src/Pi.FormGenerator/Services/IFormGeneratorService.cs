using Pi.Common.Models;
using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Services;

public interface IFormGeneratorService
{
    Task<Result<FormDefinition>> GetFormDefinitionAsync(string formCode, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FormDefinition>>> GetAllFormDefinitionsAsync(CancellationToken ct = default);
    Task<Result<FormDefinition>> CreateOrUpdateFormDefinitionAsync(FormDefinition definition, CancellationToken ct = default);
    
    Task<Result> ValidateSubmissionFieldsAsync(string formCode, Dictionary<string, object?> values, CancellationToken ct = default);
    Task<Result<FormSubmission>> SubmitFormAsync(string formCode, string submittedBy, Dictionary<string, object?> values, CancellationToken ct = default);
    Task<Result<FormSubmission>> GetSubmissionAsync(string submissionId, CancellationToken ct = default);
}
