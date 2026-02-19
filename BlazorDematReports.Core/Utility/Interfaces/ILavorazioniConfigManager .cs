using Microsoft.Extensions.Configuration;

namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface ILavorazioniConfigManager
    {
        public List<string> GetConnectionsList();

        string? PathFileBollettini { get; }

        string? UrlWebtopInps { get; }

        string? UrlWebtopInail { get; }
        string? UrlWebtopEquitalia23I { get; }
        string? UrlWebtopAciRaccomandate { get; }

        string? UserWebtopInps { get; }
        string? PasswordWebtopInps { get; }


        string? UserWebtopInpsGenova { get; }
        string? PasswordWebtopInpsGenova { get; }


        string? UserWebtopInpsPomezia { get; }
        string? PasswordWebtopInpsPomezia { get; }


        string? UserWebtopInpsMelzo { get; }
        string? PasswordWebtopInpsMelzo { get; }

        string? UserWebtopInail { get; }
        string? PasswordWebtopInail { get; }

        string? UserWebtopEquitalia23I { get; }
        string? PasswordWebtopEquitalia23I { get; }


        string? UserWebtopAciRaccomandate { get; }
        string? PasswordWebtopAciRaccomandate { get; }


        string? UserPraticheSucc { get; }
        string? PasswordPraticheSucc { get; }



        string? CnxnCaptiva206 { get; }
        string? DematReportsContext { get; }
        string? CnxnDematReports { get; }
        string? CnxnEquitaliaVR { get; }
        string? CnxnDimar { get; }
        string? CnxnUnicredit { get; }
        string? CnxnPosteMobile { get; }
        string? CnxnPdP { get; }
        string? CnxnHera { get; }
        string? CnxnAder4SorterVips { get; }
        string? CnxnAder4Sorter1 { get; }
        string? CnxnAder4Sorter2 { get; }
        string? CnxnPraticheSuccessione { get; }

        string GetConnectionString(string connectionName);

        IConfigurationSection GetConfigurationSection(string Key);
    }
}
