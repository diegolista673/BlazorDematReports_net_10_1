using BlazorDematReports.Core.Utility.Interfaces;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Servizio per la normalizzazione dei nomi degli operatori.
    /// <para>
    /// Gestisce la correzione, la pulizia e la normalizzazione dei nomi operatori acquisiti dai dati di produzione,
    /// confrontandoli con la tabella degli operatori normalizzati e applicando regole di formattazione standard.
    /// </para>
    /// </summary>
    public class NormalizzatoreOperatori : INormalizzatoreOperatori
    {
        /// <summary>
        /// Factory per la creazione del contesto dati.
        /// </summary>
        private readonly IDbContextFactory<DematReportsContext> _repoContext;

        /// <summary>
        /// Logger per la classe NormalizzatoreOperatori.
        /// </summary>
        private readonly ILogger<NormalizzatoreOperatori> _logger;

        /// <summary>
        /// Configurazione delle lavorazioni.
        /// </summary>
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Dizionario per mappare i nomi da normalizzare ai nomi normalizzati.
        /// </summary>
        private readonly Dictionary<string, string> _namesNormalizzati = new();


        /// <summary>
        /// Inizializza una nuova istanza di <see cref="NormalizzatoreOperatori"/>.
        /// </summary>
        /// <param name="repoContext">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per la classe NormalizzatoreOperatori.</param>
        /// <param name="lavorazioniConfigManager">Configurazione delle lavorazioni.</param>
        public NormalizzatoreOperatori(
            IDbContextFactory<DematReportsContext> repoContext,
            ILogger<NormalizzatoreOperatori> logger,
            ILavorazioniConfigManager lavorazioniConfigManager)
        {
            _repoContext = repoContext;
            _logger = logger;
            _lavorazioniConfigManager = lavorazioniConfigManager;
        }

        /// <summary>
        /// Sostituisce tutte le occorrenze di un pattern regex in una stringa con un'altra stringa.
        /// </summary>
        /// <param name="operName">Stringa su cui effettuare la sostituzione.</param>
        /// <param name="regexPattern">Pattern regex da cercare.</param>
        /// <param name="replacePattern">Stringa di sostituzione.</param>
        /// <returns>Stringa risultante dopo la sostituzione.</returns>
        public virtual string CleanInput(string operName, string regexPattern, string replacePattern)
        {
            MatchCollection regexResults = Regex.Matches(operName, regexPattern);
            int matchesCount = regexResults.Count;
            if (matchesCount > 0)
            {
                foreach (Match elemento in regexResults)
                {
                    operName = operName.Replace(regexPattern, replacePattern);
                }
            }

            return operName;
        }

        /// <inheritdoc />
        public async Task SetNamesNormalizzatiAsync()
        {
            _namesNormalizzati.Clear();
            var elenco = await _repoContext.CreateDbContext().OperatoriNormalizzatis.ToListAsync().ConfigureAwait(false);

            foreach (var el in elenco)
            {
                _namesNormalizzati.Add(el.OperatoreDaNormalizzare, el.OperatoreNormalizzato);
            }
        }

        /// <inheritdoc />
        public string CorreggiOperatore(string operatore)
        {
            return _namesNormalizzati.TryGetValue(operatore, out var normalizzato) ? normalizzato : operatore;
        }

        /// <inheritdoc />
        public string NormalizzaOperatore(string? operatore)
        {
            if (string.IsNullOrWhiteSpace(operatore))
                return string.Empty;

            string normalizzato = Regex.Replace(operatore, @"\s+", " ").Trim().ToLower();
            normalizzato = normalizzato.Replace(" ", ".").Replace(@"postel\", "");
            normalizzato = CleanInput(normalizzato, "(.operatore)", "");
            return CorreggiOperatore(normalizzato);
        }






    }
}
