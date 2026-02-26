using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Services.Email;
using BlazorDematReports.Core.Utility.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio ADER4/Equitalia via Exchange Web Services.
    /// Gestisce email da Verona e Genova inserendo in ProduzioneSistema.
    /// </summary>
    [Description("Import dati ADER4/Equitalia da allegati email CSV (Verona + Genova)")]
    public sealed class Ader4Handler : IProductionDataHandler
    {
        private readonly ILogger<Ader4Handler> _logger;
        private readonly EmailDailyFlagService _flagService;
        private readonly Ader4EmailService _emailService;

        public Ader4Handler(
            ILogger<Ader4Handler> logger,
            EmailDailyFlagService flagService,
            Ader4EmailService emailService)
        {
            _logger       = logger;
            _flagService  = flagService;
            _emailService = emailService;
        }

        /// <inheritdoc />
        public string HandlerCode => LavorazioniCodes.ADER4;

        /// <inheritdoc />
        public string? GetServiceCode() => LavorazioniCodes.ADER4;

        /// <inheritdoc />
        public HandlerMetadata GetMetadata() => new()
        {
            ServiceCode = LavorazioniCodes.ADER4,
            RequiresEmailService = true,
            Category = "Mail Import"
        };

        /// <inheritdoc />
        public async Task<List<DatiLavorazione>> ExecuteAsync(
            ProductionExecutionContext context,
            CancellationToken ct = default)
        {
            string taskName = $"ADER4_P{context.IDProceduraLavorazione}_F{context.IDFaseLavorazione}";

            _logger.LogInformation(
                "Inizio elaborazione {TaskName} per Periodo={Start}-{End}",
                taskName,
                context.StartDataLavorazione,
                context.EndDataLavorazione
            );

            bool isFirstToday = await _flagService.TryMarkAsProcessingAsync(
                LavorazioniCodes.ADER4,
                taskName,
                ct
            );

            if (isFirstToday)
            {
                _logger.LogInformation("Primo task oggi. Elaborazione email completa per TUTTE le fasi...");
                try
                {
                    return await ProcessEmailAndInsertAllDataAsync(context, ct);
                }
                catch (Exception ex)
                {
                    // Rollback del flag: se l'elaborazione fallisce il giorno non deve restare bloccato.
                    // MarkAsFailedAsync usa CancellationToken.None per garantire il reset
                    // anche quando il token originale è già cancellato.
                    _logger.LogError(ex, "Errore durante elaborazione ADER4 in {TaskName}", taskName);
                    await _flagService.MarkAsFailedAsync(LavorazioniCodes.ADER4, taskName);
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Email gia elaborata oggi da altro task. Skip elaborazione.");
                return new List<DatiLavorazione>();
            }
        }

        /// <summary>
        /// Processa email e inserisce dati per TUTTE le fasi in un colpo solo.
        /// Chiamato solo dal primo task che esegue oggi.
        /// </summary>
        private async Task<List<DatiLavorazione>> ProcessEmailAndInsertAllDataAsync(
            ProductionExecutionContext context,
            CancellationToken ct)
        {
            var emailResults = await _emailService.ProcessEmailsAsync(ct);

            _logger.LogInformation(
                "Email processate: Totali={Total}, Successi={Success}, Allegati={Attachments}",
                emailResults.TotalEmailsFound,
                emailResults.SuccessfulEmails.Count,
                emailResults.TotalAttachmentsDownloaded
            );

            var datiLavorazione = ConvertEmailResultsToDatiLavorazione(emailResults, context);

            _logger.LogInformation(
                "Dati estratti per TUTTE le fasi: {Count} record totali",
                datiLavorazione.Count
            );

            return datiLavorazione;
        }

        /// <summary>
        /// Converte risultati elaborazione email in lista DatiLavorazione.
        /// </summary>
        private List<DatiLavorazione> ConvertEmailResultsToDatiLavorazione(
            BatchEmailProcessingResult emailResults,
            ProductionExecutionContext context)
        {
            var datiLavorazione = new List<DatiLavorazione>();

            foreach (var email in emailResults.SuccessfulEmails)
            {
                if (!email.ExtractedMetadata.TryGetValue("DataRiferimento", out var dataRifStr))
                    continue;

                if (!DateTime.TryParse(dataRifStr, out var dataRiferimento))
                    continue;

                // Estrai totali da metadata
                var scansioneCaptiva = GetIntMetadata(email.ExtractedMetadata, "ScansioneCaptiva");
                var scansioneSorter = GetIntMetadata(email.ExtractedMetadata, "ScansioneSorter");
                var scansioneSorterBuste = GetIntMetadata(email.ExtractedMetadata, "ScansioneSorterBuste");

                // Aggiungi record per ogni tipo scansione (se > 0)
                if (scansioneCaptiva > 0)
                {
                    datiLavorazione.Add(new DatiLavorazione
                    {
                        Operatore = "SISTEMA", // Dati aggregati sistema
                        DataLavorazione = dataRiferimento,
                        Documenti = scansioneCaptiva,
                        Fogli = scansioneCaptiva / 2, // Convenzione: 2 pagine = 1 foglio
                        Pagine = scansioneCaptiva,
                        AppartieneAlCentroSelezionato = true
                    });
                }

                if (scansioneSorter > 0)
                {
                    datiLavorazione.Add(new DatiLavorazione
                    {
                        Operatore = "SISTEMA_SORTER",
                        DataLavorazione = dataRiferimento,
                        Documenti = scansioneSorter,
                        Fogli = scansioneSorter / 2,
                        Pagine = scansioneSorter,
                        AppartieneAlCentroSelezionato = true
                    });
                }

                if (scansioneSorterBuste > 0)
                {
                    datiLavorazione.Add(new DatiLavorazione
                    {
                        Operatore = "SISTEMA_SORTER_BUSTE",
                        DataLavorazione = dataRiferimento,
                        Documenti = scansioneSorterBuste,
                        Fogli = scansioneSorterBuste,
                        Pagine = scansioneSorterBuste,
                        AppartieneAlCentroSelezionato = true
                    });
                }
            }

            return datiLavorazione;
        }

        /// <summary>
        /// Estrae valore intero da metadata dictionary.
        /// </summary>
        private static int GetIntMetadata(Dictionary<string, string>? metadata, string key)
        {
            if (metadata == null || !metadata.TryGetValue(key, out var value))
                return 0;

            return int.TryParse(value, out var result) ? result : 0;
        }
    }
}
