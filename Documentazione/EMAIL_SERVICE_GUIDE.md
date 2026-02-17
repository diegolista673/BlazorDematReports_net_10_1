# Guida Email Service Base (EWS)

## Panoramica

La classe `BaseEwsEmailService` fornisce funzionalità base per la gestione di email Exchange Web Services con allegati, estratta dai servizi HERA16 e ADER4.

---

## Architettura

```
BaseEwsEmailService (Abstract)
├── EwsEmailServiceConfig (Configurazione)
├── EmailProcessingResult (Risultato singola email)
├── BatchEmailProcessingResult (Risultato batch)
└── AttachmentInfo (Informazioni allegato)
    
Implementazioni Concrete:
├── Ader4EmailService
└── Hera16EmailService (da implementare)
```

---

## Funzionalità Fornite

### ✅ Gestione Exchange Web Services
- Connessione EWS con credenziali configurabili
- Ricerca email con filtri subject multipli (OR logic)
- Download allegati file
- Spostamento email in cartelle archivio

### ✅ Elaborazione CSV
- Parsing file CSV con configurazione delimiter/header
- Ritorno DataTable per elaborazione LINQ

### ✅ Estrazione Metadata
- Regex-based extraction da body email
- Dictionary estensibile per metadata custom

### ✅ Archiviazione
- Creazione archivio zip allegati
- Cleanup automatico file temporanei
- Logging strutturato

---

## Configurazione

### appsettings.json
```json
{
  "MailServices": {
    "ADER4": {
      "Username": "verona.edp",
      "Password": "use_user_secrets",
      "Domain": "postel.it",
      "ExchangeUrl": "https://postaweb.postel.it/ews/exchange.asmx",
      "SubjectVerona": "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)",
      "SubjectGenova": "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)",
      "AttachmentPattern": "EQTMN4_Scatole_Scansionate",
      "ArchiveFolder": "EQUITALIA_4"
    },
    "HERA16": {
      "Username": "verona.edp",
      "Password": "use_user_secrets",
      "Domain": "postel.it",
      "ExchangeUrl": "https://postaweb.postel.it/ews/exchange.asmx",
      "SubjectFilter": "DEMAT_HERA16",
      "AttachmentPattern": "file di produzione giornaliera",
      "ArchiveFolder": "HERA16"
    }
  }
}
```

### User Secrets (Sviluppo)
```bash
dotnet user-secrets set "MailServices:ADER4:Password" "your_password_here"
dotnet user-secrets set "MailServices:HERA16:Password" "your_password_here"
```

---

## Implementazione Servizio Custom

### Esempio: Hera16EmailService

```csharp
public sealed class Hera16EmailService : BaseEwsEmailService
{
    private readonly IConfiguration _configuration;

    public Hera16EmailService(IConfiguration configuration, ILogger<Hera16EmailService> logger)
        : base(CreateConfig(configuration), logger)
    {
        _configuration = configuration;
    }

    private static EwsEmailServiceConfig CreateConfig(IConfiguration configuration)
    {
        var mailConfig = configuration.GetSection("MailServices:HERA16");

        return new EwsEmailServiceConfig
        {
            Username = mailConfig["Username"]!,
            Password = mailConfig["Password"]!,
            Domain = mailConfig["Domain"] ?? "postel.it",
            ExchangeUrl = new Uri(mailConfig["ExchangeUrl"]!),
            SubjectFilters = new[] { mailConfig["SubjectFilter"]! },
            AttachmentPatterns = new[] { "file di produzione giornaliera*" },
            ArchiveFolderName = mailConfig["ArchiveFolder"] ?? "HERA16",
            LocalAttachmentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report", "HERA16"),
            LocalArchivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "HERA16")
        };
    }

    // Override per logica specifica HERA16
    protected override async Task ProcessAttachmentAsync(
        AttachmentInfo attachment,
        Dictionary<string, string> metadata,
        CancellationToken ct)
    {
        Logger.LogInformation("Elaborazione allegato HERA16: {FileName}", attachment.FileName);

        if (attachment.FileName.Contains("file di produzione giornaliera"))
        {
            var csvData = ReadCsvFile(attachment.LocalFilePath);
            
            // Logica specifica HERA16
            await ProcessProduzioneGiornalieraAsync(csvData, metadata, ct);
        }
    }

    private async Task ProcessProduzioneGiornalieraAsync(
        DataTable csvData,
        Dictionary<string, string> metadata,
        CancellationToken ct)
    {
        // Elaborazione dati HERA16
        // TODO: Inserimento DB, aggregazioni, etc.
        await Task.CompletedTask;
    }

    // Override metadata extraction
    protected override void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
    {
        base.ExtractMetadataFromBody(bodyText, metadata);
        
        // Metadata specifici HERA16
        ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
        ExtractMetadataField(bodyText, "Periodo di riferimento:", metadata, "DataRiferimento");
    }
}
```

