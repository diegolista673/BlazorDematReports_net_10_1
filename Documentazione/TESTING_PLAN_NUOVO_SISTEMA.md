# 🧪 Testing Plan - Nuovo Sistema Configurazioni

**Versione:** 1.0  
**Data:** 2024  
**Prerequisiti:** Migrazione STEP 1-5 completati ✅

---

## 📋 Checklist Testing Completa

### FASE 1: Test Configurazioni SQL ✅

#### Test 1.1: Creazione Configurazione SQL Base

**Obiettivo:** Verificare wizard per tipo SQL

**Steps:**
1. Vai a `/admin/fonti-dati`
2. Clicca "Nuova Configurazione"
3. Compila:
   ```
   Tipo Fonte: SQL
   Codice: TEST_SQL_001
   Nome: Test Configurazione SQL Base
   Descrizione: Query di test per verifica sistema
   
   Connection String: CnxnCaptiva206 (o altra esistente)
   
   Query SQL:
   SELECT 
       'TEST_OPERATOR' as operatore,
       CONVERT(date, GETDATE()) as DataLavorazione,
       100 as Documenti,
       50 as Fogli,
       100 as Pagine
   ```
4. Aggiungi mapping:
   ```
   Procedura: (seleziona procedura di test)
   Fase: Scansione
   Centro: Genova
   Schedulazione: Giornaliero 05:00
   Parametri JSON: {"test": "sql_base"}
   ```
5. Salva

**Risultato Atteso:**
- Configurazione salvata
- Visibile in dashboard `/admin/fonti-dati`
- Pulsante "Crea Task" abilitato
- Mapping visibile in detail row

---

#### Test 1.2: Mapping Multipli Stessa Configurazione

**Obiettivo:** Verificare N mapping per stessa config

**Steps:**
1. Modifica config `TEST_SQL_001`
2. Aggiungi 3 mapping:
   ```
   Mapping 1:
   - Procedura: (stessa)
   - Fase: Scansione
   - Centro: Genova
   - Cron: 0 5 * * *
   
   Mapping 2:
   - Procedura: (stessa)
   - Fase: Indicizzazione
   - Centro: Genova
   - Cron: 0 6 * * *
   
   Mapping 3:
   - Procedura: (stessa)
   - Fase: Scansione
   - Centro: Verona
   - Cron: 0 5 * * *
   ```
3. Salva

**Risultato Atteso:**
- 3 mapping salvati
- Dashboard mostra "N. Mapping: 3"
- Detail row mostra 3 righe con cron diversi
- Colonna "Schedulazioni" mostra 2 chip: "Giornaliero 05:00", "Giornaliero 06:00"

---

#### Test 1.3: Cron Personalizzati

**Obiettivo:** Verificare salvataggio cron custom

**Steps:**
1. Modifica config `TEST_SQL_001`
2. Per mapping 1, seleziona "Personalizzato"
3. Inserisci cron custom: `0 */2 * * *` (ogni 2 ore)
4. Salva

**Risultato Atteso:**
- Cron salvato in `ParametriExtra` JSON
- Dashboard mostra "Ogni 2 ore" (o cron raw se non riconosciuto)
- Detail row mostra cron custom

---

### FASE 2: Test Creazione Task Automatici ✅

#### Test 2.1: Creazione Task da Dashboard Admin

**Obiettivo:** Generare task da configurazione

**Steps:**
1. Vai a `/admin/fonti-dati`
2. Trova config `TEST_SQL_001` (con 3 mapping)
3. Clicca icona ▶️ "Crea Task"
4. Attendi snackbar

**Risultato Atteso:**
- Snackbar: "3 task creati e schedulati con successo!"
- Dashboard aggiorna: "Task Attivi: 3"
- Pulsante ▶️ diventa disabilitato
- Tooltip: "Task già creati per questa configurazione"

---

#### Test 2.2: Verifica Task in Dashboard Task

**Obiettivo:** Verificare task generati correttamente

**Steps:**
1. Vai a `/dashboard-task`
2. Filtra per configurazione o procedura
3. Verifica presenza 3 task

