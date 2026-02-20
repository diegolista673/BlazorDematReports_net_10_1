using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Components.Shared
{
    /// <summary>
    /// Popolamento select e ricerca per BaseComponentPage.
    /// </summary>
    public partial class BaseComponentPage<TLogger>
    {
        #region Region Select Search

        protected List<string> SelectOperatore { get; private set; } = new();
        protected List<string> SelectLavorazione { get; private set; } = new();
        public List<string> SelectFase { get; private set; } = new();
        protected List<string> SelectTipologiaTotale { get; private set; } = new();
        protected List<string> SelectCliente { get; private set; } = new();
        protected List<string> SelectProceduraCliente { get; set; } = new();
        protected List<string> SelectReparto { get; set; } = new();
        protected List<string> SelectFormato { get; set; } = new();
        protected List<string> SelectTurno { get; set; } = new();
        protected List<string> SelectTipoTurno { get; set; } = new();
        protected List<string> SelectRuolo { get; set; } = new();

        /// <summary>
        /// Metodo generico per popolare una select
        /// </summary>
        protected List<string> SetSelectList<T, TKey>(
            IEnumerable<T>? source,
            Func<T, TKey> orderSelector,
            Func<T, string?> valueSelector)
        {
            return source == null
                ? new List<string>()
                : source.OrderBy(orderSelector)
                        .Select(valueSelector)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()!;
        }

        /// <summary>
        /// Popola la select degli operatori (aggiunge la sigla se admin)
        /// </summary>
        protected virtual void SetSelectOperatore()
        {
            if (ConfigUser!.IsAdminRole)
                SelectOperatore = SetSelectList(
                    ListOperatori,
                    x => x.Operatore,
                    x => $"{x.Operatore}({x.IdcentroNavigation.Sigla})"
                );
            else
                SelectOperatore = SetSelectList(
                    ListOperatori,
                    x => x.Operatore,
                    x => x.Operatore
                );
        }

        /// <summary>
        /// Popola la select delle lavorazioni (tutte)
        /// </summary>
        protected virtual void SetSelectLavorazione()
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioni,
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni da una lista di DTO
        /// </summary>
        protected virtual void SetSelectLavorazione(List<ProcedureLavorazioniDto> listProcedureLavorazioniDto)
        {
            SelectLavorazione = SetSelectList(
                listProcedureLavorazioniDto,
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni DTO filtrando per centro
        /// </summary>
        protected virtual void SetSelectLavorazioneDto(int idCentro)
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioniDto?.Where(x => x.Idcentro == idCentro),
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle lavorazioni filtrando per centro
        /// </summary>
        protected virtual void SetSelectLavorazione(int idCentro)
        {
            SelectLavorazione = SetSelectList(
                ListProcedureLavorazioni?.Where(x => x.Idcentro == idCentro),
                x => x.NomeProcedura,
                x => x.NomeProcedura
            );
        }

        /// <summary>
        /// Popola la select delle fasi di lavorazione (tutte)
        /// </summary>
        protected virtual void SetSelectFasi()
        {
            SelectFase = SetSelectList(
                ListFasiLavorazione,
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select delle fasi di una specifica lavorazione DTO
        /// </summary>
        protected virtual void SetSelectFasi(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            SelectFase = SetSelectList(
                procedureLavorazioniDto?.LavorazioniFasiDataReadingsDto,
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select delle fasi con flag DataReading = true
        /// </summary>
        protected virtual void SetSelectFasiOnlyWithDataReading(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            SelectFase = SetSelectList(
                procedureLavorazioniDto?.LavorazioniFasiDataReadingsDto?.Where(x => x.FlagDataReading),
                x => x.FaseLavorazione,
                x => x.FaseLavorazione
            );
        }

        /// <summary>
        /// Popola la select dei clienti
        /// </summary>
        protected void SetSelectCliente()
        {
            SelectCliente = SetSelectList(
                ListClienti,
                x => x.NomeCliente,
                x => x.NomeCliente
            );
        }

        /// <summary>
        /// Popola la select dei reparti
        /// </summary>
        protected virtual void SetSelectReparto()
        {
            SelectReparto = SetSelectList(
                ListReparti,
                x => x.Reparti,
                x => x.Reparti
            );
        }

        /// <summary>
        /// Popola la select dei formati dati
        /// </summary>
        protected virtual void SetSelectFormato()
        {
            SelectFormato = SetSelectList(
                ListFormatoDati,
                x => x.FormatoDatiProduzione,
                x => x.FormatoDatiProduzione
            );
        }

        /// <summary>
        /// Popola la select delle procedure cliente
        /// </summary>
        protected virtual void SetSelectProcedureClienti()
        {
            SelectProceduraCliente = SetSelectList(
                ListProcedureCliente,
                x => x.ProceduraCliente,
                x => x.ProceduraCliente
            );
        }

        /// <summary>
        /// Popola la select delle tipologie totali
        /// </summary>
        protected virtual void SetSelectTipologieTotali()
        {
            SelectTipologiaTotale = SetSelectList(
                ListTipologieTotali,
                x => x.TipoTotale,
                x => x.TipoTotale
            );
        }

        /// <summary>
        /// Popola la select dei turni
        /// </summary>
        protected virtual void SetSelectTurni()
        {
            SelectTurno = SetSelectList(
                ListTurni,
                x => x.Turno,
                x => x.Turno
            );
        }

        /// <summary>
        /// Popola la select dei tipi turno
        /// </summary>
        protected virtual void SetSelectTipoTurni()
        {
            SelectTipoTurno = SetSelectList(
                ListTipoTurni,
                x => x.TipoTurno,
                x => x.TipoTurno
            );
        }

        /// <summary>
        /// Popola la select dei ruoli
        /// </summary>
        protected virtual void SetSelectRuolo()
        {
            SelectRuolo = SetSelectList(
                ListRuoli,
                x => x.Ruolo,
                x => x.Ruolo
            );
        }

        /// <summary>
        /// Used for select list into datagrid - Return string from list of object
        /// </summary>
        protected async Task<IEnumerable<string>> SearchFromSelect(List<string>? Select, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return await Task.FromResult(Select!.AsEnumerable());

            return await Task.FromResult(Select!.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList().AsEnumerable());
        }

        #endregion
    }
}
