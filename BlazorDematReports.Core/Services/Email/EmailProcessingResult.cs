namespace BlazorDematReports.Core.Services.Email
{
    /// <summary>
    /// Risultato elaborazione singola email con allegati.
    /// </summary>
    public sealed class EmailProcessingResult
    {
        /// <summary>
        /// Subject dell'email processata.
        /// </summary>
        public string Subject { get; init; } = string.Empty;

        /// <summary>
        /// Data ricezione email.
        /// </summary>
        public DateTime ReceivedDate { get; init; }

        /// <summary>
        /// Corpo testuale email.
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        /// Indica se l'email è stata processata con successo.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Messaggio errore (se Success = false).
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Lista allegati trovati e scaricati.
        /// </summary>
        public IReadOnlyCollection<AttachmentInfo> Attachments { get; init; } = Array.Empty<AttachmentInfo>();

        /// <summary>
        /// Metadata estratti dal body email (es. ID evento, data riferimento).
        /// </summary>
        public Dictionary<string, string>? ExtractedMetadata { get; init; }

        /// <summary>
        /// Indica se l'email aveva allegati.
        /// </summary>
        public bool HasAttachments => Attachments.Any();
    }

    /// <summary>
    /// Informazioni su allegato email scaricato.
    /// </summary>
    public sealed class AttachmentInfo
    {
        /// <summary>
        /// Nome file allegato originale.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// Path completo file salvato localmente.
        /// </summary>
        public required string LocalFilePath { get; init; }

        /// <summary>
        /// Dimensione file in bytes.
        /// </summary>
        public long FileSizeBytes { get; init; }

        /// <summary>
        /// Indica se il file corrisponde a un pattern configurato.
        /// </summary>
        public bool MatchesPattern { get; init; }

        /// <summary>
        /// Tipo MIME allegato.
        /// </summary>
        public string? ContentType { get; init; }
    }

    /// <summary>
    /// Risultato batch elaborazione email.
    /// </summary>
    public sealed class BatchEmailProcessingResult
    {
        /// <summary>
        /// Email processate con successo.
        /// </summary>
        public IReadOnlyCollection<EmailProcessingResult> SuccessfulEmails { get; init; } = Array.Empty<EmailProcessingResult>();

        /// <summary>
        /// Email con errori.
        /// </summary>
        public IReadOnlyCollection<EmailProcessingResult> FailedEmails { get; init; } = Array.Empty<EmailProcessingResult>();

        /// <summary>
        /// Totale email trovate.
        /// </summary>
        public int TotalEmailsFound { get; init; }

        /// <summary>
        /// Totale allegati scaricati.
        /// </summary>
        public int TotalAttachmentsDownloaded => SuccessfulEmails.Sum(e => e.Attachments.Count);

        /// <summary>
        /// Path file zip creato (se abilitato).
        /// </summary>
        public string? ZipArchivePath { get; init; }
    }
}
