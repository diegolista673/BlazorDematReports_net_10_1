using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Components.Shared
{
    /// <summary>
    /// Proprietà lista DTO e entità DB per BaseComponentPage.
    /// </summary>
    public partial class BaseComponentPage<TLogger>
    {
        #region Region Property Dto

        /// <summary>List Objects ProduzioneSistemaDto used by Form data</summary>
        protected List<ProduzioneSistemaDto>? ListProduzioneSistemaDto { get; set; }

        /// <summary>List Objects ProcedureLavorazioniDto</summary>
        protected List<ProcedureLavorazioniDto>? ListProcedureLavorazioniDto { get; set; }

        /// <summary>List Objects OperatoriDto</summary>
        protected List<OperatoriDto>? ListOperatoriDto { get; set; }

        /// <summary>List Objects ListClientiDto</summary>
        protected List<ClientiDto>? ListClientiDto { get; set; }

        /// <summary>List Objects LstFasiLavorazioneDto</summary>
        protected List<FasiLavorazioneDto>? ListFasiLavorazioneDto { get; set; }

        /// <summary>List Objects ListProcedureClienteDto</summary>
        protected List<ProcedureClienteDto>? ListProcedureClienteDto { get; set; }

        /// <summary>List Objects ListTipologieTotaliDto</summary>
        protected List<TipologieTotaliDto>? ListTipologieTotaliDto { get; set; }

        /// <summary>List Objects ListOperatoriNormalizzatiDto</summary>
        protected List<OperatoriNormalizzatiDto>? ListOperatoriNormalizzatiDto { get; set; }

        /// <summary>List Objects ListLavorazioniFasiTipoTotaleDto</summary>
        protected List<LavorazioniFasiTipoTotaleDto>? ListLavorazioniFasiTipoTotaleDto { get; set; }

        /// <summary>List Objects ListProduzioneOperatoriDto</summary>
        protected List<ProduzioneOperatoriDto>? ListProduzioneOperatoriDto { get; set; }

        /// <summary>List Objects ListTaskDataReadingAggiornamentoDto</summary>
        protected List<TaskDataReadingAggiornamentoDto>? ListTaskDataReadingAggiornamentoDto { get; set; }

        /// <summary>List Objects RuoliDto</summary>
        protected List<RuoliDto>? ListRuoliDto { get; set; }

        #endregion


        #region Region Property Objects DB

        /// <summary>List Objects Ruoli used by Form data</summary>
        protected List<Ruoli>? ListRuoli { get; set; }

        /// <summary>List Objects Operatori used by Form data</summary>
        protected List<Operatori>? ListOperatori { get; set; }

        /// <summary>List Objects Procedure Lavorazioni used by Form data</summary>
        protected List<ProcedureLavorazioni>? ListProcedureLavorazioni { get; set; }

        /// <summary>List Objects Fasi Lavorazioni used by Form data</summary>
        public List<FasiLavorazione>? ListFasiLavorazione { get; set; }

        /// <summary>List Objects Centri used by Form data</summary>
        protected List<CentriLavorazione>? ListCentri { get; set; }

        /// <summary>List Objects Tipologie Totali used by Form data</summary>
        protected List<TipologieTotali>? ListTipologieTotali { get; set; }

        /// <summary>List Objects Clienti used by Form data</summary>
        protected List<Clienti>? ListClienti { get; set; }

        /// <summary>List Objects ProcedureCliente used by Form data</summary>
        protected List<ProcedureCliente>? ListProcedureCliente { get; set; }

        /// <summary>List Objects Reparti used by Form data</summary>
        protected List<RepartiProduzione>? ListReparti { get; set; }

        /// <summary>List Objects FormatoDati used by Form data</summary>
        protected List<FormatoDati>? ListFormatoDati { get; set; }

        /// <summary>List Objects Turni used by Form data</summary>
        protected List<Turni>? ListTurni { get; set; }

        /// <summary>List Objects TipoTurni used by Form data</summary>
        protected List<TipoTurni>? ListTipoTurni { get; set; }

        protected List<TaskDataReadingAggiornamento>? ListTaskDataReadingAggiornamento { get; set; }

        /// <summary>List Objects Task used by Form data</summary>
        public List<TabellaTask>? ListTask { get; set; }

        #endregion

        /// <summary>
        /// Azzera tutte le liste caricate in memoria dal componente.
        /// Chiamato da <see cref="DisposeAsync"/> al termine del ciclo di vita del componente,
        /// consentendo al GC di liberare la memoria non appena il circuito SignalR viene chiuso.
        /// </summary>
        protected void ClearCollections()
        {
            // DTO lists
            ListProduzioneSistemaDto = null;
            ListProcedureLavorazioniDto = null;
            ListOperatoriDto = null;
            ListClientiDto = null;
            ListFasiLavorazioneDto = null;
            ListProcedureClienteDto = null;
            ListTipologieTotaliDto = null;
            ListOperatoriNormalizzatiDto = null;
            ListLavorazioniFasiTipoTotaleDto = null;
            ListProduzioneOperatoriDto = null;
            ListTaskDataReadingAggiornamentoDto = null;
            ListRuoliDto = null;

            // Entity lists
            ListRuoli = null;
            ListOperatori = null;
            ListProcedureLavorazioni = null;
            ListFasiLavorazione = null;
            ListCentri = null;
            ListTipologieTotali = null;
            ListClienti = null;
            ListProcedureCliente = null;
            ListReparti = null;
            ListFormatoDati = null;
            ListTurni = null;
            ListTipoTurni = null;
            ListTaskDataReadingAggiornamento = null;
            ListTask = null;
        }
    }
}
