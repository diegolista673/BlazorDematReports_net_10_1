-- ================================================================
-- Migration: Remove task flags from ConfigurazioneFaseCentro
-- Description: Removes IsTaskEnabled, EnabledTask, UltimaModificaTask
-- Date: 2025-01-10
-- ================================================================

IF COL_LENGTH('ConfigurazioneFaseCentro', 'IsTaskEnabled') IS NOT NULL
BEGIN
    ALTER TABLE ConfigurazioneFaseCentro DROP COLUMN IsTaskEnabled;
END

IF COL_LENGTH('ConfigurazioneFaseCentro', 'EnabledTask') IS NOT NULL
BEGIN
    ALTER TABLE ConfigurazioneFaseCentro DROP COLUMN EnabledTask;
END

IF COL_LENGTH('ConfigurazioneFaseCentro', 'UltimaModificaTask') IS NOT NULL
BEGIN
    ALTER TABLE ConfigurazioneFaseCentro DROP COLUMN UltimaModificaTask;
END
