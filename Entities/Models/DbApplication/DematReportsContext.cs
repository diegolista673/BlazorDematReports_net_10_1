using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Entities.Models.DbApplication;

public partial class DematReportsContext : DbContext
{
    public DematReportsContext()
    {
    }

    public DematReportsContext(DbContextOptions<DematReportsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AderEquitalia4OperatoriGe> AderEquitalia4OperatoriGes { get; set; }

    public virtual DbSet<AderEquitalia4OperatoriVr> AderEquitalia4OperatoriVrs { get; set; }

    public virtual DbSet<AderEquitalia4ProduzioneGe> AderEquitalia4ProduzioneGes { get; set; }

    public virtual DbSet<AderEquitalia4ProduzioneVr> AderEquitalia4ProduzioneVrs { get; set; }

    public virtual DbSet<AggregatedCounter> AggregatedCounters { get; set; }

    public virtual DbSet<CentriLavorazione> CentriLavoraziones { get; set; }

    public virtual DbSet<CentriVisibili> CentriVisibilis { get; set; }

    public virtual DbSet<Clienti> Clientis { get; set; }

    public virtual DbSet<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; }

    public virtual DbSet<ConfigurazioneFontiDati> ConfigurazioneFontiDatis { get; set; }

    public virtual DbSet<ConfigurazionePipelineStep> ConfigurazionePipelineSteps { get; set; }

    public virtual DbSet<Counter> Counters { get; set; }

    public virtual DbSet<FasiLavorazione> FasiLavoraziones { get; set; }

    public virtual DbSet<FormatoDati> FormatoDatis { get; set; }

    public virtual DbSet<Hash> Hashes { get; set; }

