using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Data;

namespace BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;

/// <summary>
/// Handler base per task produzione DatiMailCsvHera16.
/// Interroga direttamente la tabella <c>DatiMailCsvHera16</c> (popolata da Hera16IngestionProcessor)
/// </summary>
public abstract class Hera16QueryHandlerBase : IProductionDataHandler
{
    protected readonly ILogger Logger;
    private readonly ILavorazioniConfigManager _configManager;

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="Hera16QueryHandlerBase"/>.
    /// </summary>
    protected Hera16QueryHandlerBase(
        ILogger logger,
        ILavorazioniConfigManager configManager)
    {
        Logger         = logger;
        _configManager = configManager;
    }

    /// <inheritdoc />
    public abstract string HandlerCode { get; }

    /// <inheritdoc />
    public string? GetServiceCode() => string.Empty;

    /// <inheritdoc />
    public HandlerMetadata GetMetadata() => new()
    {
        ServiceCode          = LavorazioniCodes.HERA16,
        RequiresEmailService = false,
        Category             = "HERA16 SQL Query"
    };

    /// <summary>Restituisce la query SQL specifica per la fase di lavorazione.</summary>
    protected abstract string GetQuery();

    /// <inheritdoc />
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        ProductionExecutionContext context,
        CancellationToken ct = default)
    {
        var startDate = context.StartDataLavorazione;
        var endDate   = context.EndDataLavorazione ?? context.StartDataLavorazione;

        Logger.LogInformation(
            "[{Code}] Elaborazione dati HERA16, periodo {Start:dd/MM/yyyy}-{End:dd/MM/yyyy}",
            HandlerCode, startDate, endDate);

        return await EseguiQueryAsync(GetQuery(), startDate, endDate, ct);
    }

    /// <summary>
    /// Esegue la query SQL su SQL Server tramite la connessione <c>CnxnDematReports</c>.
    /// I parametri <c>@startDate</c> e <c>@endDate</c> sono tipizzati come DateTime2.
    /// </summary>
    private async Task<List<DatiLavorazione>> EseguiQueryAsync(
        string query,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        QueryLoggingHelper.LogQueryExecution(logger: Logger);

        var connectionString = _configManager.CnxnDematReports
            ?? throw new InvalidOperationException("ConnectionStrings:CnxnDematReports non configurata in appsettings");

        var result    = new List<DatiLavorazione>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var cmd = new SqlCommand(query, connection);
            cmd.CommandTimeout = 30;
            cmd.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate.Date;
            cmd.Parameters.Add("@endDate",   SqlDbType.DateTime2).Value = endDate.Date;

            Logger.LogDebug("[{Code}] Esecuzione query con timeout: {Timeout}s", HandlerCode, cmd.CommandTimeout);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            // Ordinal pre-calcolati una sola volta per performance su grandi dataset
            var ordOp   = reader.GetOrdinal("Operatore");
            var ordData = reader.GetOrdinal("DataLavorazione");
            var ordDoc  = reader.GetOrdinal("Documenti");
            var ordFog  = reader.GetOrdinal("Fogli");
            var ordPag  = reader.GetOrdinal("Pagine");

            while (await reader.ReadAsync(ct))
            {
                result.Add(new DatiLavorazione
                {
                    Operatore                     = reader.IsDBNull(ordOp)  ? null : reader.GetString(ordOp).Trim(),
                    DataLavorazione               = reader.GetDateTime(ordData),
                    Documenti                     = reader.IsDBNull(ordDoc) ? null : reader.GetInt32(ordDoc),
                    Fogli                         = reader.IsDBNull(ordFog) ? null : reader.GetInt32(ordFog),
                    Pagine                        = reader.IsDBNull(ordPag) ? null : reader.GetInt32(ordPag),
                    AppartieneAlCentroSelezionato = true
                });
            }

            stopwatch.Stop();
            Logger.LogInformation(
                "[{Code}] Query eseguita con successo. Record letti: {Count}, Tempo: {Ms}ms",
                HandlerCode, result.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (SqlException sqlEx)
        {
            stopwatch.Stop();
            Logger.LogError(sqlEx,
                "[{Code}] Errore SQL. Tempo: {Ms}ms, Numero: {ErrNum}, Severita: {Class}, Stato: {State}",
                HandlerCode, stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "[{Code}] Errore generico. Tempo: {Ms}ms", HandlerCode, stopwatch.ElapsedMilliseconds);
            throw;
        }

        return result;
    }
}

