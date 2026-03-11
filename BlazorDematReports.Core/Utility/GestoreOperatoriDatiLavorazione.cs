using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Models.DbApplication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Servizio per la gestione e il recupero dell'elenco completo degli operatori di lavorazione.
    /// Fornisce metodi per caricare e restituire la lista degli operatori dal database, 
    /// garantendo che i dati siano normalizzati.
    /// </summary>
    public class GestoreOperatoriDatiLavorazione : IGestoreOperatoriDatiLavorazione
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private List<Operatori> _elencoOperatoriDemat = new();
        private List<OperatoreMondo> _elencoOperatoriMondo = new List<OperatoreMondo>();
        private readonly ILogger<GestoreOperatoriDatiLavorazione> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly INormalizzatoreOperatori _normalizzatoreOperatori;


        /// <summary>
        /// Inizializza una nuova istanza di <see cref="GestoreOperatoriDatiLavorazione"/>.
        /// </summary>
        /// <param name="repoContext">Factory per la creazione del contesto dati.</param>
        public GestoreOperatoriDatiLavorazione(IDbContextFactory<DematReportsContext> contextFactory,
            ILogger<GestoreOperatoriDatiLavorazione> logger,
            ILavorazioniConfigManager lavorazioniConfigManager,
            INormalizzatoreOperatori normalizzatoreOperatori)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzatoreOperatori = normalizzatoreOperatori;
        }


        /// <summary>
        /// Carica l'elenco completo degli operatori dal database.
        /// </summary>
        /// <returns>Task asincrono.</returns>
        public async Task SetOperatoriAsync()
        {
            _elencoOperatoriDemat = await _contextFactory.CreateDbContext().Operatoris.ToListAsync();
            foreach (var op in _elencoOperatoriDemat)
            {
                op.Operatore = op.Operatore.Trim();
            }
        }


        //<inheritedoc />
        public async Task<List<OperatoreMondo>> SetOperatoriEsterniMondoAsync()
        {
            try
            {
                // Ottieni la stringa di connessione al database esterno
                string? connectionString = _lavorazioniConfigManager.CnxnCaptiva206;

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Query che estrae gli operatori dalla tabella MONDO con join alla tabella dei centri
                string query = @"
                    SELECT ut.ID_UTENTE,ut.SUTENTE,stab.NOME_STABILIMENTO
                    FROM MND_UTENTE_STABILIMENTO as ut
                    left join MND_STABILIMENTI as stab on ut.ID_STAB_DEMAT = stab.ID_STAB_DEMAT
                    order by ID_UTENTE";

                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = 60;

                _elencoOperatoriMondo.Clear();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var operatore = new OperatoreMondo();

                    // Nome operatore principale
                    if (!reader.IsDBNull(0))
                    {
                        operatore.ID_UTENTE = _normalizzatoreOperatori.NormalizzaOperatore(reader.GetString(0));
                    }

                    // Nome operatore alternativo
                    if (!reader.IsDBNull(1))
                    {
                        operatore.SUTENTE = _normalizzatoreOperatori.NormalizzaOperatore(reader.GetString(1));
                    }

                    // Nome Centro 
                    if (!reader.IsDBNull(2))
                    {
                        operatore.Centro = reader.GetString(2);
                    }

                    // Aggiungi alla lista solo se almeno uno dei nomi è valido
                    if (!string.IsNullOrEmpty(operatore.ID_UTENTE) ||
                        !string.IsNullOrEmpty(operatore.SUTENTE))
                    {
                        _elencoOperatoriMondo.Add(operatore);
                    }
                }

                // Ora mappiamo i centri agli ID corrispondenti in DematReports
                await MappaIdCentriOperatoriEsterniAsync();

                _logger.LogInformation("Caricati {NumOperatori} operatori esterni con informazioni di centro", _elencoOperatoriMondo.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento degli operatori esterni");
            }

            return _elencoOperatoriMondo;
        }

        /// <summary>
        /// Mappa gli ID dei centri Demat per gli operatori esterni
        /// </summary>
        private async Task MappaIdCentriOperatoriEsterniAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var centriDemat = await context.CentriLavoraziones.ToListAsync();

            // Creiamo un dizionario dei nomi centro normalizzati -> ID centro per lookup veloci
            var mappaIdCentri = centriDemat.ToDictionary(
                c => _normalizzatoreOperatori.NormalizzaOperatore(c.Centro),
                c => c.Idcentro,
                StringComparer.OrdinalIgnoreCase
            );

            int operatoriMappati = 0;
            int operatoriNonMappati = 0;

            foreach (var operatore in _elencoOperatoriMondo)
            {
                if (!string.IsNullOrEmpty(operatore.Centro) &&
                    mappaIdCentri.TryGetValue(operatore.Centro, out int idCentro))
                {
                    operatore.IdCentro = idCentro;
                    operatoriMappati++;
                }
                else if (!string.IsNullOrEmpty(operatore.Centro))
                {
                    operatoriNonMappati++;
                    _logger.LogWarning("Non è stato possibile mappare il centro '{Centro}' per l'operatore {Operatore}",
                        operatore.Centro, operatore.ID_UTENTE);
                }
            }

            _logger.LogInformation("Mappatura centri completata: {Mappati} operatori mappati, {NonMappati} non mappati",
                operatoriMappati, operatoriNonMappati);
        }



        //<inheritedoc />
        public OperatoreMondo? TrovaOperatoreMondo(string? operatore)
        {
            if (string.IsNullOrWhiteSpace(operatore))
                return null;

            // Cerca prima tra i nomi principali
            var operatoreTrovato = _elencoOperatoriMondo.FirstOrDefault(op =>
                string.Equals(op.ID_UTENTE, operatore, StringComparison.OrdinalIgnoreCase));

            // Se non trovato, cerca tra i nomi alternativi
            if (operatoreTrovato == null)
            {
                operatoreTrovato = _elencoOperatoriMondo.FirstOrDefault(op =>
                    string.Equals(op.SUTENTE, operatore, StringComparison.OrdinalIgnoreCase));
            }

            return operatoreTrovato;
        }



        /// <summary>
        /// Ottiene l'ID del centro DematReports corrispondente al nome del centro Mondo
        /// </summary>
        /// <param name="centro">Nome del centro nel database Mondo</param>
        /// <returns>ID del centro corrispondente in DematReports, o 0 se non trovato</returns>
        public async Task<int> GetIDCentroDematFromMondo(string centro)
        {
            if (string.IsNullOrEmpty(centro))
                return 0;

            using var context = _contextFactory.CreateDbContext();
            var centroDemat = await context.CentriLavoraziones
                .FirstOrDefaultAsync(c => c.Centro == centro);

            if (centroDemat == null)
            {
                _logger.LogWarning("Centro '{Centro}' non trovato nel database DematReports", centro);
                return 0;
            }

            _logger.LogInformation("Centro '{Centro}' mappato con ID {IdCentro}", centro, centroDemat.Idcentro);
            return centroDemat.Idcentro;
        }


        /// <summary>
        /// Aggiunge un nuovo operatore al database DematReports con l'ID centro corretto
        /// </summary>
        /// <param name="oper">Nome dell'operatore da aggiungere</param>
        /// <param name="idCentro">ID del centro di appartenenza predefinito</param>
        /// <param name="nomeCentroMondo">Nome del centro Mondo (opzionale)</param>
        /// <returns>ID del nuovo operatore inserito</returns>
        public async Task<int> AddOperatoreDematAsync(string oper, int idCentro)
        {

            Operatori operatore = new Operatori
            {
                Operatore = oper.ToLower(),
                Idcentro = idCentro,
                Azienda = "POSTEL",
                IdRuolo = 5, // user
                FlagOperatoreAttivo = true
            };

            using var context = _contextFactory.CreateDbContext();

            // Verifica se l'operatore esiste già con lo stesso nome (EF Core non supporta StringComparison in LINQ,
            // il confronto è case-insensitive grazie alla collation SQL Server)
            var operatoreEsistente = await context.Operatoris
                .FirstOrDefaultAsync(o => o.Operatore == operatore.Operatore);

            if (operatoreEsistente != null)
            {
                _logger.LogInformation("Operatore '{Operatore}' già esistente nel database con ID {IdOperatore}", operatore.Operatore, operatoreEsistente.Idoperatore);
                return operatoreEsistente.Idoperatore;
            }

            context.Operatoris.Add(operatore);
            await context.SaveChangesAsync();

            _logger.LogInformation("Operatore '{Operatore}' aggiunto con successo al centro {IdCentro} con ID {IdOperatore}", operatore.Operatore, idCentro, operatore.Idoperatore);

            return operatore.Idoperatore;
        }



        //<inheritedoc />
        public IEnumerable<Operatori>? GetOperatoriDemat()
        {
            return _elencoOperatoriDemat;
        }

        public IEnumerable<OperatoreMondo>? GetOperatoriMondo()
        {
            return _elencoOperatoriMondo;
        }
    }
}

