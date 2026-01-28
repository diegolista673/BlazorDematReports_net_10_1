-- =====================================================
-- Migration: Remove QueryProcedureLavorazioni Dependencies
-- Data: 2024-01-26
-- Descrizione: Rimuove dipendenze legacy da QueryProcedureLavorazioni
--              Sistema migrato a ConfigurazioneFontiDati
-- =====================================================

USE DematReports;
GO

PRINT '=====================================================';
PRINT 'MIGRATION: Remove QueryProcedureLavorazioni System';
PRINT '=====================================================';
PRINT '';

-- =====================================================
-- STEP 1: Verifica Prerequisiti
-- =====================================================
PRINT 'STEP 1: Verifica prerequisiti...';

-- Verifica task attivi con IdQuery
DECLARE @TaskConIdQuery INT;
SELECT @TaskConIdQuery = COUNT(*)
FROM TaskDaEseguire
WHERE IdQuery IS NOT NULL AND Enabled = 1;

IF @TaskConIdQuery > 0
BEGIN
    PRINT '??  WARNING: Esistono ' + CAST(@TaskConIdQuery AS VARCHAR(10)) + ' task attivi che usano IdQuery!';
    PRINT '   Migrazione BLOCCATA. Migrare prima i task al nuovo sistema.';
    RAISERROR('Migration aborted: active tasks using IdQuery', 16, 1);
    RETURN;
END
ELSE
BEGIN
    PRINT '? Nessun task attivo usa IdQuery - OK per procedere';
END

-- Verifica task attivi con QueryIntegrata
DECLARE @TaskConQueryIntegrata INT;
SELECT @TaskConQueryIntegrata = COUNT(*)
FROM TaskDaEseguire
WHERE QueryIntegrata = 1 AND Enabled = 1;

IF @TaskConQueryIntegrata > 0
BEGIN
    PRINT '??  WARNING: Esistono ' + CAST(@TaskConQueryIntegrata AS VARCHAR(10)) + ' task attivi che usano QueryIntegrata!';
    PRINT '   Migrazione BLOCCATA. Migrare prima i task al nuovo sistema.';
    RAISERROR('Migration aborted: active tasks using QueryIntegrata', 16, 1);
    RETURN;
END
ELSE
BEGIN
    PRINT '? Nessun task attivo usa QueryIntegrata - OK per procedere';
END

PRINT '';

-- =====================================================
-- STEP 2: Backup Query Legacy (Optional)
-- =====================================================
PRINT 'STEP 2: Backup query legacy...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QueryProcedureLavorazioni_BACKUP]'))
BEGIN
    SELECT *
    INTO QueryProcedureLavorazioni_BACKUP
    FROM QueryProcedureLavorazioni;
    
    DECLARE @BackupCount INT;
    SELECT @BackupCount = COUNT(*) FROM QueryProcedureLavorazioni_BACKUP;
    PRINT '? Backup creato: ' + CAST(@BackupCount AS VARCHAR(10)) + ' query salvate in QueryProcedureLavorazioni_BACKUP';
END
ELSE
BEGIN
    PRINT '??  Tabella backup già esistente';
END

PRINT '';

-- =====================================================
-- STEP 3: Rimuovi FK Constraint (IdQuery)
-- =====================================================
PRINT 'STEP 3: Rimozione FK constraint...';

-- Rimuovi FK TaskDaEseguire -> QueryProcedureLavorazioni
IF EXISTS (SELECT * FROM sys.foreign_keys 
           WHERE object_id = OBJECT_ID(N'[dbo].[FK_TaskDaEseguire_QueryProcedureLavorazioni]'))
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    DROP CONSTRAINT [FK_TaskDaEseguire_QueryProcedureLavorazioni];
    
    PRINT '? FK constraint FK_TaskDaEseguire_QueryProcedureLavorazioni rimossa';
END
ELSE
BEGIN
    PRINT '??  FK constraint già rimossa o non esistente';
END

-- Rimuovi eventuali altri FK constraint legacy
DECLARE @ConstraintName NVARCHAR(200);
DECLARE constraint_cursor CURSOR FOR
SELECT name
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID('TaskDaEseguire')
  AND referenced_object_id = OBJECT_ID('QueryProcedureLavorazioni');

OPEN constraint_cursor;
FETCH NEXT FROM constraint_cursor INTO @ConstraintName;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @SQL NVARCHAR(500);
    SET @SQL = 'ALTER TABLE [dbo].[TaskDaEseguire] DROP CONSTRAINT [' + @ConstraintName + ']';
    EXEC sp_executesql @SQL;
    PRINT '? FK constraint ' + @ConstraintName + ' rimossa';
    
    FETCH NEXT FROM constraint_cursor INTO @ConstraintName;
END

CLOSE constraint_cursor;
DEALLOCATE constraint_cursor;

PRINT '';

-- =====================================================
-- STEP 4: Rimuovi Indici su IdQuery
-- =====================================================
PRINT 'STEP 4: Rimozione indici...';

IF EXISTS (SELECT * FROM sys.indexes 
           WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
           AND name = 'IX_TaskDaEseguire_IdQuery')
BEGIN
    DROP INDEX [IX_TaskDaEseguire_IdQuery] ON [dbo].[TaskDaEseguire];
    PRINT '? Indice IX_TaskDaEseguire_IdQuery rimosso';