---

## Registrazione Dependency Injection

### Program.cs / Startup.cs
```csharp
// Registra servizi email
services.AddTransient<Ader4EmailService>();
services.AddTransient<Hera16EmailService>();
```

---

## Utilizzo

### Handler Lavorazione
```csharp
public sealed class Ader4Handler : ILavorazioneHandler
{
    public string LavorazioneCode => LavorazioniCodes.ADER4;

    public async Task<List<DatiLavorazione>> ExecuteAsync(
        LavorazioneExecutionContext context,
        CancellationToken ct = default)
    {
        var emailService = context.ServiceProvider.GetRequiredService<Ader4EmailService>();
        
        // Processa email e allegati
        var result = await emailService.ProcessEmailsAsync(ct);
        
        _logger.LogInformation(
            "Email processate: Totali={Total}, Successi={Success}, Allegati={Attachments}",
            result.TotalEmailsFound,
            result.SuccessfulEmails.Count,
            result.TotalAttachmentsDownloaded
        );
        
        // Converti risultati in DatiLavorazione
        return ConvertToDatiLavorazione(result);
    }
}
```

### Background Service (Hangfire)
```csharp
public class Ader4EmailJob
{
    private readonly Ader4EmailService _emailService;
    private readonly ILogger<Ader4EmailJob> _logger;

    public Ader4EmailJob(Ader4EmailService emailService, ILogger<Ader4EmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [JobDisplayName("ADER4 - Import Email Produzione")]
    public async Task ExecuteAsync()
    {
        try
        {
            var result = await _emailService.ProcessEmailsAsync();
            
            if (result.FailedEmails.Any())
            {
                _logger.LogWarning(
                    "Alcune email ADER4 hanno avuto errori: {Count}",
                    result.FailedEmails.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore job ADER4 email import");
            throw;
        }
    }
}
```

---

## Metodi Override Disponibili

### 1. `ProcessAttachmentAsync`
**Quando:** Elaborazione specifica per tipo allegato (CSV, XML, etc.)

```csharp
protected override async Task ProcessAttachmentAsync(
    AttachmentInfo attachment,
    Dictionary<string, string> metadata,
    CancellationToken ct)
{
    // Logica custom per elaborazione allegato
    if (attachment.FileName.EndsWith(".csv"))
    {
        var csvData = ReadCsvFile(attachment.LocalFilePath);
        await SaveToDatabase(csvData, metadata, ct);
    }
}
```

### 2. `ExtractMetadataFromBody`
**Quando:** Regex custom per estrarre campi specifici da body email

```csharp
protected override void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
{
    base.ExtractMetadataFromBody(bodyText, metadata);
    
    // Pattern custom
    ExtractMetadataField(bodyText, "Codice Lavorazione:", metadata, "CodiceLavorazione");
    ExtractMetadataField(bodyText, "Centro:", metadata, "Centro");
}
```

