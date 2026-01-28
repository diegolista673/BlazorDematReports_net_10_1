# ?? Guida Implementazione Nuovi Mail Handlers

## Indice

- [Panoramica Architettura](#panoramica-architettura)
- [Prerequisiti](#prerequisiti)
- [Step 1: Definire ServiceCode](#step-1-definire-servicecode)
- [Step 2: Creare Handler](#step-2-creare-handler)
- [Step 3: Implementare Logica Business](#step-3-implementare-logica-business)
- [Step 4: Configurazione](#step-4-configurazione)
- [Step 5: Registrazione](#step-5-registrazione)
- [Step 6: Creazione Task](#step-6-creazione-task)
- [Step 7: Sync Hangfire](#step-7-sync-hangfire)
- [Step 8: Test e Monitoring](#step-8-test-e-monitoring)
- [Templates per Protocolli](#templates-per-protocolli)
- [Esempi Pratici](#esempi-pratici)
- [Troubleshooting](#troubleshooting)

---

## Panoramica Architettura

### Componenti Chiave

```
???????????????????????????????????????????????????????????????
?                    HANGFIRE SCHEDULER                       ?
?                 RecurringJob (CRON Expression)              ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?              UNIFIED HANDLER SERVICE                        ?
?         (Dispatcher per tutti gli handler)                  ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?                 MAIL IMPORT HANDLER                         ?
?            (Es: Hera16Handler, Ader4Handler)                ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?               MAIL IMPORT SERVICE                           ?
?         (Business logic per elaborazione email)             ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?              EMAIL SERVER (EWS/IMAP/POP3)                   ?
?         (Exchange, Gmail, Yahoo, server custom)             ?
???????????????????????????????????????????????????????????????
```

### Pattern Unified Handler

Il sistema utilizza un **pattern unificato** che permette di:

- ? Trattare **Query SQL** e **Servizi Mail** con la stessa interfaccia
- ? Registrazione automatica tramite **Assembly Scanning**
- ? Scheduling automatico via **Hangfire**
- ? Monitoring unificato nel tab **"Monitoring"**
- ? Estensibilità senza modificare il core

---

## Prerequisiti

### Librerie NuGet

**Per Exchange Web Services (EWS)**:
```bash
dotnet add package Microsoft.Exchange.WebServices.NETCore
```

**Per IMAP/POP3**:
```bash
dotnet add package MailKit
dotnet add package MimeKit
```

**Per parsing CSV** (opzionale):
```bash
dotnet add package CsvHelper
```

### Permessi Email

- ? Credenziali IMAP/POP3 o OAuth2 Exchange
- ? Permessi lettura/modifica email
- ? Accesso alla cartella Inbox

---

## Step 1: Definire ServiceCode

### 1.1 Aggiungere Costante

Aggiungi il codice del servizio in `JobConstants.cs`:

```csharp
// File: ClassLibraryLavorazioni/LavorazioniViaMail/Constants/JobConstants.cs

namespace LibraryLavorazioni.LavorazioniViaMail.Constants
{
    /// <summary>
    /// Costanti per i job e servizi mail.
    /// </summary>
    public static class JobConstants
    {
        /// <summary>
        /// Codici identificativi dei servizi mail supportati.
        /// </summary>
        public static class MailServiceCodes
        {
            /// <summary>
            /// Servizio HERA16 via Exchange Web Services.
            /// </summary>
            public const string Hera16 = "HERA16";
            
            /// <summary>
            /// Servizio ADER4 via IMAP.
            /// </summary>
            public const string Ader4 = "ADER4";
            
            /// <summary>
            /// Servizio Equitalia via IMAP/POP3.
            /// </summary>
            public const string Equitalia = "EQUITALIA";
            
            // ? AGGIUNGI NUOVO SERVIZIO QUI
            /// <summary>
            /// Servizio [NOME_SERVIZIO] via [PROTOCOLLO].
            /// </summary>
            public const string MioServizio = "MIO_SERVIZIO";
        }
    }
}
```

**Convenzioni naming**:
- ? **Uppercase** (es. `HERA16`, `ADER4`)
- ? **Univoco** nel sistema
- ? **Descrittivo** del servizio
- ? Evitare spazi o caratteri speciali

---

## Step 2: Creare Handler

### 2.1 Implementare IMailImportHandler

Crea un nuovo file handler:

```csharp
// File: ClassLibraryLavorazioni/LavorazioniViaMail/Handlers/Ader4Handler.cs

using LibraryLavorazioni.LavorazioniViaMail.Constants;
using LibraryLavorazioni.LavorazioniViaMail.Interfaces;
using LibraryLavorazioni.LavorazioniViaMail.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryLavorazioni.LavorazioniViaMail.Handlers
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio ADER4 via IMAP.
    /// Gestisce la lettura e processamento delle email ADER4 con allegati CSV.
    /// 
    /// Caratteristiche:
    /// - Protocol: IMAP
    /// - Server: Gmail/Custom
    /// - Formato dati: CSV allegato
    /// - Frequenza: Giornaliera
    /// </summary>
    public sealed class Ader4Handler : IMailImportHandler
    {
        /// <inheritdoc />
        public string ServiceCode => JobConstants.MailServiceCodes.Ader4;

        /// <inheritdoc />
        public Task<int> ExecuteAsync(
            IServiceProvider sp, 
            MailImportExecutionContext ctx, 
            CancellationToken ct)
        {
            // Risolve il servizio dal DI container
            var mailService = sp.GetRequiredService<IMailImportService>();
            
            // Delega la logica business al servizio
            return mailService.ProcessAder4Async(ct);
        }
    }
}
```

**Pattern chiave**:

| Elemento | Descrizione |
|----------|-------------|
| `sealed class` | Performance optimization |
| `ServiceCode` | Identificatore univoco (deve matchare costante) |
| `ExecuteAsync` | Entry point chiamato da Hangfire |
| `IServiceProvider` | Dependency Injection per servizi |
| `MailImportExecutionContext` | Contesto esecuzione (date, centro, ecc.) |
| `CancellationToken` | Supporto cancellazione operazione |

---

## Step 3: Implementare Logica Business

### 3.1 Aggiungere Metodo in MailImportService

Implementa la logica specifica nel servizio:

```csharp
// File: ClassLibraryLavorazioni/LavorazioniViaMail/Services/MailImportService.cs

using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class MailImportService : IMailImportService
{
    private readonly ILogger<MailImportService> _logger;
    private readonly IDbContextFactory<DematReportsContext> _dbContextFactory;
    private readonly IConfiguration _configuration;

    public MailImportService(
        ILogger<MailImportService> logger,
        IDbContextFactory<DematReportsContext> dbContextFactory,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Processa le email ADER4 via IMAP con allegati CSV.
    /// 
    /// Flusso:
    /// 1. Connessione IMAP al server
    /// 2. Ricerca email non lette con allegati
    /// 3. Download e parse allegati CSV
    /// 4. Salvataggio dati in DB
    /// 5. Marcatura email come lette
    /// </summary>
    /// <param name="ct">Token di cancellazione.</param>
    /// <returns>Numero di righe processate.</returns>
    public async Task<int> ProcessAder4Async(CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("[ADER4] Inizio elaborazione email");
            
            int totalProcessed = 0;

            // 1. Configurazione IMAP da appsettings
            var config = _configuration.GetSection("MailServices:Ader4");
            var imapServer = config["ImapServer"] ?? throw new InvalidOperationException("ImapServer not configured");
            var imapPort = int.Parse(config["ImapPort"] ?? "993");
            var username = config["Username"] ?? throw new InvalidOperationException("Username not configured");
            var password = config["Password"] ?? throw new InvalidOperationException("Password not configured");
            var useSsl = bool.Parse(config["UseSsl"] ?? "true");

            _logger.LogDebug("[ADER4] Connessione a {Server}:{Port} come {Username}", 
                imapServer, imapPort, username);

            using var client = new ImapClient();
            
            // 2. Connessione IMAP
            await client.ConnectAsync(imapServer, imapPort, useSsl, ct);
            
            // Autenticazione
            await client.AuthenticateAsync(username, password, ct);
            
            _logger.LogInformation("[ADER4] Autenticazione completata con successo");

            // 3. Apri cartella Inbox
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

            // 4. Cerca email non lette con allegati e subject specifico
            var searchCriteria = SearchQuery.NotSeen
                .And(SearchQuery.SubjectContains("ADER"))
                .And(SearchQuery.HasAttachment);
            
            var uids = await inbox.SearchAsync(searchCriteria, ct);

            _logger.LogInformation("[ADER4] Trovate {Count} email non lette con allegati", uids.Count);

            // 5. Processa ogni email
            foreach (var uid in uids)
            {
                try
                {
                    var message = await inbox.GetMessageAsync(uid, ct);
                    
                    _logger.LogDebug("[ADER4] Elaborazione email UID={Uid}: {Subject}", 
                        uid, message.Subject);

                    // 6. Estrai e processa allegati CSV
                    var csvAttachments = message.Attachments
                        .OfType<MimePart>()
                        .Where(a => a.FileName?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true);

                    foreach (var attachment in csvAttachments)
                    {
                        _logger.LogDebug("[ADER4] Processamento allegato: {FileName}", attachment.FileName);
                        
                        // Decode allegato in memoria
                        using var stream = new MemoryStream();
                        await attachment.Content.DecodeToAsync(stream, ct);
                        
                        var csvContent = Encoding.UTF8.GetString(stream.ToArray());
                        
                        // 7. Parse CSV e salva dati
                        var rowsInserted = await ParseAndSaveAder4CsvAsync(csvContent, ct);
                        totalProcessed += rowsInserted;
                        
                        _logger.LogInformation("[ADER4] Allegato {FileName} processato: {Rows} righe inserite", 
                            attachment.FileName, rowsInserted);
                    }

                    // 8. Marca email come letta
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true, ct);
                    
                    _logger.LogDebug("[ADER4] Email UID={Uid} marcata come letta", uid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ADER4] Errore durante l'elaborazione email UID={Uid}", uid);
                    // Continua con le altre email
                }
            }

            // 9. Disconnessione
            await client.DisconnectAsync(quit: true, ct);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "[ADER4] Elaborazione completata. Totale righe: {Total}, Tempo: {ElapsedMs}ms", 
                totalProcessed, stopwatch.ElapsedMilliseconds);
            
            return totalProcessed;
        }
        catch (ImapProtocolException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[ADER4] Errore protocollo IMAP dopo {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[ADER4] Errore generico durante l'elaborazione email dopo {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Parse del CSV ADER4 e salvataggio dati nel database.
    /// </summary>
    /// <param name="csvContent">Contenuto CSV come stringa.</param>
    /// <param name="ct">Token di cancellazione.</param>
    /// <returns>Numero di righe inserite.</returns>
    private async Task<int> ParseAndSaveAder4CsvAsync(string csvContent, CancellationToken ct)
    {
        var rows = 0;
        
        using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        try
        {
            // Parse CSV (formato: Operatore;Data;Documenti;Fogli;Pagine)
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            _logger.LogDebug("[ADER4] Parse CSV: {Lines} righe totali", lines.Length);
            
            // Skip header row
            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split(';');
                
                if (fields.Length < 5)
                {
                    _logger.LogWarning("[ADER4] Riga CSV invalida (campi insufficienti): {Line}", line);
                    continue;
                }

                try
                {
                    var produzione = new ProduzioneOperatore
                    {
                        Operatore = fields[0].Trim(),
                        DataLavorazione = DateTime.Parse(fields[1].Trim()),
                        Documenti = int.Parse(fields[2].Trim()),
                        Fogli = int.Parse(fields[3].Trim()),
                        Pagine = int.Parse(fields[4].Trim()),
                        IdProceduraLavorazione = 15, // TODO: Configurabile
                        IdFaseLavorazione = 1,       // TODO: Configurabile
                        DataInserimento = DateTime.Now
                    };

                    context.ProduzioneOperatores.Add(produzione);
                    rows++;
                }
                catch (FormatException ex)
                {
                    _logger.LogWarning(ex, "[ADER4] Errore parsing riga CSV: {Line}", line);
                }
            }

            await context.SaveChangesAsync(ct);
            
            _logger.LogInformation("[ADER4] Salvate {Rows} righe nel database", rows);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "[ADER4] Errore durante il salvataggio nel database");
            throw;
        }
        
        return rows;
    }

    /// <summary>
    /// Dispatcher per servizi mail multipli.
    /// </summary>
    public async Task<int> ProcessMailServiceAsync(string serviceCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[MailImportService] Elaborazione servizio mail: {ServiceCode}", serviceCode);

        return serviceCode switch
        {
            JobConstants.MailServiceCodes.Hera16 => await ProcessHera16Async(ct),
            JobConstants.MailServiceCodes.Ader4 => await ProcessAder4Async(ct),
            // Aggiungi nuovi servizi qui
            _ => throw new NotSupportedException($"Servizio mail non supportato: {serviceCode}")
        };
    }
}
```

---

## Step 4: Configurazione

### 4.1 appsettings.json

Aggiungi configurazione servizio:

```json
{
  "MailServices": {
    "Hera16": {
      "ExchangeUrl": "https://outlook.office365.com/EWS/Exchange.asmx",
      "Username": "hera@domain.com",
      "Password": "use_user_secrets_in_dev"
    },
    "Ader4": {
      "ImapServer": "imap.gmail.com",
      "ImapPort": 993,
      "UseSsl": true,
      "Username": "ader4@domain.com",
      "Password": "use_user_secrets_in_dev"
    }
  }
}
```

### 4.2 User Secrets (Development)

**NON salvare password in appsettings.json!**

```bash
# Set password in User Secrets
dotnet user-secrets set "MailServices:Ader4:Password" "your_actual_password"

# List secrets
dotnet user-secrets list
```

### 4.3 Environment Variables (Production)

**Linux/Docker**:
```bash
export MailServices__Ader4__Password="production_password"
```

**Windows**:
```powershell
$env:MailServices__Ader4__Password = "production_password"
```

**Azure App Service**:
```
Configuration > Application Settings
Name: MailServices__Ader4__Password
Value: production_password
```

---

## Step 5: Registrazione

### 5.1 Auto-registrazione (Consigliato)

Il sistema **auto-registra** tutti gli handler tramite **Assembly Scanning**:

```csharp
// File: BlazorDematReports/Program.cs

// ? Registrazione automatica di tutti gli IMailImportHandler
builder.Services.Scan(scan => scan
    .FromAssembliesOf(typeof(IMailImportHandler))
    .AddClasses(classes => classes.AssignableTo<IMailImportHandler>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());

// ? Il registry li raccoglie automaticamente
builder.Services.AddSingleton<IUnifiedHandlerRegistry, UnifiedHandlerRegistry>();
```

**Nessuna modifica necessaria** - il nuovo handler viene rilevato automaticamente! ?

### 5.2 Registrazione Manuale (Opzionale)

Se preferisci controllo esplicito:

```csharp
builder.Services.AddTransient<IMailImportHandler, Hera16EwsHandler>();
builder.Services.AddTransient<IMailImportHandler, Ader4Handler>();
// Aggiungi altri handler...
```

---

## Step 6: Creazione Task

### Opzione A: Da UI (Consigliato)

1. Apri `/procedure-lavorazioni/edit/{id}`
2. Vai al tab **"Servizi Mail"**
3. Click **"Aggiungi Servizio Mail"**
4. Seleziona **"ADER4"** dal dropdown
5. Seleziona tipo task:
   - **Giornaliero**: Eseguito ogni giorno alla stessa ora
   - **Temporizzato**: Specifica orario preciso (es. 08:00)
   - **Mensile**: Primo giorno del mese
6. Se temporizzato, imposta orario
7. Click **"Aggiungi Servizio"**
8. Click **"Salva Modifiche"** nella floating action bar

### Opzione B: SQL Diretto

```sql
-- 1. Verifica ID procedura
SELECT IdproceduraLavorazione, NomeProcedura 
FROM ProcedureLavorazioni
WHERE NomeProcedura LIKE '%ADER%';

-- 2. Crea fase (se non esiste)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione,
    IdProceduraLavorazione,
    FlagDataReading,
    FlagGraficoDocumenti
) VALUES (
    1,   -- Fase "PROCEDURA_COMPLETA"
    15,  -- ID procedura ADER4
    1,   -- Abilita DataReading
    0    -- Grafico disabilitato
);

-- 3. Crea task con MailServiceCode
INSERT INTO TaskDaEseguire (
    IdTask,                    -- Tipo task
    IdLavorazioneFaseDateReading,
    MailServiceCode,           -- ? Campo chiave per mail service
    TimeTask,
    GiorniPrecedenti,
    Enabled,
    Stato,
    DataStato,
    IdTaskHangFire
) VALUES (
    2,                         -- 2 = Temporizzato
    SCOPE_IDENTITY(),          -- ID fase creata sopra
    'ADER4',                   -- ? ServiceCode
    '08:00:00',                -- Orario esecuzione
    1,                         -- Range dati (giorni precedenti)
    1,                         -- Abilitato
    'CONFIGURED',              -- Stato iniziale
    GETDATE(),
    'mail_ader4_daily_08:00'   -- Hangfire job ID
);
```

---

## Step 7: Sync Hangfire

Dopo aver creato/modificato task, **sincronizza Hangfire**:

### Da UI (Automatico)

Quando salvi modifiche da `/procedure-lavorazioni/edit/{id}`, viene chiamato automaticamente:

```csharp
await ProductionScheduler.SyncAllAsync();
```

### Manualmente (C#)

```csharp
[Inject] 
private IProductionJobScheduler _productionScheduler { get; set; }

private async Task SyncJobsAsync()
{
    try
    {
        await _productionScheduler.SyncAllAsync();
        _snackbar.Add("Job Hangfire sincronizzati", Severity.Success);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore sync Hangfire");
        _snackbar.Add($"Errore: {ex.Message}", Severity.Error);
    }
}
```

### Verifica in Hangfire Dashboard

1. Vai a `/hangfire`
2. Tab **"Recurring jobs"**
3. Verifica job `mail_ader4_daily_08:00`:
   - ? **Next execution**: Prossima esecuzione prevista
   - ? **Last execution**: Ultima esecuzione (se già eseguito)
   - ? **CRON**: `0 8 * * *` (ogni giorno alle 08:00)

---

## Step 8: Test e Monitoring

### 8.1 Test Manuale

```csharp
// In una pagina Blazor o controller
[Inject] private IMailImportService _mailService { get; set; }
[Inject] private ISnackbar _snackbar { get; set; }

private async Task TestAder4ManuallyAsync()
{
    try
    {
        _snackbar.Add("Inizio test ADER4...", Severity.Info);
        
        var result = await _mailService.ProcessAder4Async();
        
        _snackbar.Add($"? Successo! Processate {result} righe", Severity.Success);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore test ADER4");
        _snackbar.Add($"? Errore: {ex.Message}", Severity.Error);
    }
}
```

### 8.2 Monitoring da UI

**Tab "Monitoring" in PageEditProcedura**:

1. Vai a `/procedure-lavorazioni/edit/{id}`
2. Tab **"Monitoring"**
3. Visualizza:
   - **KPI Cards**: Fasi totali, Task totali, Servizi Mail
   - **Distribuzione Stati**: Grafico CONFIGURED/COMPLETED/ERROR
   - **DataGrid Fasi**: Stati task per fase

**Colori stati**:
- ?? **CONFIGURED** - Task configurato, mai eseguito
- ?? **COMPLETED** - Ultima esecuzione OK
- ?? **ERROR** - Ultima esecuzione fallita

### 8.3 Log Database

Query diagnostiche:

```sql
-- Ultimi 10 task eseguiti
SELECT TOP 10 
    DataAggiornamento,
    Lavorazione,
    FaseLavorazione,
    DescrizioneEsito,
    Risultati,
    EsitoLetturaDato
FROM TaskDataReadingAggiornamento
WHERE Lavorazione LIKE '%ADER4%'
ORDER BY DataAggiornamento DESC;

-- Task con errori
SELECT 
    t.IdTaskDaEseguire,
    t.MailServiceCode,
    t.Stato,
    t.LastError,
    t.DataStato
FROM TaskDaEseguire t
WHERE t.Stato = 'ERROR'
  AND t.MailServiceCode = 'ADER4';

-- Produzione inserita oggi
SELECT 
    Operatore,
    DataLavorazione,
    SUM(Documenti) as TotDocumenti,
    SUM(Fogli) as TotFogli,
    SUM(Pagine) as TotPagine
FROM ProduzioneOperatore
WHERE DataInserimento >= CAST(GETDATE() AS DATE)
  AND IdProceduraLavorazione = 15  -- ADER4
GROUP BY Operatore, DataLavorazione
ORDER BY DataLavorazione DESC;
```

### 8.4 Monitoring Logs

**NLog / Serilog**:

```csharp
// Filtra log per servizio
_logger.LogInformation("[ADER4] Messaggio importante");
```

**Visualizza log**:
```bash
# In Development
tail -f logs/app-{date}.log | grep "\[ADER4\]"

# In Docker
docker logs -f container_name | grep "\[ADER4\]"
```

---

## Templates per Protocolli

### Template 1: Exchange Web Services (EWS)

**Caso d'uso**: Office 365, Exchange Server 2013+

```csharp
using Microsoft.Exchange.WebServices.Data;

public async Task<int> ProcessEquitaliaEwsAsync(CancellationToken ct)
{
    var service = new ExchangeService(ExchangeVersion.Exchange2016);
    
    // Autenticazione Basic
    service.Credentials = new WebCredentials(username, password);
    service.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

    // O con OAuth2
    // service.Credentials = new OAuthCredentials(accessToken);

    // Filtro email
    var filter = new SearchFilter.SearchFilterCollection(
        LogicalOperator.And,
        new SearchFilter.IsEqualTo(EmailMessageSchema.IsRead, false),
        new SearchFilter.ContainsSubstring(EmailMessageSchema.Subject, "Equitalia"),
        new SearchFilter.HasAttachment(EmailMessageSchema.HasAttachments, true)
    );

    var view = new ItemView(100);
    var results = await service.FindItems(WellKnownFolderName.Inbox, filter, view);

    int processed = 0;

    foreach (var item in results)
    {
        var email = await EmailMessage.Bind(
            service, 
            item.Id, 
            new PropertySet(BasePropertySet.FirstClassProperties, 
                           EmailMessageSchema.Attachments));

        // Processa allegati
        foreach (var attachment in email.Attachments)
        {
            if (attachment is FileAttachment fileAttachment)
            {
                await fileAttachment.Load();
                
                byte[] fileBytes = fileAttachment.Content;
                string fileName = fileAttachment.Name;
                
                // Parse CSV/Excel/PDF
                var rows = await ParseAttachmentAsync(fileBytes, fileName, ct);
                processed += rows;
            }
        }

        // Marca come letta
        email.IsRead = true;
        await email.Update(ConflictResolutionMode.AutoResolve);
    }

    return processed;
}
```

### Template 2: IMAP (MailKit)

**Caso d'uso**: Gmail, Yahoo, server IMAP custom

```csharp
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

public async Task<int> ProcessImapServiceAsync(CancellationToken ct)
{
    using var client = new ImapClient();
    
    // Connessione
    await client.ConnectAsync(imapServer, 993, useSsl: true, ct);
    
    // Autenticazione
    await client.AuthenticateAsync(username, password, ct);

    // Apri Inbox
    var inbox = client.Inbox;
    await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

    // Cerca email
    var query = SearchQuery.NotSeen
        .And(SearchQuery.SubjectContains("Keyword"))
        .And(SearchQuery.DeliveredAfter(DateTime.Today.AddDays(-7)));
    
    var uids = await inbox.SearchAsync(query, ct);

    int processed = 0;

    foreach (var uid in uids)
    {
        var message = await inbox.GetMessageAsync(uid, ct);

        // Processa corpo email o allegati
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            using var stream = new MemoryStream();
            await attachment.Content.DecodeToAsync(stream, ct);
            
            // Parse allegato
            var rows = await ParseAttachmentStreamAsync(stream, ct);
            processed += rows;
        }

        // Marca come letta
        await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true, ct);
    }

    await client.DisconnectAsync(quit: true, ct);
    
    return processed;
}
```

### Template 3: POP3 (MailKit)

**Caso d'uso**: Server legacy, alcuni provider email

```csharp
using MailKit.Net.Pop3;

public async Task<int> ProcessPop3ServiceAsync(CancellationToken ct)
{
    using var client = new Pop3Client();
    
    // Connessione
    await client.ConnectAsync(pop3Server, 995, useSsl: true, ct);
    
    // Autenticazione
    await client.AuthenticateAsync(username, password, ct);

    var count = await client.GetMessageCountAsync(ct);
    
    _logger.LogInformation("Trovate {Count} email in POP3", count);

    int processed = 0;

    for (int i = 0; i < count; i++)
    {
        var message = await client.GetMessageAsync(i, ct);

        // Filtra per subject
        if (!message.Subject.Contains("MyKeyword"))
            continue;

        // Processa messaggio
        var rows = await ProcessMessageAsync(message, ct);
        processed += rows;

        // ?? POP3 elimina email dopo download!
        await client.DeleteMessageAsync(i, ct);
    }

    await client.DisconnectAsync(quit: true, ct);
    
    return processed;
}
```

---

## Esempi Pratici

### Esempio 1: ADER4 Verona + Genova

**Scenario**: Due centri diversi con configurazione separata

```csharp
public async Task<int> ProcessAder4Async(CancellationToken ct)
{
    var totalRows = 0;
    
    // Processa Verona
    totalRows += await ProcessAder4ByCentroAsync("VERONA", ct);
    
    // Processa Genova
    totalRows += await ProcessAder4ByCentroAsync("GENOVA", ct);
    
    return totalRows;
}

private async Task<int> ProcessAder4ByCentroAsync(string centro, CancellationToken ct)
{
    _logger.LogInformation("[ADER4-{Centro}] Inizio elaborazione", centro);
    
    // Configurazione specifica per centro
    var config = _configuration.GetSection($"MailServices:Ader4:{centro}");
    var imapServer = config["ImapServer"];
    var username = config["Username"];
    var password = config["Password"];
    
    using var client = new ImapClient();
    await client.ConnectAsync(imapServer, 993, true, ct);
    await client.AuthenticateAsync(username, password, ct);

    var inbox = client.Inbox;
    await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

    // Filtra email per centro (es. da Subject o From)
    var filter = SearchQuery.NotSeen
        .And(SearchQuery.SubjectContains($"ADER4_{centro}"));
    
    var uids = await inbox.SearchAsync(filter, ct);
    
    _logger.LogInformation("[ADER4-{Centro}] Trovate {Count} email", centro, uids.Count);

    int processed = 0;

    foreach (var uid in uids)
    {
        var message = await inbox.GetMessageAsync(uid, ct);
        
        // Processa allegati CSV
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            if (attachment.FileName?.EndsWith(".csv") == true)
            {
                using var stream = new MemoryStream();
                await attachment.Content.DecodeToAsync(stream, ct);
                var csvContent = Encoding.UTF8.GetString(stream.ToArray());
                
                // Salva con IdCentro specifico
                var rows = await SaveCsvWithCentroAsync(csvContent, centro, ct);
                processed += rows;
            }
        }

        await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true, ct);
    }

    await client.DisconnectAsync(quit: true, ct);
    
    _logger.LogInformation("[ADER4-{Centro}] Elaborazione completata: {Rows} righe", 
        centro, processed);
    
    return processed;
}
```

**Configurazione**:

```json
{
  "MailServices": {
    "Ader4": {
      "Verona": {
        "ImapServer": "imap.verona.it",
        "Username": "ader4.vr@domain.com",
        "Password": "set_in_user_secrets"
      },
      "Genova": {
        "ImapServer": "imap.genova.it",
        "Username": "ader4.ge@domain.com",
        "Password": "set_in_user_secrets"
      }
    }
  }
}
```

### Esempio 2: Parsing CSV Avanzato

Con **CsvHelper** library:

```csharp
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

private async Task<int> ParseCsvWithCsvHelperAsync(string csvContent, CancellationToken ct)
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        HasHeaderRecord = true,
        MissingFieldFound = null,
        BadDataFound = context => 
            _logger.LogWarning("Riga CSV invalida alla riga {Row}", context.RawRecord)
    };

    using var reader = new StringReader(csvContent);
    using var csv = new CsvReader(reader, config);
    
    var records = csv.GetRecords<ProduzioneOperatoreCsv>();
    
    using var context = await _dbContextFactory.CreateDbContextAsync(ct);
    
    int rows = 0;
    
    foreach (var record in records)
    {
        var produzione = new ProduzioneOperatore
        {
            Operatore = record.Operatore,
            DataLavorazione = record.DataLavorazione,
            Documenti = record.Documenti,
            Fogli = record.Fogli,
            Pagine = record.Pagine,
            IdProceduraLavorazione = record.IdProcedura,
            DataInserimento = DateTime.Now
        };

        context.ProduzioneOperatores.Add(produzione);
        rows++;
    }

    await context.SaveChangesAsync(ct);
    
    return rows;
}

// DTO per mapping CSV
public class ProduzioneOperatoreCsv
{
    public string Operatore { get; set; }
    public DateTime DataLavorazione { get; set; }
    public int Documenti { get; set; }
    public int Fogli { get; set; }
    public int Pagine { get; set; }
    public int IdProcedura { get; set; }
}
```

---

## Troubleshooting

### Problema: Handler non rilevato

**Sintomo**: Handler non appare nel dropdown servizi mail

**Cause**:
1. ? `ServiceCode` non univoco
2. ? Handler non implementa `IMailImportHandler`
3. ? Assembly non scansionato in `Program.cs`

**Soluzione**:

```csharp
// Verifica registrazione
builder.Services.Scan(scan => scan
    .FromAssembliesOf(typeof(IMailImportHandler))  // ? Assembly corretto?
    .AddClasses(classes => classes.AssignableTo<IMailImportHandler>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());

// Debug in runtime
var handlers = serviceProvider.GetServices<IMailImportHandler>();
foreach (var handler in handlers)
{
    Console.WriteLine($"Handler: {handler.ServiceCode}");
}
```

### Problema: Autenticazione fallita

**Sintomo**: Exception `AuthenticationException` o `ImapProtocolException`

**Cause**:
1. ? Credenziali errate
2. ? App Password non configurata (Gmail, Outlook)
3. ? 2FA attivo senza token
4. ? SSL/TLS configurazione errata

**Soluzione Gmail**:

```
1. Abilita IMAP: Settings > Forwarding and POP/IMAP > Enable IMAP
2. Genera App Password: Google Account > Security > 2-Step Verification > App passwords
3. Usa App Password invece della password normale
```

**Soluzione Outlook/Office 365**:

```
1. Abilita IMAP: Settings > Sync email > IMAP
2. Usa OAuth2 invece di Basic Auth (deprecato)
3. Registra app in Azure AD per OAuth2
```

### Problema: Email non trovate

**Sintomo**: `uids.Count = 0` anche se ci sono email

**Debug query**:

```csharp
// Test incrementale filtri
var allUids = await inbox.SearchAsync(SearchQuery.All, ct);
_logger.LogInformation("Totale email: {Count}", allUids.Count);

var unseenUids = await inbox.SearchAsync(SearchQuery.NotSeen, ct);
_logger.LogInformation("Email non lette: {Count}", unseenUids.Count);

var subjectUids = await inbox.SearchAsync(
    SearchQuery.SubjectContains("ADER"), ct);
_logger.LogInformation("Email con subject ADER: {Count}", subjectUids.Count);

// Verifica flag Seen
foreach (var uid in allUids.Take(5))
{
    var message = await inbox.GetMessageAsync(uid, ct);
    var flags = await inbox.GetFlagsAsync(uid, ct);
    _logger.LogInformation("Email: {Subject}, Flags: {Flags}", 
        message.Subject, flags);
}
```

### Problema: Allegati non scaricati

**Sintomo**: `message.Attachments.Count() = 0`

**Soluzione**:

```csharp
// ? SBAGLIATO - non carica allegati
var message = await inbox.GetMessageAsync(uid, ct);

// ? CORRETTO - carica allegati esplicitamente
var message = await inbox.GetMessageAsync(uid, ct, 
    new CancellationToken(), 
    new Progress<ImapFolderFetch>());

// O con MailKit 3.0+
var message = await inbox.GetMessageAsync(uid, ct);
// Gli allegati sono già caricati
```

### Problema: Hangfire job non eseguito

**Sintomo**: Job in dashboard ma mai eseguito

**Verifica**:

```sql
-- Check job in Hangfire DB
SELECT * FROM [Hangfire].[Job]
WHERE Arguments LIKE '%ADER4%';

-- Check recurring job
SELECT * FROM [Hangfire].[Set]
WHERE [Key] = 'recurring-jobs';
```

**Soluzione**:

```csharp
// Re-sync tutti i job
await _productionScheduler.SyncAllAsync();

// Trigger manuale da Hangfire Dashboard
// /hangfire > Recurring Jobs > [Nome Job] > Trigger now
```

### Problema: Timeout IMAP

**Sintomo**: `OperationCanceledException` o `TimeoutException`

**Soluzione**:

```csharp
client.Timeout = 60000; // 60 secondi (default 100s)

// O aumenta timeout operazione
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
await client.ConnectAsync(server, port, useSsl, cts.Token);
```

---

## Checklist Finale

Prima del deployment in produzione:

- [ ] **Handler**
  - [ ] `ServiceCode` univoco e in `JobConstants`
  - [ ] Implementa `IMailImportHandler`
  - [ ] Gestisce `CancellationToken`
  - [ ] Logging completo con prefisso `[ServiceCode]`

- [ ] **Business Logic**
  - [ ] Metodo `Process{Service}Async()` implementato
  - [ ] Parsing robusto (gestisce righe invalide)
  - [ ] Transazioni DB per atomicità
  - [ ] Marca email come lette dopo successo

- [ ] **Configurazione**
  - [ ] Parametri in `appsettings.json`
  - [ ] Password in User Secrets (Dev)
  - [ ] Password in Environment Variables (Prod)
  - [ ] Nessuna credenziale in chiaro nel codice

- [ ] **Test**
  - [ ] Test manuale esecuzione
  - [ ] Verifica dati in DB
  - [ ] Test con email malformate
  - [ ] Test timeout e cancellazione

- [ ] **Monitoring**
  - [ ] Job visibile in Hangfire Dashboard
  - [ ] Stati tracciati in `TaskDataReadingAggiornamento`
  - [ ] Alert configurati per errori
  - [ ] Log centralizzati (NLog/Serilog)

- [ ] **Sicurezza**
  - [ ] SSL/TLS abilitato
  - [ ] Credenziali criptate
  - [ ] App Password per 2FA
  - [ ] OAuth2 se disponibile

---

## Risorse Utili

### Documentazione

- **MailKit**: https://github.com/jstedfast/MailKit
- **MimeKit**: https://github.com/jstedfast/MimeKit
- **Exchange EWS**: https://github.com/OfficeDev/ews-managed-api
- **Hangfire**: https://docs.hangfire.io/

### Esempi Codice

- **MailKit Samples**: https://github.com/jstedfast/MailKit/tree/master/samples
- **EWS Samples**: https://docs.microsoft.com/en-us/exchange/client-developer/

### Support

- **GitHub Issues**: https://github.com/jstedfast/MailKit/issues
- **Stack Overflow**: Tag `mailkit`, `mimekit`, `exchange-ews`

---

## Conclusione

Seguendo questa guida, puoi implementare handler per qualsiasi servizio mail (IMAP, POP3, EWS) nel sistema **BlazorDematReports**.

**Vantaggi architettura**:
- ? **Unified Pattern** - Stessa interfaccia per SQL e Mail
- ? **Auto-discovery** - Registrazione automatica handler
- ? **Monitoring integrato** - Stati visibili in UI
- ? **Scheduling robusto** - Hangfire con retry automatici
- ? **Extensibility** - Aggiungi handler senza toccare core

**Prossimi step**:
1. Implementa il tuo handler seguendo i template
2. Configura credenziali in User Secrets
3. Crea task da UI
4. Monitora esecuzioni nel tab Monitoring

---

*Versione: 1.0 - Ultimo aggiornamento: 2024*
