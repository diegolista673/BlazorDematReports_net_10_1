#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class CentriLavorazione
    {

        public override bool Equals(object obj)
        {
            return Equals(obj as CentriLavorazione);
        }


        public bool Equals(CentriLavorazione other)
        {
            return other != null &&
                   Centro == other.Centro;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Centro);
        }


        public override string ToString() => Centro;


    }
}
