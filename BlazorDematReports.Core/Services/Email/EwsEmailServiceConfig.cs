using Microsoft.Exchange.WebServices.Data;

namespace BlazorDematReports.Core.Services.Email
{
    /// <summary>
    /// Configurazione per il servizio email Exchange Web Services.
    /// </summary>
    public sealed class EwsEmailServiceConfig
    {
        /// <summary>
        /// Username per autenticazione Exchange.
        /// </summary>
        public required string Username { get; init; }

        /// <summary>
        /// Password per autenticazione Exchange.
        /// </summary>
        public required string Password { get; init; }

        /// <summary>
        /// Dominio Active Directory.
        /// </summary>
        public required string Domain { get; init; }

        /// <summary>
        /// URL del servizio Exchange Web Services.
        /// </summary>
        public required Uri ExchangeUrl { get; init; }

        /// <summary>
        /// Versione Exchange (default: Exchange2013_SP1).
        /// </summary>
        public ExchangeVersion ExchangeVersion { get; init; } = ExchangeVersion.Exchange2013_SP1;

        /// <summary>
        /// Filtri subject per ricerca email (OR logic).
        /// </summary>
        public required IReadOnlyCollection<string> SubjectFilters { get; init; }

        /// <summary>
        /// Pattern per filtrare nomi allegati (es. "*.csv", "file di produzione*").
        /// </summary>
        public IReadOnlyCollection<string>? AttachmentPatterns { get; init; }

        /// <summary>
        /// Nome cartella Exchange per archiviazione email processate.
        /// </summary>
        public required string ArchiveFolderName { get; init; }

        /// <summary>
        /// Path directory locale per salvataggio allegati temporanei.
        /// </summary>
        public required string LocalAttachmentPath { get; init; }

        /// <summary>
        /// Path directory locale per archivio zip.
        /// </summary>
        public required string LocalArchivePath { get; init; }

        /// <summary>
        /// Numero massimo email da processare per esecuzione (default: 100).
        /// </summary>
        public int MaxEmailsPerRun { get; init; } = 100;

        /// <summary>
        /// Indica se creare automaticamente zip degli allegati dopo processamento.
        /// </summary>
        public bool CreateZipArchive { get; init; } = true;

        /// <summary>
        /// Indica se eliminare allegati locali dopo creazione zip.
        /// </summary>
        public bool CleanupAfterProcessing { get; init; } = true;
    }
}
