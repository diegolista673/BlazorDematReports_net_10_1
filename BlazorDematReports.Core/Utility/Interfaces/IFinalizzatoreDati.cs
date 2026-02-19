using System.Data;

namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface IFinalizzatoreDati
    {
        DataTable FinalizzaDati(DataTable tableData, int? idCentro);
    }
}