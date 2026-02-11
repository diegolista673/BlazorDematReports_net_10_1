-- ================================================================
-- Migration: Remove FlagAttiva from ConfigurazioneFontiDati
-- Description: Removes FlagAttiva column (state handled by TaskDaEseguire)
-- Date: 2025-01-10
-- ================================================================

IF COL_LENGTH('ConfigurazioneFontiDati', 'FlagAttiva') IS NOT NULL
BEGIN
    ALTER TABLE ConfigurazioneFontiDati DROP COLUMN FlagAttiva;
END
