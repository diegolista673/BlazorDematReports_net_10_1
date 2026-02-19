namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO per rappresentare il dettaglio di un mapping fase/centro/crontab all'interno di una configurazione.
    /// </summary>
    public class MappingDettaglioDto
    {
        public string NomeProcedura { get; set; } = null!;
        public string NomeFase { get; set; } = null!;
        public string NomeCentro { get; set; } = null!;
        public string Cron { get; set; } = "0 5 * * *";
        public string? ParametriExtra { get; set; }
    }
}
