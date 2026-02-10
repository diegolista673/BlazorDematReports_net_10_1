# Query Parameter Names - Best Practices

## ?? Standard dei Parametri SQL

Il sistema `QueryService` fornisce **SOLO** i seguenti parametri per le query SQL:

| Parametro | Tipo | Descrizione |
|-----------|------|-------------|
| `@startDate` | `DateTime2` | Data di inizio del range di filtraggio |
| `@endDate` | `DateTime2` | Data di fine del range di filtraggio |

> ?? **IMPORTANTE**: I nomi dei parametri sono **case-insensitive** in SQL, ma devono corrispondere esattamente a `@startDate` e `@endDate`.

---

## ? Parametri NON Supportati

I seguenti nomi di parametri **NON** sono supportati e causeranno errori SQL:
- `@startDataDe` / `@endDataDe` (legacy, deprecato)
- `@startData` / `@endData` (non standard)
- Qualsiasi altra variante

---

## ? Esempio Query Corretta

```sql
SELECT
    OP_INDEX AS Operatore,
    CAST(DATA_INDEX AS DATE) AS DataLavorazione,
    COUNT(*) AS Documenti,
    SUM(CAST(NUM_PAG AS INT)) / 2 AS Fogli,
    SUM(CAST(NUM_PAG AS INT)) AS Pagine
FROM Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
WHERE DATA_INDEX >= @startDate
  AND DATA_INDEX < DATEADD(DAY, 1, @endDate)
GROUP BY OP_INDEX, CAST(DATA_INDEX AS DATE)
```

---

## ?? Migrazione da Query Legacy

Se hai query esistenti che usano `@startDataDe` / `@endDataDe`:

1. **Automatico**: Esegui lo script SQL:
   ```
   Database\Migrations\Fix_QueryParameterNames.sql
   ```

2. **Manuale**: Trova e sostituisci:
   - `@startDataDe` ? `@startDate`
   - `@endDataDe` ? `@endDate`

---

## ??? Validazione

Il `SqlValidationService` ora verifica automaticamente:
- ? Presenza dei parametri `@startDate` e `@endDate`
- ? Blocca query con nomi di parametri errati
- ?? Fornisce messaggi di errore descrittivi

**Esempio di errore**:
```
Nomi di parametri errati.
Il sistema fornisce SOLO @startDate e @endDate (case-insensitive).
NON utilizzare @startDataDe, @endDataDe, @startData o @endData.
```

---

## ?? Riferimenti

- **Servizio**: `DataReading\Services\QueryService.cs`
- **Validazione**: `BlazorDematReports\Services\Validation\SqlValidationService.cs`
- **Template UI**: `BlazorDematReports\Components\Pages\Impostazioni\ConfigurazioneFonti\Steps\Step4_Mapping.razor`

---

## ?? History

- **2025**: Standardizzazione nomi parametri a `@startDate` / `@endDate`
- **Legacy**: Vecchie implementazioni usavano `@startDataDe` / `@endDataDe` (deprecato)
