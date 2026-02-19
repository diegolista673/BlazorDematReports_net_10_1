using Entities.Models.DbApplication;
using Entities.Enums;
using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlazorDematReports.Core.Handlers;

/// <summary>
/// Handler unificato per TUTTE le fonti dati.
/// Legge configurazione dal DB ed esegue in base al TipoFonte.
/// </summary>
public class UnifiedDataSourceHandler : ILavorazioneHandler
{
    private readonly IDbContextFactory<DematReportsContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnifiedDataSourceHandler> _logger;

    public string LavorazioneCode => "UNIFIED_DATASOURCE";

    public UnifiedDataSourceHandler(
        IDbContextFactory<DematReportsContext> dbFactory,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<UnifiedDataSourceHandler> logger)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Esegue elaborazione basata su configurazione DB.
    /// </summary>
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        LavorazioneExecutionContext context,
        CancellationToken ct = default)
    {
        if (!context.IdConfigurazioneDatabase.HasValue)
        {
            throw new InvalidOperationException(
                "IdConfigurazioneDatabase è richiesto per UnifiedDataSourceHandler");
        }

        // 1. Carica configurazione dal DB
        var config = await LoadConfigurationAsync(context.IdConfigurazioneDatabase.Value, ct);

        _logger.LogInformation("[UnifiedHandler] Esecuzione {Codice} (Tipo: {Tipo})",
            config.CodiceConfigurazione, config.TipoFonte);

        // 2. Routing basato su TipoFonte (già enum, no conversione!)
        return config.TipoFonte switch
        {
            TipoFonteData.SQL => await ExecuteSqlQueryAsync(config, context, ct),
            TipoFonteData.HandlerIntegrato => await ExecuteCustomHandlerAsync(config, context, ct),
            _ => throw new NotSupportedException($"TipoFonte '{config.TipoFonte}' non supportato. Per servizi email, utilizzare i job Hangfire dedicati.")
        };
    }

    #region SQL Query Execution

    private async Task<List<DatiLavorazione>> ExecuteSqlQueryAsync(
        ConfigurazioneFontiDati config,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        var result = new List<DatiLavorazione>();

        // Trova mapping per questa fase/centro
        var mapping = config.ConfigurazioneFaseCentros.FirstOrDefault(fc =>
            fc.IdProceduraLavorazione == context.IDProceduraLavorazione &&
            fc.IdFaseLavorazione == context.IDFaseLavorazione &&
            fc.IdCentro == context.IDCentro &&
            fc.FlagAttiva == true);

        // Usa query specifica del task se presente, altrimenti query base
        var query = mapping?.TestoQueryTask ;

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("[UnifiedHandler:SQL] Nessuna query configurata per {Codice}",
                config.CodiceConfigurazione);
            return result;
        }

        // Ottieni connection string
        var connectionString = _configuration.GetConnectionString(config.ConnectionStringName!);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionString '{config.ConnectionStringName}' non trovata in appsettings.json");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Esegui query
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var cmd = new SqlCommand(query, connection);
            cmd.CommandTimeout = TaskConfigurationDefaults.DefaultQueryTimeoutSeconds;
            cmd.Parameters.AddWithValue("@startData", context.StartDataLavorazione.ToString("yyyyMMdd"));
            cmd.Parameters.AddWithValue("@endData",
                (context.EndDataLavorazione ?? context.StartDataLavorazione).ToString("yyyyMMdd"));

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(MapToDatiLavorazione(reader, mapping?.MappingColonne));
            }

            stopwatch.Stop();

            _logger.LogInformation("[UnifiedHandler:SQL] Eseguita query {Codice}: {Count} record in {Ms}ms",
                config.CodiceConfigurazione, result.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (SqlException sqlEx)
        {
            stopwatch.Stop();
            _logger.LogError(sqlEx, "[UnifiedHandler:SQL] Errore SQL per {Codice}. Numero: {Num}, Tempo: {Ms}ms",
                config.CodiceConfigurazione, sqlEx.Number, stopwatch.ElapsedMilliseconds);
            throw;
        }

        return result;
    }

    private DatiLavorazione MapToDatiLavorazione(SqlDataReader reader, string? mappingJson)
    {
        // Mapping colonne: default o custom da configurazione
        Dictionary<string, string> mapping;

        if (!string.IsNullOrWhiteSpace(mappingJson))
        {
            try
            {
                mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson)
                    ?? GetDefaultMapping();
            }
            catch
            {
                mapping = GetDefaultMapping();
            }
        }
        else
        {
            mapping = GetDefaultMapping();
        }

        return new DatiLavorazione
        {
            Operatore = SafeGetString(reader, mapping.GetValueOrDefault("Operatore", "operatore")),
            DataLavorazione = SafeGetDateTime(reader, mapping.GetValueOrDefault("DataLavorazione", "DataLavorazione")),
            Documenti = SafeGetInt(reader, mapping.GetValueOrDefault("Documenti", "Documenti")),
            Fogli = SafeGetInt(reader, mapping.GetValueOrDefault("Fogli", "Fogli")),
            Pagine = SafeGetInt(reader, mapping.GetValueOrDefault("Pagine", "Pagine")),
            AppartieneAlCentroSelezionato = true
        };
    }

    private static Dictionary<string, string> GetDefaultMapping() => new()
    {
        ["Operatore"] = "operatore",
        ["DataLavorazione"] = "DataLavorazione",
        ["Documenti"] = "Documenti",
        ["Fogli"] = "Fogli",
        ["Pagine"] = "Pagine"
    };

    #endregion

    #region Custom Handler Execution

    private async Task<List<DatiLavorazione>> ExecuteCustomHandlerAsync(
        ConfigurazioneFontiDati config,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.HandlerClassName))
        {
            throw new InvalidOperationException("HandlerClassName non specificato");
        }

        // Risolvi handler C# dal nome classe via reflection
        var handlerType = Type.GetType(
            $"LibraryLavorazioni.Lavorazioni.Handlers.{config.HandlerClassName}, LibraryLavorazioni");

        if (handlerType == null)
        {
            _logger.LogError("[UnifiedHandler:Custom] Handler '{Handler}' non trovato",
                config.HandlerClassName);
            throw new InvalidOperationException($"Handler {config.HandlerClassName} non trovato");
        }

        // Crea istanza con DI
        var handler = (ILavorazioneHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);

        _logger.LogInformation("[UnifiedHandler:Custom] Esecuzione handler {Handler}",
            config.HandlerClassName);

        return await handler.ExecuteAsync(context, ct);
    }

    #endregion

  

    #region Helpers

    private async Task<ConfigurazioneFontiDati?> LoadConfigurationAsync(int idConfigurazione, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.ConfigurazioneFontiDatis
            .Include(c => c.ConfigurazioneFaseCentros)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdConfigurazione == idConfigurazione, ct);
    }

    private static string? SafeGetString(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch { return null; }
    }

    private static DateTime SafeGetDateTime(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }
        catch { return DateTime.MinValue; }
    }

    private static int? SafeGetInt(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch { return null; }
    }

    #endregion
}
