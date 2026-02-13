using System.Text.Json;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione della schedulazione CRON nei mapping configurazioni.
    /// </summary>
    public interface ICronSchedulingService
    {
        /// <summary>
        /// Estrae il valore CRON dal JSON ParametriExtra.
        /// </summary>
        string GetCronFromMapping(ConfigurazioneFaseCentro mapping);

        /// <summary>
        /// Imposta il valore CRON nel JSON ParametriExtra, preservando altri parametri.
        /// </summary>
        void SetCronForMapping(ConfigurazioneFaseCentro mapping, string cronValue);

        /// <summary>
        /// Sincronizza i CRON da una lista di mapping.
        /// </summary>
        List<string> SyncCronsFromMappings(List<ConfigurazioneFaseCentro> mappings);

        /// <summary>
        /// Verifica se il CRON è uno dei preset disponibili.
        /// </summary>
        bool IsPresetCron(string cron);

        /// <summary>
        /// Ottiene la lista dei CRON preset disponibili.
        /// </summary>
        List<(string Value, string Label)> GetPresetCrons();
    }

    /// <summary>
    /// Implementazione del servizio per la gestione della schedulazione CRON nei mapping configurazioni.
    /// </summary>
    public class CronSchedulingService : ICronSchedulingService
    {
        private static readonly List<(string Value, string Label)> PresetCrons = new()
        {
            ("0 5 * * *", "Giornaliero 05:00"),
            ("0 2 * * *", "Giornaliero 02:00"),
            ("0 */4 * * *", "Ogni 4 ore"),
            ("0 * * * *", "Ogni ora"),
            ("0 0 1 * *", "Mensile (1° giorno)"),
            ("custom", "Personalizzato")
        };

        public string GetCronFromMapping(ConfigurazioneFaseCentro mapping)
        {
            return mapping?.CronExpression ?? "0 5 * * *";
        }

        public void SetCronForMapping(ConfigurazioneFaseCentro mapping, string cronValue)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(cronValue))
                return;

            mapping.CronExpression = cronValue;
        }

        public List<string> SyncCronsFromMappings(List<ConfigurazioneFaseCentro> mappings)
        {
            if (mappings == null || mappings.Count == 0)
                return new List<string>();

            return mappings.Select(GetCronFromMapping).ToList();
        }

        public bool IsPresetCron(string cron)
        {
            return !string.IsNullOrWhiteSpace(cron) && PresetCrons.Any(p => p.Value == cron);
        }

        public List<(string Value, string Label)> GetPresetCrons()
        {
            return new List<(string, string)>(PresetCrons);
        }
    }
}
