-- =============================================
-- MIGRATION: Sistema Unificato Configurazione Fonti Dati
-- Versione: 1.0
-- Data: 2024
-- Descrizione: Crea tabelle per configurazione unificata fonti dati
-- =============================================

USE [DematReports]
GO

PRINT '=========================================='
PRINT 'Inizio migration ConfigurazioneFontiDati'
PRINT '=========================================='

-- =============================================
-- 1. TABELLA PRINCIPALE: ConfigurazioneFontiDati
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfigurazioneFontiDati')
BEGIN
    CREATE TABLE ConfigurazioneFontiDati (
        -- Primary Key
        IdConfigurazione INT PRIMARY KEY IDENTITY(1,1),
        
        -- Identificazione
        CodiceConfigurazione VARCHAR(100) NOT NULL,
        NomeConfigurazione NVARCHAR(200) NOT NULL,
        DescrizioneConfigurazione NVARCHAR(500) NULL,
        
        -- Tipo fonte dati: SQL, EmailCSV, HandlerIntegrato, Pipeline
        TipoFonte VARCHAR(50) NOT NULL,
        
        -- Configurazione SQL
        TestoQuery NVARCHAR(MAX) NULL,
        ConnectionStringName VARCHAR(100) NULL,
        
        -- Configurazione Email (riferimento a sezione appsettings.json)
        MailServiceCode VARCHAR(100) NULL,
        
        -- Configurazione Handler C# Integrato
        HandlerClassName VARCHAR(200) NULL,
        
        -- Metadata
        CreatoDa VARCHAR(100) NULL,
        CreatoIl DATETIME DEFAULT GETDATE(),
        ModificatoDa VARCHAR(100) NULL,
        ModificatoIl DATETIME NULL,
        FlagAttiva BIT DEFAULT 1,
        
        -- Constraints
        CONSTRAINT UQ_ConfigFonte_Codice UNIQUE (CodiceConfigurazione),
        CONSTRAINT CK_ConfigFonte_TipoFonte 
            CHECK (TipoFonte IN ('SQL', 'EmailCSV', 'HandlerIntegrato', 'Pipeline'))
    );
    
    PRINT '? Tabella ConfigurazioneFontiDati creata'
END
ELSE
BEGIN
    PRINT '?? Tabella ConfigurazioneFontiDati già esistente'
END
GO

-- =============================================
-- 2. TABELLA: ConfigurazioneFaseCentro
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfigurazioneFaseCentro')
BEGIN
    CREATE TABLE ConfigurazioneFaseCentro (
        -- Primary Key
        IdFaseCentro INT PRIMARY KEY IDENTITY(1,1),
        
        -- Foreign Key
        IdConfigurazione INT NOT NULL,
        
        -- Mapping
        IdProceduraLavorazione INT NOT NULL,
        IdFaseLavorazione INT NOT NULL,
        IdCentro INT NOT NULL,
        
        -- Query override per questa combinazione (opzionale)
        TestoQueryOverride NVARCHAR(MAX) NULL,
        
        -- Parametri extra in JSON (es: {"department": "GENOVA"})
        ParametriExtra NVARCHAR(MAX) NULL,
        
        -- Mapping colonne in JSON (es: {"Operatore": "OP_SCAN"})
        MappingColonne NVARCHAR(MAX) NULL,
        
        FlagAttiva BIT DEFAULT 1,
        
        -- Constraints
        CONSTRAINT FK_FaseCentro_Configurazione 
            FOREIGN KEY (IdConfigurazione)
            REFERENCES ConfigurazioneFontiDati(IdConfigurazione)
            ON DELETE CASCADE,
            
        CONSTRAINT FK_FaseCentro_Procedura 
            FOREIGN KEY (IdProceduraLavorazione)
            REFERENCES ProcedureLavorazioni(IdproceduraLavorazione),
            
        CONSTRAINT FK_FaseCentro_Fase 
            FOREIGN KEY (IdFaseLavorazione)
            REFERENCES FasiLavorazione(IdFaseLavorazione),
            
        CONSTRAINT FK_FaseCentro_Centro 
            FOREIGN KEY (IdCentro)
            REFERENCES CentriLavorazione(IdCentro),
            
        CONSTRAINT UQ_FaseCentro_Unique 
            UNIQUE (IdConfigurazione, IdProceduraLavorazione, IdFaseLavorazione, IdCentro)
    );
    
    PRINT '? Tabella ConfigurazioneFaseCentro creata'
END
ELSE
BEGIN
    PRINT '?? Tabella ConfigurazioneFaseCentro già esistente'
END
GO

-- =============================================
-- 3. TABELLA: ConfigurazionePipelineStep
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfigurazionePipelineStep')
BEGIN
    CREATE TABLE ConfigurazionePipelineStep (
        -- Primary Key
        IdPipelineStep INT PRIMARY KEY IDENTITY(1,1),
        
        -- Foreign Key
        IdConfigurazione INT NOT NULL,
        
        -- Step info
        NumeroStep INT NOT NULL,
        NomeStep NVARCHAR(100) NOT NULL,
        TipoStep VARCHAR(50) NOT NULL,
        
        -- Configurazione step in JSON
        ConfigurazioneStep NVARCHAR(MAX) NOT NULL,
        
        FlagAttiva BIT DEFAULT 1,
        
        -- Constraints
        CONSTRAINT FK_Pipeline_Configurazione 
            FOREIGN KEY (IdConfigurazione)
            REFERENCES ConfigurazioneFontiDati(IdConfigurazione)
            ON DELETE CASCADE,
            
        CONSTRAINT CK_Pipeline_TipoStep 
            CHECK (TipoStep IN ('Query', 'Filter', 'Transform', 'Aggregate', 'Merge'))
    );
    
    PRINT '? Tabella ConfigurazionePipelineStep creata'
