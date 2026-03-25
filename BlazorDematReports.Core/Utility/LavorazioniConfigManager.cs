using BlazorDematReports.Core.Utility.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Servizio centralizzato per la gestione delle configurazioni delle lavorazioni.
    /// Fornisce l’accesso ai parametri di configurazione, alle stringhe di connessione e ad altri valori di configurazione
    /// necessari per l’esecuzione delle lavorazioni. Utilizza <see cref="IConfiguration"/> per leggere i valori dal file di configurazione dell’applicazione.
    /// </summary>
    public class LavorazioniConfigManager : ILavorazioniConfigManager
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="LavorazioniConfigManager"/>.
        /// </summary>
        /// <param name="configuration">Istanza di <see cref="IConfiguration"/> per l’accesso ai parametri di configurazione.</param>
        public LavorazioniConfigManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        /// <summary>
        /// Restituisce una sezione di configurazione in base alla chiave specificata.
        /// </summary>
        /// <param name="key">Chiave della sezione di configurazione.</param>
        /// <returns>Sezione di configurazione richiesta.</returns>
        public IConfigurationSection GetConfigurationSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Restituisce la lista dei nomi delle proprietà contrassegnate come connessioni disponibili.
        /// </summary>
        /// <returns>Lista dei nomi delle connessioni.</returns>
        public List<string> GetConnectionsList()
        {
            List<string> list = new List<string>();
            var props = GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(ConnectionAttribute)));
            foreach (var conn in props)
            {
                if (conn.Name != "CnxnPdP" &&
                    conn.Name != "CnxnPraticheSuccessione" &&
                    conn.Name != "HangfireConnection" &&
                    conn.Name != "DematReportsContext")
                {
                    list.Add(conn.Name);
                }

            }
            return list;
        }


        public string? PathFileBollettini
        {
            get
            {
                return _configuration["PathFileConfig:PathFileBollettini"];
            }
        }



        public string GetConnectionString(string connectionName)
        {
            return _configuration.GetConnectionString(connectionName) ?? string.Empty;
        }

        public string? DematReportsContext
        {
            get
            {
                return _configuration["ConnectionStrings:DematReportsContext"];
            }
        }

        [Connection]
        public string? CnxnCaptiva206
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnCaptiva206"];
            }
        }


        [Connection]
        public string? CnxnDematReports
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnDematReports"];
            }
        }

        [Connection]
        public string? CnxnEquitaliaVR
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnEquitaliaVR"];
            }
        }


        public string? CnxnDimar
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnDimar"];
            }
        }

        [Connection]
        public string? CnxnUnicredit
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnUnicredit"];
            }
        }


        public string? CnxnPosteMobile
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnPosteMobile"];
            }
        }


        [Connection]
        public string? CnxnPdP
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnPdP"];
            }
        }

        [Connection]
        public string? CnxnHera
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnHera"];
            }
        }

        public string? UrlWebtopInps
        {
            get
            {
                return _configuration["UrlConfig:UrlWebtopInps"];
            }
        }
        public string? UserWebtopInps
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopInps"];
            }
        }
        public string? PasswordWebtopInps
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopInps"];
            }
        }


        public string? UserWebtopInpsGenova
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopInpsGenova"];
            }
        }
        public string? PasswordWebtopInpsGenova
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopInpsGenova"];
            }
        }



        public string? UrlWebtopInail
        {
            get
            {
                return _configuration["UrlConfig:UrlWebtopInail"];
            }
        }
        public string? UserWebtopInail
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopInail"];
            }
        }
        public string? PasswordWebtopInail
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopInail"];
            }
        }


        public string? UrlWebtopEquitalia23I
        {
            get
            {
                return _configuration["UrlConfig:UrlWebtopEquitalia23I"];
            }
        }
        public string? UserWebtopEquitalia23I
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopEquitalia23I"];
            }
        }
        public string? PasswordWebtopEquitalia23I
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopEquitalia23I"];
            }
        }


        public string? UrlWebtopAciRaccomandate
        {
            get
            {
                return _configuration["UrlConfig:UrlWebtopAciRaccomandate"];
            }
        }
        public string? UserWebtopAciRaccomandate
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopAciRaccomandate"];
            }
        }

        public string? PasswordWebtopAciRaccomandate
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopAciRaccomandate"];
            }
        }

        public string? UserPraticheSucc
        {
            get
            {
                return _configuration["UrlConfig:UserPraticheSucc"];
            }
        }

        public string? PasswordPraticheSucc
        {
            get
            {
                return _configuration["UrlConfig:PasswordPraticheSucc"];
            }
        }


        public string? UserWebtopInpsPomezia
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopInpsPomezia"];
            }
        }

        public string? PasswordWebtopInpsPomezia
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopInpsPomezia"];
            }
        }


        public string? UserWebtopInpsMelzo
        {
            get
            {
                return _configuration["UrlConfig:UserWebtopInpsMelzo"];
            }
        }

        public string? PasswordWebtopInpsMelzo
        {
            get
            {
                return _configuration["UrlConfig:PasswordWebtopInpsMelzo"];
            }
        }

        [Connection]
        public string? CnxnAder4SorterVips
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnAder4SorterVips"];
            }
        }

        [Connection]
        public string? CnxnAder4Sorter1
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnAder4Sorter1"];
            }
        }

        [Connection]
        public string? CnxnAder4Sorter2
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnAder4Sorter2"];
            }
        }


        [Connection]
        public string? CnxnPraticheSuccessione
        {
            get
            {
                return _configuration["ConnectionStrings:CnxnPraticheSuccessione"];
            }
        }

        [Connection]
        public string? HangfireConnection
        {
            get
            {
                return _configuration["ConnectionStrings:HangfireConnection"];
            }
        }
    }
}