**Risultato Atteso:**
```
Task 1:
- Procedura: (test)
- Fase: Scansione
- Centro: Genova
- Cron: 0 5 * * *
- Enabled: true
- Stato: CONFIGURED
- IdConfigurazioneDatabase: (id config SQL)

Task 2:
- Procedura: (test)
- Fase: Indicizzazione  
- Centro: Genova
- Cron: 0 6 * * *
- ...

Task 3:
- Procedura: (test)
- Fase: Scansione
- Centro: Verona
- Cron: 0 5 * * *
- ...
```

---

#### Test 2.3: Verifica Hangfire Schedule

**Obiettivo:** Task schedulati in Hangfire

**Steps:**
1. Apri Hangfire Dashboard (`/hangfire`)
2. Vai a "Recurring Jobs"
3. Cerca job creati

**Risultato Atteso:**
- 3 recurring jobs visibili
- Cron expression corretto (0 5 * * *, 0 6 * * *, etc.)
- Next execution time calcolato correttamente
- Job enabled

---

### FASE 3: Test Configurazioni EmailCSV ✅

#### Test 3.1: Configurazione Mail Service

**Obiettivo:** Creare config tipo EmailCSV

**Steps:**
1. Vai a `/admin/fonti-dati`
2. Nuova configurazione:
   ```
   Tipo: EmailCSV
   Codice: TEST_EMAIL_001
   Nome: Test Servizio Mail HERA
   Mail Service: HERA16 (o altro esistente)
   
   Mapping:
   - Procedura: (procedura mail)
   - Fase: Scansione
   - Centro: Genova
   - Cron: 0 7 * * * (ogni giorno alle 7)
   ```
3. Salva
4. Crea task

**Risultato Atteso:**
- Config salvata con `MailServiceCode = "HERA16"`
- Task creato con riferimento a configurazione
- `UnifiedDataSourceHandler` router verso handler mail

---

### FASE 4: Test Configurazioni HandlerIntegrato ✅

#### Test 4.1: Handler Legacy Disponibili

**Obiettivo:** Verificare handler legacy nel dropdown

**Steps:**
1. Vai a `/admin/configura-fonte-dati`
2. Seleziona "Tipo: HandlerIntegrato"
3. Clicca dropdown "Handler C#"

**Risultato Atteso:**
- Dropdown mostra handler:
  ```
  - Z0072370_28AUTHandler
  - Z0082041_SOFTLINEHandler
  - ANT_ADER4_SORTER_1_2Handler
  - PRATICHE_SUCCESSIONEHandler
  - DefaultLavorazioneHandler
  - ... (altri handler custom)
  ```

---

#### Test 4.2: Configurazione con Handler Legacy

**Obiettivo:** Usare handler legacy nel nuovo sistema

**Steps:**
1. Nuova configurazione:
   ```
   Tipo: HandlerIntegrato
   Codice: TEST_HANDLER_001
   Nome: Test Handler Legacy Z0072370
   Handler: Z0072370_28AUTHandler
   
   Mapping:
   - Procedura: Z0072370_28AUT
   - Fase: Scansione
   - Centro: Genova
   - Cron: 0 8 * * *
   ```
2. Salva
3. Crea task
4. Attendi esecuzione schedulata o trigger manuale

**Risultato Atteso:**
- Config salvata con `HandlerClassName = "Z0072370_28AUTHandler"`
- Task creato
- Esecuzione instrada a handler legacy corretto
- Dati elaborati correttamente (verifica in Produzione)

---

### FASE 5: Test Widget Edit Procedura ✅

#### Test 5.1: Visualizzazione Configurazioni

**Obiettivo:** Widget mostra config associate

**Steps:**
1. Vai a `/procedure-lavorazioni/edit/{id}` (procedura con config)
2. Espandi "⚙️ Configurazioni Fonti Dati"

**Risultato Atteso:**
- Widget mostra badge con conteggio: "(3)"
- Card per ogni configurazione con:
  - Chip tipo (SQL/Email/Handler)
  - Nome configurazione
  - Descrizione
  - N. mapping
  - N. task attivi
  - Pulsante "Crea Task" (se 0 task)
  - Icona modifica (link a wizard)

