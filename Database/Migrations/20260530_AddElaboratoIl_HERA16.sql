-- ============================================================
-- Migration: 20260530_AddElaboratoIl_HERA16
-- Descrizione: Aggiunge la colonna elaborato_il alla tabella DatiMailCsvHera16.
--              Viene valorizzata dagli handler di produzione (Hera16ScansioneHandler,
--              Hera16IndexHandler, Hera16ClassificazioneHandler) al momento
--              in cui le righe vengono lette e inserite in ProduzioneSistema.
--              Usare solo se la tabella esiste gia senza la colonna.
--              Se la tabella non esiste, eseguire prima 20260530_CreateTable_DatiMailCsvHera16.sql.
-- Rollback: ALTER TABLE dbo.DatiMailCsvHera16 DROP COLUMN elaborato_il;
-- ============================================================

IF OBJECT_ID(N'dbo.DatiMailCsvHera16', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.DatiMailCsvHera16')
         AND name = 'elaborato_il')
BEGIN
    ALTER TABLE dbo.DatiMailCsvHera16
        ADD elaborato_il DATETIME NULL;

    PRINT 'Colonna elaborato_il aggiunta a DatiMailCsvHera16.';
END
ELSE
BEGIN
    PRINT 'Colonna elaborato_il gia presente o tabella non esistente - nessuna azione.';
END
GO
