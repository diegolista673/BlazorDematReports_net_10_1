using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryLavorazioni
{

    public class DettaglioClasseLavorazione
    {
        public string NomeClasse { get; set; }
        public Type ClasseLavorazione { get; set; }
        public List<string> ListCodiciLavorazione { get; set; }
        public bool ricercaRaccomandata { get; set; }
        public bool ricercaSLA { get; set; }

        public long CreationTime { get; set; }
        public int ThreadNum { get; set; }

        public string PathFile { get; set; }

        public DettaglioClasseLavorazione()
        {

        }

    }
}
