# Pi.Services.Stateless

Suite applicativa universale a microservizi (.NET 9) per consentire all'utente di **costruire la propria piattaforma personalizzata**:
- **Compositore Maschere Low-Code** (`Pi.FormGenerator`): composizione visiva/API di maschere, validazione stateless e **generazione automatica dello schema SQL (CREATE TABLE / INSERT)** per il database a scelta dell'utente (**PostgreSQL**, **Microsoft SQL Server**, **MySQL**, **SQLite**).
- **Workflow Designer Libero a Stati** (`Pi.Workflow`): definizione di catene approvative personalizzate con N passi, ruoli e transizioni, operante sia in modalità **pura stateless** che gestita.
- **Fascicolo Documentale & Streaming Allegati** (`Pi.Documents`): gestione pratiche, note istruttorie e streaming file con hash SHA-256.
- **Anagrafica & Risoluzione Gerarchica Attori** (`Pi.Identity`): RBAC e risoluzione dinamica dei dirigenti/responsabili.
- **Integrazioni Gateway** (`Pi.Integrations`): protocollazione e generazione dinamica di report (PDF, Excel, CSV, JSON).

---

## Documentazione Ufficiale

1. **[01 - Descrizione Dettagliata dei Servizi](docs/01-DESCRIZIONE-SERVIZI.md)**: Analisi dei 5 moduli, modelli, contratti e mappe API.
2. **[02 - Guida all'Integrazione](docs/02-GUIDA-INTEGRAZIONE.md)**: Come comporre maschere, generare lo schema DDL per il proprio database e gestire flussi liberi.
3. **[03 - Installazione e Deploy](docs/03-INSTALLAZIONE-DEPLOY.md)**: Esecuzione in locale, pubblicazione, container Docker e servizi Windows/Linux.

---

## Sito Vetrina Interattivo (Showcase)

È disponibile un sito vetrina interattivo completo con compositore Low-Code e Workflow Designer visivo:
- File pronto all'uso: **`showcase/index.html`** (apribile con qualsiasi browser).

---

## Avvio Rapido

```powershell
cd "Pi.Services.Stateless\src\Pi.Services.Stateless.Api"
dotnet run --urls http://localhost:5000
```
In un altro terminale, esegui i test end-to-end:
```powershell
powershell.exe -ExecutionPolicy Bypass -File "Pi.Services.Stateless\test-api.ps1"
```
