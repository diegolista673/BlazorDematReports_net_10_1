using Microsoft.Exchange.WebServices.Data;

namespace BlazorDematReports.Core.Services.Email
{
    /// <summary>
    /// Configurazione per il servizio email Exchange Web Services.
    /// Gli allegati vengono letti interamente in memoria: nessun file viene scritto su disco.
    /// </summary>
    public sealed class EwsEmailServiceConfig
    {
        /// <summary>Username per autenticazione Exchange.</summary>
        public required string Username { get; init; }

        /// <summary>Password per autenticazione Exchange.</summary>
        public required string Password { get; init; }

        /// <summary>Dominio Active Directory.</summary>
        public required string Domain { get; init; }

        /// <summary>URL del servizio Exchange Web Services.</summary>
        public required Uri ExchangeUrl { get; init; }

        /// <summary>Versione Exchange (default: Exchange2013_SP1).</summary>
        public ExchangeVersion ExchangeVersion { get; init; } = ExchangeVersion.Exchange2013_SP1;

        /// <summary>Filtri subject per ricerca email (OR logic).</summary>
        public required IReadOnlyCollection<string> SubjectFilters { get; init; }

        /// <summary>Pattern per filtrare nomi allegati (es. "*.csv", "file di produzione*").</summary>
        public IReadOnlyCollection<string>? AttachmentPatterns { get; init; }

        /// <summary>Nome cartella Exchange per archiviazione email processate.</summary>
        public required string ArchiveFolderName { get; init; }

        /// <summary>Numero massimo email da processare per esecuzione (default: 100).</summary>
        public int MaxEmailsPerRun { get; init; } = 100;

        /// <summary>
        /// Filtri subject ampi per identificare email da archiviare senza processare (OR logic).
        /// Le email che corrispondono a questi filtri ma NON ai <see cref="SubjectFilters"/> vengono
        /// spostate in archivio senza leggere allegati.
        /// </summary>
        public IReadOnlyCollection<string>? ArchiveUnmatchedSubjectFilters { get; init; }
    }
}
