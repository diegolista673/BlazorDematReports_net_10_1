using BlazorDematReports.Application;
using BlazorDematReports.Components;
using BlazorDematReports.Components.Dialog;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Mapping;
using BlazorDematReports.Core.DataReading.Infrastructure;
using BlazorDematReports.Core.DataReading.Interfaces;
using BlazorDematReports.Core.DataReading.Services;
using BlazorDematReports.Core.Handlers.LavorazioniHandlers;
using BlazorDematReports.Core.Handlers.MailHandlers.Ader4;
using BlazorDematReports.Core.Handlers.MailHandlers.Hera16;
using BlazorDematReports.Core.Services.Email;
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

            // Carica User Secrets in Development
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets(typeof(Program).Assembly);
            }

            // In Production, carica variabili ambiente (sovrascrivono appsettings)
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
            // In altri ambienti usa implementazione reale con AD
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
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                EnableHeavyMigrations = true,
                PrepareSchemaIfNecessary = true,
                SchemaName = "HangFire"
            }));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 10);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
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
        builder.Services.AddSingleton<ILavorazioniConfigManager, LavorazioniConfigManager>();

        builder.Services.AddScoped<UiStateService>();
        builder.Services.AddScoped<ConfigUser>();
        builder.Services.AddScoped<INormalizzatoreOperatori, NormalizzatoreOperatori>();
        builder.Services.AddScoped<IGestoreOperatoriDatiLavorazione, GestoreOperatoriDatiLavorazione>();
        builder.Services.AddScoped<IElaboratoreDatiLavorazione, ElaboratoreDatiLavorazione>();
        builder.Services.AddScoped<IClipboardService, ClipboardService>();
        builder.Services.AddScoped<IPdfExportService, PdfExportService>();
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
        builder.Services.AddScoped<IServiceQueryProcedureLavorazioni, ServiceQueryProcedureLavorazioni>();
        builder.Services.AddScoped<IServiceCentriVisibili, ServiceCentriVisibili>();
        builder.Services.AddScoped<IServiceTipoTurni, ServiceTipoTurni>();
        builder.Services.AddScoped<IServiceTaskDaEseguire, ServiceTaskDaEseguire>();
        builder.Services.AddScoped<IServiceRuoli, ServiceRuoli>();
        builder.Services.AddScoped<IServiceConfigurazioneFontiDati, ServiceConfigurazioneFontiDati>();
        builder.Services.AddScoped<IServiceMail, ServiceMail>();
        builder.Services.AddScoped<IServiceTaskManagement, ServiceTaskManagement>();
        builder.Services.AddScoped<ProcedureEditStateService>();
        builder.Services.AddScoped<ProcedureValidationService>();

        // ? Wizard Multi-Step Configuration Services
        builder.Services.AddScoped<ConfigurationWizardStateService>();
        builder.Services.AddScoped<ConfigurationStepValidator>();

        // Servizio validazione SQL (sicurezza SQL injection + test connessioni)
        builder.Services.AddScoped<SqlValidationService>();

         // Servizio esecuzione di query SQL 
        builder.Services.AddSingleton<IQueryService, QueryService>();

        // Servizi per Configurazione Fonti Dati
        builder.Services.AddScoped<IConfigurazioneDataReaderService, ConfigurazioneDataReaderService>();
        builder.Services.AddScoped<ITaskGenerationService, TaskGenerationService>();

        // Singleton: IDbContextFactory e IRecurringJobManager (Hangfire) sono entrambi Singleton
        builder.Services.AddSingleton<IProductionJobScheduler, ProductionJobScheduler>();
        builder.Services.AddSingleton<IRecurringJobManagerAdapter, HangfireRecurringJobManagerAdapter>();


        // Registrazione handler lavorazioni (SQL/Oracle/Email)
        // Singleton: tutti dipendono solo da ILavorazioniConfigManager e ILoggerFactory (entrambi Singleton)
        builder.Services.AddSingleton<IProductionDataHandler, Z0072370_28AutHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Z0082041_SoftlineHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Ant_Ader4_Sorter_1_2Handler>();
        builder.Services.AddSingleton<IProductionDataHandler, PraticheSuccessioneHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Rdmkt_RSPHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Hera16EwsHandler>();
        builder.Services.AddSingleton<IProductionDataHandler, Ader4Handler>();

        // Registry e servizio unificato: Singleton perche' il dizionario e' immutabile dopo costruzione
        builder.Services.AddSingleton<IUnifiedHandlerRegistry, UnifiedHandlerRegistry>();

        // Registra servizi email
        builder.Services.AddSingleton<EmailDailyFlagService>();

        // Registrazione condizionale: mock (cartella locale) in sviluppo, Exchange EWS in produzione.
        // Attivare con "MailServices:ADER4:UseMockService": true in appsettings.Development.json.
        if (builder.Configuration.GetValue<bool>("MailServices:ADER4:UseMockService"))
        {
            builder.Services.AddSingleton<IEmailBatchProcessor, LocalCsvAder4EmailService>();
            NLog.LogManager.GetCurrentClassLogger().Info("ADER4: modalita mock attiva - lettura CSV da cartella locale");
        }
        else
        {
            builder.Services.AddSingleton<IEmailBatchProcessor, Ader4EmailService>();
        }
        //builder.Services.AddSingleton<Hera16EmailService>(); 


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
        builder.Services.AddSingleton<QueryProcedureLavorazioniMapper>();
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
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        QueryLoggingHelper.Initialize(loggerFactory, "LibraryLavorazioni.SqlQueries", "BlazorDematReports");

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

    private static async Task SyncRecurringJobsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IProductionJobScheduler>();
        await scheduler.SyncAllAsync();
    }

    private static void ScheduleSystemJobs()
    {
        RecurringJob.AddOrUpdate("system:cleanup-orphans",() => CleanupJobsOrfani(), "30 2 * * *");
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
