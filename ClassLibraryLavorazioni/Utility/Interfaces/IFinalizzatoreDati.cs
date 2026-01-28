using System.Data;

namespace LibraryLavorazioni.Utility.Interfaces
{
    public interface IFinalizzatoreDati
    {
        DataTable FinalizzaDati(DataTable tableData, int? idCentro);
    }
}