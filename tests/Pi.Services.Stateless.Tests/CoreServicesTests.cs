using Pi.Common.Models;
using Pi.Documents.Models;
using Pi.Documents.Services;
using Pi.Documents.Storage.InMemory;
using Pi.FormGenerator.Models;
using Pi.FormGenerator.Services;
using Pi.FormGenerator.Storage.InMemory;
using Pi.Identity.Models;
using Pi.Identity.Services;
using Pi.Identity.Storage.InMemory;
using Pi.Workflow.Abstractions;
using Pi.Workflow.Engine;
using Pi.Workflow.Models;
using Pi.Workflow.Storage.InMemory;
using Xunit;

namespace Pi.Services.Stateless.Tests;

public class CoreServicesTests
{
    [Fact]
    public async Task FormGenerator_Validate_ShouldReturnErrors_WhenRequiredFieldsAreMissing()
    {
        var store = new InMemoryFormStore();
        var svc = new FormGeneratorService(store);

        var form = new FormDefinition
        {
            FormCode = "TEST_FORM",
            Title = "Test Form",
            Sections = new List<FormSection>
            {
                new()
                {
                    SectionId = "s1",
                    Title = "Sezione 1",
                    Fields = new List<FieldDefinition>
                    {
                        new()
                        {
                            Key = "email",
                            Label = "Indirizzo Email",
                            Validation = new FieldValidation(IsRequired: true, RegexPattern: @"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                        }
                    }
                }
            }
        };
        await svc.CreateOrUpdateFormDefinitionAsync(form);

        // Test 1: Mancante
        var resMissing = await svc.ValidateSubmissionFieldsAsync("TEST_FORM", new Dictionary<string, object?>());
        Assert.True(resMissing.IsFailure);
        Assert.Contains(resMissing.Errors, e => e.PropertyName == "email");

        // Test 2: Regex errata
        var resInvalid = await svc.ValidateSubmissionFieldsAsync("TEST_FORM", new Dictionary<string, object?> { ["email"] = "non-valida" });
        Assert.True(resInvalid.IsFailure);

        // Test 3: Valido
        var resOk = await svc.ValidateSubmissionFieldsAsync("TEST_FORM", new Dictionary<string, object?> { ["email"] = "test@example.com" });
        Assert.True(resOk.IsSuccess);
    }

    [Fact]
    public void Workflow_EvaluateTransition_Stateless_ShouldAdvanceStep()
    {
        var engine = new WorkflowEngine(new InMemoryWorkflowStore(), Enumerable.Empty<IWorkflowAction>());

        var def = new WorkflowDefinition
        {
            WorkflowCode = "WF_TEST",
            Title = "Workflow Test",
            Steps = new List<StepDefinition>
            {
                new() { StepIndex = 0, Title = "Passo 0", AllowedActions = new[] { "Approva", "Rifiuta" } },
                new() { StepIndex = 1, Title = "Passo 1", AllowedActions = new[] { "Approva" } }
            }
        };

        var state = new WorkflowState
        {
            CurrentStepIndex = 0,
            Status = WorkflowStatus.InCorso
        };

        // Esegue Approva sul passo 0
        var result = engine.EvaluateTransition(def, state, "Approva", "attore_1", "Note di approvazione");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.CurrentStepIndex);
        Assert.Equal(WorkflowStatus.InCorso, result.Value.Status);
        Assert.Single(result.Value.History);
        Assert.Equal("attore_1", result.Value.History[0].Actor);
    }

    [Fact]
    public void Workflow_EvaluateTransition_Stateless_ShouldReject_WhenActionIsRifiuta()
    {
        var engine = new WorkflowEngine(new InMemoryWorkflowStore(), Enumerable.Empty<IWorkflowAction>());

        var def = new WorkflowDefinition
        {
            WorkflowCode = "WF_TEST",
            Title = "Workflow Test",
            Steps = new List<StepDefinition>
            {
                new() { StepIndex = 0, Title = "Passo 0", AllowedActions = new[] { "Approva", "Rifiuta" } }
            }
        };

        var state = new WorkflowState { CurrentStepIndex = 0, Status = WorkflowStatus.InCorso };

        var result = engine.EvaluateTransition(def, state, "Rifiuta", "attore_1", "Motivo rigetto");

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowStatus.Rifiutato, result.Value?.Status);
    }

    [Fact]
    public async Task Identity_ResolveActor_ShouldReturnManager_WhenRoleIsDirigente()
    {
        var store = new InMemoryUserStore();
        var resolver = new ActorResolver(store);

        // Nella seed di default: dipendente 1003 ha ManagerMatricola = 1002 (Giuseppe Bianchi)
        var result = await resolver.ResolveStepActorAsync("Dirigente", "1003");

        Assert.True(result.IsSuccess);
        Assert.Equal("1002", result.Value);
    }

    [Fact]
    public async Task Documents_UploadAndDownloadAttachment_ShouldWorkCorrectly()
    {
        var docStore = new InMemoryDocumentStore();
        var fileStore = new InMemoryFileStore();
        var svc = new DocumentService(docStore, fileStore);

        var docRes = await svc.CreateDocumentAsync("RICH_GEN", "1003", "Documento di test");
        Assert.True(docRes.IsSuccess);
        var docId = docRes.Value!.DocumentId;

        var sampleBytes = "Contenuto del file di prova allegato."u8.ToArray();
        using var stream = new MemoryStream(sampleBytes);

        var uploadRes = await svc.UploadAttachmentAsync(docId, "test.txt", "text/plain", stream, "1003");
        Assert.True(uploadRes.IsSuccess);
        Assert.NotNull(uploadRes.Value);

        var downloadRes = await svc.DownloadAttachmentAsync(uploadRes.Value.AttachmentId);
        Assert.True(downloadRes.IsSuccess);
        using var ms = new MemoryStream();
        await downloadRes.Value.Content.CopyToAsync(ms);
        Assert.Equal(sampleBytes, ms.ToArray());
    }
}
