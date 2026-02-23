# Configurazione FluentEmail - BlazorDematReports

## ? Registrazione Completata

FluentEmail è stato registrato con successo nel `Program.cs` e `ServiceMail` è ora completamente operativo.

## ?? Configurazione Email (appsettings.json)

Aggiungi la seguente sezione al tuo `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": "587",
    "EnableSsl": "true",
    "Username": "noreply@example.com",
    "Password": "YourPasswordHere",
    "DefaultFrom": "noreply@postel.it",
    "DefaultFromName": "Sistema BlazorDematReports"
  }
}
```

### Parametri

| Parametro | Descrizione | Default |
|-----------|-------------|---------|
| `SmtpHost` | Server SMTP | `localhost` |
| `SmtpPort` | Porta SMTP | `587` |
| `EnableSsl` | Abilita SSL/TLS | `true` |
| `Username` | Username SMTP (opzionale) | - |
| `Password` | Password SMTP (opzionale) | - |
| `DefaultFrom` | Email mittente predefinita | `noreply@blazordemat.local` |
| `DefaultFromName` | Nome mittente predefinito | `Sistema BlazorDematReports` |

## ?? User Secrets per Development

Per **non** committare password sensibili:

```bash
cd BlazorDematReports
dotnet user-secrets set "Email:Username" "your-email@example.com"
dotnet user-secrets set "Email:Password" "YourSecurePassword"
```

## ?? Configurazione Production (Variabili Ambiente)

In produzione (IIS, Docker, Azure), usa le variabili d'ambiente:

### Windows (IIS)
```cmd
setx Email__SmtpHost "smtp.postel.it"
setx Email__SmtpPort "587"
setx Email__Username "noreply@postel.it"
setx Email__Password "SecurePassword"
```

### Linux/Docker
```bash
export Email__SmtpHost="smtp.postel.it"
export Email__SmtpPort="587"
export Email__Username="noreply@postel.it"
export Email__Password="SecurePassword"
```

### Azure App Service
Aggiungi le variabili nell'App Configuration:
- `Email__SmtpHost`
- `Email__SmtpPort`
- `Email__Username`
- `Email__Password`

## ?? Pacchetti NuGet Richiesti

Assicurati di avere installati i seguenti pacchetti:

```xml
<PackageReference Include="FluentEmail.Core" Version="3.0.2" />
<PackageReference Include="FluentEmail.Smtp" Version="3.0.2" />
```

Installa se mancanti:
```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
```

## ?? Test Invio Email

### Test semplice via ServiceWrapper

```csharp
@inject IServiceWrapper ServiceWrapper

private async Task TestEmailAsync()
{
    bool success = await ServiceMail.SendEmailAsync(
        to: "test@example.com",
        subject: "Test Email da BlazorDematReports",
        body: "Questo è un test di invio email."
    );
    
    if (success)
        Snackbar.Add("Email inviata!", Severity.Success);
    else
        Snackbar.Add("Errore invio email", Severity.Error);
}
```

### Test con mittente personalizzato

```csharp
bool success = await ServiceMail.SendEmailAsync(
    from: "custom@postel.it",
    to: "test@example.com",
    toName: "Destinatario Test",
    subject: "Test con mittente custom",
    body: "Email inviata con mittente personalizzato"
);
```

## ?? Debug/Troubleshooting

### Test SMTP Server Locale (Development)

Per testare senza server SMTP reale, usa **Papercut SMTP**:
1. Scarica: https://github.com/ChangemakerStudios/Papercut-SMTP/releases
2. Esegui Papercut
3. Configura in `appsettings.Development.json`:
```json
{
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": "25",
    "EnableSsl": "false"
  }
}
```

### Log Errori

Gli errori di invio vengono loggati automaticamente in:
- `logs/nlog-own-{yyyy-MM-dd}.log`
- Categoria: `ServiceMail`

Esempio log:
```
2025-01-09 10:30:45 [INFO] ServiceMail - Invio email a test@example.com con oggetto: Test
2025-01-09 10:30:46 [ERROR] ServiceMail - Eccezione durante l'invio email: Connection refused
```

## ?? Monitoraggio Servizi Mail

Conta servizi mail configurati:

```csharp
// Totale servizi EmailCSV attivi
int totalServices = await ServiceMail.GetMailServicesCountAsync();

// Servizi per procedura specifica
int procServices = await ServiceMail.GetMailServicesCountByProceduraAsync(idProcedura);

// Configurazioni complete
List<ConfigurazioneFontiDati> configs = await ServiceMail.GetMailServicesAsync();
```

## ?? Configurazioni Avanzate

### SMTP con autenticazione personalizzata

Se il tuo server SMTP richiede configurazioni particolari, modifica direttamente in `Program.cs > RegisterFluentEmail()`.

### Template HTML (Razor)

FluentEmail supporta template Razor. Per abilitarli:

```bash
dotnet add package FluentEmail.Razor
```

Poi in `Program.cs`:
```csharp
emailBuilder.AddRazorRenderer();
```

## ? Checklist Deployment

Prima del deployment in produzione:

- [ ] Configurate le variabili ambiente SMTP
- [ ] Testato invio email in ambiente staging
- [ ] Verificata firewall rule per porta SMTP (587/465)
- [ ] Configurato SPF/DKIM sul dominio mittente
- [ ] Impostata autenticazione SMTP se richiesta
- [ ] Verificato limite rate del server SMTP

## ?? Supporto

Per problemi di configurazione:
1. Verifica i log NLog
2. Testa connessione SMTP con tool esterni (telnet/openssl)
3. Verifica credenziali e permessi server SMTP
