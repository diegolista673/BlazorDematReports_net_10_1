-- ============================================================
-- Migration: 20260530_CreateTable_DatiMailCsvHera16
-- Descrizione: Crea la tabella DatiMailCsvHera16 per lo staging dei dati CSV da email.
--              Popolata da Hera16IngestionProcessor via Hera16DataService.BulkInsertAsync.
--              Letta dagli handler di produzione (Hera16ScansioneHandler,
--              Hera16IndexHandler, Hera16ClassificazioneHandler).
-- Rollback: DROP TABLE IF EXISTS dbo.DatiMailCsvHera16;
-- ============================================================

IF OBJECT_ID(N'dbo.DatiMailCsvHera16', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatiMailCsvHera16
    (
        id_counter                  INT           IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DatiMailCsvHera16 PRIMARY KEY,

        -- Dati CSV grezzi
        codice_mercato              VARCHAR(50)   NULL,
        codice_offerta              VARCHAR(50)   NULL,
        tipo_documento              VARCHAR(50)   NULL,

        data_scansione              DATETIME      NULL,
        operatore_scan              VARCHAR(50)   NULL,

        data_classificazione        DATETIME      NULL,
        operatore_classificazione   VARCHAR(50)   NULL,

        data_index                  DATETIME      NULL,
        operatore_index             VARCHAR(50)   NULL,

        data_pubblicazione          DATETIME      NULL,
        codice_scatola              VARCHAR(50)   NULL,
        progr_scansione             VARCHAR(50)   NULL,

        -- Audit ingestion
        nome_file                   VARCHAR(100)  NULL,
        data_caricamento_file       DATETIME      NULL,
        identificativo_allegato     INT           NULL,

        -- Audit elaborazione: valorizzato dagli handler produzione
        -- quando le righe vengono lette e inserite in ProduzioneSistema.
        elaborato_il                DATETIME      NULL
    );

    -- Indice per query handler produzione su data_scansione
    CREATE INDEX IX_DatiMailCsvHera16_DataScansione
        ON dbo.DatiMailCsvHera16 (data_scansione)
        INCLUDE (id_counter, operatore_scan, codice_mercato, codice_offerta, tipo_documento);

    -- Indice per query handler produzione su data_index
    CREATE INDEX IX_DatiMailCsvHera16_DataIndex
        ON dbo.DatiMailCsvHera16 (data_index)
        INCLUDE (id_counter, operatore_index, codice_mercato, codice_offerta, tipo_documento);

    -- Indice per query handler produzione su data_classificazione
    CREATE INDEX IX_DatiMailCsvHera16_DataClassificazione
        ON dbo.DatiMailCsvHera16 (data_classificazione)
        INCLUDE (id_counter, operatore_classificazione, codice_mercato, codice_offerta, tipo_documento);

    -- Indice per delete-then-reinsert idempotente (Hera16DataService.BulkInsertAsync)
    CREATE INDEX IX_DatiMailCsvHera16_NomeFile
        ON dbo.DatiMailCsvHera16 (nome_file);

    PRINT 'Tabella DatiMailCsvHera16 creata con successo.';
END
ELSE
BEGIN
    PRINT 'Tabella DatiMailCsvHera16 gia esistente - nessuna azione eseguita.';
END
GO