END
ELSE
BEGIN
    PRINT '??  Indice IX_TaskDaEseguire_IdQuery non esistente';
END

PRINT '';

-- =====================================================
-- STEP 5: Rimuovi Colonne Legacy da TaskDaEseguire
-- =====================================================
PRINT 'STEP 5: Rimozione colonne legacy...';

-- Rimuovi IdQuery
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
           AND name = 'IdQuery')
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    DROP COLUMN [IdQuery];
    
    PRINT '? Colonna IdQuery rimossa da TaskDaEseguire';
END
ELSE
BEGIN
    PRINT '??  Colonna IdQuery già rimossa';
END

-- Rimuovi QueryIntegrata
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
           AND name = 'QueryIntegrata')
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    DROP COLUMN [QueryIntegrata];
    
    PRINT '? Colonna QueryIntegrata rimossa da TaskDaEseguire';
END
ELSE
BEGIN
    PRINT '??  Colonna QueryIntegrata già rimossa';
END

-- Rimuovi Connessione
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
           AND name = 'Connessione')
BEGIN
    ALTER TABLE [dbo].[TaskDaEseguire]
    DROP COLUMN [Connessione];
    
    PRINT '? Colonna Connessione rimossa da TaskDaEseguire';
END
ELSE
BEGIN
    PRINT '??  Colonna Connessione già rimossa';
END

-- Rimuovi MailServiceCode (se non usato da nuovo sistema)
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[TaskDaEseguire]') 
           AND name = 'MailServiceCode')
BEGIN
    -- Verifica se usato da nuovo sistema
    DECLARE @TaskConMailService INT;
    SELECT @TaskConMailService = COUNT(*)
    FROM TaskDaEseguire
    WHERE MailServiceCode IS NOT NULL 
      AND IdConfigurazioneDatabase IS NULL
      AND Enabled = 1;
    
    IF @TaskConMailService > 0
    BEGIN
        PRINT '??  WARNING: ' + CAST(@TaskConMailService AS VARCHAR(10)) + ' task attivi usano MailServiceCode senza nuova config';
        PRINT '   Colonna MailServiceCode MANTENUTA per backward compatibility';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[TaskDaEseguire]
        DROP COLUMN [MailServiceCode];
        
        PRINT '? Colonna MailServiceCode rimossa da TaskDaEseguire';
    END
END
ELSE
BEGIN
    PRINT '??  Colonna MailServiceCode già rimossa';
END

PRINT '';

-- =====================================================
-- STEP 6: Depreca Tabella QueryProcedureLavorazioni (OPTIONAL)
-- =====================================================
PRINT 'STEP 6: Deprecazione tabella QueryProcedureLavorazioni...';

-- Commenta la riga seguente se vuoi mantenere la tabella per reference
/*
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QueryProcedureLavorazioni]'))
BEGIN
    DROP TABLE [dbo].[QueryProcedureLavorazioni];
    PRINT '? Tabella QueryProcedureLavorazioni eliminata';
    PRINT '   Backup disponibile in QueryProcedureLavorazioni_BACKUP';
END
*/

-- Per ora la tabella viene mantenuta ma segnata come DEPRECATED
PRINT '??  Tabella QueryProcedureLavorazioni MANTENUTA (deprecata)';
PRINT '   Backup disponibile in QueryProcedureLavorazioni_BACKUP';

PRINT '';

-- =====================================================
-- STEP 7: Verifica Finale
-- =====================================================
PRINT 'STEP 7: Verifica finale...';

-- Conta task con nuovo sistema
DECLARE @TaskNuovoSistema INT;
SELECT @TaskNuovoSistema = COUNT(*)
FROM TaskDaEseguire
WHERE IdConfigurazioneDatabase IS NOT NULL AND Enabled = 1;

PRINT '??  Task attivi con nuovo sistema: ' + CAST(@TaskNuovoSistema AS VARCHAR(10));

-- Verifica colonne rimosse
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TaskDaEseguire') AND name = 'IdQuery')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TaskDaEseguire') AND name = 'QueryIntegrata')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TaskDaEseguire') AND name = 'Connessione')
BEGIN
    PRINT '? Tutte le colonne legacy rimosse con successo';
END
ELSE
BEGIN
    PRINT '??  Alcune colonne legacy potrebbero essere ancora presenti';
END

PRINT '';
PRINT '=====================================================';
PRINT '? MIGRATION COMPLETATA CON SUCCESSO!';
PRINT '=====================================================';
PRINT '';
PRINT 'Riepilogo:';
PRINT '  - FK constraint rimosse';
PRINT '  - Colonne legacy rimosse: IdQuery, QueryIntegrata, Connessione';
PRINT '  - Backup query legacy: QueryProcedureLavorazioni_BACKUP';
PRINT '  - Tabella QueryProcedureLavorazioni: DEPRECATA (mantenuta)';
PRINT '';
PRINT 'Prossimi passi:';
PRINT '  1. Aggiornare codice Entity Framework';
PRINT '  2. Rimuovere attributi [Obsolete] da TaskDaEseguire.cs';
PRINT '  3. Testare applicazione';
PRINT '  4. Opzionale: DROP TABLE QueryProcedureLavorazioni (dopo 6+ mesi)';
PRINT '=====================================================';

GO
