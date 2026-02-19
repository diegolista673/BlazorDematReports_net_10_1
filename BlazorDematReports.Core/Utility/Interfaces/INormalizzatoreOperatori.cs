namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface INormalizzatoreOperatori
    {

        /// <summary>
        /// Carica e aggiorna la mappa dei nomi operatori da normalizzare e i relativi nomi normalizzati dal database.
        /// </summary>
        /// <returns>Task asincrono.</returns>
        Task SetNamesNormalizzatiAsync();

        /// <summary>
        /// Normalizza il nome dell'operatore applicando le seguenti regole:
        /// - Rimuove spazi multipli
        /// - Converte il nome in minuscolo
        /// - Sostituisce spazi con punti
        /// - Rimuove il prefisso "postel\"
        /// - Applica eventuali correzioni tramite la mappa dei nomi normalizzati
        /// </summary>
        /// <param name="operatore">Nome operatore da normalizzare.</param>
        /// <returns>Nome operatore normalizzato.</returns>
        string NormalizzaOperatore(string? operatore);


        /// <summary>
        /// Corregge il nome operatore ricavato da Demat restituendo il nome normalizzato se presente, altrimenti restituisce il nome originale.
        /// </summary>
        /// <param name="operatore">Nome operatore da correggere.</param>
        /// <returns>Nome operatore normalizzato o originale.</returns>
        string CorreggiOperatore(string operatore);

    }
}