/// <summary>
/// Handler HERA16 per fase Scansione.
/// Aggrega COUNT DISTINCT (codice_mercato + codice_offerta + tipo_documento) per operatore_scan e data_scansione.
/// </summary>
[Description("HERA16 Scansione - query diretta su tabella DatiMailCsvHera16")]
public sealed class Hera16ScansioneHandler : Hera16QueryHandlerBase
{
    /// <summary>Inizializza una nuova istanza di <see cref="Hera16ScansioneHandler"/>.</summary>
    public Hera16ScansioneHandler(
        ILogger<Hera16ScansioneHandler> logger,
        ILavorazioniConfigManager configManager)
        : base(logger, configManager) { }

    /// <inheritdoc />
    public override string HandlerCode => LavorazioniCodes.HERA16_SCANSIONE;

    /// <inheritdoc />
    protected override string GetQuery() => QuerySql;

    private const string QuerySql = """
        SELECT
            operatore_scan                                                       AS Operatore,
            CONVERT(date, data_scansione)                                        AS DataLavorazione,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Documenti,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Fogli,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento) * 2 AS Pagine
        FROM DatiMailCsvHera16
        WHERE CONVERT(date, data_scansione) >= @startDate
          AND CONVERT(date, data_scansione) <= @endDate
        GROUP BY operatore_scan, CONVERT(date, data_scansione)
        ORDER BY CONVERT(date, data_scansione)
        """;
}

/// <summary>
/// Handler HERA16 per fase Index.
/// Esclude operatori '-' e 'engine' e tipi documento 'BRIT', 'DR01', 'XXXX'.
/// </summary>
[Description("HERA16 Index - query diretta su tabella DatiMailCsvHera16")]
public sealed class Hera16IndexHandler : Hera16QueryHandlerBase
{
    /// <summary>Inizializza una nuova istanza di <see cref="Hera16IndexHandler"/>.</summary>
    public Hera16IndexHandler(
        ILogger<Hera16IndexHandler> logger,
        ILavorazioniConfigManager configManager)
        : base(logger, configManager) { }

    /// <inheritdoc />
    public override string HandlerCode => LavorazioniCodes.HERA16_INDEX;

    /// <inheritdoc />
    protected override string GetQuery() => QuerySql;

    private const string QuerySql = """
        SELECT
            operatore_index                                                      AS Operatore,
            CONVERT(date, data_index)                                            AS DataLavorazione,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Documenti,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Fogli,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento) * 2 AS Pagine
        FROM DatiMailCsvHera16
        WHERE CONVERT(date, data_index) >= @startDate
          AND CONVERT(date, data_index) <= @endDate
          AND operatore_index <> '-'
          AND operatore_index <> 'engine'
          AND tipo_documento NOT IN ('BRIT', 'DR01', 'XXXX')
        GROUP BY operatore_index, CONVERT(date, data_index)
        ORDER BY CONVERT(date, data_index)
        """;
}

/// <summary>
/// Handler HERA16 per fase Classificazione.
/// Esclude operatori '-' e 'engine'.
/// </summary>
[Description("HERA16 Classificazione - query diretta su tabella DatiMailCsvHera16")]
public sealed class Hera16ClassificazioneHandler : Hera16QueryHandlerBase
{
    /// <summary>Inizializza una nuova istanza di <see cref="Hera16ClassificazioneHandler"/>.</summary>
    public Hera16ClassificazioneHandler(
        ILogger<Hera16ClassificazioneHandler> logger,
        ILavorazioniConfigManager configManager)
        : base(logger, configManager) { }

    /// <inheritdoc />
    public override string HandlerCode => LavorazioniCodes.HERA16_CLASSIFICAZIONE;

    /// <inheritdoc />
    protected override string GetQuery() => QuerySql;

    private const string QuerySql = """
        SELECT
            operatore_classificazione                                            AS Operatore,
            CONVERT(date, data_classificazione)                                  AS DataLavorazione,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Documenti,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento)     AS Fogli,
            COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento) * 2 AS Pagine
        FROM DatiMailCsvHera16
        WHERE CONVERT(date, data_classificazione) >= @startDate
          AND CONVERT(date, data_classificazione) <= @endDate
          AND operatore_classificazione <> '-'
          AND operatore_classificazione <> 'engine'
        GROUP BY operatore_classificazione, CONVERT(date, data_classificazione)
        ORDER BY CONVERT(date, data_classificazione)
        """;
}
