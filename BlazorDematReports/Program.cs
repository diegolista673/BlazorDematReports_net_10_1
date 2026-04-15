using BlazorDematReports.Application;
using BlazorDematReports.Components;
using BlazorDematReports.Components.Dialog;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Mapping;
using BlazorDematReports.Core.DataReading.Infrastructure;
using BlazorDematReports.Core.DataReading.Interfaces;
using BlazorDematReports.Core.DataReading.Services;
using BlazorDematReports.Core.Handlers.LavorazioniHandlers;
using BlazorDematReports.Core.Handlers.MailHandlers;
using BlazorDematReports.Core.Handlers.MailHandlers.Ader4;
using BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;
using BlazorDematReports.Core.Handlers.Registry;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Services;
using BlazorDematReports.Core.Services.DataService;
using BlazorDematReports.Core.Services.ProcedureEdit;
using BlazorDematReports.Core.Services.Validation;
using BlazorDematReports.Core.Services.Wizard;
using BlazorDematReports.Core.Utility;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Helpers;
using BlazorDematReports.Services;
using BlazorDematReports.Services.Authentication;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using NLog.Web;

/// <summary>
/// Classe principale dell'applicazione che gestisce la configurazione e l'avvio del sistema.
/// </summary>
public static class Program
{
    private static WebApplication _app = default!;

