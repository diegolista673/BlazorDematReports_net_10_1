using System;

namespace LibraryLavorazioni
{

    /// <summary>
    /// Colonna procedura lavorazione in tabella procedure_lavorazioni
    /// </summary>
    public class ProcessingLavorazioneAttribute : Attribute
    {
        public ProcessingLavorazioneAttribute(string name)
        {
            this.NomeProcedura = name;
        }

        public string NomeProcedura { get; }
    }


}



