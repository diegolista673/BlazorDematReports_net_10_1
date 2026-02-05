-- =====================================================
-- Migration: Add Task Granular Configuration
-- Data: 2024
-- Descrizione: Aggiunge campi per gestione granulare dei task
-- =====================================================

BEGIN TRANSACTION;

GO

-- Step 1: Aggiungere nuove colonne alla tabella ConfigurazioneFaseCentro
ALTER TABLE ConfigurazioneFaseCentro
ADD 
    TipoTask NVARCHAR(50) NULL,              -- 'SQL', 'EmailCSV', 'HandlerIntegrato', 'Pipeline'
    CronExpression NVARCHAR(100) NULL,        -- Espressione CRON del task
    TestoQueryTask NVARCHAR(MAX) NULL,        -- Query specifica per il task
    MailServiceCode NVARCHAR(100) NULL,       -- Codice servizio mail (per EmailCSV)
    HandlerClassName NVARCHAR(255) NULL,      -- Nome classe handler (per HandlerIntegrato)
    EnabledTask BIT NOT NULL DEFAULT 1,       -- Abilita/Disabilita task
    UltimaModificaTask DATETIME NULL;         -- Timestamp ultima modifica task

GO

-- Step 2: Migrare dati esistenti da ParametriExtra a CronExpression
-- Estrae il CRON da JSON in ParametriExtra e lo sposta nella nuova colonna
UPDATE ConfigurazioneFaseCentro
SET CronExpression = 
    CASE 
        WHEN ParametriExtra IS NOT NULL 
             AND CHARINDEX('"cron"', ParametriExtra) > 0
        THEN 
            -- Estrae valore CRON dal JSON (formato: "cron":"0 5 * * *")
            SUBSTRING(
                ParametriExtra,
                CHARINDEX('"cron"', ParametriExtra) + 8,
                CHARINDEX('"', ParametriExtra, CHARINDEX('"cron"', ParametriExtra) + 9) - CHARINDEX('"cron"', ParametriExtra) - 8
            )
        ELSE '0 5 * * *' -- Default CRON (05:00 ogni giorno)
    END
WHERE CronExpression IS NULL;

GO

-- Step 3: Impostare TipoTask basandosi sulla configurazione parent
UPDATE fc
SET fc.TipoTask = cfd.TipoFonte
FROM ConfigurazioneFaseCentro fc
INNER JOIN ConfigurazioneFontiDati cfd ON fc.IdConfigurazione = cfd.IdConfigurazione
WHERE fc.TipoTask IS NULL;

GO

-- Step 4: Migrare TestoQueryOverride in TestoQueryTask
UPDATE ConfigurazioneFaseCentro
SET TestoQueryTask = TestoQueryOverride
WHERE TestoQueryTask IS NULL AND TestoQueryOverride IS NOT NULL;

GO

-- Step 5: Creare constraint per evitare duplicati (Fase + Cron)
-- Prima rimuove eventuali constraint esistenti con lo stesso nome
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_FaseCentro_Fase_Cron' AND object_id = OBJECT_ID('ConfigurazioneFaseCentro'))
BEGIN
    ALTER TABLE ConfigurazioneFaseCentro DROP CONSTRAINT UQ_FaseCentro_Fase_Cron;
END

GO

-- Crea unique index per prevenire duplicati
CREATE UNIQUE NONCLUSTERED INDEX UQ_FaseCentro_Fase_Cron
ON ConfigurazioneFaseCentro (IdConfigurazione, IdFaseLavorazione, CronExpression)
WHERE CronExpression IS NOT NULL;

GO

-- Step 6: Aggiungere indici per performance
CREATE NONCLUSTERED INDEX IX_ConfigurazioneFaseCentro_TipoTask
ON ConfigurazioneFaseCentro (TipoTask)
INCLUDE (EnabledTask);

GO

CREATE NONCLUSTERED INDEX IX_ConfigurazioneFaseCentro_EnabledTask
ON ConfigurazioneFaseCentro (EnabledTask)
WHERE EnabledTask = 1;

GO

-- Step 7: Aggiornare FlagAttiva per allinearlo con EnabledTask
UPDATE ConfigurazioneFaseCentro
SET EnabledTask = CASE WHEN FlagAttiva = 1 THEN 1 ELSE 0 END
WHERE EnabledTask IS NULL;

GO

COMMIT TRANSACTION;

GO

-- =====================================================
-- Verifica Migration
-- =====================================================

-- Controlla che le nuove colonne siano state create
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ConfigurazioneFaseCentro'
  AND COLUMN_NAME IN ('TipoTask', 'CronExpression', 'TestoQueryTask', 'MailServiceCode', 'HandlerClassName', 'EnabledTask', 'UltimaModificaTask')
ORDER BY ORDINAL_POSITION;

GO

-- Mostra statistiche migrazione dati
SELECT 
    'Totale record' AS Descrizione,
    COUNT(*) AS Conteggio
FROM ConfigurazioneFaseCentro

UNION ALL

SELECT 
    'Record con TipoTask valorizzato',
    COUNT(*)
FROM ConfigurazioneFaseCentro
WHERE TipoTask IS NOT NULL

UNION ALL

SELECT 
    'Record con CronExpression valorizzato',
    COUNT(*)
FROM ConfigurazioneFaseCentro
WHERE CronExpression IS NOT NULL

UNION ALL

SELECT 
    'Record con TestoQueryTask valorizzato',
    COUNT(*)
FROM ConfigurazioneFaseCentro
WHERE TestoQueryTask IS NOT NULL

UNION ALL

SELECT 
    'Task abilitati (EnabledTask = 1)',
    COUNT(*)
FROM ConfigurazioneFaseCentro
WHERE EnabledTask = 1;

GO

PRINT 'Migration completata con successo!';
