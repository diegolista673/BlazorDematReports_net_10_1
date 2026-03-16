using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class DematReportsContext
    {
        public virtual DbSet<ReportOreDocumenti> ReportOreDocumentis { get; set; }
        public virtual DbSet<ReportAnniSistema> ReportAnniSistemas { get; set; }
        public virtual DbSet<ReportAnnuale> ReportAnnuales { get; set; }
        public virtual DbSet<ReportAnnualeSistema> ReportAnnualeSistemas { get; set; }
        public virtual DbSet<ReportAnnualeTotaliDedicati> ReportAnnualeTotaliDedicatis { get; set; }
        public virtual DbSet<ReportGiornalieroTotaliDedicati> ReportGiornalieroTotaliDedicatis { get; set; }
        public virtual DbSet<ReportProduzioneCompleta> ReportProduzioneCompletas { get; set; }

        public virtual DbSet<ReportEsportazioneOreDocumenti> ReportEsportazioneOreDocumenti { get; set; }

        public virtual DbSet<ReportDocumenti> ReportDocumentis { get; set; }

        public virtual DbSet<ReportFogli> ReportFoglis { get; set; }

        public virtual DbSet<ReportChartStackedLine> ReportChartStackedLines { get; set; }

        public virtual DbSet<ReportChartStackedLineDocumenti> ReportChartStackedLineDocumentis { get; set; }

        public virtual DbSet<ReportChartStackedLineFogli> ReportChartStackedLineFoglis { get; set; }

        public virtual DbSet<ReportChartStackedLineOre> ReportChartStackedLineOres { get; set; }

        /// <summary>Staging per-operatore da CSV email ADER4.</summary>
        public virtual DbSet<DatiMailCsvAder4> DatiMailCsvAder4 { get; set; }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReportOreDocumenti>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportAnniSistema>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportAnnuale>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportAnnualeSistema>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportAnnualeTotaliDedicati>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportGiornalieroTotaliDedicati>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportProduzioneCompleta>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportDocumenti>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportFogli>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportEsportazioneOreDocumenti>(entity =>
            {
                entity.HasNoKey();
            });


            modelBuilder.Entity<ReportChartStackedLine>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportChartStackedLineDocumenti>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportChartStackedLineFogli>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ReportChartStackedLineOre>(entity =>
            {
                entity.HasNoKey();
            });

            // Configurazione Value Converter per TipoFonte (enum -> string nel DB)
            modelBuilder.Entity<ConfigurazioneFontiDati>(entity =>
            {
                entity.Property(e => e.TipoFonte)
                    .HasConversion<Entities.Converters.TipoFonteDataConverter>()
                    .HasColumnType("nvarchar(50)");
            });

            // DatiMailCsv: staging per-operatore da CSV email
            modelBuilder.Entity<DatiMailCsvAder4>(entity =>
            {
                entity.ToTable("DatiMailCsvAder4");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodiceServizio).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Operatore).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TipoRisultato).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IdEvento).HasMaxLength(100);
                entity.Property(e => e.Centro).HasMaxLength(50);
                entity.Property(e => e.NomeFile).HasMaxLength(200);
                entity.Property(e => e.DataIngestione).HasDefaultValueSql("GETDATE()");

            });
        }


    }
}