### 3. `OnNoEmailsFoundAsync`
**Quando:** Notifica/alert quando nessuna email viene trovata

```csharp
protected override async Task OnNoEmailsFoundAsync(CancellationToken ct)
{
    await _notificationService.SendAlertAsync(
        "ADER4: Nessuna email produzione ricevuta oggi",
        "Verificare invio report da sistema sorgente"
    );
}
```

### 4. `BuildSearchFilter`
**Quando:** Logica ricerca email più complessa (es. filtro su date)

```csharp
protected override SearchFilter BuildSearchFilter()
{
    var baseFilter = base.BuildSearchFilter();
    
    // Aggiungi filtro data (ultimi 7 giorni)
    var dateFilter = new SearchFilter.IsGreaterThanOrEqualTo(
        ItemSchema.DateTimeReceived,
        DateTime.Now.AddDays(-7)
    );
    
    return new SearchFilter.SearchFilterCollection(
        LogicalOperator.And,
        baseFilter,
        dateFilter
    );
}
```

---

## Testing

### Unit Test Example
```csharp
public class Ader4EmailServiceTests
{
    [Fact]
    public async Task ProcessEmailsAsync_ShouldDownloadAttachments()
    {
        // Arrange
        var config = new EwsEmailServiceConfig { /* ... */ };
        var logger = new Mock<ILogger<Ader4EmailService>>();
        var service = new Ader4EmailService(config, logger.Object);

        // Act
        var result = await service.ProcessEmailsAsync();

        // Assert
        Assert.True(result.TotalEmailsFound > 0);
        Assert.True(result.SuccessfulEmails.Any());
    }
}
```

---

## Logging

Il servizio produce log strutturati con i seguenti livelli:

- **Information**: Operazioni normali (email trovate, allegati scaricati, archivio creato)
- **Warning**: Email senza allegati, pattern non matchati
- **Error**: Errori connessione EWS, parsing CSV fallito, spostamento email fallito

### Esempio Log Output
```
[INFO] Inizializzazione servizio Exchange: URL=https://postaweb.postel.it/ews/exchange.asmx
[INFO] Trovate 3 email matching filtri
[INFO] Allegato scaricato: EQTMN4_Scatole_Scansionate_20250115.csv, Size=145678 bytes, MatchesPattern=True
[INFO] Email "Report produzione (Verona)" spostata in cartella EQUITALIA_4
[INFO] Archivio zip creato: 20250115.zip, Size=98765 bytes
[INFO] Elaborazione batch completata: Totali=3, Successi=3, Errori=0, Allegati=4
```

---

## Troubleshooting

### Errore: "Cannot find EQUITALIA_4 folder"
**Soluzione:** Creare manualmente la cartella in Outlook/Exchange o verificare `ArchiveFolderName` in config.

### Errore: "Authentication failed"
**Soluzione:** Verificare credenziali in user secrets / appsettings. Controllare permessi mailbox Exchange.

### CSV parsing fallito
**Soluzione:** Verificare delimiter configurato (`;` default). Controllare encoding file CSV (UTF-8 vs Windows-1252).

### Allegati non scaricati
**Soluzione:** Controllare `AttachmentPatterns` in config. Usare wildcard `*` per pattern flessibili.

---

## Roadmap Miglioramenti

- [ ] Supporto OAuth 2.0 Modern Auth (deprecazione Basic Auth Exchange)
- [ ] Retry automatico con backoff esponenziale per errori temporanei
- [ ] Supporto lettura file XML (oltre CSV)
- [ ] Notifiche push (email/Teams) per errori elaborazione
- [ ] Dashboard monitoring allegati processati (Blazor component)
- [ ] Unit test coverage > 80%

---

## Contatti

**Maintainer:** Team BlazorDematReports  
**Repo:** `https://github.com/diegolista673/BlazorDematReports_net_10_1`  
**Documentazione:** `/Documentazione/REQUISITI_SISTEMA.md`
