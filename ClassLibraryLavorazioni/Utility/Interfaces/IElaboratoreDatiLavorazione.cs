using LibraryLavorazioni.Utility.Models;

namespace LibraryLavorazioni.Utility.Interfaces
{
    public interface IElaboratoreDatiLavorazione
    {
        public Task<List<DatiElaborati>> ElaboraDatiLavorazioneAsync(
                    List<DatiLavorazione> datiOriginali,
                    int idCentro,
                    int idProceduraLavorazione,
                    int idFaseLavorazione);

    }
}