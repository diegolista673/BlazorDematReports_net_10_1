# ServiceMail - Documentazione

## Panoramica
`ServiceMail` è un servizio per la gestione dell'invio di email tramite FluentEmail e per l'interrogazione delle configurazioni dei servizi mail (tipo EmailCSV) dalla tabella `ConfigurazioneFontiDati`.

## Funzionalità

### 1. Conteggio Servizi Mail
```csharp
// Conta tutti i servizi mail (EmailCSV) attivi
int count = await ServiceWrapper.ServiceMail.GetMailServicesCountAsync();

// Conta servizi mail per una specifica procedura
int countByProc = await ServiceWrapper.ServiceMail.GetMailServicesCountByProceduraAsync(idProcedura);
```

### 2. Recupero Configurazioni
```csharp
// Ottieni tutte le configurazioni EmailCSV attive
List<ConfigurazioneFontiDati> allConfigs = await ServiceWrapper.ServiceMail.GetMailServicesAsync();

// Ottieni configurazioni per una specifica procedura
List<ConfigurazioneFontiDati> configsByProc = await ServiceWrapper.ServiceMail.GetMailServicesByProceduraAsync(idProcedura);
```

### 3. Invio Email
```csharp
// Invio semplice
bool success = await ServiceWrapper.ServiceMail.SendEmailAsync(
    to: "destinatario@example.com",
    subject: "Oggetto Email",
    body: "Corpo del messaggio"
);

// Invio completo con mittente personalizzato
bool success = await ServiceWrapper.ServiceMail.SendEmailAsync(
    from: "mittente@example.com",
    to: "destinatario@example.com",
    toName: "Nome Destinatario",
    subject: "Oggetto Email",
    body: "Corpo del messaggio"
);
```

## Registrazione nel DI Container

**IMPORTANTE**: Per utilizzare `ServiceMail` è necessario registrare `IFluentEmail` nel container DI.

### Configurazione in `Program.cs`:
```csharp
// Configurazione base FluentEmail
builder.Services
    .AddFluentEmail("default@example.com")
    .AddRazorRenderer()
    .AddSmtpSender("smtp.example.com", 587);

// Oppure configurazione SMTP personalizzata
builder.Services
    .AddFluentEmail("noreply@postel.it", "Sistema BlazorDematReports")
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient
    {
        Host = builder.Configuration["Email:SmtpHost"],
        Port = int.Parse(builder.Configuration["Email:SmtpPort"]),
        EnableSsl = true,
        Credentials = new NetworkCredential(
            builder.Configuration["Email:Username"],
            builder.Configuration["Email:Password"]
        )
    });
```

### Abilitazione in ServiceWrapper:
Una volta registrato `IFluentEmail`, decommentare nel costruttore di `ServiceWrapper`:
```csharp
// Aggiungere IFluentEmail al costruttore
public ServiceWrapper(
    IMapper mapper, 
    ConfigUser configUser, 
    IDbContextFactory<DematReportsContext> contextFactory, 
    ILoggerFactory loggerFactory,
    IFluentEmail fluentEmail)  // <-- Aggiungere questo parametro
{
    // ... altre inizializzazioni ...
    
    // Decommentare questa riga:
    _serviceMail = new Lazy<IServiceMail>(() => 
        new ServiceMail(mapper, configUser, contextFactory, fluentEmail, loggerFactory.CreateLogger<ServiceMail>()));
}
```

## Esempio d'uso completo

```csharp
@inject IServiceWrapper ServiceWrapper

private async Task InviaNotificaAsync(int idProcedura)
{
    try
    {
        // Verifica quanti servizi mail sono configurati
        var count = await ServiceWrapper.ServiceMail.GetMailServicesCountByProceduraAsync(idProcedura);
        
        if (count == 0)
        {
            Snackbar.Add("Nessun servizio mail configurato per questa procedura", Severity.Warning);
            return;
        }
        
        // Ottieni le configurazioni
        var configs = await ServiceWrapper.ServiceMail.GetMailServicesByProceduraAsync(idProcedura);
        
        // Invia email di notifica
        bool success = await ServiceWrapper.ServiceMail.SendEmailAsync(
            to: "admin@example.com",
            subject: $"Notifica Procedura {idProcedura}",
            body: $"La procedura ha {count} servizi mail attivi. Configurazioni: {string.Join(", ", configs.Select(c => c.MailServiceCode))}"
        );
        
        if (success)
            Snackbar.Add("Email inviata con successo", Severity.Success);
        else
            Snackbar.Add("Errore nell'invio email", Severity.Error);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Errore durante l'invio notifica");
        Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
    }
}
```

## Note
- Tutti i metodi utilizzano `QueryLoggingHelper.LogQueryExecution(logger)` per tracciare le query
- Le configurazioni restituite includono solo quelle con `FlagAttiva = true` e `TipoFonte = "EmailCSV"`
- Gli errori vengono loggati automaticamente e i metodi restituiscono valori di default sicuri (0, lista vuota, false)
