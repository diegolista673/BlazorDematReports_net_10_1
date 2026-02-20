#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class QueryProcedureLavorazioni : IEquatable<QueryProcedureLavorazioni>
    {

        public bool Equals(QueryProcedureLavorazioni other)
        {
            if (other is null)
                return false;

            return this.IdQuery == other.IdQuery;
            //return this.IdproceduraLavorazione == other.IdproceduraLavorazione && this.IdFaseLavorazione == other.IdFaseLavorazione;
        }

        public override bool Equals(object obj) => Equals(obj as QueryProcedureLavorazioni);

        public override int GetHashCode() => IdQuery.GetHashCode();

        //public override int GetHashCode() => (IdproceduraLavorazione, IdFaseLavorazione).GetHashCode();

    }
}
