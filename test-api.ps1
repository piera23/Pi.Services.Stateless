# Script di test end-to-end per Pi.Services.Stateless
param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

function Print-Header($title) {
    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host "  $title" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
}

try {
    # 1. Root diagnostico
    Print-Header "1. Verifica Root Diagnostico e Moduli Attivi"
    $root = Invoke-RestMethod -Uri "$BaseUrl/" -Method Get
    $root | Format-List

    # 2. Schema Form
    Print-Header "2. Recupero Schema Form 'RICHIESTA_GENERICA'"
    $form = Invoke-RestMethod -Uri "$BaseUrl/api/forms/RICHIESTA_GENERICA" -Method Get
    Write-Host "Titolo: $($form.title) - Versione: $($form.version)" -ForegroundColor Green
    Write-Host "Numero Sezioni: $($form.sections.Count)"

    # 3. Validazione Stateless (Dati Non Validi)
    Print-Header "3. Test Validazione Stateless con Dati ERRATI (Atteso 422 Unprocessable Entity)"
    try {
        $invalidPayload = @{
            values = @{
                tipologia_richiesta = "valore_inesistente"
                motivazione = "corta" # Minimo 10 caratteri richiesti
            }
        } | ConvertTo-Json
        Invoke-RestMethod -Uri "$BaseUrl/api/forms/RICHIESTA_GENERICA/validate" -Method Post -Body $invalidPayload -ContentType "application/json"
    } catch {
        Write-Host "Convalida fallita come previsto con codice: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
        $respStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($respStream)
        Write-Host $reader.ReadToEnd() -ForegroundColor Red
    }

    # 4. Validazione Stateless (Dati Validi)
    Print-Header "4. Test Validazione Stateless con Dati CORRETTI"
    $validPayload = @{
        values = @{
            tipologia_richiesta = "richiesta_permesso"
            motivazione = "Richiesta permesso studio per esami universitari sessione estiva"
            data_decorrenza = "2026-06-15"
        }
    } | ConvertTo-Json
    $valRes = Invoke-RestMethod -Uri "$BaseUrl/api/forms/RICHIESTA_GENERICA/validate" -Method Post -Body $validPayload -ContentType "application/json"
    Write-Host "Esito Convalida: $($valRes.isValid)" -ForegroundColor Green

    # 5. Sottomissione Form
    Print-Header "5. Sottomissione Form per Dipendente 1003"
    $subPayload = @{
        submittedBy = "1003"
        values = @{
            tipologia_richiesta = "richiesta_permesso"
            motivazione = "Richiesta permesso studio per esami universitari sessione estiva"
            data_decorrenza = "2026-06-15"
        }
    } | ConvertTo-Json
    $subRes = Invoke-RestMethod -Uri "$BaseUrl/api/forms/RICHIESTA_GENERICA/submissions" -Method Post -Body $subPayload -ContentType "application/json"
    Write-Host "Sottomissione registrata con ID: $($subRes.submissionId)" -ForegroundColor Green

    # 6. Valutazione Transizione Stateless Pura (Zero-DB)
    Print-Header "6. Valutazione Transizione Stateless Pura (WorkflowEngine.EvaluateTransition)"
    $evalPayload = @{
        definition = @{
            workflowCode = "WF_TEST"
            title = "Flusso Test"
            steps = @(
                @{
                    stepIndex = 0
                    title = "Istruttoria"
                    requiredRole = "Istruttore"
                    allowedActions = @("Approva", "Rifiuta")
                },
                @{
                    stepIndex = 1
                    title = "Firma Dirigente"
                    requiredRole = "Dirigente"
                    allowedActions = @("Approva", "Rifiuta")
                }
            )
        }
        currentState = @{
            currentStepIndex = 0
            status = 0
            history = @()
        }
        action = "Approva"
        actor = "1001"
        notes = "Verifica requisiti superata"
    } | ConvertTo-Json -Depth 5
    $evalRes = Invoke-RestMethod -Uri "$BaseUrl/api/workflow/evaluate-transition" -Method Post -Body $evalPayload -ContentType "application/json"
    Write-Host "Nuovo Passo Calcolato: $($evalRes.currentStepIndex) - Stato: $($evalRes.status)" -ForegroundColor Green

    # 7. Risoluzione Attore (Profilatura Dinamica)
    Print-Header "7. Profilatura Dinamica: Risoluzione Dirigente per Dipendente 1003"
    $actorPayload = @{
        stepRequiredRole = "Dirigente"
        applicantMatricola = "1003"
    } | ConvertTo-Json
    $actorRes = Invoke-RestMethod -Uri "$BaseUrl/api/identity/resolve-actor" -Method Post -Body $actorPayload -ContentType "application/json"
    Write-Host "Matricola Dirigente Risolta: $($actorRes.actorMatricola)" -ForegroundColor Green

    # 8. Protocollazione
    Print-Header "8. Protocollazione Documentale"
    $protPayload = @{
        documentId = "DOC-TEST-123"
        documentTypeCode = "RICH_GEN"
        oggetto = "Richiesta permesso studio"
        codiceFiscaleAutore = "VRDLGI90C03C222C"
        cognomeAutore = "Verdi"
        nomeAutore = "Luigi"
        codiceStruttura = "DIP_AMM"
    } | ConvertTo-Json
    $protRes = Invoke-RestMethod -Uri "$BaseUrl/api/integrations/protocollo/registra" -Method Post -Body $protPayload -ContentType "application/json"
    Write-Host "Numero Protocollo: $($protRes.numeroProtocollo)" -ForegroundColor Green
    Write-Host "Segnatura: $($protRes.segnatura)" -ForegroundColor Green

    Write-Host "`n>>> TUTTI I TEST SONO STATI ESEGUITI CON SUCCESSO! <<<" -ForegroundColor Green
}
catch {
    Write-Host "`nErrore durante l'esecuzione del test: $_" -ForegroundColor Red
}
