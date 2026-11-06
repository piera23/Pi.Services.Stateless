using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Abstractions;

public interface IFormStore
{
    Task<FormDefinition?> GetDefinitionAsync(string formCode, CancellationToken ct = default);
    Task<IReadOnlyList<FormDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default);
    Task SaveDefinitionAsync(FormDefinition definition, CancellationToken ct = default);
    Task<FormSubmission?> GetSubmissionAsync(string submissionId, CancellationToken ct = default);
    Task SaveSubmissionAsync(FormSubmission submission, CancellationToken ct = default);
}
