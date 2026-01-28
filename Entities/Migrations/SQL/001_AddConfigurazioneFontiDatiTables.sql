-- =====================================================
-- Migration: Add ConfigurazioneFontiDati System
-- Data: 2024-01-26
-- Descrizione: Aggiunge tabelle per sistema unificato
--              configurazione fonti dati
-- =====================================================

USE DematReports;
GO

-- =====================================================
-- 1. Tabella ConfigurazioneFontiDati (principale)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConfigurazioneFontiDati]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConfigurazioneFontiDati] (
        [IdConfigurazione] INT IDENTITY(1,1) NOT NULL,
        [CodiceConfigurazione] VARCHAR(100) NOT NULL,
        [NomeConfigurazione] NVARCHAR(200) NOT NULL,
        [DescrizioneConfigurazione] NVARCHAR(500) NULL,
        [TipoFonte] VARCHAR(50) NOT NULL,
        [TestoQuery] NVARCHAR(MAX) NULL,
        [ConnectionStringName] VARCHAR(100) NULL,
        [MailServiceCode] VARCHAR(100) NULL,
        [HandlerClassName] VARCHAR(200) NULL,
        [CreatoDa] VARCHAR(100) NULL,
        [CreatoIl] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModificatoDa] VARCHAR(100) NULL,
        [ModificatoIl] DATETIME NULL,
        [FlagAttiva] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ConfigurazioneFontiDati] PRIMARY KEY CLUSTERED ([IdConfigurazione] ASC),
        CONSTRAINT [UK_ConfigurazioneFontiDati_Codice] UNIQUE ([CodiceConfigurazione])
    );
    
    PRINT 'Tabella ConfigurazioneFontiDati creata con successo.';
END
ELSE
BEGIN
    PRINT 'Tabella ConfigurazioneFontiDati esiste già.';
END
GO

-- =====================================================
-- 2. Tabella ConfigurazioneFaseCentro (mapping N:N)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConfigurazioneFaseCentro]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConfigurazioneFaseCentro] (
        [IdFaseCentro] INT IDENTITY(1,1) NOT NULL,
        [IdConfigurazione] INT NOT NULL,
        [IdProceduraLavorazione] INT NOT NULL,
        [IdFaseLavorazione] INT NOT NULL,
        [IdCentro] INT NOT NULL,
        [ParametriExtra] NVARCHAR(MAX) NULL,
        [TestoQueryOverride] NVARCHAR(MAX) NULL,
        [MappingColonne] NVARCHAR(MAX) NULL,
        [FlagAttiva] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ConfigurazioneFaseCentro] PRIMARY KEY CLUSTERED ([IdFaseCentro] ASC),
        CONSTRAINT [FK_ConfigurazioneFaseCentro_Configurazione] 
            FOREIGN KEY ([IdConfigurazione]) 
            REFERENCES [dbo].[ConfigurazioneFontiDati] ([IdConfigurazione]),
        CONSTRAINT [FK_ConfigurazioneFaseCentro_Procedura] 
            FOREIGN KEY ([IdProceduraLavorazione]) 
            REFERENCES [dbo].[ProcedureLavorazioni] ([IDProceduraLavorazione]),
        CONSTRAINT [FK_ConfigurazioneFaseCentro_Fase] 
            FOREIGN KEY ([IdFaseLavorazione]) 
            REFERENCES [dbo].[FasiLavorazione] ([IDFaseLavorazione]),
        CONSTRAINT [FK_ConfigurazioneFaseCentro_Centro] 
            FOREIGN KEY ([IdCentro]) 
            REFERENCES [dbo].[CentriLavorazione] ([IDCentro])
    );
    
    -- Indice per prestazioni query
    CREATE NONCLUSTERED INDEX [IX_ConfigurazioneFaseCentro_IdConfigurazione] 
        ON [dbo].[ConfigurazioneFaseCentro] ([IdConfigurazione]);
    
    CREATE NONCLUSTERED INDEX [IX_ConfigurazioneFaseCentro_ProcFaseCentro] 
        ON [dbo].[ConfigurazioneFaseCentro] ([IdProceduraLavorazione], [IdFaseLavorazione], [IdCentro]);
    
    PRINT 'Tabella ConfigurazioneFaseCentro creata con successo.';
