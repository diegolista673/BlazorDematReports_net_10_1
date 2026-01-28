# Istruzioni per Generare i Diagrammi JPG

## Metodo 1: Script PowerShell Automatico

```powershell
# Eseguire dalla directory principale del progetto
.\Docs\Images\GenerateImages.ps1
```

Questo script:
1. ? Scarica automaticamente PlantUML.jar se non presente
2. ? Verifica l'installazione di Java
3. ? Converte tutti i file .puml in JPG
4. ? Salva le immagini nella stessa directory

## Metodo 2: Online PlantUML Server

1. ?? Vai su: https://www.plantuml.com/plantuml/uml/
2. ?? Copia il contenuto dei file .puml
3. ?? Incolla nell'editor online
4. ?? Scarica come JPG

### File da convertire:
- `CreazioneProduzione.puml` ? `CreazioneProduzione.jpg`
- `CreazioneMailService.puml` ? `CreazioneMailService.jpg`
- `EsecuzioneRuntime.puml` ? `EsecuzioneRuntime.jpg`
- `ArchitetturaGenerale.puml` ? `ArchitetturaGenerale.jpg`

## Metodo 3: VS Code Extension

1. ?? Installa l'estensione "PlantUML" in VS Code
2. ?? Apri i file .puml
3. ?? Usa `Ctrl+Shift+P` ? "PlantUML: Export Current Diagram"
4. ??? Seleziona formato JPG

## Metodo 4: Command Line (se hai PlantUML installato)

```bash
# Genera tutti i diagrammi
java -jar plantuml.jar -tjpg Docs/Images/*.puml

# Genera singolo diagramma
java -jar plantuml.jar -tjpg Docs/Images/CreazioneProduzione.puml
```

## Requisiti
- ? Java 8+ installato
- ?? plantuml.jar (scaricato automaticamente dallo script)

## Output Atteso
Dopo la generazione, dovresti avere:
```
Docs/Images/
??? CreazioneProduzione.puml
??? CreazioneProduzione.jpg     ? Generato
??? CreazioneMailService.puml
??? CreazioneMailService.jpg    ? Generato
??? EsecuzioneRuntime.puml
??? EsecuzioneRuntime.jpg       ? Generato
??? ArchitetturaGenerale.puml
??? ArchitetturaGenerale.jpg    ? Generato
??? GenerateImages.ps1
??? README_Generazione.md
```