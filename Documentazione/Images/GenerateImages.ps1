# Script PowerShell per generare diagrammi JPG da file PlantUML
# Richiede Java e plantuml.jar scaricato localmente

param(
    [string]$PlantUMLJarPath = ".\plantuml.jar",
    [string]$InputDirectory = ".\Docs\Images",
    [string]$OutputFormat = "jpg"
)

Write-Host "?? Generazione Diagrammi PlantUML..." -ForegroundColor Green

# Verifica se Java è installato
try {
    $javaVersion = java -version 2>&1
    Write-Host "? Java trovato: $($javaVersion[0])" -ForegroundColor Green
} catch {
    Write-Error "? Java non trovato. Installare Java per utilizzare PlantUML."
    exit 1
}

# Verifica se plantuml.jar esiste
if (-not (Test-Path $PlantUMLJarPath)) {
    Write-Host "?? Download PlantUML.jar..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://github.com/plantuml/plantuml/releases/latest/download/plantuml.jar" -OutFile $PlantUMLJarPath
}

# Trova tutti i file .puml
$pumlFiles = Get-ChildItem -Path $InputDirectory -Filter "*.puml"

if ($pumlFiles.Count -eq 0) {
    Write-Warning "?? Nessun file .puml trovato in $InputDirectory"
    exit 0
}

Write-Host "?? Trovati $($pumlFiles.Count) file PlantUML" -ForegroundColor Cyan

foreach ($file in $pumlFiles) {
    Write-Host "?? Generando: $($file.Name) -> $($file.BaseName).$OutputFormat" -ForegroundColor Blue
    
    try {
        # Genera l'immagine usando PlantUML
        $arguments = @(
            "-jar", $PlantUMLJarPath,
            "-t$OutputFormat",
            "-o", $file.Directory.FullName,
            $file.FullName
        )
        
        Start-Process -FilePath "java" -ArgumentList $arguments -Wait -NoNewWindow
        
        # Verifica se il file è stato generato
        $expectedOutput = Join-Path $file.Directory.FullName "$($file.BaseName).$OutputFormat"
        if (Test-Path $expectedOutput) {
            Write-Host "? Generato: $expectedOutput" -ForegroundColor Green
        } else {
            Write-Warning "?? Non è stato possibile generare: $expectedOutput"
        }
    }
    catch {
        Write-Error "? Errore nella generazione di $($file.Name): $_"
    }
}

Write-Host "?? Generazione completata!" -ForegroundColor Green
Write-Host "?? I diagrammi sono stati salvati in: $InputDirectory" -ForegroundColor Cyan

# Mostra i file generati
$generatedFiles = Get-ChildItem -Path $InputDirectory -Filter "*.$OutputFormat"
if ($generatedFiles.Count -gt 0) {
    Write-Host "`n?? File generati:" -ForegroundColor Yellow
    foreach ($genFile in $generatedFiles) {
        Write-Host "   ??? $($genFile.Name)" -ForegroundColor White
    }
}