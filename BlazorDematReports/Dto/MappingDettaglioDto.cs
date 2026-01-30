namespace BlazorDematReports.Dto
{
    public class MappingDettaglioDto
    {
        public string NomeProcedura { get; set; } = null!;
        public string NomeFase { get; set; } = null!;
        public string NomeCentro { get; set; } = null!;
        public string Cron { get; set; } = "0 5 * * *";
        public string? ParametriExtra { get; set; }
    }
}
