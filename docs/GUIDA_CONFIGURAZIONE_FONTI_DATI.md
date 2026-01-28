# ?? Guida Configurazione Fonti Dati

## Manuale Operatore per la Creazione di Configurazioni

**Versione**: 1.0  
**Ultima Modifica**: 2024  
**Percorso UI**: `/admin/fonti-dati`

---

## ?? Indice

1. [Introduzione](#-introduzione)
2. [Accesso alla Funzionalità](#-accesso-alla-funzionalità)
3. [Tipi di Fonte Dati](#-tipi-di-fonte-dati)
4. [Esempi Completi per Tipo](#-esempi-completi-per-tipo)
   - [SQL Query](#1-sql-query)
   - [Email CSV](#2-email-csv)
   - [Handler C# Integrato](#3-handler-c-integrato)
   - [Pipeline Multi-Step](#4-pipeline-multi-step)
5. [Mapping Fasi e Centri](#-mapping-fasi-e-centri)
6. [Parametri JSON Avanzati](#-parametri-json-avanzati)
7. [Best Practices](#-best-practices)
8. [Troubleshooting](#-troubleshooting)

---

## ?? Introduzione

Il Sistema Unificato Configurazione Fonti Dati permette di definire **come e da dove** recuperare i dati di produzione per ogni procedura/fase/centro.

### Vantaggi

| Prima | Dopo |
|-------|------|
| ? Ogni fonte richiedeva codice C# | ? Configurazione da UI |
| ? Deploy per ogni modifica | ? Modifica immediata |
| ? Solo sviluppatori | ? Operatori autorizzati |
| ? Query sparse in più file | ? Tutto centralizzato in DB |

---

## ?? Accesso alla Funzionalità

1. **Login** con ruolo `ADMIN` o `SUPERVISOR`
2. **Menu laterale** ? Settings ? **?? Fonti Dati**
3. Oppure naviga direttamente a: `/admin/fonti-dati`

### Schermata Principale

```
???????????????????????????????????????????????????????????????
?  ?? Configurazioni Fonti Dati          [+ Nuova Config]     ?
???????????????????????????????????????????????????????????????
?  Codice          ? Nome          ? Tipo   ? Fasi ? Stato    ?
?  INPS_SCAN_VR    ? INPS Scan VR  ? SQL    ? 2    ? Attiva   ?
?  HERA16_MAIL     ? HERA16 Email  ? Email  ? 3    ? Attiva   ?
?  Z0072370_28AUT  ? RDMKT 28AUT   ? Handler? 2    ? Attiva   ?
???????????????????????????????????????????????????????????????
```

---

## ?? Tipi di Fonte Dati

| Tipo | Icona | Quando Usarlo |
|------|-------|---------------|
| **SQL** | ?? | Query diretta a database esterni (Captiva, HERA, etc.) |
| **EmailCSV** | ?? | Dati ricevuti via email con allegati CSV |
| **HandlerIntegrato** | ?? | Logica complessa che richiede codice C# |
| **Pipeline** | ?? | Elaborazioni multi-step con trasformazioni |

---

## ?? Esempi Completi per Tipo

### 1. SQL Query

#### Scenario
Recuperare dati di scansione dalla tabella Captiva per la procedura INPS Verona.

#### Configurazione

| Campo | Valore |
|-------|--------|
| **Codice** | `INPS_SCANSIONE_VR` |
| **Nome** | `INPS Scansione Verona` |
| **Tipo** | SQL |
| **Connection String** | `CnxnCaptiva206` |

#### Query SQL

```sql
SELECT 
    OP_SCAN as operatore,
    CONVERT(date, DATA_SCAN) as DataLavorazione,
    COUNT(*) as Documenti,
    SUM(CONVERT(int, NUM_PAG)) / 2 AS Fogli,
    SUM(CONVERT(int, NUM_PAG)) as Pagine
FROM Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
WHERE CONVERT(date, DATA_SCAN) >= @startData 
  AND CONVERT(date, DATA_SCAN) <= @endData 
  AND department = 'VERONA'
GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)
```

#### ?? Regole Query SQL

1. **Parametri obbligatori**: Usa sempre `@startData` e `@endData`
2. **Colonne richieste**: La query DEVE restituire queste colonne:
   - `operatore` (VARCHAR) - Nome operatore
   - `DataLavorazione` (DATE) - Data della lavorazione
   - `Documenti` (INT) - Numero documenti
   - `Fogli` (INT) - Numero fogli
   - `Pagine` (INT) - Numero pagine

3. **Alias obbligatori**: Se le colonne hanno nomi diversi, usa `AS`:
   ```sql
   SELECT OP_SCAN AS operatore, ...
   ```

#### Mapping Fasi

| Procedura | Fase | Centro | Parametri Extra |
|-----------|------|--------|-----------------|
| INPS Verona (ID: 5) | Scansione (ID: 4) | Verona (ID: 1) | `{"department": "VERONA"}` |
| INPS Verona (ID: 5) | Indicizzazione (ID: 5) | Verona (ID: 1) | `{"department": "VERONA"}` |

---

### 2. Email CSV

#### Scenario
Recuperare dati di produzione HERA16 da email con allegati CSV.

#### Configurazione

| Campo | Valore |
|-------|--------|
| **Codice** | `HERA16_MAIL_VR` |
| **Nome** | `HERA16 Email Verona` |
| **Tipo** | EmailCSV |
| **Mail Service Code** | `HERA16` |

#### Mapping Fasi

| Procedura | Fase | Centro |
|-----------|------|--------|
| HERA16 (ID: 10) | Scansione (ID: 4) | Verona (ID: 1) |
| HERA16 (ID: 10) | Classificazione (ID: 5) | Verona (ID: 1) |
| HERA16 (ID: 10) | Indicizzazione (ID: 6) | Verona (ID: 1) |

#### ?? Note Mail Service

- Il servizio legge automaticamente le email con subject configurato
- Gli allegati CSV vengono processati secondo il pattern definito
- La configurazione del server email è in `appsettings.json` sezione `MailServices`

#### Mail Services Disponibili

| Codice | Descrizione | Subject Email |
|--------|-------------|---------------|
| `HERA16` | Produzione HERA16 | `DEMAT_HERA16` |
| `ADER4` | ADER Equitalia 4 | `DEMAT_EQTMN4@RMHRPRD0 - Report...` |

---

### 3. Handler C# Integrato

#### Scenario
Usare un handler C# esistente per lavorazioni con logica complessa (es: più query, calcoli, API esterne).

#### Configurazione

| Campo | Valore |
|-------|--------|
| **Codice** | `Z0072370_28AUT_CUSTOM` |
| **Nome** | `RDMKT 28AUT Custom Handler` |
| **Tipo** | HandlerIntegrato |
| **Handler Class Name** | `Z0072370_28AUTHandler` |

#### Handler Disponibili

| Classe Handler | Descrizione |
|----------------|-------------|
| `Z0072370_28AUTHandler` | RDMKT 28AUT Genova - Scansione e Indicizzazione |
| `Z0082041_SOFTLINEHandler` | Softline elaborazioni |
| `ANT_ADER4_SORTER_1_2Handler` | ADER4 Sorter fasi 1-2 |
| `PRATICHE_SUCCESSIONEHandler` | Pratiche successione |
| `RDMKT_RSPHandler` | RDMKT RSP |
| `DefaultLavorazioneHandler` | Handler generico per query DB |

#### Mapping Fasi

| Procedura | Fase | Centro |
|-----------|------|--------|
| RDMKT 28AUT (ID: 8) | Scansione (ID: 4) | Genova (ID: 2) |
| RDMKT 28AUT (ID: 8) | Indicizzazione (ID: 5) | Genova (ID: 2) |

#### ?? Quando Usare Handler C#

Usa un handler C# quando:
- ? La logica richiede **più query** correlate
- ? Servono **calcoli complessi** sui dati
- ? Devi chiamare **API esterne**
- ? La query ha **condizioni dinamiche** basate sulla fase
- ? Serve **trasformazione dati** non esprimibile in SQL

**Non usare** per query SQL semplici ? usa tipo `SQL` direttamente!

---

### 4. Pipeline Multi-Step

#### Scenario
Elaborazione complessa con più passaggi: query iniziale, filtro, aggregazione.

#### Configurazione

| Campo | Valore |
|-------|--------|
| **Codice** | `PIPELINE_ESEMPIO` |
| **Nome** | `Pipeline Esempio Multi-Step` |
| **Tipo** | Pipeline |
| **Connection String** | `CnxnCaptiva206` |

#### ?? Stato Pipeline

> **NOTA**: La funzionalità Pipeline è attualmente in sviluppo.
> I tipi di step supportati saranno:
> - `Query` - Esecuzione query SQL
> - `Filter` - Filtro dati
> - `Transform` - Trasformazione colonne
> - `Aggregate` - Aggregazione (GROUP BY)
> - `Merge` - Unione dati da più fonti

#### Esempio Configurazione Step (JSON)

```json
// Step 1: Query
{
  "Query": "SELECT * FROM Tabella WHERE data >= @startData"
}

// Step 2: Filter
{
  "Field": "stato",
  "Operator": "equals",
  "Value": "completato"
}

// Step 3: Aggregate
{
  "GroupBy": ["operatore", "data"],
  "Aggregations": [
    { "Field": "documenti", "Function": "SUM" }
  ]
}
```

---

## ?? Mapping Fasi e Centri

### Struttura Mapping

Ogni configurazione può avere **N mapping** che associano:
- **Procedura** (es: INPS, HERA16, ADER4)
- **Fase** (es: Scansione, Indicizzazione, Classificazione)
- **Centro** (es: Verona, Genova)

### Esempio Visuale

```
Configurazione: INPS_SCANSIONE
??? Mapping 1: INPS Verona ? Scansione ? Verona
??? Mapping 2: INPS Verona ? Scansione ? Genova
??? Mapping 3: INPS Genova ? Scansione ? Genova
```

### Campi Mapping

| Campo | Descrizione | Obbligatorio |
|-------|-------------|--------------|
| **Procedura** | Seleziona dalla lista procedure attive | ? |
| **Fase** | Fase lavorazione (4=Scan, 5=Index, etc.) | ? |
| **Centro** | Centro produzione | ? |
| **Parametri JSON** | Parametri extra per query/handler | ? |

---

## ?? Parametri JSON Avanzati

### Quando Usarli

I parametri JSON permettono di **personalizzare** la query o l'handler per ogni combinazione fase/centro.

### Formato

```json
{
  "chiave1": "valore1",
  "chiave2": "valore2"
}
```

### Esempi Pratici

#### Esempio 1: Filtro Department

Per filtrare per sede nella query SQL:

```json
{"department": "GENOVA"}
```

La query deve contenere:
```sql
WHERE department = @department
```

#### Esempio 2: Filtro Tipo Documento

```json
{"tipoDoc": "FATTURA", "stato": "COMPLETATO"}
```

#### Esempio 3: Mapping Colonne Custom

Se la tabella sorgente ha nomi colonne diversi:

```json
{
  "Operatore": "USER_NAME",
  "DataLavorazione": "WORK_DATE",
  "Documenti": "DOC_COUNT",
  "Fogli": "SHEET_COUNT",
  "Pagine": "PAGE_COUNT"
}
```

---

## ? Best Practices

### Naming Convention

| Elemento | Formato | Esempio |
|----------|---------|---------|
| Codice | `PROCEDURA_TIPO_CENTRO` | `INPS_SCAN_VR` |
| Nome | Descrittivo italiano | `INPS Scansione Verona` |

### Query SQL

1. **Sempre testare** la query prima di salvare (pulsante "Test Query")
2. **Usare alias** per le colonne
3. **Includere filtro date** con `@startData` e `@endData`
4. **Limitare i dati** con WHERE appropriati

### Performance

- ? Aggiungere **indici** sulle colonne filtrate (DATA_SCAN, department)
- ? Usare **TOP** durante i test
- ? Evitare **SELECT *** - selezionare solo colonne necessarie

### Manutenzione

- ? **Descrizione** sempre compilata
- ? **Disattivare** (non eliminare) configurazioni obsolete
- ? **Duplicare** configurazioni simili anziché ricreare da zero

---

## ?? Troubleshooting

### Problema: Query restituisce 0 record

**Causa possibile**: Filtro date non corretto

**Soluzione**:
```sql
-- Verificare formato date
WHERE CONVERT(date, DATA_SCAN) >= @startData

-- NON usare stringhe dirette
-- ? WHERE DATA_SCAN >= '20240101'
-- ? WHERE CONVERT(date, DATA_SCAN) >= @startData
```

### Problema: Errore "ConnectionString non trovata"

**Causa**: Il nome ConnectionString non esiste in appsettings.json

**Soluzione**:
1. Verificare in appsettings.json sezione `ConnectionStrings`
2. Usare esattamente lo stesso nome (case-sensitive)

```json
"ConnectionStrings": {
  "CnxnCaptiva206": "Server=...",  // ? Usa questo nome
}
```

### Problema: Handler non trovato

**Causa**: Nome classe handler errato

**Soluzione**:
1. Verificare che l'handler esista in `ClassLibraryLavorazioni/Lavorazioni/Handlers/`
2. Il nome deve essere **esatto** (case-sensitive)
3. L'handler deve implementare `ILavorazioneHandler`

### Problema: Mapping non funziona

**Causa**: JSON ParametriExtra malformato

**Soluzione**:
```json
// ? Corretto
{"department": "GENOVA"}

// ? Errato - virgolette sbagliate
{'department': 'GENOVA'}

// ? Errato - manca virgolette
{department: "GENOVA"}
```

### Problema: Task non usa nuova configurazione

**Causa**: Task esistente usa sistema legacy

**Soluzione**:
1. Verificare che il task abbia `IdConfigurazioneDatabase` impostato
2. Se NULL, il task usa ancora il sistema legacy
3. Ricreare il task associandolo alla nuova configurazione

---

## ?? Riepilogo Flusso Operativo

```
1. CREA CONFIGURAZIONE
   ??? Scegli Tipo (SQL/Email/Handler/Pipeline)
   ??? Compila Dettagli (Codice, Nome)
   ??? Configura Specifica (Query/MailService/Handler)
   ??? Aggiungi Mapping Fasi/Centri

2. TESTA
   ??? Per SQL: usa "Test Query"
   ??? Per altri: verifica log dopo esecuzione

3. ASSOCIA A TASK
   ??? Il task Hangfire legge la configurazione
   ??? Esegue secondo scheduling

4. MONITORA
   ??? Dashboard Task mostra stato
   ??? Log dettagliati in NLog
```

---

## ?? Riferimenti

- [Documentazione Tecnica Implementazione](./IMPLEMENTATION_UNIFIED_DATASOURCE_SYSTEM.md)
- [Architettura Sistema](./ARCHITECTURE.md)

---

**Autore**: Team Sviluppo  
**Contatto**: Per supporto, contattare il responsabile IT
