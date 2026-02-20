-- =============================================
-- Migration: Aggiungere campi metadata a ProduzioneSistema
-- Data: 2024
-- Descrizione: Aggiunge EventoId, NomeAllegato, CentroElaborazione
--              per tracciare dati da servizi mail (HERA16, ADER4)
-- =============================================

USE [DematReports]
GO

-- Step 1: Aggiungere colonne
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProduzioneSistema') AND name = 'EventoId')
BEGIN
    ALTER TABLE ProduzioneSistema
    ADD EventoId VARCHAR(100) NULL;
    
    PRINT 'Colonna EventoId aggiunta';
END
ELSE
BEGIN
    PRINT 'Colonna EventoId già presente';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProduzioneSistema') AND name = 'NomeAllegato')
BEGIN
    ALTER TABLE ProduzioneSistema
    ADD NomeAllegato VARCHAR(500) NULL;
    
    PRINT 'Colonna NomeAllegato aggiunta';
END
ELSE
BEGIN
    PRINT 'Colonna NomeAllegato già presente';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProduzioneSistema') AND name = 'CentroElaborazione')
BEGIN
    ALTER TABLE ProduzioneSistema
    ADD CentroElaborazione VARCHAR(50) NULL;
    
    PRINT 'Colonna CentroElaborazione aggiunta';
END
ELSE
BEGIN
    PRINT 'Colonna CentroElaborazione già presente';
END
GO

-- Step 2: Creare indici
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProdSistema_EventoId' AND object_id = OBJECT_ID('ProduzioneSistema'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProdSistema_EventoId
    ON ProduzioneSistema(EventoId)
    WHERE EventoId IS NOT NULL;
    
    PRINT 'Indice IX_ProdSistema_EventoId creato';
END
ELSE
BEGIN
    PRINT 'Indice IX_ProdSistema_EventoId già presente';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProdSistema_NomeAllegato' AND object_id = OBJECT_ID('ProduzioneSistema'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProdSistema_NomeAllegato
    ON ProduzioneSistema(NomeAllegato)
    WHERE NomeAllegato IS NOT NULL;
    
    PRINT 'Indice IX_ProdSistema_NomeAllegato creato';
END
ELSE
BEGIN
    PRINT 'Indice IX_ProdSistema_NomeAllegato già presente';
END
GO

-- Step 3: Verifica finale
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ProduzioneSistema'
  AND COLUMN_NAME IN ('EventoId', 'NomeAllegato', 'CentroElaborazione')
ORDER BY COLUMN_NAME;

PRINT 'Migration completata con successo!';
GO
