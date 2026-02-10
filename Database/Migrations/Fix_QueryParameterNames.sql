-- ================================================================
-- Migration: Fix Query Parameter Names
-- Description: Corregge i nomi dei parametri nelle query SQL salvate
--              da @startDataDe/@endDataDe a @startDate/@endDate
-- Date: 2025
-- ================================================================

USE [your_database_name]; -- Modificare con il nome del database corretto
GO

PRINT 'Inizio correzione nomi parametri nelle query SQL...';
GO

-- Aggiorna le query in ConfigurazioneFontiDati (TestoQuery)
UPDATE ConfigurazioneFontiDati
SET TestoQuery = REPLACE(REPLACE(TestoQuery, '@startDataDe', '@startDate'), '@endDataDe', '@endDate')
WHERE TestoQuery IS NOT NULL
  AND (TestoQuery LIKE '%@startDataDe%' OR TestoQuery LIKE '%@endDataDe%');

PRINT 'Aggiornate ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' query in ConfigurazioneFontiDati.TestoQuery';
GO

-- Aggiorna le query in ConfigurazioneFaseCentro (TestoQueryTask)
UPDATE ConfigurazioneFaseCentro
SET TestoQueryTask = REPLACE(REPLACE(TestoQueryTask, '@startDataDe', '@startDate'), '@endDataDe', '@endDate')
WHERE TestoQueryTask IS NOT NULL
  AND (TestoQueryTask LIKE '%@startDataDe%' OR TestoQueryTask LIKE '%@endDataDe%');

PRINT 'Aggiornate ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' query in ConfigurazioneFaseCentro.TestoQueryTask';
GO

-- Aggiorna le query in ConfigurazioneFaseCentro (TestoQueryOverride) se esiste ancora
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfigurazioneFaseCentro') AND name = 'TestoQueryOverride')
BEGIN
    UPDATE ConfigurazioneFaseCentro
    SET TestoQueryOverride = REPLACE(REPLACE(TestoQueryOverride, '@startDataDe', '@startDate'), '@endDataDe', '@endDate')
    WHERE TestoQueryOverride IS NOT NULL
      AND (TestoQueryOverride LIKE '%@startDataDe%' OR TestoQueryOverride LIKE '%@endDataDe%');

    PRINT 'Aggiornate ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' query in ConfigurazioneFaseCentro.TestoQueryOverride';
END
GO

-- Verifica query rimaste con parametri errati
DECLARE @QueryConParametriErrati INT = 0;

SELECT @QueryConParametriErrati = COUNT(*)
FROM (
    SELECT TestoQuery AS Query FROM ConfigurazioneFontiDati WHERE TestoQuery LIKE '%@startDataDe%' OR TestoQuery LIKE '%@endDataDe%'
    UNION ALL
    SELECT TestoQueryTask FROM ConfigurazioneFaseCentro WHERE TestoQueryTask LIKE '%@startDataDe%' OR TestoQueryTask LIKE '%@endDataDe%'
) AS QueryErrate;

IF @QueryConParametriErrati > 0
BEGIN
    PRINT 'ATTENZIONE: Trovate ancora ' + CAST(@QueryConParametriErrati AS VARCHAR(10)) + ' query con parametri errati!';
    
    -- Mostra le query problematiche
    SELECT 'ConfigurazioneFontiDati' AS Tabella, IdConfigurazione, CodiceUnivoco, TestoQuery AS Query
    FROM ConfigurazioneFontiDati 
    WHERE TestoQuery LIKE '%@startDataDe%' OR TestoQuery LIKE '%@endDataDe%'
    
    UNION ALL
    
    SELECT 'ConfigurazioneFaseCentro' AS Tabella, IdMapping, CAST(IdFaseLavorazione AS VARCHAR(10)) AS CodiceUnivoco, TestoQueryTask
    FROM ConfigurazioneFaseCentro 
    WHERE TestoQueryTask LIKE '%@startDataDe%' OR TestoQueryTask LIKE '%@endDataDe%';
END
ELSE
BEGIN
    PRINT 'Correzione completata con successo. Nessuna query con parametri errati rimasta.';
END
GO

PRINT 'Fine correzione nomi parametri.';
GO
