# Cronologia Migrazioni Manuali

Questa soluzione utilizza una migrazione manuale per la rimozione della colonna legacy `Hour` dalla tabella `TaskDaEseguire`.

## 2025-09-15 RemoveHourFromTaskDaEseguire
Motivazioni:
- Colonna ridondante: l'orario è rappresentato da `TimeTask` (minuti+ore) e/o `CronExpression`.
- Prevenzione inconsistenze: doppia fonte di verità poteva generare disallineamenti.
- Semplificazione mapping e codice scheduler.

Script Up:
```
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'Hour' AND Object_ID = OBJECT_ID('dbo.TaskDaEseguire'))
BEGIN
    ALTER TABLE dbo.TaskDaEseguire DROP COLUMN [Hour];
END
```

Script Down:
```
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'Hour' AND Object_ID = OBJECT_ID('dbo.TaskDaEseguire'))
BEGIN
    ALTER TABLE dbo.TaskDaEseguire ADD [Hour] int NULL;
END
```

Esecuzione:
1. Backup DB (raccomandato).
2. Applicare migrazione (dotnet ef database update) se integrate le migrazioni EF.
3. Verificare assenza colonna `Hour`.

Note: La migrazione è idempotente (usa IF EXISTS / IF NOT EXISTS) per ambienti già puliti.
