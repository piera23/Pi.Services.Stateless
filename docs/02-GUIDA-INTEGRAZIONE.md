# 02 - Guida all'Integrazione

Questa guida illustra come integrare i servizi di **`Pi.Services.Stateless`** nella tua applicazione (frontend React, Angular, Vue, Blazor o microservizi backend) e come sfruttare la generazione automatica degli schemi per salvare i dati nel database che preferisci.

---

## 1. Come Comporre una Maschera e Generare il Database

Il servizio `Pi.FormGenerator` ti permette di comporre qualsiasi maschera Low-Code e generare istantaneamente lo script per il tuo database relazionale.

### Chiamata API per Generare lo Schema SQL
```http
POST /api/forms/MIA_MASCHERA/database-schema?dialect=PostgreSql
Content-Type: application/json
```

**Risposta JSON:**
```json
{
  "tableName": "mia_maschera",
  "dialect": "PostgreSql",
  "createTableScript": "CREATE TABLE \"mia_maschera\" (\n    \"id\" VARCHAR(64) PRIMARY KEY,\n    \"submitted_by\" VARCHAR(128) NOT NULL,\n    \"submitted_at\" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,\n    \"titolo\" VARCHAR(255) NOT NULL,\n    \"budget\" NUMERIC(18,2) NULL\n);",
  "sampleInsertScript": "INSERT INTO \"mia_maschera\" (\"id\", \"submitted_by\", \"submitted_at\", \"titolo\", \"budget\")\nVALUES ('sub_demo_001', 'user_123', CURRENT_TIMESTAMP, 'Valore di prova per Titolo', 150.50);"
}
```

Puoi scegliere tra 4 dialetti supportati tramite il parametro `dialect`:
- `PostgreSql`
- `SqlServer`
- `MySql`
- `Sqlite`

---

## 2. Come Definire e Far Girare un Workflow Personalizzato

Puoi registrare qualsiasi workflow con N passi e regole arbitrarie:

```http
POST /api/workflow/definitions
Content-Type: application/json

{
  "workflowCode": "WF_ACQUISTI_AZIENDALI",
  "title": "Procedimento Approvazione Spese",
  "version": 1,
  "steps": [
    {
      "stepIndex": 0,
      "title": "Richiesta Preventivo",
      "requiredRole": "Richiedente",
      "allowedActions": ["Invia", "Annulla"]
    },
    {
      "stepIndex": 1,
      "title": "Validazione Budget",
      "requiredRole": "CFO",
      "allowedActions": ["Approva", "Richiedi Modifiche", "Rifiuta"]
    },
    {
      "stepIndex": 2,
      "title": "Firma Amministratore Delegato",
      "requiredRole": "CEO",
      "allowedActions": ["Approva", "Rifiuta"]
    }
  ]
}
```

### Valutazione Transizione Pura Stateless (Zero-DB)
Invia l'azione dell'operatore e lo stato corrente:
```http
POST /api/workflow/evaluate-transition
Content-Type: application/json

{
  "definition": { /* schema workflow */ },
  "currentState": { "currentStepIndex": 1, "status": 0, "history": [] },
  "action": "Approva",
  "actor": "cfo_user",
  "notes": "Budget verificato e approvato"
}
```
Il motore restituisce immediatamente il nuovo stato calcolato matematicamente (`currentStepIndex = 2`, audit log aggiornato) senza salvare nulla sul server.

---

## 3. Come Integrare la Persistenza Dati a Livello Codice

Se preferisci che il servizio salvi direttamente nel tuo database invece di usare lo store in-memory di default, ti basta implementare l'interfaccia `IFormStore` o `IWorkflowStore`:

```csharp
public class MyCustomDbStore : IFormStore
{
    private readonly IDbConnection _db;
    public MyCustomDbStore(IDbConnection db) => _db = db;

    public async Task SaveSubmissionAsync(FormSubmission submission, CancellationToken ct = default)
    {
        // Salva nel database configurato nella tua applicazione
    }
    // ...
}
```

E in `Program.cs`:
```csharp
builder.Services.AddPiFormGeneratorCore();
builder.Services.AddScoped<IFormStore, MyCustomDbStore>();
```
Tutte le logiche di validazione, layout e API rimangono identiche e continuano a funzionare in modo trasparente.