    public virtual DbSet<Hera16> Hera16s { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobParameter> JobParameters { get; set; }

    public virtual DbSet<JobQueue> JobQueues { get; set; }

    public virtual DbSet<LavorazioniFasiDataReading> LavorazioniFasiDataReadings { get; set; }

    public virtual DbSet<LavorazioniFasiTipoTotale> LavorazioniFasiTipoTotales { get; set; }

    public virtual DbSet<List> Lists { get; set; }

    public virtual DbSet<MndStabilimenti> MndStabilimentis { get; set; }

    public virtual DbSet<MndUtenteStabilimento> MndUtenteStabilimentos { get; set; }

    public virtual DbSet<Operatori> Operatoris { get; set; }

    public virtual DbSet<OperatoriNormalizzati> OperatoriNormalizzatis { get; set; }

    public virtual DbSet<ProcedureCliente> ProcedureClientes { get; set; }

    public virtual DbSet<ProcedureLavorazioni> ProcedureLavorazionis { get; set; }

    public virtual DbSet<ProduzioneOperatori> ProduzioneOperatoris { get; set; }

    public virtual DbSet<ProduzioneSistema> ProduzioneSistemas { get; set; }

    public virtual DbSet<QueryProcedureLavorazioni> QueryProcedureLavorazionis { get; set; }

    public virtual DbSet<QueryProcedureLavorazioniBackup> QueryProcedureLavorazioniBackups { get; set; }

    public virtual DbSet<RepartiProduzione> RepartiProduziones { get; set; }

    public virtual DbSet<Ruoli> Ruolis { get; set; }

    public virtual DbSet<Schema> Schemas { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<Set> Sets { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<TaskDaEseguire> TaskDaEseguires { get; set; }

    public virtual DbSet<TaskDataReadingAggiornamento> TaskDataReadingAggiornamentos { get; set; }

    public virtual DbSet<TaskServiceLavorazioni> TaskServiceLavorazionis { get; set; }

    public virtual DbSet<TipoTurni> TipoTurnis { get; set; }

    public virtual DbSet<TipologieTotali> TipologieTotalis { get; set; }

    public virtual DbSet<TipologieTotaliProduzione> TipologieTotaliProduziones { get; set; }

    public virtual DbSet<Turni> Turnis { get; set; }

    public virtual DbSet<VwConfigurazioneTaskSummary> VwConfigurazioneTaskSummaries { get; set; }

    public virtual DbSet<VwConfigurazioniFontiDatiCompletum> VwConfigurazioniFontiDatiCompleta { get; set; }

    public virtual DbSet<Z0072370Rdmkt28autGeUdaDettaglio> Z0072370Rdmkt28autGeUdaDettaglios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=VEVRFL1M031H;Database=DematReports;Integrated Security=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AderEquitalia4OperatoriGe>(entity =>
        {
            entity.HasKey(e => e.IdCount).HasName("PK_Ader_Operatori_ge");

            entity.ToTable("Ader_Equitalia4_Operatori_GE");

            entity.Property(e => e.IdCount).HasColumnName("ID_COUNT");
            entity.Property(e => e.DataScansione).HasColumnType("datetime");
            entity.Property(e => e.IdEvento)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Operatore)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TipoScansione)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AderEquitalia4OperatoriVr>(entity =>
        {
            entity.HasKey(e => e.IdCount).HasName("PK_Ader_Equitalia4_Produzione_Operatori");

            entity.ToTable("Ader_Equitalia4_Operatori_VR");

            entity.Property(e => e.IdCount).HasColumnName("ID_COUNT");
            entity.Property(e => e.DataScansione).HasColumnType("datetime");
            entity.Property(e => e.IdEvento)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Operatore)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TipoScansione)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AderEquitalia4ProduzioneGe>(entity =>
        {
            entity.HasKey(e => e.Idequitalia4).HasName("PK_Ader_Produzione_GE");

            entity.ToTable("Ader_Equitalia4_Produzione_GE");

            entity.Property(e => e.Idequitalia4).HasColumnName("IDEquitalia4");
            entity.Property(e => e.DataLavorazione).HasColumnType("datetime");
            entity.Property(e => e.IdEvento)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AderEquitalia4ProduzioneVr>(entity =>
        {
            entity.HasKey(e => e.Idequitalia4).HasName("PK_Table_1_2");

            entity.ToTable("Ader_Equitalia4_Produzione_VR");

            entity.Property(e => e.Idequitalia4).HasColumnName("IDEquitalia4");
            entity.Property(e => e.DataLavorazione).HasColumnType("datetime");
            entity.Property(e => e.IdEvento)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AggregatedCounter>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("PK_HangFire_CounterAggregated");

            entity.ToTable("AggregatedCounter", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_AggregatedCounter_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CentriLavorazione>(entity =>
        {
            entity.HasKey(e => e.Idcentro).HasName("PK_CENTRI_LAV");

            entity.ToTable("CentriLavorazione");

            entity.HasIndex(e => e.Centro, "IX_CentriLavorazione").IsUnique();

            entity.Property(e => e.Idcentro).HasColumnName("IDCentro");
            entity.Property(e => e.Centro)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sigla)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CentriVisibili>(entity =>
        {
            entity.HasKey(e => e.IdCentriVisibili);

            entity.ToTable("CentriVisibili");

            entity.HasOne(d => d.IdCentroNavigation).WithMany(p => p.CentriVisibilis)
                .HasForeignKey(d => d.IdCentro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CentriVisibili_CentriLavorazione");

            entity.HasOne(d => d.IdOperatoreNavigation).WithMany(p => p.CentriVisibilis)
                .HasForeignKey(d => d.IdOperatore)
                .HasConstraintName("FK_CentriVisibili_Operatori");
        });

        modelBuilder.Entity<Clienti>(entity =>
        {
            entity.HasKey(e => e.IdCliente);

            entity.ToTable("Clienti");

            entity.HasIndex(e => new { e.NomeCliente, e.IdCentroLavorazione }, "IX_Clienti").IsUnique();

            entity.Property(e => e.DataCreazioneCliente).HasColumnType("datetime");
            entity.Property(e => e.NomeCliente)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdCentroLavorazioneNavigation).WithMany(p => p.Clientis)
                .HasForeignKey(d => d.IdCentroLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Clienti_CentriLavorazione");
        });

        modelBuilder.Entity<ConfigurazioneFaseCentro>(entity =>
        {
            entity.HasKey(e => e.IdFaseCentro).HasName("PK__Configur__367E17090E01CBD1");

            entity.ToTable("ConfigurazioneFaseCentro");

            entity.HasIndex(e => e.IdConfigurazione, "IX_FaseCentro_Config");

            entity.HasIndex(e => new { e.IdConfigurazione, e.IdFaseLavorazione, e.CronExpression }, "UQ_FaseCentro_Fase_Cron").IsUnique();

            entity.HasIndex(e => new { e.IdConfigurazione, e.IdProceduraLavorazione, e.IdFaseLavorazione, e.IdCentro }, "UQ_FaseCentro_Unique").IsUnique();

            entity.Property(e => e.CronExpression).HasMaxLength(100);
            entity.Property(e => e.EnabledTask).HasDefaultValue(true);
            entity.Property(e => e.FlagAttiva).HasDefaultValue(true);
            entity.Property(e => e.HandlerClassName).HasMaxLength(255);
            entity.Property(e => e.IsTaskEnabled).HasDefaultValue(true);
            entity.Property(e => e.MailServiceCode).HasMaxLength(100);
            entity.Property(e => e.TaskDescription).HasMaxLength(255);
            entity.Property(e => e.TipoTask).HasMaxLength(50);
            entity.Property(e => e.UltimaModificaTask).HasColumnType("datetime");

            entity.HasOne(d => d.IdCentroNavigation).WithMany(p => p.ConfigurazioneFaseCentros)
                .HasForeignKey(d => d.IdCentro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FaseCentro_Centro");

            entity.HasOne(d => d.IdConfigurazioneNavigation).WithMany(p => p.ConfigurazioneFaseCentros)
                .HasForeignKey(d => d.IdConfigurazione)
                .HasConstraintName("FK_FaseCentro_Configurazione");

            entity.HasOne(d => d.IdFaseLavorazioneNavigation).WithMany(p => p.ConfigurazioneFaseCentros)
                .HasForeignKey(d => d.IdFaseLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FaseCentro_Fase");

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.ConfigurazioneFaseCentros)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FaseCentro_Procedura");
        });

        modelBuilder.Entity<ConfigurazioneFontiDati>(entity =>
        {
            entity.HasKey(e => e.IdConfigurazione).HasName("PK__Configur__826F74F04A5D3C25");

            entity.ToTable("ConfigurazioneFontiDati");

            entity.HasIndex(e => e.CodiceConfigurazione, "IX_ConfigFonte_Codice");

            entity.HasIndex(e => e.TipoFonte, "IX_ConfigFonte_TipoFonte");

            entity.HasIndex(e => e.CodiceConfigurazione, "UQ_ConfigFonte_Codice").IsUnique();

            entity.Property(e => e.CodiceConfigurazione)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ConnectionStringName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatoDa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatoIl)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DescrizioneConfigurazione).HasMaxLength(500);
            entity.Property(e => e.FlagAttiva).HasDefaultValue(true);
            entity.Property(e => e.HandlerClassName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MailServiceCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ModificatoDa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ModificatoIl).HasColumnType("datetime");
            entity.Property(e => e.TipoFonte)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ConfigurazionePipelineStep>(entity =>
        {
            entity.HasKey(e => e.IdPipelineStep).HasName("PK__Configur__A6B2F34757045CBD");

            entity.ToTable("ConfigurazionePipelineStep");

            entity.HasIndex(e => new { e.IdConfigurazione, e.NumeroStep }, "IX_Pipeline_Config");

            entity.Property(e => e.FlagAttiva).HasDefaultValue(true);
            entity.Property(e => e.NomeStep).HasMaxLength(100);
            entity.Property(e => e.TipoStep)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdConfigurazioneNavigation).WithMany(p => p.ConfigurazionePipelineSteps)
                .HasForeignKey(d => d.IdConfigurazione)
                .HasConstraintName("FK_Pipeline_Configurazione");
        });

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_Counter");

            entity.ToTable("Counter", "HangFire");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<FasiLavorazione>(entity =>
        {
            entity.HasKey(e => e.IdFaseLavorazione);

            entity.ToTable("FasiLavorazione");

            entity.Property(e => e.FaseLavorazione)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FormatoDati>(entity =>
        {
            entity.HasKey(e => e.IdformatoDati);

            entity.ToTable("FormatoDati");

            entity.Property(e => e.IdformatoDati).HasColumnName("IDFormatoDati");
            entity.Property(e => e.FormatoDatiProduzione)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Hash>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Field }).HasName("PK_HangFire_Hash");

            entity.ToTable("Hash", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Hash_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Field).HasMaxLength(100);
        });

        modelBuilder.Entity<Hera16>(entity =>
        {
            entity.HasKey(e => e.IdCounter).HasName("PK_HERA32_1");

            entity.ToTable("HERA16");

            entity.Property(e => e.IdCounter).HasColumnName("id_counter");
            entity.Property(e => e.CodiceMercato)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codice_mercato");
            entity.Property(e => e.CodiceOfferta)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codice_offerta");
            entity.Property(e => e.CodiceScatola)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codice_scatola");
            entity.Property(e => e.DataCaricamentoFile)
                .HasColumnType("datetime")
                .HasColumnName("data_caricamento_file");
            entity.Property(e => e.DataClassificazione)
                .HasColumnType("datetime")
                .HasColumnName("data_classificazione");
            entity.Property(e => e.DataIndex)
                .HasColumnType("datetime")
                .HasColumnName("data_index");
            entity.Property(e => e.DataPubblicazione)
                .HasColumnType("datetime")
                .HasColumnName("data_pubblicazione");
            entity.Property(e => e.DataScansione)
                .HasColumnType("datetime")
                .HasColumnName("data_scansione");
            entity.Property(e => e.IdentificativoAllegato).HasColumnName("identificativo_allegato");
            entity.Property(e => e.NomeFile)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nome_file");
            entity.Property(e => e.OperatoreClassificazione)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("operatore_classificazione");
            entity.Property(e => e.OperatoreIndex)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("operatore_index");
            entity.Property(e => e.OperatoreScan)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("operatore_scan");
            entity.Property(e => e.ProgrScansione)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("progr_scansione");
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tipo_documento");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Job");

            entity.ToTable("Job", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Job_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => e.StateName, "IX_HangFire_Job_StateName").HasFilter("([StateName] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.StateName).HasMaxLength(20);
        });

        modelBuilder.Entity<JobParameter>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Name }).HasName("PK_HangFire_JobParameter");

            entity.ToTable("JobParameter", "HangFire");

            entity.Property(e => e.Name).HasMaxLength(40);

            entity.HasOne(d => d.Job).WithMany(p => p.JobParameters)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_JobParameter_Job");
        });

        modelBuilder.Entity<JobQueue>(entity =>
        {
            entity.HasKey(e => new { e.Queue, e.Id }).HasName("PK_HangFire_JobQueue");

            entity.ToTable("JobQueue", "HangFire");

            entity.Property(e => e.Queue).HasMaxLength(50);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FetchedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<LavorazioniFasiDataReading>(entity =>
        {
            entity.HasKey(e => e.IdlavorazioneFaseDateReading).HasName("PK_SettingsProcedureLav");

            entity.ToTable("LavorazioniFasiDataReading");

            entity.Property(e => e.IdlavorazioneFaseDateReading).HasColumnName("IDLavorazioneFaseDateReading");

            entity.HasOne(d => d.IdFaseLavorazioneNavigation).WithMany(p => p.LavorazioniFasiDataReadings)
                .HasForeignKey(d => d.IdFaseLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SettingsProcedureLav_FasiLavorazione");

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.LavorazioniFasiDataReadings)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .HasConstraintName("FK_SettingsProcedureLav_ProcedureLavorazioni");
        });

        modelBuilder.Entity<LavorazioniFasiTipoTotale>(entity =>
        {
            entity.HasKey(e => e.IdLavorazioneFaseTipoTotale);

            entity.ToTable("LavorazioniFasiTipoTotale");

            entity.HasIndex(e => new { e.IdFase, e.IdProceduraLavorazione, e.IdTipologiaTotale }, "IX_LavorazioniFasiTipoTotale").IsUnique();

            entity.HasOne(d => d.IdFaseNavigation).WithMany(p => p.LavorazioniFasiTipoTotales)
                .HasForeignKey(d => d.IdFase)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LavorazioniFasiTipoTotale_FasiLavorazione");

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.LavorazioniFasiTipoTotales)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LavorazioniFasiTipoTotale_ProcedureLavorazioni");

            entity.HasOne(d => d.IdTipologiaTotaleNavigation).WithMany(p => p.LavorazioniFasiTipoTotales)
                .HasForeignKey(d => d.IdTipologiaTotale)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LavorazioniFasiTipoTotale_TipologieTotali1");
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_List");

            entity.ToTable("List", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_List_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<MndStabilimenti>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MND_STABILIMENTI");

            entity.Property(e => e.Desc)
                .HasMaxLength(50)
                .HasColumnName("DESC");
            entity.Property(e => e.IdStabDemat).HasColumnName("ID_STAB_DEMAT");
            entity.Property(e => e.NomeStabilimento)
                .HasMaxLength(50)
                .HasColumnName("NOME_STABILIMENTO");
        });

        modelBuilder.Entity<MndUtenteStabilimento>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MND_UTENTE_STABILIMENTO");

            entity.Property(e => e.IdStabDemat).HasColumnName("ID_STAB_DEMAT");
            entity.Property(e => e.IdUtente)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("ID_UTENTE");
            entity.Property(e => e.Sutente)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("SUTENTE");
        });

        modelBuilder.Entity<Operatori>(entity =>
        {
            entity.HasKey(e => e.Idoperatore).HasName("PK_Table_1");

            entity.ToTable("Operatori");

            entity.HasIndex(e => e.Operatore, "IX_Operatori").IsUnique();

            entity.Property(e => e.Idoperatore).HasColumnName("IDOperatore");
            entity.Property(e => e.Azienda)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Idcentro).HasColumnName("IDCentro");
            entity.Property(e => e.Operatore)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRuoloNavigation).WithMany(p => p.Operatoris)
                .HasForeignKey(d => d.IdRuolo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Operatori_Ruoli");

            entity.HasOne(d => d.IdcentroNavigation).WithMany(p => p.Operatoris)
                .HasForeignKey(d => d.Idcentro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Operatori_CentriLavorazione");
        });

        modelBuilder.Entity<OperatoriNormalizzati>(entity =>
        {
            entity.HasKey(e => e.IdNorm);

            entity.ToTable("OperatoriNormalizzati");

            entity.HasIndex(e => e.OperatoreDaNormalizzare, "IX_OperatoriNormalizzati").IsUnique();

            entity.Property(e => e.OperatoreDaNormalizzare)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OperatoreNormalizzato)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProcedureCliente>(entity =>
        {
            entity.HasKey(e => e.IdproceduraCliente);

            entity.ToTable("ProcedureCliente");

            entity.HasIndex(e => e.ProceduraCliente, "IX_ProcedureCliente").IsUnique();

            entity.Property(e => e.IdproceduraCliente).HasColumnName("IDproceduraCliente");
            entity.Property(e => e.Commessa)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DataInserimento).HasColumnType("datetime");
            entity.Property(e => e.DescrizioneProcedura)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Idcentro).HasColumnName("IDCentro");
            entity.Property(e => e.Idcliente).HasColumnName("IDCliente");
            entity.Property(e => e.Idoperatore).HasColumnName("IDOperatore");
            entity.Property(e => e.ProceduraCliente)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdclienteNavigation).WithMany(p => p.ProcedureClientes)
                .HasForeignKey(d => d.Idcliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcedureCliente_Clienti");

            entity.HasOne(d => d.IdoperatoreNavigation).WithMany(p => p.ProcedureClientes)
                .HasForeignKey(d => d.Idoperatore)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcedureCliente_Operatori");
        });

        modelBuilder.Entity<ProcedureLavorazioni>(entity =>
        {
            entity.HasKey(e => e.IdproceduraLavorazione);

            entity.ToTable("ProcedureLavorazioni");

            entity.HasIndex(e => e.NomeProcedura, "IX_ProcedureLavorazioni").IsUnique();

            entity.Property(e => e.IdproceduraLavorazione).HasColumnName("IDProceduraLavorazione");
            entity.Property(e => e.DataInserimento).HasColumnType("datetime");
            entity.Property(e => e.Idcentro).HasColumnName("IDCentro");
            entity.Property(e => e.IdformatoDatiProduzione).HasColumnName("IDFormatoDatiProduzione");
            entity.Property(e => e.Idoperatore).HasColumnName("IDOperatore");
            entity.Property(e => e.IdproceduraCliente).HasColumnName("IDproceduraCliente");
            entity.Property(e => e.Idreparti).HasColumnName("IDReparti");
            entity.Property(e => e.LogoBase64).IsUnicode(false);
            entity.Property(e => e.NomeProcedura)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeProceduraProgramma)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeServizio)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .IsUnicode(false);

            entity.HasOne(d => d.IdformatoDatiProduzioneNavigation).WithMany(p => p.ProcedureLavorazionis)
                .HasForeignKey(d => d.IdformatoDatiProduzione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcedureLavorazioni_FormatoDati");

            entity.HasOne(d => d.IdoperatoreNavigation).WithMany(p => p.ProcedureLavorazionis)
                .HasForeignKey(d => d.Idoperatore)
                .HasConstraintName("FK_ProcedureLavorazioni_Operatori");

            entity.HasOne(d => d.IdproceduraClienteNavigation).WithMany(p => p.ProcedureLavorazionis)
                .HasForeignKey(d => d.IdproceduraCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcedureLavorazioni_ProcedureCliente");

            entity.HasOne(d => d.IdrepartiNavigation).WithMany(p => p.ProcedureLavorazionis)
                .HasForeignKey(d => d.Idreparti)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcedureLavorazioni_RepartiProduzione");
        });

        modelBuilder.Entity<ProduzioneOperatori>(entity =>
        {
            entity.HasKey(e => e.IdProduzione);

            entity.ToTable("ProduzioneOperatori");

            entity.HasIndex(e => new { e.IdOperatore, e.IdProceduraLavorazione, e.IdFaseLavorazione, e.IdTurno, e.IdCentro }, "IX_ProduzioneOperatori");

            entity.Property(e => e.AltraUtenza).HasMaxLength(100);
            entity.Property(e => e.DataLavorazione).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(100);

            entity.HasOne(d => d.IdFaseLavorazioneNavigation).WithMany(p => p.ProduzioneOperatoris)
                .HasForeignKey(d => d.IdFaseLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneOperatori_FasiLavorazione");

            entity.HasOne(d => d.IdOperatoreNavigation).WithMany(p => p.ProduzioneOperatoris)
                .HasForeignKey(d => d.IdOperatore)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneOperatori_Table_1");

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.ProduzioneOperatoris)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneOperatori_ProcedureLavorazioni");

            entity.HasOne(d => d.IdRepartiNavigation).WithMany(p => p.ProduzioneOperatoris)
                .HasForeignKey(d => d.IdReparti)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneOperatori_RepartiProduzione");

            entity.HasOne(d => d.IdTurnoNavigation).WithMany(p => p.ProduzioneOperatoris)
                .HasForeignKey(d => d.IdTurno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneOperatori_Turni");
        });

        modelBuilder.Entity<ProduzioneSistema>(entity =>
        {
            entity.HasKey(e => e.IdProduzioneSistema);

            entity.ToTable("ProduzioneSistema");

            entity.HasIndex(e => new { e.IdOperatore, e.IdProceduraLavorazione, e.IdFaseLavorazione, e.DataLavorazione, e.IdCentro }, "IX_ProduzioneSistema");

            entity.Property(e => e.CentroElaborazione)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DataAggiornamento).HasColumnType("datetime");
            entity.Property(e => e.DataLavorazione).HasColumnType("datetime");
            entity.Property(e => e.EventoId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeAllegato)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Operatore).IsUnicode(false);
            entity.Property(e => e.OperatoreNonRiconosciuto).IsUnicode(false);

            entity.HasOne(d => d.IdCentroNavigation).WithMany(p => p.ProduzioneSistemas)
                .HasForeignKey(d => d.IdCentro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneSistema_CentriLavorazione");

            entity.HasOne(d => d.IdFaseLavorazioneNavigation).WithMany(p => p.ProduzioneSistemas)
                .HasForeignKey(d => d.IdFaseLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneSistema_FasiLavorazione");

            entity.HasOne(d => d.IdOperatoreNavigation).WithMany(p => p.ProduzioneSistemas)
                .HasForeignKey(d => d.IdOperatore)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneSistema_Table_1");

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.ProduzioneSistemas)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduzioneSistema_ProcedureLavorazioni");
        });

        modelBuilder.Entity<QueryProcedureLavorazioni>(entity =>
        {
            entity.HasKey(e => e.IdQuery);

            entity.ToTable("QueryProcedureLavorazioni");

            entity.HasIndex(e => new { e.Titolo, e.IdproceduraLavorazione }, "IX_QueryProcedureLavorazioni").IsUnique();

            entity.Property(e => e.Connessione)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DataCreazioneQuery).HasColumnType("datetime");
            entity.Property(e => e.Descrizione).IsUnicode(false);
            entity.Property(e => e.IdproceduraLavorazione).HasColumnName("IDProceduraLavorazione");
            entity.Property(e => e.Note).IsUnicode(false);
            entity.Property(e => e.Titolo)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdproceduraLavorazioneNavigation).WithMany(p => p.QueryProcedureLavorazionis)
                .HasForeignKey(d => d.IdproceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QueryProcedureLavorazioni_ProcedureLavorazioni");
        });

        modelBuilder.Entity<QueryProcedureLavorazioniBackup>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("QueryProcedureLavorazioni_BACKUP");

            entity.Property(e => e.Connessione)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DataCreazioneQuery).HasColumnType("datetime");
            entity.Property(e => e.Descrizione).IsUnicode(false);
            entity.Property(e => e.IdQuery).ValueGeneratedOnAdd();
            entity.Property(e => e.IdproceduraLavorazione).HasColumnName("IDProceduraLavorazione");
            entity.Property(e => e.Note).IsUnicode(false);
            entity.Property(e => e.Titolo)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RepartiProduzione>(entity =>
        {
            entity.HasKey(e => e.IdReparti);

            entity.ToTable("RepartiProduzione");

            entity.Property(e => e.Reparti)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Ruoli>(entity =>
        {
            entity.HasKey(e => e.IdRuolo);

            entity.ToTable("Ruoli");

            entity.Property(e => e.Ruolo).HasMaxLength(50);
        });

        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("PK_HangFire_Schema");

            entity.ToTable("Schema", "HangFire");

            entity.Property(e => e.Version).ValueGeneratedNever();
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Server");

            entity.ToTable("Server", "HangFire");

            entity.HasIndex(e => e.LastHeartbeat, "IX_HangFire_Server_LastHeartbeat");

            entity.Property(e => e.Id).HasMaxLength(200);
            entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Value }).HasName("PK_HangFire_Set");

            entity.ToTable("Set", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Set_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => new { e.Key, e.Score }, "IX_HangFire_Set_Score");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(256);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Id }).HasName("PK_HangFire_State");

            entity.ToTable("State", "HangFire");

            entity.HasIndex(e => e.CreatedAt, "IX_HangFire_State_CreatedAt");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(100);

            entity.HasOne(d => d.Job).WithMany(p => p.States)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_State_Job");
        });

        modelBuilder.Entity<TaskDaEseguire>(entity =>
        {
            entity.HasKey(e => e.IdTaskDaEseguire).HasName("PK_Table_1_3");

            entity.ToTable("TaskDaEseguire");

            entity.HasIndex(e => e.IdConfigurazioneDatabase, "IX_Task_ConfigDB").HasFilter("([IdConfigurazioneDatabase] IS NOT NULL)");

            entity.Property(e => e.CronExpression)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DataStato).HasColumnType("datetime");
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.IdTaskHangFire).HasMaxLength(100);
            entity.Property(e => e.LastError)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.LastRunUtc).HasColumnType("datetime");
            entity.Property(e => e.Stato)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdConfigurazioneDatabaseNavigation).WithMany(p => p.TaskDaEseguires)
                .HasForeignKey(d => d.IdConfigurazioneDatabase)
                .HasConstraintName("FK_TaskDaEseguire_ConfigurazioneFontiDati");

            entity.HasOne(d => d.IdLavorazioneFaseDateReadingNavigation).WithMany(p => p.TaskDaEseguires)
                .HasForeignKey(d => d.IdLavorazioneFaseDateReading)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskDaEseguire_LavorazioniFasiDataReading");
        });

        modelBuilder.Entity<TaskDataReadingAggiornamento>(entity =>
        {
            entity.HasKey(e => e.IdAggiornamento).HasName("PK_DataReadingAggiornamento");

            entity.ToTable("TaskDataReadingAggiornamento");

            entity.Property(e => e.DataAggiornamento).HasColumnType("datetime");
            entity.Property(e => e.DataFineLavorazione).HasColumnType("datetime");
            entity.Property(e => e.DataInizioLavorazione).HasColumnType("datetime");
            entity.Property(e => e.DescrizioneEsito).IsUnicode(false);
            entity.Property(e => e.FaseLavorazione).IsUnicode(false);
            entity.Property(e => e.Lavorazione).IsUnicode(false);
        });

        modelBuilder.Entity<TaskServiceLavorazioni>(entity =>
        {
            entity.HasKey(e => e.IdService).HasName("PK_ServiceLavorazioni");

            entity.ToTable("TaskServiceLavorazioni");

            entity.Property(e => e.IdTaskHangFire).HasMaxLength(100);
            entity.Property(e => e.TaskService)
                .HasMaxLength(500)
                .IsFixedLength();

            entity.HasOne(d => d.IdProceduraLavorazioneNavigation).WithMany(p => p.TaskServiceLavorazionis)
                .HasForeignKey(d => d.IdProceduraLavorazione)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceLavorazioni_ProcedureLavorazioni");
        });

        modelBuilder.Entity<TipoTurni>(entity =>
        {
            entity.HasKey(e => e.IdTipoTurno);

            entity.ToTable("TipoTurni");

            entity.Property(e => e.TipoTurno)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipologieTotali>(entity =>
        {
            entity.HasKey(e => e.IdTipoTotale);

            entity.ToTable("TipologieTotali");

            entity.HasIndex(e => e.TipoTotale, "IX_TipologieTotali").IsUnique();

            entity.Property(e => e.TipoTotale)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipologieTotaliProduzione>(entity =>
        {
            entity.HasKey(e => e.IdproduzioneTotale);

            entity.ToTable("TipologieTotaliProduzione");

            entity.Property(e => e.IdproduzioneTotale).HasColumnName("IDProduzioneTotale");
            entity.Property(e => e.IdproduzioneOperatore).HasColumnName("IDProduzioneOperatore");
            entity.Property(e => e.IdtipologiaTotale).HasColumnName("IDTipologiaTotale");
            entity.Property(e => e.TipoTotale).IsUnicode(false);

            entity.HasOne(d => d.IdproduzioneOperatoreNavigation).WithMany(p => p.TipologieTotaliProduziones)
                .HasForeignKey(d => d.IdproduzioneOperatore)
                .HasConstraintName("FK_TipologieTotaliProduzione_ProduzioneOperatori");

            entity.HasOne(d => d.IdtipologiaTotaleNavigation).WithMany(p => p.TipologieTotaliProduziones)
                .HasForeignKey(d => d.IdtipologiaTotale)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TipologieTotaliProduzione_TipologieTotali");
        });

        modelBuilder.Entity<Turni>(entity =>
        {
            entity.HasKey(e => e.IdTurno).HasName("PK_Table_1_1");

            entity.ToTable("Turni");

            entity.Property(e => e.Turno).IsUnicode(false);
        });

        modelBuilder.Entity<VwConfigurazioneTaskSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ConfigurazioneTaskSummary");

            entity.Property(e => e.CodiceConfigurazione)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoFonte)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwConfigurazioniFontiDatiCompletum>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ConfigurazioniFontiDatiCompleta");

            entity.Property(e => e.Centro)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodiceConfigurazione)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ConnectionStringName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatoDa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatoIl).HasColumnType("datetime");
            entity.Property(e => e.FaseLavorazione)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HandlerClassName)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MailServiceCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeConfigurazione).HasMaxLength(200);
            entity.Property(e => e.NomeProcedura)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoFonte)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Z0072370Rdmkt28autGeUdaDettaglio>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO");

            entity.Property(e => e.BatchidSv)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("BATCHID_SV");
            entity.Property(e => e.CampoDe1)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_1");
            entity.Property(e => e.CampoDe10)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_10");
            entity.Property(e => e.CampoDe101)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_101");
            entity.Property(e => e.CampoDe102)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_102");
            entity.Property(e => e.CampoDe103)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_103");
            entity.Property(e => e.CampoDe104)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_104");
            entity.Property(e => e.CampoDe105)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_105");
            entity.Property(e => e.CampoDe106)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_106");
            entity.Property(e => e.CampoDe107)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_107");
            entity.Property(e => e.CampoDe108)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_108");
            entity.Property(e => e.CampoDe109)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_109");
            entity.Property(e => e.CampoDe11)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_11");
            entity.Property(e => e.CampoDe110)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_110");
            entity.Property(e => e.CampoDe111)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_111");
            entity.Property(e => e.CampoDe112)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_112");
            entity.Property(e => e.CampoDe113)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_113");
            entity.Property(e => e.CampoDe114)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_114");
            entity.Property(e => e.CampoDe115)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_115");
            entity.Property(e => e.CampoDe116)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_116");
            entity.Property(e => e.CampoDe117)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_117");
            entity.Property(e => e.CampoDe118)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_118");
            entity.Property(e => e.CampoDe119)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_119");
            entity.Property(e => e.CampoDe12)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_12");
            entity.Property(e => e.CampoDe120)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_120");
            entity.Property(e => e.CampoDe121)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_121");
            entity.Property(e => e.CampoDe122)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_122");
            entity.Property(e => e.CampoDe123)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_123");
            entity.Property(e => e.CampoDe124)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_124");
            entity.Property(e => e.CampoDe125)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_125");
            entity.Property(e => e.CampoDe126)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_126");
            entity.Property(e => e.CampoDe127)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_127");
            entity.Property(e => e.CampoDe128)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_128");
            entity.Property(e => e.CampoDe129)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_129");
            entity.Property(e => e.CampoDe13)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_13");
            entity.Property(e => e.CampoDe130)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_130");
            entity.Property(e => e.CampoDe131)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_131");
            entity.Property(e => e.CampoDe132)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_132");
            entity.Property(e => e.CampoDe133)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_133");
            entity.Property(e => e.CampoDe134)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_134");
            entity.Property(e => e.CampoDe135)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_135");
            entity.Property(e => e.CampoDe136)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_136");
            entity.Property(e => e.CampoDe137)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_137");
            entity.Property(e => e.CampoDe138)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_138");
            entity.Property(e => e.CampoDe139)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_139");
            entity.Property(e => e.CampoDe14)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_14");
            entity.Property(e => e.CampoDe140)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_140");
            entity.Property(e => e.CampoDe141)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_141");
            entity.Property(e => e.CampoDe142)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_142");
            entity.Property(e => e.CampoDe143)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_143");
            entity.Property(e => e.CampoDe144)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_144");
            entity.Property(e => e.CampoDe145)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_145");
            entity.Property(e => e.CampoDe146)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_146");
            entity.Property(e => e.CampoDe147)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_147");
            entity.Property(e => e.CampoDe148)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_148");
            entity.Property(e => e.CampoDe149)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_149");
            entity.Property(e => e.CampoDe15)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_15");
            entity.Property(e => e.CampoDe150)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_150");
            entity.Property(e => e.CampoDe2)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_2");
            entity.Property(e => e.CampoDe3)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_3");
            entity.Property(e => e.CampoDe4)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_4");
            entity.Property(e => e.CampoDe5)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_5");
            entity.Property(e => e.CampoDe6)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_6");
            entity.Property(e => e.CampoDe7)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_7");
            entity.Property(e => e.CampoDe8)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_8");
            entity.Property(e => e.CampoDe9)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_DE_9");
            entity.Property(e => e.CampoU1)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U1");
            entity.Property(e => e.CampoU10)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U10");
            entity.Property(e => e.CampoU2)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U2");
            entity.Property(e => e.CampoU3)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U3");
            entity.Property(e => e.CampoU4)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U4");
            entity.Property(e => e.CampoU5)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U5");
            entity.Property(e => e.CampoU6)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U6");
            entity.Property(e => e.CampoU7)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U7");
            entity.Property(e => e.CampoU8)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U8");
            entity.Property(e => e.CampoU9)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CAMPO_U9");
            entity.Property(e => e.Cda)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CDA");
            entity.Property(e => e.CodPrelievo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("COD_PRELIEVO");
            entity.Property(e => e.CodScarto)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("COD_SCARTO");
            entity.Property(e => e.CodiceMazzetta)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("CODICE_MAZZETTA");
            entity.Property(e => e.DataAccettazione)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_ACCETTAZIONE");
            entity.Property(e => e.DataIndex)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_INDEX");
            entity.Property(e => e.DataPrelievo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_PRELIEVO");
            entity.Property(e => e.DataPubbl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_PUBBL");
            entity.Property(e => e.DataScadSla)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SCAD_SLA");
            entity.Property(e => e.DataScan)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SCAN");
            entity.Property(e => e.DataScartIn)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SCART_IN");
            entity.Property(e => e.DataScartOut)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SCART_OUT");
            entity.Property(e => e.DataSospIn)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SOSP_IN");
            entity.Property(e => e.DataSospOut)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DATA_SOSP_OUT");
            entity.Property(e => e.Department)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DEPARTMENT");
            entity.Property(e => e.Hash256)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("HASH256");
            entity.Property(e => e.Id)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.IdFatt)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ID_FATT");
            entity.Property(e => e.IsCampione)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IS_CAMPIONE");
            entity.Property(e => e.NomeBatch)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NOME_BATCH");
            entity.Property(e => e.NomeFile)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NOME_FILE");
            entity.Property(e => e.NomePacchetto)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NOME_PACCHETTO");
            entity.Property(e => e.NotePrelievo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NOTE_PRELIEVO");
            entity.Property(e => e.NumChar)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NUM_CHAR");
            entity.Property(e => e.NumPag)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NUM_PAG");
            entity.Property(e => e.OpIndex)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("OP_INDEX");
            entity.Property(e => e.OpPrelievo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("OP_PRELIEVO");
            entity.Property(e => e.OpScan)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("OP_SCAN");
            entity.Property(e => e.OpScart)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("OP_SCART");
            entity.Property(e => e.OpSosp)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("OP_SOSP");
            entity.Property(e => e.ProgRilavorazione)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("PROG_RILAVORAZIONE");
            entity.Property(e => e.Progressivo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("PROGRESSIVO");
            entity.Property(e => e.Protocollo)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("PROTOCOLLO");
            entity.Property(e => e.Scatola)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("SCATOLA");
            entity.Property(e => e.StatoDoc)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("STATO_DOC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
