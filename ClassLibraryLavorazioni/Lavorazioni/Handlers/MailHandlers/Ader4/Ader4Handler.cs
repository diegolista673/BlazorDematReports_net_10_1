using ClassLibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Ader4;
using LibraryLavorazioni.Lavorazioni.Constants;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Shared.Services.Email;
using LibraryLavorazioni.Utility.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Ader4
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio ADER4/Equitalia via Exchange Web Services.
    /// Gestisce email da Verona e Genova inserendo in ProduzioneSistema.
    /// </summary>
    [Description("Import dati ADER4/Equitalia da allegati email CSV (Verona + Genova)")]
    public sealed class Ader4Handler : ILavorazioneHandler
    {
        /// <inheritdoc />
        public string LavorazioneCode => LavorazioniCodes.ADER4;

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
            LavorazioneExecutionContext context, 
            CancellationToken ct = default)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<Ader4Handler>>();
            var flagService = context.ServiceProvider.GetRequiredService<EmailDailyFlagService>();

            string taskName = $"ADER4_P{context.IDProceduraLavorazione}_F{context.IDFaseLavorazione}";

            logger.LogInformation(
                "Inizio elaborazione {TaskName} per Periodo={Start}-{End}",
                taskName,
                context.StartDataLavorazione,
                context.EndDataLavorazione
            );

            // ✅ CHECK FLAG: Primo task oggi?
            bool isFirstToday = await flagService.TryMarkAsProcessingAsync(
                LavorazioniCodes.ADER4, 
                taskName, 
                ct
            );

            if (isFirstToday)
            {
                logger.LogInformation("✅ Primo task oggi. Elaborazione email completa per TUTTE le fasi...");
                return await ProcessEmailAndInsertAllDataAsync(context, logger, ct);
            }
            else
            {
                logger.LogInformation("⏭️ Email già elaborata oggi da altro task. Skip elaborazione.");
                return new List<DatiLavorazione>(); // Dati già inseriti dal primo task
            }
        }

        /// <summary>
        /// Processa email e inserisce dati per TUTTE le fasi in un colpo solo.
        /// Chiamato solo dal primo task che esegue oggi.
        /// </summary>
        private async Task<List<DatiLavorazione>> ProcessEmailAndInsertAllDataAsync(
            LavorazioneExecutionContext context,
            ILogger logger,
            CancellationToken ct)
        {
            var emailService = context.ServiceProvider.GetRequiredService<Ader4EmailService>();

            // Processa email con allegati CSV
            var emailResults = await emailService.ProcessEmailsAsync(ct);

            logger.LogInformation(
                "Email processate: Totali={Total}, Successi={Success}, Allegati={Attachments}",
                emailResults.TotalEmailsFound,
                emailResults.SuccessfulEmails.Count,
                emailResults.TotalAttachmentsDownloaded
            );

            // Converti risultati email in DatiLavorazione per TUTTE le fasi
            var datiLavorazione = ConvertEmailResultsToDatiLavorazione(emailResults, context);

            logger.LogInformation(
                "✅ Dati estratti per TUTTE le fasi: {Count} record totali",
                datiLavorazione.Count
            );

            return datiLavorazione;
        }

        /// <summary>
        /// Converte risultati elaborazione email in lista DatiLavorazione.
        /// </summary>
        private List<DatiLavorazione> ConvertEmailResultsToDatiLavorazione(
            BatchEmailProcessingResult emailResults,
            LavorazioneExecutionContext context)
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
