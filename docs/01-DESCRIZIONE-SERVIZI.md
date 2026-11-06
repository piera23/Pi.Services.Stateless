# 01 - Descrizione Dettagliata dei Servizi

La suite **`Pi.Services.Stateless`** è un'infrastruttura applicativa universale a microservizi (.NET 9), progettata per consentire all'utente di **comporre e orchestrare la propria piattaforma personalizzata** in modalità **stateless pura** (senza obbligo di database fisso) e con supporto alla **generazione di schemi di persistenza per qualsiasi database relazionale** (PostgreSQL, SQL Server, MySQL, SQLite).

---

## Indice dei Servizi
1. [Pi.FormGenerator (Compositore Maschere Low-Code & DB Generator)](#1-piformgenerator-compositore-maschere-low-code)
2. [Pi.Workflow (Workflow Designer Libero a Stati)](#2-piworkflow-workflow-designer-libero-a-stati)
3. [Pi.Documents (Fascicolo Documentale & File Streaming)](#3-pidocuments-fascicolo-documentale-e-file)
4. [Pi.Identity (Anagrafica, Ruoli & Risoluzione Gerarchica)](#4-piidentity-anagrafica-ruoli-e-profilatura-attori)
5. [Pi.Integrations (Gateway di Protocollo & Report Multi-Formato)](#5-piintegrations-protocollo-e-reportistica)

---

## 1. Pi.FormGenerator (Compositore Maschere Low-Code)

Consente all'utente di comporre qualsiasi maschera di inserimento dati e, una volta generata, fornisce automaticamente la funzione per **esportare lo schema DDL e le query di inserimento** per persistere i dati nel database a propria scelta.

### Funzionalità Cardine
- **Composizione Dinamica di Maschere con 40+ Componenti Web**:
  1. **Testo Standard & Avanzato**: `Text` (singola riga), `TextArea` (multilinea), `Email` (con regex RFC), `Password` (mascherato), `Phone` (internazionale), `Url` (validazione schema http/https), `RichText` (editor WYSIWYG HTML), `Markdown` (editor con formattazione Markdown).
  2. **Dati Personali, Finanziari & Geografici**: `FiscalCode` (Codice Fiscale/Tax ID con regex), `IBAN` (coordinate bancarie internazionali), `Currency` (importo monetario con valuta EUR/USD/GBP), `Location` (indirizzo, civico, città, CAP o coordinate GPS).
  3. **Numeri, Metriche & Rating**: `Number` (intero/quantità), `Decimal` (virgola mobile a 2 decimali), `Percentage` (0-100%), `Slider` (range continuo), `Rating` (stelle di gradimento 1-5), `LikertScale` (scala psicometrica di accordo 1-7), `Nps` (Net Promoter Score da 0 a 10 con classificazione detrattori/promotori).
  4. **Date & Calendari**: `Date` (anno-mese-giorno), `Time` (hh:mm), `DateTime` (timestamp con fuso orario), `DateRange` (intervallo dal/al).
  5. **Scelte & Selettori**: `Select` (menu a tendina), `MultiSelect` (scelta multipla a tag), `CascadingSelect` (menu a cascata gerarchici genitore-figlio), `Radio` (scelta esclusiva a cerchi), `Checkbox` (singolo flag booleano), `CheckboxGroup` (gruppo a caselle multiple), `Switch` (interruttore toggle On/Off), `TagInput` (inserimento rapido parole chiave a chip).
  6. **Avanzati, Codici, Firma & Media**: `ColorPicker` (selettore cromatico esadecimale #HEX), `Signature` (canvas per firma grafica autografa touch/mouse), `ImageUpload` (upload con anteprima miniatura), `File` (allegato generico), `AudioRecorder` (registrazione vocale/memo audio), `Otp` (box per PIN/codice di verifica a caselle), `BarcodeQrCode` (generazione/lettura barcode o QR Code), `JsonEditor` (editor JSON strutturato con syntax lint).
  7. **Dati Complessi & Sub-Form**: `DataGrid` (tabella dinamica a matrice modificabile con righe aggiungibili), `RepeatableSection` (sotto-maschera per record figli correlati es. familiari, titoli, spese).
  8. **Layout, Indicatori & Sicurezza**: `SectionHeader` (separatore titolato per raggruppamento visivo), `InfoAlert` (box di avviso contestuale informativo/warning), `Divider` (linea orizzontale di separazione), `ProgressStep` (indicatore di avanzamento per wizard multi-fase), `Captcha` (verifica anti-bot umana), `Hidden` (chiave tecnica invisibile).
- **Validatore Dichiarativo Stateless**: Verifica obbligatorietà, min/max numerici, vincoli di lunghezza, email, url, phone, codici fiscali, IBAN, OTP, JSON e regex personalizzate senza salvare record su server.
- **Generatore di Schemi Database (`ISchemaGeneratorService`)**:
  - `GenerateSchema(form, dialect)`: traduce istantaneamente lo schema della maschera in uno script SQL `CREATE TABLE` formattato per il database scelto dall'utente:
    - **PostgreSQL** (`TIMESTAMPTZ`, `VARCHAR`, `NUMERIC`, `BOOLEAN`, `JSONB`, `TEXT`)
    - **Microsoft SQL Server** (`DATETIME2`, `NVARCHAR`, `DECIMAL`, `BIT`, `NVARCHAR(MAX)`)
    - **MySQL** (`DATETIME`, `VARCHAR`, `DECIMAL`, `BOOLEAN`, `JSON`, `TEXT`)
    - **SQLite** (`TEXT`, `INTEGER`, `REAL`)
  - `GenerateInsertQuery(form, values, dialect)`: produce la query SQL `INSERT INTO` pronta con i valori tipizzati ed escaped per salvare i dati inseriti nel proprio database.

### Superficie API (`/api/forms`)
| Rotta | Metodo | Descrizione |
| :--- | :--- | :--- |
| `/api/forms` | `GET` | Elenca tutti i template di maschere create. |
| `/api/forms/{formCode}` | `GET` | Restituisce lo schema JSON completo di campi e regole. |
| `/api/forms` | `POST` | Registra o aggiorna una nuova maschera composta via Low-Code. |
| `/api/forms/{formCode}/validate` | `POST` | **Stateless puro**: valida i valori compilati restituendo esito o errori senza toccare lo storage. |
| `/api/forms/{formCode}/submissions` | `POST` | Convalida e archivia la sottomissione. |
| `/api/forms/{formCode}/database-schema` | `POST` | **Esportazione DB**: genera lo script SQL `CREATE TABLE` per il dialetto selezionato (`PostgreSql`, `SqlServer`, `MySql`, `Sqlite`). |
| `/api/forms/{formCode}/export-insert` | `POST` | **Salvataggio DB**: genera la query `INSERT INTO` con i valori compilati pronti per il proprio DB. |

---

## 2. Pi.Workflow (Workflow Designer Libero a Stati)

Fornisce la totale libertà di definire qualsiasi catena approvativa a stati personalizzata, con N passi, ruoli e transizioni arbitrarie.

### Due Modalità di Funzionamento
1. **Modalità Pure Stateless (Zero Storage)**:
   - Endpoint: `POST /api/workflow/evaluate-transition`
   - Riceve via body `(WorkflowDefinition, CurrentState, Action, Actor, Notes)` e calcola il nuovo stato matematicamente con l'audit log arricchito, senza necessità di persistere nulla a livello server.
2. **Modalità Gestita (con Store Pluggabile)**:
   - Avvia un'istanza (`POST /api/workflow/instances/start`) e permette avanzamenti progressivi nel tempo (`execute-step`).

### Capacità di Configurazione
- Definizione di N passi sequenziali o paralleli (`StepDefinition`).
- Assegnazione del ruolo preposto per ciascun passo (`Richiedente`, `TeamLeader`, `Dirigente`, `CFO`, `Legale`, `UfficioOperativo`).
- Configurazione libera delle azioni permesse per ogni step (`Invia`, `Approva`, `Rifiuta`, `Richiedi Integrazioni`, `Riporta Indietro`).
- Aggancio opzionale di azioni automatiche (`IWorkflowAction`).

---

## 3. Pi.Documents (Fascicolo Documentale e File)

Gestisce il contenitore della pratica (metadati, allegati in streaming e scambi di note istruttorie):
- **Tipologie Documento**: associazione univoca tra codice pratica, form template e workflow applicato.
- **Fascicolo della Pratica**: identificativo univoco, autore, oggetto, note, motivazioni di rifiuto e stato finale.
- **Streaming Allegati**: upload e download binario con calcolo automatico dell'hash **SHA-256** per garantire l'integrità del file, disaccoppiato tramite l'interfaccia `IFileStore`.
- **Note Istruttorie**: messaggistica interna tra richiedente e approvatori per richieste di chiarimento.

---

## 4. Pi.Identity (Anagrafica, Ruoli e Profilatura Attori)

Gestisce utenti, ruoli applicativi e la risoluzione dinamica degli attori:
- **Modello Utente**: anagrafica minimale, ruoli attribuiti e puntatore gerarchico al proprio responsabile (`ManagerMatricola`).
- **`IActorResolver`**: algoritmo dinamico che, dato un ruolo richiesto da un passo (es. "Dirigente"), risale in tempo reale alla struttura dell'autore senza dover cablare matricole fisse nella definizione del flusso.

---

## 5. Pi.Integrations (Protocollo e Reportistica)

Interfaccia servizi esterni per la formalizzazione delle pratiche:
- **Protocollazione (`IProtocolloService`)**: emette segnature ufficiali e numeri progressivi di protocollo tramite gateway agnostico (con mock locale per sviluppo offline).
- **Reportistica (`IReportGenerator`)**: generazione dinamica e rendering di attestati o riepiloghi in formato **PDF**, **Excel (.xlsx)**, **CSV** e **JSON**.