    /// <summary>
    /// Punto di ingresso principale dell'applicazione.
    /// </summary>
    /// <param name="args">Argomenti da riga di comando.</param>
    /// <returns>Task asincrono per l'esecuzione dell'applicazione.</returns>
    public static async Task Main(string[] args)
    {
        NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
        var bootstrapLogger = NLog.LogManager.GetCurrentClassLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Carica User Secrets in Development, ProductionSim e Production-NoActiveDirectory (debug locale)
            if (builder.Environment.IsDevelopment()
                || builder.Environment.IsEnvironment("ProductionSim")
                || builder.Environment.IsEnvironment("Production-NoActiveDirectory"))
            {
                builder.Configuration.AddUserSecrets(typeof(Program).Assembly);
            }


// Static Web Assets (MudBlazor CSS/JS, _framework/blazor.web.js, contenuti NuGet):
// in Development sono abilitati automaticamente; in ProductionSim e Production-NoActiveDirectory
// (debug locale fuori dalla cartella publish) vanno abilitati esplicitamente.
if (builder.Environment.IsEnvironment("ProductionSim")
    || builder.Environment.IsEnvironment("Production-NoActiveDirectory"))
            {
                builder.WebHost.UseStaticWebAssets();
            }


// In Production e ProductionSim carica variabili ambiente
            // Esempio: ConnectionStrings__DematReportsContext oppure DEMAT_ConnectionStrings__DematReportsContext
            if (!builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddEnvironmentVariables();
            }

            ConfigureLogging(builder);
            RegisterFramework(builder);
            RegisterLoginSettings(builder);
            RegisterDb(builder);
            RegisterMappers(builder);     // ← prima di RegisterServices che usa i mapper
            RegisterServices(builder);
            RegisterHangfire(builder);
            RegisterAuthentication(builder);
            RegisterMudBlazor(builder);

            var app = builder.Build();
            InitializeApp(app);
            await RunStartupDiagnosticsAsync(app, bootstrapLogger);
            MapMiddleware(app);
            await SyncRecurringJobsAsync(app);
            ScheduleSystemJobs();
            ScheduleMailIngestion();

            _app = app;
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            bootstrapLogger.Error(ex, "Fatal exception on startup");
            throw;
        }
        finally { NLog.LogManager.Shutdown(); }
    }

    #region Configuration Helpers
    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Host.UseNLog();
    }

    private static void RegisterFluentEmail(WebApplicationBuilder builder)
    {
        // Configurazione FluentEmail per invio email
        var emailConfig = builder.Configuration.GetSection("Email");
        var smtpHost = emailConfig["SmtpHost"] ?? "localhost";
        var smtpPort = int.TryParse(emailConfig["SmtpPort"], out var port) ? port : 587;
        var enableSsl = bool.TryParse(emailConfig["EnableSsl"], out var ssl) && ssl;
        var username = emailConfig["Username"];
        var password = emailConfig["Password"];
        var defaultFrom = emailConfig["DefaultFrom"] ?? "noreply@blazordemat.local";
        var defaultFromName = emailConfig["DefaultFromName"] ?? "Sistema BlazorDematReports";

        var emailBuilder = builder.Services
            .AddFluentEmail(defaultFrom, defaultFromName);

        // Configurazione SMTP con System.Net.Mail.SmtpClient
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            // SMTP con autenticazione
            emailBuilder.AddSmtpSender(new System.Net.Mail.SmtpClient
            {
                Host = smtpHost,
                Port = smtpPort,
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(username, password)
            });
        }
        else
        {
            // SMTP senza autenticazione (per sviluppo/test)
            emailBuilder.AddSmtpSender(new System.Net.Mail.SmtpClient
            {
                Host = smtpHost,
                Port = smtpPort,
                EnableSsl = enableSsl
            });
        }

        NLog.LogManager.GetCurrentClassLogger().Info(
            "FluentEmail configurato: Host={0}, Port={1}, EnableSsl={2}, From={3}",
            smtpHost, smtpPort, enableSsl, defaultFrom);
    }

    private static void RegisterLoginSettings(WebApplicationBuilder builder)
    {
        builder.Services.Configure<LoginSettings>(
            builder.Configuration.GetSection("LoginSettings"));

        // Configurazione Active Directory
        builder.Services.Configure<ActiveDirectorySettings>(
            builder.Configuration.GetSection("ActiveDirectory"));

        // Registrazione servizi di autenticazione
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Registrazione condizionale del servizio Active Directory
        if (builder.Environment.IsDevelopment())
        {
            // In ambiente Development usa implementazione mock senza AD
            builder.Services.AddScoped<IActiveDirectoryService, MockActiveDirectoryService>();
        }
        else
        {
            // In Production e ProductionSim usa implementazione reale con AD
            builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
        }
    }

    private static void RegisterFramework(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddServerSideBlazor().AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCascadingAuthenticationState();

        // Root directory per Razor Pages spostato sotto /Components/Pages così /Account/Login viene trovato
        builder.Services.AddRazorPages(o => { o.RootDirectory = "/Components/Pages"; });
        builder.Services.AddControllers();
    }

    private static void RegisterDb(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<DematReportsContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DematReportsContext"))
                   .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
    }

    private static void RegisterHangfire(WebApplicationBuilder builder)
    {
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete });
        builder.Services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new Hangfire.SqlServer.SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromMinutes(1),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                EnableHeavyMigrations = true,
                PrepareSchemaIfNecessary = true,
                SchemaName = "HangFire"
            }));

        builder.Services.AddHangfireServer(options =>
        {
            // Code "critical": job mattutini produzione — 8 worker paralleli
            options.Queues = ["critical", "default", "mail", "maintenance"];
            options.WorkerCount = 8;
            options.SchedulePollingInterval = TimeSpan.FromMinutes(1);
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.StopTimeout = TimeSpan.FromSeconds(30);
            options.ShutdownTimeout = TimeSpan.FromSeconds(60);
            options.ServerCheckInterval = TimeSpan.FromMinutes(1);
            options.CancellationCheckInterval = TimeSpan.FromSeconds(5);
        });
    }

    private static void RegisterServices(WebApplicationBuilder builder)
    {
        //Check diagnostico utilizzato solo all'avvio per verificare la connettività e lo stato di Hangfire,
        //non è un servizio utilizzato direttamente nei job o nelle operazioni quotidiane
        builder.Services.AddSingleton<IHangfireHealthService, HangfireHealthService>();

        // Singleton: configurazione globale condivisa tra tutti gli utenti.
        builder.Services.AddSingleton<ILavorazioniConfigManager, LavorazioniConfigManager>();

        // Scoped: stato UI legato alla sessione del singolo utente.
        // In Blazor Server, lo "scope" = connessione SignalR dell'utente.
        builder.Services.AddScoped<UiStateService>();
        builder.Services.AddScoped<ConfigUser>();

        // Scoped: servizi di elaborazione dati operatori.
        // Dipendono da DbContext (Scoped) → devono essere Scoped.
        builder.Services.AddScoped<INormalizzatoreOperatori, NormalizzatoreOperatori>();
        builder.Services.AddScoped<IGestoreOperatoriDatiLavorazione, GestoreOperatoriDatiLavorazione>();
        builder.Services.AddScoped<IElaboratoreDatiLavorazione, ElaboratoreDatiLavorazione>();

        // Scoped: servizi UI ausiliari per clipboard e PDF.
        builder.Services.AddScoped<IClipboardService, ClipboardService>();
        builder.Services.AddScoped<IPdfExportService, PdfExportService>();

        // Scoped: dialog registrato come servizio (pattern Blazor per dialogs riutilizzabili).
        builder.Services.AddScoped<NotificationDialog>();
        
        // Data Access Services 
        builder.Services.AddScoped<IServiceCentri, ServiceCentri>();
        builder.Services.AddScoped<IServiceOperatori, ServiceOperatori>();
        builder.Services.AddScoped<IServiceClienti, ServiceClienti>();
        builder.Services.AddScoped<IServiceProcedureClienti, ServiceProcedureClienti>();
        builder.Services.AddScoped<IServiceProcedureLavorazioni, ServiceProcedureLavorazioni>();
        builder.Services.AddScoped<IServiceFormatoDati, ServiceFormatoDati>();
        builder.Services.AddScoped<IServiceRepartiProduzione, ServiceRepartiProduzione>();
        builder.Services.AddScoped<IServiceFasiLavorazioni, ServiceFasiLavorazioni>();
        builder.Services.AddScoped<IServiceProduzioneOperatori, ServiceProduzioneOperatori>();
        builder.Services.AddScoped<IServiceTipologieTotali, ServiceTipologieTotali>();
        builder.Services.AddScoped<IServiceLavorazioniFasiTipoTotale, ServiceLavorazioniFasiTipoTotale>();
        builder.Services.AddScoped<IServiceTurni, ServiceTurni>();
        builder.Services.AddScoped<IServiceProduzioneSistema, ServiceProduzioneSistema>();
        builder.Services.AddScoped<IServiceConfigReportDocumenti, ServiceConfigReportDocumenti>();
        builder.Services.AddScoped<IServiceOperatoriNormalizzati, ServiceOperatoriNormalizzati>();
        builder.Services.AddScoped<IServiceTaskDataReadingAggiornamento, ServiceTaskDataReadingAggiornamento>();
        builder.Services.AddScoped<IServiceCentriVisibili, ServiceCentriVisibili>();
        builder.Services.AddScoped<IServiceTipoTurni, ServiceTipoTurni>();
        builder.Services.AddScoped<IServiceTaskDaEseguire, ServiceTaskDaEseguire>();
        builder.Services.AddScoped<IServiceRuoli, ServiceRuoli>();
        builder.Services.AddScoped<IServiceConfigurazioneFontiDati, ServiceConfigurazioneFontiDati>();
        builder.Services.AddScoped<IServiceMail, ServiceMail>();
        builder.Services.AddScoped<IServiceTaskManagement, ServiceTaskManagement>();
        builder.Services.AddScoped<IAder4MailCsvService, Ader4MailCsvService>();
        builder.Services.AddScoped<ProcedureEditStateService>();
        builder.Services.AddScoped<ProcedureValidationService>();

        // Wizard Multi-Step Configuration Services
        builder.Services.AddScoped<ConfigurationWizardStateService>();
        builder.Services.AddScoped<ConfigurationStepValidator>();

        // Servizio validazione SQL (sicurezza SQL injection + test connessioni)
        builder.Services.AddScoped<SqlValidationService>();

         // Servizio bulk insert CSV grezzo in tabella HERA16 (usato da Hera16IngestionProcessor)
         builder.Services.AddSingleton<IHera16DataService, Hera16DataService>();

         // Servizio esecuzione di query SQL 
        builder.Services.AddSingleton<IQueryService, QueryService>();

        // Servizi per Configurazione Fonti Dati
        builder.Services.AddScoped<IConfigurazioneDataReaderService, ConfigurazioneDataReaderService>();
        builder.Services.AddScoped<ITaskGenerationService, TaskGenerationService>();

        // Singleton: IDbContextFactory e IRecurringJobManager (Hangfire) sono entrambi Singleton
        builder.Services.AddSingleton<IProductionJobScheduler, ProductionJobScheduler>();
        builder.Services.AddSingleton<IRecurringJobManagerAdapter, HangfireRecurringJobManagerAdapter>();


        // Registrazione handler lavorazioni (SQL/Oracle/)
        // Singleton: tutti dipendono solo da ILavorazioniConfigManager e ILoggerFactory (entrambi Singleton)
        builder.Services.AddSingleton<IProductionDataHandler, Z0072370_28AutHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Z0082041_SoftlineHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Ant_Ader4_Sorter_1_2Handler>();
        builder.Services.AddSingleton<IProductionDataHandler, PraticheSuccessioneHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Rdmkt_RSPHandler>();


        // Handler ingestion generico mail (orchestratore MAIL_INGESTION)
        builder.Services.AddSingleton<IProductionDataHandler, GenericMailIngestionHandler>();

        // Registry e servizio unificato: Singleton perche' il dizionario e' immutabile dopo costruzione
        builder.Services.AddSingleton<IUnifiedHandlerRegistry, UnifiedHandlerRegistry>();

        // Processori mail ingestion (chiamati da GenericMailIngestionHandler)
        builder.Services.AddSingleton<IMailIngestionProcessor, Ader4IngestionProcessor>();
        builder.Services.AddSingleton<IMailIngestionProcessor, Hera16IngestionProcessor>();

        // Ader4EmailService registrata come tipo concreto (iniettato in Ader4IngestionProcessor).
        // In modalita mock, LocalCsvAder4EmailService estende Ader4EmailService e viene usata al suo posto.
        if (builder.Configuration.GetValue<bool>("MailServices:ADER4:UseMockService"))
        {
            builder.Services.AddSingleton<Ader4EmailService, LocalCsvAder4EmailService>();
            NLog.LogManager.GetCurrentClassLogger().Info("ADER4: modalita mock attiva - lettura CSV da cartella locale");
        }
        else
        {
            builder.Services.AddSingleton<Ader4EmailService>();
        }

        //// Hera16EmailService registrata come tipo concreto (iniettato in Hera16IngestionProcessor).
        //// In modalita mock, LocalCsvHera16EmailService estende Hera16EmailService e viene usata al suo posto.
        if (builder.Configuration.GetValue<bool>("MailServices:HERA16:UseMockService"))
        {
            builder.Services.AddSingleton<Hera16EmailService, LocalCsvHera16EmailService>();
            NLog.LogManager.GetCurrentClassLogger().Info("HERA16: modalita mock attiva - lettura CSV da cartella locale");
        }
        else
        {
            builder.Services.AddSingleton<Hera16EmailService>();
        }


        // Servizio unificato principale
        builder.Services.AddSingleton<IUnifiedHandlerService, UnifiedHandlerService>();

        // Configurazione FluentEmail per ServiceMail
        RegisterFluentEmail(builder);

    }


    /// <summary>
    /// Registra i mapper Mapperly come Singleton.
    /// Mapperly genera mapper tipizzati a compile-time (nessun overhead runtime).
    /// </summary>
    private static void RegisterMappers(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ProduzioneSistemaMapper>();
        builder.Services.AddSingleton<ProduzioneOperatoriMapper>();
        builder.Services.AddSingleton<OperatoriMapper>();
        builder.Services.AddSingleton<CentriMapper>();
        builder.Services.AddSingleton<ClientiMapper>();
        builder.Services.AddSingleton<TurniMapper>();
        builder.Services.AddSingleton<TipologieTotaliMapper>();
        builder.Services.AddSingleton<LavorazioniFasiMapper>();
        builder.Services.AddSingleton<ProcedureLavorazioniMapper>();
        builder.Services.AddSingleton<TaskDaEseguireMapper>();
        builder.Services.AddSingleton<TaskDataReadingAggiornamentoMapper>();
        builder.Services.AddSingleton<ReportsMapper>();
        builder.Services.AddSingleton<AltriDatiMapper>();
    }

    private static void RegisterAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                o.SlidingExpiration = true;
                o.LoginPath = "/Account/Login";
                o.AccessDeniedPath = "/Account/Login"; // non abbiamo pagina dedicata AccessDenied
            });
    }

    private static void RegisterMudBlazor(WebApplicationBuilder builder)
    {
        builder.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
            cfg.SnackbarConfiguration.PreventDuplicates = true;
            cfg.SnackbarConfiguration.VisibleStateDuration = 3000;
            cfg.SnackbarConfiguration.HideTransitionDuration = 500;
            cfg.SnackbarConfiguration.ShowTransitionDuration = 500;
            cfg.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });
    }
    #endregion

    #region App Pipeline
    private static void InitializeApp(WebApplication app)
    {
        var loggerFactory       = app.Services.GetRequiredService<ILoggerFactory>();
        var enableQueryLog      = app.Configuration.GetValue<bool>("Logging:EnableQueryExecutionLog", false);
        QueryLoggingHelper.Initialize(loggerFactory, "LibraryLavorazioni.SqlQueries", "BlazorDematReports", enableQueryLog);

        //class statica per eseguire i job di produzione, necessita di un IServiceScopeFactory per creare scope nei job
        ProductionJobRunner.Initialize(app.Services.GetRequiredService<IServiceScopeFactory>());
    }

    private static async Task RunStartupDiagnosticsAsync(WebApplication app, NLog.Logger bootstrapLogger)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var health = scope.ServiceProvider.GetRequiredService<IHangfireHealthService>();
            var ok = await health.CheckHangfireHealthAsync();
            if (!ok)
                bootstrapLogger.Warn("Hangfire non completamente operativo all'avvio");
            else
            {
                var stats = await health.GetHangfireStatsAsync();
                bootstrapLogger.Info($"Hangfire Online: Servers={stats.ActiveServers} Enqueued={stats.EnqueuedJobs} Failed={stats.FailedJobs}");
            }
        }
        catch (Exception ex)
        {
            bootstrapLogger.Error(ex, "Errore health check Hangfire");
        }
    }

    private static void MapMiddleware(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/Error", createScopeForErrors: true);

        app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Strict });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapRazorPages();

        app.MapStaticAssets();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new MyAuthorizationFilter() },
            AppPath = null
        });
    }

    /// <summary>
    /// Sincronizza i recurring job Hangfire con la configurazione attuale delle lavorazioni.
    /// Crea uno scope temporaneo per risolvere <see cref="IProductionJobScheduler"/> e chiama
    /// <c>SyncAllAsync</c> che aggiunge, aggiorna o rimuove i job in base alle fasi/centri configurati.
    /// </summary>
    /// <param name="app">L'istanza di <see cref="WebApplication"/> usata per creare lo scope DI.</param>
    private static async Task SyncRecurringJobsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IProductionJobScheduler>();
        await scheduler.SyncAllAsync();
    }


    /// <summary>
    /// Registra i recurring job di manutenzione del sistema in Hangfire.
    /// Attualmente schedula <c>system:cleanup-orphans</c> ogni giorno alle 02:30
    /// per rimuovere i job orfani (configurazioni eliminate ma job ancora presenti in Hangfire).
    /// </summary>
    private static void ScheduleSystemJobs()
    {
        RecurringJob.AddOrUpdate("system:cleanup-orphans",() => CleanupJobsOrfani(), "30 2 * * *");
    }


    /// <summary>
    /// Registra il recurring job di ingestion email in Hangfire.
    /// Schedula <c>MAIL_INGESTION</c> ogni 2 ore per orchestrare tutti i processori mail
    /// registrati (ADER4, HERA16, ecc.) tramite <see cref="ExecuteMailIngestionAsync"/>.
    /// </summary>
    private static void ScheduleMailIngestion()
    {
        RecurringJob.AddOrUpdate(
            "MAIL_INGESTION",
            () => ExecuteMailIngestionAsync(),
            "0 */2 * * *");  // Ogni 2 ore

    }

    /// <summary>
    /// Esegue il job di ingestion mail generico.
    /// Chiamato da Hangfire recurring job MAIL_INGESTION.
    /// Questo job orchestra tutti i processori mail registrati (ADER4, HERA16, etc.)
    /// e salva i dati aggregati nella tabella staging DatiMailIngestion.
    /// </summary>
    public static async Task ExecuteMailIngestionAsync()
    {
        using var scope = _app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MailIngestion");

        try
        {
            logger.LogInformation("Inizio job MAIL_INGESTION");

            var handler = scope.ServiceProvider
                .GetServices<IProductionDataHandler>()
                .FirstOrDefault(h => h.HandlerCode == "MAIL_INGESTION");

            if (handler is null)
            {
                logger.LogError("Handler MAIL_INGESTION (GenericMailIngestionHandler) non trovato nel registry");
                return;
            }

            // Context minimale per handler ingestion (non legato a TaskDaEseguire)
            var context = new BlazorDematReports.Core.Lavorazioni.Models.ProductionExecutionContext
            {
                StartDataLavorazione = DateTime.Today,
                EndDataLavorazione = DateTime.Today,
                IDProceduraLavorazione = 0,
                IDFaseLavorazione = 0,
                IDCentro = 0
            };

            await handler.ExecuteAsync(context, CancellationToken.None);
            logger.LogInformation("Job MAIL_INGESTION completato con successo");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore durante esecuzione job MAIL_INGESTION");
            throw;
        }
    }


    #endregion


    #region Maintenance Jobs
    /// <summary>
    /// Esegue la pulizia asincrona dei job orfani dal sistema Hangfire.
    /// </summary>
    /// <returns>Task asincrono per l'operazione di pulizia.</returns>
    public static async Task CleanupJobsOrfani()
    {
        try
        {
            using var scope = _app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IProductionJobScheduler>();
            var removed = await scheduler.CleanupOrphansAsync();
            var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CleanupOrphans");
            log.LogInformation("Cleanup orfani completato. Removed={Count}", removed);
        }
        catch (Exception ex)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Errore cleanup orfani");
            throw;
        }
    }


    #endregion
}
