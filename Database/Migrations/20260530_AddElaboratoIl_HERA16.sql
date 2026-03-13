-- ============================================================
-- Migration: 20260530_AddElaboratoIl_HERA16
-- Descrizione: Aggiunge la colonna elaborato_il alla tabella HERA16.
--              Viene valorizzata dagli handler di produzione (Hera16ScansioneHandler,
--              Hera16IndexHandler, Hera16ClassificazioneHandler) al momento
--              in cui le righe vengono lette e inserite in ProduzioneSistema.
-- Rollback:    ALTER TABLE HERA16 DROP COLUMN elaborato_il;
-- ============================================================

ALTER TABLE HERA16
    ADD elaborato_il DATETIME NULL;
GO
