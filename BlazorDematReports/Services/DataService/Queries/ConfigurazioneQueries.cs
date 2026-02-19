using Entities.Models.DbApplication;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using BlazorDematReports.Core.Constants;

namespace BlazorDematReports.Services.DataService.Queries
{
    /// <summary>
    /// Query ottimizzate per ConfigurazioneFontiDati con projection diretta a DTO.
    /// Evita il caricamento di entità complete e riduce il numero di query.
    /// </summary>
    public static class ConfigurazioneQueries
    {
        /// <summary>
        /// Recupera configurazione con mappings in una singola query ottimizzata.
        /// Usa projection per caricare solo i campi necessari.
        /// </summary>
        public static IQueryable<ConfigurazioneConMappingsDto> GetConfigurazioneWithMappings(
            this DbSet<ConfigurazioneFontiDati> configurazioni)
        {
            return configurazioni
                .Select(c => new ConfigurazioneConMappingsDto
                {
                    IdConfigurazione = c.IdConfigurazione,
                    CodiceConfigurazione = c.CodiceConfigurazione,
                    DescrizioneConfigurazione = c.DescrizioneConfigurazione,
                    TipoFonte = c.TipoFonte,
                    ConnectionStringName = c.ConnectionStringName,
                    HandlerClassName = c.HandlerClassName,
                    
                    // Projection diretta senza Include espliciti
                    Mappings = c.ConfigurazioneFaseCentros
                        .Where(fc => fc.FlagAttiva)
                        .Select(fc => new MappingInfoDto
                        {
                            IdFaseCentro = fc.IdFaseCentro,
                            IdProceduraLavorazione = fc.IdProceduraLavorazione,
                            IdFaseLavorazione = fc.IdFaseLavorazione,
                            IdCentro = fc.IdCentro,
                            
                            // Campi denormalizzati (no navigation properties)
                            NomeProcedura = fc.IdProceduraLavorazioneNavigation != null 
                                ? fc.IdProceduraLavorazioneNavigation.NomeProcedura 
                                : "N/A",
                            NomeFase = fc.IdFaseLavorazioneNavigation != null
                                ? fc.IdFaseLavorazioneNavigation.FaseLavorazione
                                : "N/A",
                            NomeCentro = fc.IdCentroNavigation != null
                                ? fc.IdCentroNavigation.Centro
                                : "N/A",

                            CronExpression = fc.CronExpression ?? TaskConfigurationDefaults.DefaultCronExpression,
                            TestoQueryTask = fc.TestoQueryTask,
                            HandlerClassName = fc.HandlerClassName,
                            GiorniPrecedenti = fc.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti,
                            FlagAttiva = fc.FlagAttiva
                        })
                        .ToList()
                });
        }

        /// <summary>
        /// Recupera solo le informazioni essenziali per la lista configurazioni.
        /// </summary>
        public static IQueryable<ConfigurazioneSummaryDto> GetConfigurazioniSummary(
            this DbSet<ConfigurazioneFontiDati> configurazioni)
        {
            return configurazioni
                .Select(c => new ConfigurazioneSummaryDto
                {
                    IdConfigurazione = c.IdConfigurazione,
                    CodiceConfigurazione = c.CodiceConfigurazione,
                    DescrizioneConfigurazione = c.DescrizioneConfigurazione,
                    TipoFonte = c.TipoFonte,
                    NumeroMappings = c.ConfigurazioneFaseCentros.Count(fc => fc.FlagAttiva),
                    CreatoIl = c.CreatoIl ?? DateTime.MinValue,
                    ModificatoIl = c.ModificatoIl
                });
        }
    }

    #region DTOs

    /// <summary>
    /// DTO ottimizzato per ConfigurazioneFontiDati con mappings.
    /// Tutti i campi sono denormalizzati per evitare lazy loading.
    /// </summary>
    public class ConfigurazioneConMappingsDto
    {
        public int IdConfigurazione { get; set; }
        public string CodiceConfigurazione { get; set; } = null!;
        public string? DescrizioneConfigurazione { get; set; }
        public TipoFonteData TipoFonte { get; set; }
        public string? ConnectionStringName { get; set; }
        public string? HandlerClassName { get; set; }
        public List<MappingInfoDto> Mappings { get; set; } = new();
    }

    /// <summary>
    /// DTO per informazioni mapping denormalizzate.
    /// </summary>
    public class MappingInfoDto
    {
        public int IdFaseCentro { get; set; }
        public int IdProceduraLavorazione { get; set; }
        public int IdFaseLavorazione { get; set; }
        public int IdCentro { get; set; }
        
        // Campi denormalizzati (no lazy loading)
        public string NomeProcedura { get; set; } = null!;
        public string NomeFase { get; set; } = null!;
        public string NomeCentro { get; set; } = null!;
        
        public string CronExpression { get; set; } = null!;
        public string? TestoQueryTask { get; set; }
        public string? HandlerClassName { get; set; }
        public int GiorniPrecedenti { get; set; }
        public bool FlagAttiva { get; set; }
    }

    /// <summary>
    /// DTO per lista configurazioni (solo campi essenziali).
    /// </summary>
    public class ConfigurazioneSummaryDto
    {
        public int IdConfigurazione { get; set; }
        public string CodiceConfigurazione { get; set; } = null!;
        public string? DescrizioneConfigurazione { get; set; }
        public TipoFonteData TipoFonte { get; set; }
        public int NumeroMappings { get; set; }
        public DateTime CreatoIl { get; set; }
        public DateTime? ModificatoIl { get; set; }
    }

    #endregion
}
