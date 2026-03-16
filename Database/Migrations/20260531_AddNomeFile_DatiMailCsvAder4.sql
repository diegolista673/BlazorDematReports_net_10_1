-- ============================================================
-- Migrazione: 20260531_AddNomeFile_DatiMailCsvAder4
-- Aggiunge la colonna NomeFile alla tabella DatiMailCsvAder4
-- per tracciare il file CSV di origine di ogni riga di staging.
-- ============================================================

ALTER TABLE [dbo].[DatiMailCsvAder4]
    ADD [NomeFile] NVARCHAR(260) NULL;
GO

-- Rollback:
-- ALTER TABLE [dbo].[DatiMailCsvAder4] DROP COLUMN [NomeFile];
