using Entities.Models.DbApplication;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.LavorazioniViaMail.Services;
using LibraryLavorazioni.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LibraryLavorazioni.Shared.Handlers;

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

        if (config == null || !config.FlagAttiva)
        {
            _logger.LogWarning("[UnifiedHandler] Config {Id} non trovata o non attiva",
                context.IdConfigurazioneDatabase);
            return new List<DatiLavorazione>();
        }

        _logger.LogInformation("[UnifiedHandler] Esecuzione {Codice} (Tipo: {Tipo})",
            config.CodiceConfigurazione, config.TipoFonte);

        // 2. Routing basato su TipoFonte
        return config.TipoFonte switch
        {
            "SQL" => await ExecuteSqlQueryAsync(config, context, ct),
            "EmailCSV" => await ExecuteMailServiceAsync(config, context, ct),
            "HandlerIntegrato" => await ExecuteCustomHandlerAsync(config, context, ct),
            "Pipeline" => await ExecutePipelineAsync(config, context, ct),
            _ => throw new NotSupportedException($"TipoFonte '{config.TipoFonte}' non supportato")
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
            fc.FlagAttiva);

        // Usa query override se presente, altrimenti query base
        var query = mapping?.TestoQueryOverride ?? config.TestoQuery;

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("[UnifiedHandler:SQL] Nessuna query configurata per {Codice}",
                config.CodiceConfigurazione);
            return result;
        }

        // Sostituisci parametri extra da mapping
        if (!string.IsNullOrWhiteSpace(mapping?.ParametriExtra))
        {
            try
            {
                var extraParams = JsonSerializer.Deserialize<Dictionary<string, string>>(mapping.ParametriExtra);
                if (extraParams != null)
                {
                    foreach (var param in extraParams)
                    {
                        // Sostituzione sicura per parametri stringa
                        query = query.Replace($"@{param.Key}", $"'{param.Value.Replace("'", "''")}'");
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[UnifiedHandler:SQL] Errore parsing ParametriExtra JSON");
            }
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
            cmd.CommandTimeout = 60;
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

    #region Mail Service Execution

    private async Task<List<DatiLavorazione>> ExecuteMailServiceAsync(
        ConfigurazioneFontiDati config,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        // Delega a UnifiedMailProduzioneService esistente
        var mailService = _serviceProvider.GetRequiredService<UnifiedMailProduzioneService>();

        var rowsInserted = config.MailServiceCode switch
        {
            "HERA16" => await mailService.ProcessHera16Async(ct),
            "ADER4" => await mailService.ProcessAder4Async(ct),
            _ => throw new NotSupportedException($"MailServiceCode '{config.MailServiceCode}' non supportato")
        };

        _logger.LogInformation("[UnifiedHandler:Mail] Elaborato {Service}: {Count} righe",
            config.MailServiceCode, rowsInserted);

        // Mail service inserisce direttamente in ProduzioneSistema
        // Ritorna lista vuota perché dati già inseriti
        return new List<DatiLavorazione>();
    }

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

    #region Pipeline Execution

    private async Task<List<DatiLavorazione>> ExecutePipelineAsync(
        ConfigurazioneFontiDati config,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        var result = new List<DatiLavorazione>();
        var pipelineData = new List<Dictionary<string, object?>>();

        // Ordina step per NumeroStep
        var steps = config.PipelineSteps
            .Where(s => s.FlagAttiva)
            .OrderBy(s => s.NumeroStep)
            .ToList();

        _logger.LogInformation("[UnifiedHandler:Pipeline] Esecuzione {Count} step per {Codice}",
            steps.Count, config.CodiceConfigurazione);

        foreach (var step in steps)
        {
            _logger.LogDebug("[UnifiedHandler:Pipeline] Step {Num}: {Nome} ({Tipo})",
                step.NumeroStep, step.NomeStep, step.TipoStep);

            Dictionary<string, object?>? stepConfig = null;
            try
            {
                stepConfig = JsonSerializer.Deserialize<Dictionary<string, object?>>(step.ConfigurazioneStep);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[UnifiedHandler:Pipeline] Errore parsing ConfigurazioneStep per step {Num}", step.NumeroStep);
            }

            pipelineData = step.TipoStep switch
            {
                "Query" => await ExecutePipelineQueryStepAsync(stepConfig!, config.ConnectionStringName!, context, ct),
                "Filter" => ApplyPipelineFilter(pipelineData, stepConfig!),
                "Transform" => ApplyPipelineTransform(pipelineData, stepConfig!),
                "Aggregate" => ApplyPipelineAggregate(pipelineData, stepConfig!),
                "Merge" => await MergePipelineDataAsync(pipelineData, stepConfig!, context, ct),
                _ => throw new NotSupportedException($"TipoStep '{step.TipoStep}' non supportato")
            };
        }

        // Converti risultato finale in DatiLavorazione
        foreach (var item in pipelineData)
        {
            result.Add(new DatiLavorazione
            {
                Operatore = item.GetValueOrDefault("operatore")?.ToString(),
                DataLavorazione = DateTime.TryParse(item.GetValueOrDefault("DataLavorazione")?.ToString(), out var dt)
                    ? dt : DateTime.Today,
                Documenti = int.TryParse(item.GetValueOrDefault("Documenti")?.ToString(), out var doc) ? doc : null,
                Fogli = int.TryParse(item.GetValueOrDefault("Fogli")?.ToString(), out var fogli) ? fogli : null,
                Pagine = int.TryParse(item.GetValueOrDefault("Pagine")?.ToString(), out var pag) ? pag : null,
                AppartieneAlCentroSelezionato = true
            });
        }

        _logger.LogInformation("[UnifiedHandler:Pipeline] Completato: {Count} record",
            result.Count);

        return result;
    }

    private async Task<List<Dictionary<string, object?>>> ExecutePipelineQueryStepAsync(
        Dictionary<string, object?> stepConfig,
        string connectionStringName,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        var result = new List<Dictionary<string, object?>>();

        if (!stepConfig.TryGetValue("Query", out var queryObj) || queryObj == null)
        {
            throw new InvalidOperationException("Pipeline Query step richiede campo 'Query'");
        }

        var query = queryObj.ToString()!;
        var connectionString = _configuration.GetConnectionString(connectionStringName);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var cmd = new SqlCommand(query, connection);
        cmd.CommandTimeout = 60;
        cmd.Parameters.AddWithValue("@startData", context.StartDataLavorazione.ToString("yyyyMMdd"));
        cmd.Parameters.AddWithValue("@endData",
            (context.EndDataLavorazione ?? context.StartDataLavorazione).ToString("yyyyMMdd"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            result.Add(row);
        }

        return result;
    }

    private List<Dictionary<string, object?>> ApplyPipelineFilter(
        List<Dictionary<string, object?>> data,
        Dictionary<string, object?> stepConfig)
    {
        // TODO: Implementare filtri configurabili
        // Esempio: {"Field": "status", "Operator": "equals", "Value": "active"}
        _logger.LogDebug("[UnifiedHandler:Pipeline:Filter] Filtro non implementato, dati passati senza modifiche");
        return data;
    }

    private List<Dictionary<string, object?>> ApplyPipelineTransform(
        List<Dictionary<string, object?>> data,
        Dictionary<string, object?> stepConfig)
    {
        // TODO: Implementare trasformazioni configurabili
        // Esempio: {"SourceField": "raw_date", "TargetField": "date", "Transform": "ParseDate"}
        _logger.LogDebug("[UnifiedHandler:Pipeline:Transform] Trasformazione non implementata, dati passati senza modifiche");
        return data;
    }

    private List<Dictionary<string, object?>> ApplyPipelineAggregate(
        List<Dictionary<string, object?>> data,
        Dictionary<string, object?> stepConfig)
    {
        // TODO: Implementare aggregazioni configurabili
        // Esempio: {"GroupBy": ["operatore", "data"], "Aggregations": [{"Field": "docs", "Function": "SUM"}]}
        _logger.LogDebug("[UnifiedHandler:Pipeline:Aggregate] Aggregazione non implementata, dati passati senza modifiche");
        return data;
    }

    private async Task<List<Dictionary<string, object?>>> MergePipelineDataAsync(
        List<Dictionary<string, object?>> data,
        Dictionary<string, object?> stepConfig,
        LavorazioneExecutionContext context,
        CancellationToken ct)
    {
        // TODO: Implementare merge dati da fonti multiple
        _logger.LogDebug("[UnifiedHandler:Pipeline:Merge] Merge non implementato, dati passati senza modifiche");
        return await Task.FromResult(data);
    }

    #endregion

    #region Helpers

    private async Task<ConfigurazioneFontiDati?> LoadConfigurationAsync(int idConfigurazione, CancellationToken ct)
    {
        using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.ConfigurazioneFontiDatis
            .Include(c => c.ConfigurazioneFaseCentros)
            .Include(c => c.PipelineSteps)
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