---

#### Test 5.2: Creazione Task da Widget

**Obiettivo:** Creare task direttamente da procedura

**Steps:**
1. Da Edit Procedura, widget configurazioni
2. Trova config senza task
3. Clicca "Crea Task"

**Risultato Atteso:**
- Snackbar: "X task creati..."
- Widget si aggiorna: pulsante diventa "Task già creati (X)"
- Badge contatore si aggiorna

---

#### Test 5.3: Pre-compilazione da Procedura

**Obiettivo:** Link "Nuova Config" pre-compila procedura

**Steps:**
1. Da Edit Procedura (es: id=5)
2. Widget configurazioni → Clicca "Nuova Configurazione"
3. Verifica URL: `/admin/configura-fonte-dati?idProcedura=5`
4. Verifica wizard

**Risultato Atteso:**
- Wizard si apre
- Mapping pre-compilato con procedura 5
- Snackbar info: "📍 Mapping pre-compilato..."

---

### FASE 6: Test Validazioni ✅

#### Test 6.1: Validazione Campi Obbligatori

**Obiettivo:** Sistema impedisce salvataggio incompleto

**Steps:**
1. Nuova configurazione
2. Lascia campi vuoti
3. Tenta salvataggio

**Risultato Atteso:**
-  Snackbar warning: "Compila i campi obbligatori"
-  Configurazione NON salvata

---

#### Test 6.2: Validazione Mapping Duplicati

**Obiettivo:** Impedire duplicati Proc+Fase+Centro

**Steps:**
1. Nuova configurazione
2. Aggiungi 2 mapping identici:
   ```
   Mapping 1: Proc A, Fase X, Centro Y
   Mapping 2: Proc A, Fase X, Centro Y (duplicato!)
   ```
3. Salva

**Risultato Atteso:**
-  Warning: "Mapping duplicati rilevati..."
-  Salvataggio bloccato

---

#### Test 6.3: Protezione Eliminazione con Task Attivi

**Obiettivo:** Impedire eliminazione se ci sono task

**Steps:**
1. Dashboard `/admin/fonti-dati`
2. Config con task attivi (es: Task Attivi: 3)
3. Clicca icona 🗑️ Delete

**Risultato Atteso:**
-  Pulsante DISABILITATO (grigio)
- ℹ️ Tooltip: "Impossibile eliminare: 3 task attivi associati..."

---

#### Test 6.4: Soft Delete Configurazione

**Obiettivo:** Eliminazione logica, non fisica

**Steps:**
1. Config senza task attivi
2. Clicca Delete
3. Dialog conferma → "Disattiva"

**Risultato Atteso:**
- Dialog: "Sei sicuro di disattivare..."
- Dopo conferma: `FlagAttiva = false` nel DB
- Config sparisce dalla dashboard
- Task associati NON eliminati (solo configurazione disattivata)
- Dati in DB mantenuti (soft delete)

---

### FASE 7: Test Duplicazione ✅

#### Test 7.1: Duplicazione Configurazione

**Obiettivo:** Copia completa config

**Steps:**
1. Dashboard `/admin/fonti-dati`
2. Config esistente → Clicca icona 📋 Duplica
3. Verifica nuova config

**Risultato Atteso:**
```
Config Originale:
- Codice: TEST_SQL_001
- Nome: Test Config SQL

Config Duplicata:
- Codice: TEST_SQL_001_COPIA_20241201120000
- Nome: Test Config SQL (Copia)
- TipoFonte: (stesso)
- Query: (stessa)
- Mapping: (stessi - copiati)
- Cron: (stessi - preservati in JSON)
- Task: 0 (nuova config, nessun task)
```

---

### FASE 8: Test Esecuzione Real-World ✅

#### Test 8.1: Esecuzione Task SQL

**Obiettivo:** Verificare esecuzione end-to-end

