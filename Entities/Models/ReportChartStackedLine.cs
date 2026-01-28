namespace Entities.Models
{
    public class ReportChartStackedLine
    {
        public string? Periodo { get; set; }
        public int Documenti { get; set; }
        public int Fogli { get; set; }
        public double Ore { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return this.Periodo == ((ReportChartStackedLine)obj).Periodo;
        }
        public override int GetHashCode()
        {
            return Periodo!.GetHashCode();
        }

    }
}
