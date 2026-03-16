-- =============================================================
-- Migrazione: DatiMailCsv
-- Versione:   2.0 - Staging per-operatore unificato (ADER4 + HERA16)
-- Data:       2025-03-04
-- =============================================================

-- Ogni riga contiene il totale documenti per (Operatore, TipoRisultato, DataLavorazione).
-- ADER4:  Operatore = colonna 'postazione' del CSV
-- HERA16: Operatore = colonna OperatoreScansione/Index/Classificazione del CSV

CREATE TABLE DatiMailCsv
(
    Id                 INT           IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_DatiMailCsv PRIMARY KEY,

    -- Sorgente
    CodiceServizio     NVARCHAR(50)  NOT NULL,   -- 'ADER4' | 'HERA16'
    DataLavorazione    DATE          NOT NULL,
    Operatore          NVARCHAR(100) NOT NULL,   -- valore 'postazione' (ADER4) o nome op. (HERA16)
    TipoRisultato      NVARCHAR(100) NOT NULL,   -- 'ScansioneCaptiva' | 'Scansione' | 'Index' | ...

    -- Totale documenti: SUM("Numero documenti") o COUNT(*) raggruppato per operatore
    Documenti          INT           NOT NULL DEFAULT 0,

    -- Contesto
    IdEvento           NVARCHAR(100) NULL,
    Centro             NVARCHAR(50)  NULL,       -- 'VERONA' | 'GENOVA' | NULL

    -- Audit
    DataIngestione     DATETIME      NOT NULL CONSTRAINT DF_DatiMailCsv_DataIngestione DEFAULT GETDATE(),

    -- Stato elaborazione (letto dagli handler produzione)
    Elaborata          BIT           NOT NULL CONSTRAINT DF_DatiMailCsv_Elaborata DEFAULT 0,
    ElaborataIl        DATETIME      NULL,
    ElaborataDaTaskId  INT           NULL,

    -- Deduplicazione: una riga per (servizio, data, operatore, tipo, evento, centro)
    CONSTRAINT UQ_DatiMailCsv
        UNIQUE (CodiceServizio, DataLavorazione, Operatore, TipoRisultato, IdEvento, Centro)
);
GO

-- Indice per query handler produzione (lookup principale)
CREATE INDEX IX_DatiMailCsv_Lookup
    ON DatiMailCsv (CodiceServizio, TipoRisultato, DataLavorazione)
    INCLUDE (Id, Operatore, Documenti, IdEvento, Centro, Elaborata);
GO

-- Indice filtered per record non ancora elaborati (query frequente)
CREATE INDEX IX_DatiMailCsv_NonElaborati
    ON DatiMailCsv (CodiceServizio, TipoRisultato, DataLavorazione)
    WHERE Elaborata = 0;
GO