END
ELSE
BEGIN
    PRINT 'Tabella ConfigurazioneFaseCentro esiste già.';
END
GO

-- =====================================================
-- 3. Tabella ConfigurazionePipelineStep (futuro)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConfigurazionePipelineStep]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConfigurazionePipelineStep] (
        [IdPipelineStep] INT IDENTITY(1,1) NOT NULL,
        [IdConfigurazione] INT NOT NULL,
        [NumeroStep] INT NOT NULL,
        [NomeStep] VARCHAR(100) NULL,
        [TipoStep] VARCHAR(50) NULL,
        [ConfigurazioneStep] NVARCHAR(MAX) NULL,
        [FlagAttiva] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ConfigurazionePipelineStep] PRIMARY KEY CLUSTERED ([IdPipelineStep] ASC),
        CONSTRAINT [FK_ConfigurazionePipelineStep_Configurazione] 
            FOREIGN KEY ([IdConfigurazione]) 
            REFERENCES [dbo].[ConfigurazioneFontiDati] ([IdConfigurazione])
    );
    
    -- Indice per ordinamento step
    CREATE NONCLUSTERED INDEX [IX_ConfigurazionePipelineStep_ConfigStep] 
        ON [dbo].[ConfigurazionePipelineStep] ([IdConfigurazione], [NumeroStep]);
    
    PRINT 'Tabella ConfigurazionePipelineStep creata con successo.';
END
ELSE
BEGIN
    PRINT 'Tabella ConfigurazionePipelineStep esiste già.';
END
GO

-- =====================================================
-- 4. Modifica TaskDaEseguire (aggiunta colonna FK)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
               AND name = 'IdConfigurazioneDatabase')
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    ADD [IdConfigurazioneDatabase] INT NULL;
    
    PRINT 'Colonna IdConfigurazioneDatabase aggiunta a TaskDaEseguire.';
END
ELSE
BEGIN
    PRINT 'Colonna IdConfigurazioneDatabase esiste già in TaskDaEseguire.';
END
GO

-- Aggiungi FK constraint solo se non esiste
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'[dbo].[FK_TaskDaEseguire_ConfigurazioneFontiDati]'))
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    ADD CONSTRAINT [FK_TaskDaEseguire_ConfigurazioneFontiDati]
        FOREIGN KEY ([IdConfigurazioneDatabase])
        REFERENCES [dbo].[ConfigurazioneFontiDati] ([IdConfigurazione]);
    
    PRINT 'FK constraint FK_TaskDaEseguire_ConfigurazioneFontiDati creata.';
END
ELSE
BEGIN
    PRINT 'FK constraint FK_TaskDaEseguire_ConfigurazioneFontiDati esiste già.';
END
GO

-- Indice per FK
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
               AND name = 'IX_TaskDaEseguire_IdConfigurazioneDatabase')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaskDaEseguire_IdConfigurazioneDatabase]
        ON [dbo].[TaskDaEseguire] ([IdConfigurazioneDatabase]);
    
    PRINT 'Indice IX_TaskDaEseguire_IdConfigurazioneDatabase creato.';
END
GO

-- =====================================================
-- 5. Verifica finale
-- =====================================================
PRINT '';
PRINT '=====================================================';
PRINT 'Migration completata con successo!';
PRINT '=====================================================';
PRINT 'Tabelle create/verificate:';
PRINT '  - ConfigurazioneFontiDati';
PRINT '  - ConfigurazioneFaseCentro';
PRINT '  - ConfigurazionePipelineStep';
PRINT 'Modifiche TaskDaEseguire:';
PRINT '  - Colonna IdConfigurazioneDatabase (NULL)';
PRINT '  - FK constraint a ConfigurazioneFontiDati';
PRINT '=====================================================';

-- Conteggio record
SELECT 'ConfigurazioneFontiDati' AS [Tabella], COUNT(*) AS [Record] 
FROM [dbo].[ConfigurazioneFontiDati]
UNION ALL
SELECT 'ConfigurazioneFaseCentro', COUNT(*) 
FROM [dbo].[ConfigurazioneFaseCentro]
UNION ALL
SELECT 'ConfigurazionePipelineStep', COUNT(*) 
FROM [dbo].[ConfigurazionePipelineStep];
GO