**Steps:**
1. Config SQL di test con query reale
2. Crea task
3. Attendi scheduled execution o trigger manuale
4. Verifica log NLog
5. Verifica dati in tabella Produzione

**Risultato Atteso:**
- Task eseguito senza errori
- Log NLog mostra:
  ```
  [INFO] Executing task {TaskId} with configuration {ConfigId}
  [INFO] SQL query executed, rows returned: X
  [INFO] Data processed successfully
  ```
- Dati inseriti in `Produzione` con:
  - Operatore
  - DataLavorazione
  - Documenti, Fogli, Pagine
- Stato task → "COMPLETED"

---

#### Test 8.2: Esecuzione Task Handler Legacy

**Obiettivo:** Handler legacy funziona tramite nuovo sistema

**Steps:**
1. Config HandlerIntegrato con `Z0072370_28AUTHandler`
2. Crea task
3. Trigger esecuzione
4. Verifica routing

**Risultato Atteso:**
- `UnifiedDataSourceHandler` identifica tipo `HandlerIntegrato`
- Istanzia `Z0072370_28AUTHandler`
- Passa `LavorazioneExecutionContext`
- Handler esegue logica legacy
- Dati elaborati come prima

---

#### Test 8.3: Gestione Errori

**Obiettivo:** Sistema gestisce errori gracefully

**Steps:**
1. Crea config SQL con query SBAGLIATA (es: tabella inesistente)
2. Crea task
3. Trigger esecuzione
4. Verifica stato

**Risultato Atteso:**
-  Esecuzione fallisce
- Stato task → "ERROR"
- `LastError` campo popolato con messaggio
- Log NLog mostra exception
- Hangfire mostra job failed
- Task rimane schedulato (retry prossima esecuzione)

---

### FASE 9: Test Performance ✅

#### Test 9.1: Caricamento Dashboard

**Obiettivo:** Dashboard responsive con molte config

**Steps:**
1. Crea 20+ configurazioni
2. Apri `/admin/fonti-dati`
3. Misura tempo caricamento

**Risultato Atteso:**
- Caricamento < 2 secondi
- Paginazione funzionante
- Filtri reattivi
- Sorting funzionante
- Detail row lazy-load

---

#### Test 9.2: Creazione Task Massiva

**Obiettivo:** Creazione 50+ task simultanei

**Steps:**
1. Config con 50 mapping (proc/fasi/centri diverse)
2. Clicca "Crea Task"
3. Monitora tempo

**Risultato Atteso:**
- Tutti i task creati
- Tempo < 10 secondi
- Nessun deadlock DB
- Hangfire schedule OK

---

## 📊 Riepilogo Testing

### Coverage

| Area | Test | Passed | Failed | Pending |
|------|------|--------|--------|---------|
| Configurazione SQL | 3 | ⏳ | ⏳ | ⏳ |
| Creazione Task | 3 | ⏳ | ⏳ | ⏳ |
| EmailCSV | 1 | ⏳ | ⏳ | ⏳ |
| HandlerIntegrato | 2 | ⏳ | ⏳ | ⏳ |
| Widget Procedura | 3 | ⏳ | ⏳ | ⏳ |
| Validazioni | 4 | ⏳ | ⏳ | ⏳ |
| Duplicazione | 1 | ⏳ | ⏳ | ⏳ |
| Esecuzione | 3 | ⏳ | ⏳ | ⏳ |
| Performance | 2 | ⏳ | ⏳ | ⏳ |
| **TOTALE** | **22** | **0** | **0** | **22** |

---

## Criteri Accettazione

Prima di deploy produzione:

- [ ] Almeno 18/22 test PASS (80%+)
- [ ] Nessun test critico FAILED
- [ ] Log NLog puliti (no ERROR)
- [ ] Hangfire dashboard senza job falliti
- [ ] Performance accettabile (dashboard < 3s)
- [ ] Nessun deadlock/timeout DB
- [ ] Backup database eseguito
- [ ] Documentazione utente condivisa

---

**Ultimo Aggiornamento:** 2024  
**Tester:** Team Sviluppo  
**Ambiente:** Dev/Test → Staging → Produzione
