namespace BlazorDematReports.Dto
{
    public class ConfigurazioneRiepilogoDto
    {
        public int IdConfigurazione { get; set; }
        public string CodiceConfigurazione { get; set; } = null!;
        public string Descrizione { get; set; } = null!;
        public string TipoFonte { get; set; } = null!;
        public DateTime CreatoIl { get; set; }
        public int NumeroFasi { get; set; }
        public int TaskAttivi { get; set; }
        
        // Nuovi campi per dettagli
        public List<string> FasiDettaglio { get; set; } = new();
        public List<string> CronExpressions { get; set; } = new();
        public List<MappingDettaglioDto> MappingDettaglio { get; set; } = new();
    }
}
