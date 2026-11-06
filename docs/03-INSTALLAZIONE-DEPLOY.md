# 03 - Guida all'Installazione e al Deploy

Questa guida descrive le modalità per configurare, compilare ed eseguire **`Pi.Services.Stateless`** in locale, come servizio di sistema o in container Docker.

---

## 1. Prerequisiti

- **.NET 9 SDK** (versione 9.0.100 o superiore).
  Verifica la versione con:
  ```powershell
  dotnet --version
  ```
- **Porte di rete**: La porta predefinita per l'esecuzione locale è `5000` (HTTP) o `5001` (HTTPS).

---

## 2. Esecuzione in Locale

### Da Riga di Comando (.NET CLI)
Posizionati nella cartella del progetto host ed esegui:
```powershell
cd "Pi.Services.Stateless\src\Pi.Services.Stateless.Api"
dotnet run --urls "http://localhost:5000"
```

L'API sarà immediatamente raggiungibile su:
- Root Diagnostico: `http://localhost:5000/`
- Elenco Form: `http://localhost:5000/api/forms`
- Elenco Flussi: `http://localhost:5000/api/workflow/definitions`

### Da Visual Studio
1. Apri la solution [`Pi.Services.Stateless.sln`](file:///c:/Users/ripie/OneDrive/Desktop/Nuova%20cartella/BorseLibriSolution/Pi.Services.Stateless/Pi.Services.Stateless.sln).
2. Imposta `Pi.Services.Stateless.Api` come **Progetto di Avvio** (clic destro sul progetto -> *Imposta come progetto di avvio*).
3. Premi **F5** (Avvia con debug) oppure **Ctrl+F5** (Avvia senza debug).

---

## 3. Pubblicazione e Build di Release

Per compilare una versione ottimizzata per la produzione:
```powershell
dotnet publish "Pi.Services.Stateless\src\Pi.Services.Stateless.Api\Pi.Services.Stateless.Api.csproj" `
  -c Release `
  -o "C:\Deployment\Pi.Services.Stateless" `
  --self-contained false
```

I file binari di output conterranno l'eseguibile `Pi.Services.Stateless.Api.dll` e le configurazioni `appsettings.json`.

---

## 4. Esecuzione con Docker e Docker Compose

Nella root della cartella `Pi.Services.Stateless` sono inclusi un `Dockerfile` multi-stage e un file `docker-compose.yml`.

### Avvio con Docker Compose
```bash
cd Pi.Services.Stateless
docker compose up -d --build
```
Il container compilerà i progetti in modalità multi-stage, esporrà il servizio sulla porta `5000` e rimarrà in esecuzione in background:
```bash
curl http://localhost:5000/
```

### Stop del Container
```bash
docker compose down
```

---

## 5. Esecuzione come Servizio di Sistema

### Come Servizio Windows
1. Pubblica il progetto in una cartella permanente (es. `C:\Services\Pi.Services.Stateless`).
2. Apri PowerShell come Amministratore e crea il servizio Windows:
```powershell
New-Service -Name "PiServicesStateless" `
  -BinaryPathName "dotnet.exe C:\Services\Pi.Services.Stateless\Pi.Services.Stateless.Api.dll --urls http://0.0.0.0:5000" `
  -DisplayName "Pi Services Stateless Web API" `
  -StartupType Automatic

Start-Service -Name "PiServicesStateless"
```

### Come Servizio Linux (systemd)
Crea il file `/etc/systemd/system/pi-services.service`:
```ini
[Unit]
Description=Pi.Services.Stateless .NET Web API
After=network.target

[Service]
WorkingDirectory=/var/www/pi-services
ExecStart=/usr/bin/dotnet /var/www/pi-services/Pi.Services.Stateless.Api.dll --urls "http://0.0.0.0:5000"
Restart=always
RestartSec=10
SyslogIdentifier=pi-services
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```
Abilita e avvia:
```bash
sudo systemctl enable pi-services.service
sudo systemctl start pi-services.service
```
