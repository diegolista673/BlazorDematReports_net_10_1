using Microsoft.Extensions.Configuration;

namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface ILavorazioniConfigManager
    {
        public List<string> GetConnectionsList();


        string? CnxnCaptiva206 { get; }
        string? DematReportsContext { get; }
        string? CnxnDematReports { get; }
        string? CnxnUnicredit { get; }
        string? CnxnPdP { get; }
        string? CnxnAder4SorterVips { get; }
        string? CnxnAder4Sorter1 { get; }
        string? CnxnAder4Sorter2 { get; }
        string? CnxnPraticheSuccessione { get; }

        string GetConnectionString(string connectionName);

        IConfigurationSection GetConfigurationSection(string Key);
    }
}
