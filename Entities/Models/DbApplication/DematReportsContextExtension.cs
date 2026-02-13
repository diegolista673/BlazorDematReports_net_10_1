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

            // Configurazione per enum TipoFonte salvato come stringa
            modelBuilder.Entity<ConfigurazioneFontiDati>(entity =>
            {
                entity.Property(e => e.TipoFonte)
                    .HasConversion<string>();
            });
        }


    }
}
