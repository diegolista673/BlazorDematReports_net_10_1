# Script di fix automatico post-scaffolding per ConfigurazioneFontiDati.cs
# Questo script ripristina le customizzazioni manuali che vengono sovrascritte durante lo scaffolding

$ErrorActionPreference = "Stop"

$filePath = Join-Path $PSScriptRoot "ConfigurazioneFontiDati.cs"

if (-not (Test-Path $filePath)) {
    Write-Error "File non trovato: $filePath"
    exit 1
}

Write-Host "🔧 Fix scaffolding per ConfigurazioneFontiDati.cs..." -ForegroundColor Cyan

# Leggi il contenuto del file
$content = Get-Content $filePath -Raw

# Backup del file originale
$backupPath = "$filePath.pre-fix"
Copy-Item $filePath $backupPath -Force
Write-Host "✅ Backup creato: $backupPath" -ForegroundColor Green

# 1. Verifica e aggiungi using Entities.Enums se mancante
if ($content -notmatch "using Entities\.Enums;") {
    Write-Host "⚙️  Aggiunta using Entities.Enums..." -ForegroundColor Yellow
    $content = $content -replace "(using System\.Collections\.Generic;)", "`$1`nusing Entities.Enums;"
}

# 2. Sostituisci la proprietà TipoFonte da string a TipoFonteData
$oldProperty = "public string TipoFonte \{ get; set; \} = null!;"
$newProperty = @"
// IMPORTANTE: Non rigenerare questa proprietà con scaffold - TipoFonte è un enum (TipoFonteData), non string
    // Il converter TipoFonteDataConverter gestisce la conversione enum ↔ string per il database
    public TipoFonteData TipoFonte { get; set; }
"@

if ($content -match $oldProperty) {
    Write-Host "⚙️  Conversione TipoFonte da string a TipoFonteData..." -ForegroundColor Yellow
    $content = $content -replace $oldProperty, $newProperty
} else {
    Write-Host "ℹ️  Proprietà TipoFonte già corretta o non trovata" -ForegroundColor Gray
}

# 3. Aggiungi banner di warning se mancante
$bannerPattern = "╔══════════════════════════════════════════════════════════════════════════════════════╗"
if ($content -notmatch [regex]::Escape($bannerPattern)) {
    Write-Host "⚙️  Aggiunta banner di warning..." -ForegroundColor Yellow
    
    $banner = @"

// ╔══════════════════════════════════════════════════════════════════════════════════════╗
// ║ ⚠️  ATTENZIONE - FILE GENERATO DA SCAFFOLDING CON CUSTOMIZZAZIONI MANUALI          ║
// ║                                                                                      ║
// ║ Questa classe è stata modificata manualmente:                                       ║
// ║ • La proprietà TipoFonte è di tipo TipoFonteData (enum), NON string                ║
// ║ • Il converter TipoFonteDataConverter gestisce la conversione per il database      ║
// ║                                                                                      ║
// ║ SE VIENE RIGENERATA CON SCAFFOLDING:                                                ║
// ║ 1. Backup di questo file prima dello scaffolding                                    ║
// ║ 2. Dopo lo scaffolding, ripristinare la riga:                                       ║
// ║    public TipoFonteData TipoFonte { get; set; }                                    ║
// ║    (invece di: public string TipoFonte { get; set; } = null!;)                     ║
// ║ 3. Verificare che l'using Entities.Enums; sia presente                             ║
// ║                                                                                      ║
// ║ Oppure eseguire lo script: Entities/Models/DbApplication/fix-scaffolding.ps1       ║
// ╚══════════════════════════════════════════════════════════════════════════════════════╝

"@
    
    $content = $content -replace "(namespace Entities\.Models\.DbApplication;)", "$banner`$1"
}

# 4. Assicurati che la classe sia partial
if ($content -notmatch "public partial class ConfigurazioneFontiDati") {
    Write-Host "⚙️  Conversione a classe parziale..." -ForegroundColor Yellow
    $content = $content -replace "public class ConfigurazioneFontiDati", "public partial class ConfigurazioneFontiDati"
}

# Salva il file modificato
Set-Content $filePath $content -NoNewline

Write-Host ""
Write-Host "✅ Fix completato con successo!" -ForegroundColor Green
Write-Host ""
Write-Host "Verifiche consigliate:" -ForegroundColor Cyan
Write-Host "  1. dotnet build - per verificare la compilazione" -ForegroundColor Gray
Write-Host "  2. Controllare il file: $filePath" -ForegroundColor Gray
Write-Host ""
Write-Host "Per annullare le modifiche, ripristinare da: $backupPath" -ForegroundColor Yellow