END
ELSE
BEGIN
    PRINT '?? Tabella ConfigurazionePipelineStep già esistente'
END
GO

-- =============================================
-- 4. MODIFICA: TaskDaEseguire - Aggiunta FK
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('TaskDaEseguire') 
    AND name = 'IdConfigurazioneDatabase'
)
BEGIN
    ALTER TABLE TaskDaEseguire
    ADD IdConfigurazioneDatabase INT NULL;
    
    ALTER TABLE TaskDaEseguire
    ADD CONSTRAINT FK_Task_ConfigFonte 
        FOREIGN KEY (IdConfigurazioneDatabase)
        REFERENCES ConfigurazioneFontiDati(IdConfigurazione);
    
    PRINT '? Campo IdConfigurazioneDatabase aggiunto a TaskDaEseguire'
END
ELSE
BEGIN
    PRINT '?? Campo IdConfigurazioneDatabase già esistente'
END
GO

-- =============================================
-- 5. INDICI PER PERFORMANCE
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ConfigFonte_TipoFonte')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConfigFonte_TipoFonte 
        ON ConfigurazioneFontiDati(TipoFonte) 
        WHERE FlagAttiva = 1;
    PRINT '? Indice IX_ConfigFonte_TipoFonte creato'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ConfigFonte_Codice')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConfigFonte_Codice 
        ON ConfigurazioneFontiDati(CodiceConfigurazione);
    PRINT '? Indice IX_ConfigFonte_Codice creato'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FaseCentro_Config')
BEGIN
    CREATE NONCLUSTERED INDEX IX_FaseCentro_Config 
        ON ConfigurazioneFaseCentro(IdConfigurazione);
    PRINT '? Indice IX_FaseCentro_Config creato'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Pipeline_Config')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pipeline_Config 
        ON ConfigurazionePipelineStep(IdConfigurazione, NumeroStep);
    PRINT '? Indice IX_Pipeline_Config creato'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Task_ConfigDB')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Task_ConfigDB 
        ON TaskDaEseguire(IdConfigurazioneDatabase) 
        WHERE IdConfigurazioneDatabase IS NOT NULL;
    PRINT '? Indice IX_Task_ConfigDB creato'
END
GO

-- =============================================
-- 6. VISTA RIEPILOGATIVA
-- =============================================
CREATE OR ALTER VIEW vw_ConfigurazioniFontiDatiCompleta AS
SELECT 
    cf.IdConfigurazione,
    cf.CodiceConfigurazione,
    cf.NomeConfigurazione,
    cf.TipoFonte,
    cf.TestoQuery,
    cf.MailServiceCode,
    cf.HandlerClassName,
    cf.ConnectionStringName,
    cf.FlagAttiva AS ConfigAttiva,
    cf.CreatoIl,
    cf.CreatoDa,
    fc.IdFaseCentro,
    fc.IdProceduraLavorazione,
    p.NomeProcedura,
    fc.IdFaseLavorazione,
    f.FaseLavorazione,
    fc.IdCentro,
    c.Centro,
    fc.ParametriExtra,
    fc.MappingColonne,
    fc.TestoQueryOverride,
    (SELECT COUNT(*) 
     FROM TaskDaEseguire t 
     WHERE t.IdConfigurazioneDatabase = cf.IdConfigurazione 
     AND t.Enabled = 1) AS TaskAttivi,
    (SELECT COUNT(*) 
     FROM ConfigurazionePipelineStep ps 
     WHERE ps.IdConfigurazione = cf.IdConfigurazione 
     AND ps.FlagAttiva = 1) AS PipelineSteps
FROM ConfigurazioneFontiDati cf
LEFT JOIN ConfigurazioneFaseCentro fc 
    ON cf.IdConfigurazione = fc.IdConfigurazione AND fc.FlagAttiva = 1
LEFT JOIN ProcedureLavorazioni p 
    ON fc.IdProceduraLavorazione = p.IdproceduraLavorazione
LEFT JOIN FasiLavorazione f 
    ON fc.IdFaseLavorazione = f.IdFaseLavorazione
LEFT JOIN CentriLavorazione c 
    ON fc.IdCentro = c.IdCentro
WHERE cf.FlagAttiva = 1;
GO

PRINT '? Vista vw_ConfigurazioniFontiDatiCompleta creata'

-- =============================================
-- 7. VERIFICA FINALE
-- =============================================
PRINT ''
PRINT '=========================================='
PRINT 'VERIFICA STRUTTURA CREATA'
PRINT '=========================================='

SELECT 
    t.name AS Tabella,
    (SELECT COUNT(*) FROM sys.columns WHERE object_id = t.object_id) AS Colonne,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id = t.object_id AND is_primary_key = 0 AND is_unique_constraint = 0) AS Indici
FROM sys.tables t
WHERE t.name IN ('ConfigurazioneFontiDati', 'ConfigurazioneFaseCentro', 'ConfigurazionePipelineStep')
ORDER BY t.name;

PRINT ''
PRINT '=========================================='
PRINT '? MIGRATION COMPLETATA CON SUCCESSO!'
PRINT '=========================================='
GO
