-- Migration: Verifica compatibilità TipoFonte con enum
-- Data: 2025-01-13
-- Descrizione: Verifica che i valori in ConfigurazioneFontiDati.TipoFonte siano compatibili con l'enum TipoFonteData

-- STEP 1: Verifica valori esistenti
SELECT DISTINCT TipoFonte, COUNT(*) AS Conteggio
FROM ConfigurazioneFontiDati
GROUP BY TipoFonte;

-- Valori attesi:
-- "SQL" -> TipoFonteData.SQL (0)
-- "EmailCSV" -> TipoFonteData.EmailCSV (1) - OBSOLETO
-- "HandlerIntegrato" -> TipoFonteData.HandlerIntegrato (2)

-- STEP 2: Aggiorna eventuali valori legacy non conformi (se necessario)
-- UPDATE ConfigurazioneFontiDati
-- SET TipoFonte = 'SQL'
-- WHERE TipoFonte NOT IN ('SQL', 'EmailCSV', 'HandlerIntegrato');

-- STEP 3: Migrazioni EmailCSV obsolete (se presenti)
-- Le configurazioni EmailCSV legacy dovrebbero essere migrate a HandlerIntegrato
-- con handler HERA16 o ADER4

-- Verifica configurazioni EmailCSV
SELECT IdConfigurazione, CodiceConfigurazione, DescrizioneConfigurazione, MailServiceCode
FROM ConfigurazioneFontiDati
WHERE TipoFonte = 'EmailCSV';

-- NOTA: Il converter EF Core manterrà i valori stringa nel database
-- ma mapperà automaticamente all'enum in C#
-- Nessuna modifica al database è necessaria!

-- Il database continua a memorizzare: 'SQL', 'EmailCSV', 'HandlerIntegrato'
-- Il codice C# usa: TipoFonteData.SQL, TipoFonteData.EmailCSV, TipoFonteData.HandlerIntegrato
