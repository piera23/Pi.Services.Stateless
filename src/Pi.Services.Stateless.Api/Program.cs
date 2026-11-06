using Pi.Documents.Abstractions;
using Pi.Documents.Extensions;
using Pi.Documents.Models;
using Pi.FormGenerator.Abstractions;
using Pi.FormGenerator.Extensions;
using Pi.FormGenerator.Models;
using Pi.Identity.Extensions;
using Pi.Integrations.Extensions;
using Pi.Services.Stateless.Api.Endpoints;
using Pi.Workflow.Abstractions;
using Pi.Workflow.Extensions;
using Pi.Workflow.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------
// Configurazione Dependency Injection (Completamente Stateless & Zero-DB di default)
// -------------------------------------------------------------
builder.Services.AddPiFormGeneratorInMemory();
builder.Services.AddPiWorkflowInMemory();
builder.Services.AddPiDocumentsInMemory();
builder.Services.AddPiIdentityInMemory();
builder.Services.AddPiProtocolloMock();
builder.Services.AddPiReporting();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

// -------------------------------------------------------------
// Registrazione Endpoints Minimal API dei vari moduli
// -------------------------------------------------------------
app.MapFormEndpoints();
app.MapWorkflowEndpoints();
app.MapDocumentEndpoints();
app.MapIdentityEndpoints();
app.MapIntegrationEndpoints();

// Root endpoint diagnostico
app.MapGet("/", () => Results.Ok(new
{
    service = "Pi.Services.Stateless",
    version = "1.0.0",
    status = "Online",
    modules = new[]
    {
        "Pi.FormGenerator",
        "Pi.Workflow",
        "Pi.Documents",
        "Pi.Identity",
        "Pi.Integrations.Protocollo",
        "Pi.Integrations.Reporting"
    },
    documentation = "Visualizza le rotte disponibili tramite OpenAPI / Minimal APIs",
    timestamp = DateTime.UtcNow
}));

// -------------------------------------------------------------
// Seed dati demo iniziali per test immediato
// -------------------------------------------------------------
await SeedDemoDataAsync(app.Services);

app.Run();

static async Task SeedDemoDataAsync(IServiceProvider sp)
{
    using var scope = sp.CreateScope();
    var formStore = scope.ServiceProvider.GetRequiredService<IFormStore>();
    var wfStore = scope.ServiceProvider.GetRequiredService<IWorkflowStore>();
    var docStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

    // 1. Seed Form "RICHIESTA_GENERICA"
    var form = new FormDefinition
    {
        FormCode = "RICHIESTA_GENERICA",
        Title = "Modulo di Richiesta Generica",
        Description = "Modulo per la sottomissione di istanze e richieste generiche al dirigente.",
        Version = 1,
        Sections = new List<FormSection>
        {
            new()
            {
                SectionId = "dati_istanza",
                Title = "Dati dell'Istanza",
                Order = 1,
                Fields = new List<FieldDefinition>
                {
                    new()
                    {
                        Key = "tipologia_richiesta",
                        Label = "Tipologia Richiesta",
                        Type = FieldType.Select,
                        Order = 1,
                        Validation = new FieldValidation(IsRequired: true),
                        Options = new List<FieldOption>
                        {
                            new("richiesta_ferie", "Ferie Straordinarie"),
                            new("richiesta_permesso", "Permesso Studio"),
                            new("richiesta_attrezzatura", "Attrezzatura da Ufficio"),
                            new("altro", "Altra Comunicazione")
                        }
                    },
                    new()
                    {
                        Key = "motivazione",
                        Label = "Descrizione / Motivazione",
                        Type = FieldType.TextArea,
                        Order = 2,
                        Validation = new FieldValidation(IsRequired: true, MinLength: 10, MaxLength: 1000)
                    },
                    new()
                    {
                        Key = "data_decorrenza",
                        Label = "Data Decorrenza Desiderata",
                        Type = FieldType.Date,
                        Order = 3,
                        Validation = new FieldValidation(IsRequired: false)
                    }
                }
            }
        }
    };
    await formStore.SaveDefinitionAsync(form);

    // 2. Seed Workflow "WF_APPROVAZIONE_DUE_PASSI"
    var wf = new WorkflowDefinition
    {
        WorkflowCode = "WF_APPROVAZIONE_DUE_PASSI",
        Title = "Flusso Approvativo Standard a 2 Passi",
        Description = "Passo 1: Istruttoria Segreteria; Passo 2: Firma Dirigente",
        Version = 1,
        Steps = new List<StepDefinition>
        {
            new()
            {
                StepIndex = 0,
                Title = "Istruttoria e Verifica Requisiti",
                RequiredRole = "Istruttore",
                IsBlocking = true,
                AllowedActions = new List<string> { "Approva", "Rifiuta", "RiportaIndietro" }
            },
            new()
            {
                StepIndex = 1,
                Title = "Approvazione e Firma Dirigente",
                RequiredRole = "Dirigente",
                IsBlocking = true,
                AllowedActions = new List<string> { "Approva", "Rifiuta", "RiportaIndietro" }
            }
        }
    };
    await wfStore.SaveDefinitionAsync(wf);

    // 3. Seed Document Type Code "RICHIESTA_GENERICA"
    var docType = new DocumentTypeCode
    {
        Code = "RICH_GEN",
        Description = "Richiesta Generica Dipendente",
        FormCode = "RICHIESTA_GENERICA",
        WorkflowCode = "WF_APPROVAZIONE_DUE_PASSI",
        IsProtocolloRequired = true,
        IsActive = true
    };
    await docStore.SaveTypeCodeAsync(docType);
}
