namespace LibraryLavorazioni.Utility
{
    /// <summary>
    /// Attributo per associare una classe di lavorazione al valore del campo <c>NomeProceduraProgramma</c>
    /// presente nella tabella <c>procedure_lavorazioni</c>.
    /// <para>
    /// Utilizzare questo attributo sulle classi di lavorazione per identificarle in modo univoco
    /// in base al nome della procedura di programma.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ProcessingLavorazioneAttribute : Attribute
    {
        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ProcessingLavorazioneAttribute"/>.
        /// </summary>
        /// <param name="NomeProceduraProgramma">
        /// Valore da associare alla proprietà <see cref="NomeProceduraProgramma"/>, corrispondente al campo in tabella.
        /// </param>
        public ProcessingLavorazioneAttribute(string NomeProceduraProgramma)
        {
            this.NomeProceduraProgramma = NomeProceduraProgramma;
        }

        /// <summary>
        /// Nome della procedura di programma associata alla classe di lavorazione.
        /// </summary>
        public string NomeProceduraProgramma { get; }
    }
}

