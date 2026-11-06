using System.Collections.Concurrent;
using Pi.FormGenerator.Abstractions;
using Pi.FormGenerator.Models;

namespace Pi.FormGenerator.Storage.InMemory;

public class InMemoryFormStore : IFormStore
{
    private readonly ConcurrentDictionary<string, FormDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, FormSubmission> _submissions = new(StringComparer.OrdinalIgnoreCase);

    public Task<FormDefinition?> GetDefinitionAsync(string formCode, CancellationToken ct = default)
    {
        _definitions.TryGetValue(formCode, out var def);
        return Task.FromResult(def);
    }

    public Task<IReadOnlyList<FormDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<FormDefinition> list = _definitions.Values.ToList();
        return Task.FromResult(list);
    }

    public Task SaveDefinitionAsync(FormDefinition definition, CancellationToken ct = default)
    {
        _definitions[definition.FormCode] = definition;
        return Task.CompletedTask;
    }

    public Task<FormSubmission?> GetSubmissionAsync(string submissionId, CancellationToken ct = default)
    {
        _submissions.TryGetValue(submissionId, out var sub);
        return Task.FromResult(sub);
    }

    public Task SaveSubmissionAsync(FormSubmission submission, CancellationToken ct = default)
    {
        _submissions[submission.SubmissionId] = submission;
        return Task.CompletedTask;
    }
}
