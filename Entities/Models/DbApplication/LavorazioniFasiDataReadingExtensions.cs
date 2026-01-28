#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class LavorazioniFasiDataReading : IEquatable<LavorazioniFasiDataReading>
    {

        public bool Equals(LavorazioniFasiDataReading other)
        {
            if (other is null)
                return false;

            return this.IdProceduraLavorazione == other.IdProceduraLavorazione && this.IdFaseLavorazione == other.IdFaseLavorazione;
        }

        public override bool Equals(object obj) => Equals(obj as LavorazioniFasiDataReading);
        public override int GetHashCode() => (IdProceduraLavorazione, IdFaseLavorazione).GetHashCode();

    }
}
