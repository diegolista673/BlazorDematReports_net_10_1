namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO di utilità per rappresentare un'opzione selezionabile (es. in una select).
    /// </summary>
    public class Option
    {
        /// <summary>
        /// Tipo o valore dell'opzione.
        /// </summary>
        public int? Type { get; set; }
        /// <summary>
        /// Descrizione testuale dell'opzione.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Costruttore che inizializza una nuova opzione.
        /// </summary>
        public Option(int? type, string? description)
        {
            Type = type;
            Description = description;
        }

        /// <summary>
        /// Confronta due opzioni in base alla descrizione (necessario per MudSelect).
        /// </summary>
        public override bool Equals(object? o)
        {
            var other = o as Option;
            return other?.Description == Description;
        }

        /// <summary>
        /// Restituisce l'hash code basato sulla descrizione.
        /// </summary>
        public override int GetHashCode() => Description?.GetHashCode() ?? 0;

        /// <summary>
        /// Restituisce la descrizione come stringa (necessario per la visualizzazione in MudSelect).
        /// </summary>
        public override string ToString() => Description!;
    }
}
