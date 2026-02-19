using BlazorDematReports.Core.Constants;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace  BlazorDematReports.Core.Services.Email
{
    /// <summary>
    /// Servizio per gestione flag elaborazione email giornaliera.
    /// Previene elaborazioni duplicate da task concorrenti usando flag giornaliero per servizio.
    /// </summary>
    public sealed class EmailDailyFlagService
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly ILogger<EmailDailyFlagService> _logger;

        public EmailDailyFlagService(
            IDbContextFactory<DematReportsContext> contextFactory,
            ILogger<EmailDailyFlagService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Tenta di marcare email come "in elaborazione" per oggi con gestione race condition.
        /// Ritorna true se questo task è il primo (vince la race).
        /// </summary>
        /// <param name="codiceServizio">Codice servizio email (es. ADER4, HERA16).</param>
        /// <param name="nomeTask">Nome task esecutore (per logging).</param>
        /// <param name="ct">Token cancellazione.</param>
        /// <returns>True se questo task ha acquisito il lock (primo oggi), false altrimenti.</returns>
        public async System.Threading.Tasks.Task<bool> TryMarkAsProcessingAsync(
            string codiceServizio, 
            string nomeTask,
            CancellationToken ct = default)
        {
            for (int attempt = 1; attempt <= TaskConfigurationDefaults.MaxRetryAttempts; attempt++)
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(ct);
                    var oggi = DateOnly.FromDateTime(DateTime.Today);

                    // Cerca flag esistente per oggi
                    var flag = await context.ElaborazioneEmailGiornalieras
                        .FirstOrDefaultAsync(f => 
                            f.CodiceServizio == codiceServizio && 
                            f.DataElaborazione == oggi, 
                            ct);

                    if (flag != null && flag.Elaborata)
                    {
                        _logger.LogInformation(
                            "Email {CodiceServizio} già elaborata da {Task} il {Data}. Skip elaborazione.",
                            codiceServizio,
                            flag.ElaborataDaTask,
                            flag.ElaborataIl
                        );
                        return false; // ⏭️ Già elaborata
                    }

                    if (flag == null)
                    {
                        // Crea nuovo flag
                        flag = new ElaborazioneEmailGiornaliera
                        {
                            CodiceServizio = codiceServizio,
                            DataElaborazione = oggi,
                            Elaborata = true,
                            ElaborataIl = DateTime.UtcNow,
                            ElaborataDaTask = nomeTask
                        };
                        context.ElaborazioneEmailGiornalieras.Add(flag);
                    }
                    else
                    {
                        // Aggiorna flag esistente (non ancora elaborata)
                        flag.Elaborata = true;
                        flag.ElaborataIl = DateTime.UtcNow;
                        flag.ElaborataDaTask = nomeTask;
                    }

                    await context.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Lock acquisito su {CodiceServizio} da {Task}",
                        codiceServizio,
                        nomeTask
                    );

                    return true; // ✅ Questo task ha vinto
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Race condition: altro task ha acquisito lock
                    if (attempt < TaskConfigurationDefaults.MaxRetryAttempts)
                    {
                        _logger.LogWarning(
                            "Race condition su {CodiceServizio}, tentativo {Attempt}/{MaxRetries}. Retry in {Delay}ms...",
                            codiceServizio,
                            attempt,
                            TaskConfigurationDefaults.MaxRetryAttempts,
                            TaskConfigurationDefaults.RetryDelayMilliseconds
                        );

                        await System.Threading.Tasks.Task.Delay(TaskConfigurationDefaults.RetryDelayMilliseconds, ct);
                        continue;
                    }

                    _logger.LogWarning(
                        "Tutti i {MaxRetries} tentativi falliti per {CodiceServizio}. Altro task ha vinto la race.",
                        TaskConfigurationDefaults.MaxRetryAttempts,
                        codiceServizio
                    );

                    return false;
                }
            }

            return false; // Fallback (non dovrebbe mai arrivare qui)
        }

        /// <summary>
        /// Verifica se email già elaborata oggi per il servizio specificato.
        /// </summary>
        /// <param name="codiceServizio">Codice servizio email.</param>
        /// <param name="ct">Token cancellazione.</param>
        /// <returns>True se già elaborata oggi.</returns>
        public async System.Threading.Tasks.Task<bool> IsProcessedTodayAsync(
            string codiceServizio,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var oggi = DateOnly.FromDateTime(DateTime.Today);

            return await context.ElaborazioneEmailGiornalieras
                .AnyAsync(f => 
                    f.CodiceServizio == codiceServizio && 
                    f.DataElaborazione == oggi && 
                    f.Elaborata, 
                    ct);
        }

        /// <summary>
        /// Reset flag per permettere ri-elaborazione (solo per testing/debug).
        /// </summary>
        /// <param name="codiceServizio">Codice servizio email.</param>
        /// <param name="ct">Token cancellazione.</param>
        public async System.Threading.Tasks.Task ResetFlagAsync(
            string codiceServizio, 
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var oggi = DateOnly.FromDateTime(DateTime.Today);

            var flag = await context.ElaborazioneEmailGiornalieras
                .FirstOrDefaultAsync(f => 
                    f.CodiceServizio == codiceServizio && 
                    f.DataElaborazione == oggi, 
                    ct);

            if (flag != null)
            {
                flag.Elaborata = false;
                flag.ElaborataIl = null;
                flag.ElaborataDaTask = null;
                await context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Flag {CodiceServizio} resettato per data {Data}",
                    codiceServizio,
                    oggi
                );
            }
        }

        /// <summary>
        /// Verifica se exception è violazione unique constraint (SQL Server error 2627/2601).
        /// </summary>
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var sqlException = ex.InnerException?.InnerException as Microsoft.Data.SqlClient.SqlException;
            return sqlException?.Number is 2627 or 2601;
        }
    }
}
