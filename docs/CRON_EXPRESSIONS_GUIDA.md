# ⏰ Guida Cron Expressions per Task

## Formato Cron

```
┌───────────── minuto (0 - 59)
│ ┌───────────── ora (0 - 23)
│ │ ┌───────────── giorno del mese (1 - 31)
│ │ │ ┌───────────── mese (1 - 12)
│ │ │ │ ┌───────────── giorno della settimana (0 - 6) (Domenica=0)
│ │ │ │ │
│ │ │ │ │
* * * * *
```

---

## Preset Disponibili in UI

| Descrizione | Cron | Quando Esegue |
|-------------|------|---------------|
| **Giornaliero 05:00** | `0 5 * * *` | Ogni giorno alle 05:00 |
| **Giornaliero 02:00** | `0 2 * * *` | Ogni giorno alle 02:00 |
| **Ogni 4 ore** | `0 */4 * * *` | Alle 00:00, 04:00, 08:00, 12:00, 16:00, 20:00 |
| **Ogni ora** | `0 * * * *` | All'inizio di ogni ora (XX:00) |
| **Mensile** | `0 0 1 * *` | 1° giorno del mese alle 00:00 |

---

## Esempi Comuni

### Orari Specifici

```cron
# Alle 09:30 ogni giorno
30 9 * * *

# Alle 14:00 ogni giorno
0 14 * * *

# Alle 23:59 ogni giorno
59 23 * * *
```

### Intervalli Regolari

```cron
# Ogni 2 ore
0 */2 * * *

# Ogni 6 ore
0 */6 * * *

# Ogni 15 minuti
*/15 * * * *

# Ogni 30 minuti
*/30 * * * *
```

### Giorni Specifici

```cron
# Lunedì alle 08:00
0 8 * * 1

# Venerdì alle 17:00
0 17 * * 5

# Sabato e Domenica alle 10:00
0 10 * * 0,6

# Solo giorni feriali (Lun-Ven) alle 07:00
0 7 * * 1-5

# Solo weekend (Sab-Dom) alle 12:00
0 12 * * 0,6
```

### Date Specifiche

```cron
# 1° giorno del mese alle 06:00
0 6 1 * *

# 15° giorno del mese alle 12:00
0 12 15 * *

# Ultimo giorno del mese (non direttamente supportato)
# Usa: 1° giorno del mese successivo - 1 giorno

# 1° Gennaio alle 00:00 (Capodanno)
0 0 1 1 *

# 25 Dicembre alle 00:00 (Natale)
0 0 25 12 *
```

### Combinazioni Avanzate

```cron
# Ogni Lunedì alle 09:00 e 15:00
0 9,15 * * 1

# Dal Lunedì al Venerdì, alle 08:00, 12:00, 18:00
0 8,12,18 * * 1-5

# Ogni 3 ore, solo nei giorni feriali
0 */3 * * 1-5

# Primo Lunedì del mese alle 10:00
0 10 1-7 * 1
```

---

## Casi d'Uso Tipici

### Scansione Documenti

```cron
# Mattina presto (prima dell'orario lavorativo)
0 5 * * *

# Dopo chiusura uffici
0 18 * * *

# Più volte al giorno (mattina, pomeriggio, sera)
0 6,14,22 * * *
```

### Indicizzazione

```cron
# Dopo scansione (se scan=05:00)
0 7 * * *

# Ogni 2 ore durante orario lavorativo
0 8-18/2 * * 1-5
# Esegue: 08:00, 10:00, 12:00, 14:00, 16:00, 18:00
```

### Report Mensili

```cron
# 1° giorno del mese alle 00:00
0 0 1 * *

# Ultimo giorno lavorativo del mese (approssimazione)
0 18 28-31 * *
```

### Backup / Archiviazione

```cron
# Ogni notte alle 02:00
0 2 * * *

# Ogni Domenica alle 03:00
0 3 * * 0

# 1° Sabato del mese alle 01:00
0 1 1-7 * 6
```

---

## Validatore Cron

### Online Tools

- [Crontab.guru](https://crontab.guru) - Validatore e spiegazioni
- [CronMaker](http://www.cronmaker.com) - Generatore visuale

### Test Locale

```csharp
// In C# con Hangfire
var cron = "0 5 * * *";
var schedule = Cron.Daily(5, 0); // Equivalente
```

---

## ⚠️ Attenzioni

### Fusi Orari

> I cron sono eseguiti in **ora del server** (UTC o Local Time configurato in Hangfire).
> Verifica il fuso orario del server prima di impostare orari critici.

### Performance

| Frequenza | Raccomandazione |
|-----------|-----------------|
| Ogni minuto | ⚠️ Solo per test - evitare in produzione |
| Ogni 5-15 min | ✅ OK per monitoraggio real-time |
| Ogni ora | ✅ Ottimo per dati aggregati |
| Giornaliero | ✅ Standard per report |

### Conflitti

Evita sovrapposizioni:
```
❌ Task 1: 0 5 * * * (scansione)
❌ Task 2: 0 5 * * * (indicizzazione)
→ Possibile contesa risorse

✅ Task 1: 0 5 * * * (scansione)
✅ Task 2: 0 7 * * * (indicizzazione - 2h dopo)
```

---

## Esempi Configurazione UI

### Esempio 1: Procedura Multi-Fase

```
Configurazione: INPS_COMPLETA
├─ Mapping 1: INPS → Scansione → VR
│  └─ Schedulazione: Giornaliero 05:00
├─ Mapping 2: INPS → Indicizzazione → VR
│  └─ Schedulazione: Giornaliero 07:00
└─ Mapping 3: INPS → Verifica → VR
   └─ Schedulazione: Giornaliero 09:00
```

### Esempio 2: Centri Diversi

```
Configurazione: HERA16_NAZIONALE
├─ Mapping 1: HERA16 → Scansione → Verona
│  └─ Schedulazione: Ogni 4 ore (0 */4 * * *)
└─ Mapping 2: HERA16 → Scansione → Genova
   └─ Schedulazione: Ogni 4 ore (0 */4 * * *)
```

### Esempio 3: Personalizzato

```
Configurazione: REPORT_SETTIMANALE
└─ Mapping: Report → Aggregazione → VR
   └─ Schedulazione: Personalizzato
       Input cron custom: 0 8 * * 1
       → Ogni Lunedì alle 08:00
```

---

## Troubleshooting

### Cron Non Esegue

1. **Verifica formato**: Usa [Crontab.guru](https://crontab.guru)
2. **Controlla fuso orario**: Verifica ora server Hangfire
3. **Abilita task**: Assicurati `Enabled=true`
4. **Controlla log**: NLog mostra errori scheduling

### Esecuzione Doppia

Se il task viene eseguito 2 volte:
- Verifica che non ci siano 2 task con stessa configurazione
- Controlla `IdTaskHangFire` univoco

---

**Prossimo Step**: Configura i cron per i tuoi mapping e monitora esecuzioni in `/dashboard-task`
