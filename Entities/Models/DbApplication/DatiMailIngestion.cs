namespace Entities.Models.DbApplication;

/// <summary>
/// Tabella staging per dati aggregati provenienti da ingestion email.
/// Un job mail generico legge tutte le email (ADER4, HERA16, etc.) e salva qui i dati aggregati.
/// I task produzione fase-specifici leggono da questa tabella filtrati per servizio/tipo/centro
/// e inseriscono in ProduzioneSistema.
/// </summary>
public partial class DatiMailIngestion
{
    /// <summary>
    /// ID univoco del record (IDENTITY).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Codice servizio email (es. 'ADER4', 'HERA16').
    /// </summary>
    public string CodiceServizio { get; set; } = null!;

    /// <summary>
    /// Data di riferimento dei dati riportata nell'email.
    /// </summary>
    public DateOnly DataRiferimento { get; set; }

    /// <summary>
    /// Tipo di dato aggregato (es. 'ScansioneCaptiva', 'ScansioneSorter', 'Classificazione').
    /// </summary>
    public string TipoDato { get; set; } = null!;

    /// <summary>
    /// Centro di lavorazione (es. 'VERONA', 'GENOVA'), NULL se dato aggregato non specifico.
    /// </summary>
    public string? Centro { get; set; }

    /// <summary>
    /// Quantità aggregata (Documenti/Fogli/Pagine a seconda del TipoDato).
    /// </summary>
    public int Quantita { get; set; }

    /// <summary>
    /// Data/ora ingestion (UTC).
    /// </summary>
    public DateTime DataIngestione { get; set; }

    /// <summary>
    /// Flag: indica se il dato è già stato consumato da un task produzione.
    /// </summary>
    public bool Elaborata { get; set; }

    /// <summary>
    /// Data/ora elaborazione da parte del task produzione (NULL se non ancora elaborato).
    /// </summary>
    public DateTime? ElaborataIl { get; set; }

    /// <summary>
    /// ID del task che ha elaborato il dato (soft FK a TaskDaEseguire, nullable).
    /// </summary>
    public int? ElaborataDaTaskId { get; set; }

    /// <summary>
    /// JSON opzionale per metadata aggiuntivi (es. ID evento, dettagli specifici servizio).
    /// </summary>
    public string? MetadataJson { get; set; }
}
